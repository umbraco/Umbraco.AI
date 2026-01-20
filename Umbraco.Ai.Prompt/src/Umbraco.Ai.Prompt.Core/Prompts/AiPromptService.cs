using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.AuditLog;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.EntityAdapter;
using Umbraco.Ai.Core.RequestContext;
using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Extensions;
using Umbraco.Cms.Core.Models;
using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Service implementation for prompt management operations.
/// </summary>
internal sealed class AiPromptService : IAiPromptService
{
    private readonly IAiPromptRepository _repository;
    private readonly IAiChatService _chatService;
    private readonly IAiPromptTemplateService _templateService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AiToolCollection _tools;
    private readonly IAiFunctionFactory _functionFactory;
    private readonly AiRequestContextProcessorCollection _contextProcessors;

    public AiPromptService(
        IAiPromptRepository repository,
        IAiChatService chatService,
        IAiPromptTemplateService templateService,
        IServiceScopeFactory serviceScopeFactory,
        AiToolCollection tools,
        IAiFunctionFactory functionFactory,
        AiRequestContextProcessorCollection contextProcessors)
    {
        _repository = repository;
        _chatService = chatService;
        _templateService = templateService;
        _serviceScopeFactory = serviceScopeFactory;
        _tools = tools;
        _functionFactory = functionFactory;
        _contextProcessors = contextProcessors;
    }

    /// <inheritdoc />
    public Task<AiPrompt?> GetPromptAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AiPrompt?> GetPromptByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AiPrompt>> GetPromptsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<AiPrompt>> GetPromptsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, cancellationToken);

    /// <inheritdoc />
    public async Task<AiPrompt> SavePromptAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Instructions);

        // Generate new ID if needed
        if (prompt.Id == Guid.Empty)
        {
            prompt.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(prompt.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != prompt.Id)
        {
            throw new InvalidOperationException($"A prompt with alias '{prompt.Alias}' already exists.");
        }

        // Update timestamp
        prompt.DateModified = DateTime.UtcNow;

        return await _repository.SaveAsync(prompt, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeletePromptAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> PromptAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);

    /// <inheritdoc />
    public async Task<AiPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AiPromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Get the prompt
        var prompt = await GetPromptAsync(promptId, cancellationToken)
            ?? throw new InvalidOperationException($"Prompt {promptId} not found");

        // 2. Validate scope - ensure prompt is allowed to run for this context
        // Use a scope to resolve the scoped IAiPromptScopeValidator
        using var scope = _serviceScopeFactory.CreateScope();
        var scopeValidator = scope.ServiceProvider.GetRequiredService<IAiPromptScopeValidator>();
        var scopeValidation = await scopeValidator.ValidateAsync(prompt, request, cancellationToken);
        if (!scopeValidation.IsAllowed)
        {
            throw new InvalidOperationException(
                $"Prompt execution denied: {scopeValidation.DenialReason}");
        }

        // 3. Process context items through registered processors
        var requestContext = _contextProcessors.Process(request.Context ?? []);

        // 4. Build template context from basic request info + processor results
        var templateContext = BuildExecutionContext(request);
        foreach (var (key, value) in requestContext.Variables)
        {
            templateContext[key] = value;
        }
        templateContext["currentValue"] = templateContext.TryGetValue(request.PropertyAlias, out var propValue)
            ? propValue
            : null;

        // 5. Process template variables (returns multimodal content list)
        var contents = _templateService.ProcessTemplate(prompt.Instructions, templateContext);

        // 6. Build chat messages with multimodal content
        List<ChatMessage> messages = [new(ChatRole.User, contents.ToList())];

        // 7. Inject system message from context processors (only if IncludeEntityContext is enabled)
        if (prompt.IncludeEntityContext && requestContext?.SystemMessageParts.Count > 0)
        {
            var contextContent = string.Join("\n\n", requestContext.SystemMessageParts);

            // Insert new system message at the beginning
            messages.Insert(0, new ChatMessage(ChatRole.System, contextContent));
        }

        // 8. Create ChatOptions with PromptId for context resolution, feature tracking, and system tools
        var chatOptions = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [Constants.MetadataKeys.PromptId] = prompt.Id,
                [Constants.MetadataKeys.PromptAlias] = prompt.Alias,
                [CoreConstants.MetadataKeys.FeatureType] = "prompt",
                [CoreConstants.MetadataKeys.FeatureId] = prompt.Id,
                [CoreConstants.MetadataKeys.FeatureAlias] = prompt.Alias
            },
            Tools = _tools.ToSystemToolFunctions(_functionFactory).Cast<AITool>().ToList(),
            ToolMode = ChatToolMode.Auto
        };

        // 8. If entity was extracted by processors, set ContentId for ContentContextResolver
        var entityId = requestContext.GetValue<Guid>(AiRequestContextKeys.EntityId);
        if (entityId.HasValue)
        {
            chatOptions.AdditionalProperties[AiRequestContextKeys.ContentId] = entityId.Value;
        }
        else if (request.EntityId != Guid.Empty)
        {
            // Fallback to request.EntityId if no entity extracted from context items
            chatOptions.AdditionalProperties[AiRequestContextKeys.ContentId] = request.EntityId;
        }

        // 9. Set ParentEntityId if available (for new entities, used by ContentContextResolver)
        var parentEntityId = requestContext.GetValue<Guid>(AiRequestContextKeys.ParentEntityId);
        if (parentEntityId.HasValue)
        {
            chatOptions.AdditionalProperties[AiRequestContextKeys.ParentEntityId] = parentEntityId.Value;
        }

        // 10. Execute via chat service
        var response = prompt.ProfileId.HasValue
            ? await _chatService.GetChatResponseAsync(prompt.ProfileId.Value, messages, chatOptions, cancellationToken)
            : await _chatService.GetChatResponseAsync(messages, chatOptions, cancellationToken);

        // 11. Map response
        return new AiPromptExecutionResult
        {
            Content = response.Text ?? string.Empty,
            Usage = response.Usage,
            PropertyChanges = [
                new AiPropertyChange
                {
                    Alias = request.PropertyAlias,
                    Value = response.Text ?? string.Empty,
                    Culture = request.Culture,
                    Segment = request.Segment
                }
            ]
        };
    }

    /// <summary>
    /// Builds the execution context dictionary from the request.
    /// </summary>
    private static Dictionary<string, object?> BuildExecutionContext(AiPromptExecutionRequest request)
    {
        var context = new Dictionary<string, object?>
        {
            ["entityId"] = request.EntityId.ToString(),
            ["entityType"] = request.EntityType,
            ["propertyAlias"] = request.PropertyAlias,
        };

        if (!string.IsNullOrEmpty(request.Culture))
        {
            context["culture"] = request.Culture;
        }

        if (!string.IsNullOrEmpty(request.Segment))
        {
            context["segment"] = request.Segment;
        }

        return context;
    }
}
