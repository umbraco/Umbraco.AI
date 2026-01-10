using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.EntityAdapter;
using Umbraco.Ai.Core.RequestContext;
using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Prompt.Core.Context;
using Umbraco.Cms.Core.Models;

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

        // 5. Process template variables
        var processedContent = _templateService.ProcessTemplate(prompt.Instructions, templateContext);

        // 6. Build chat messages with system message injection from processors
        var messages = new List<ChatMessage>();
        if (requestContext.SystemMessageParts.Count > 0)
        {
            var systemContent = string.Join("\n\n", requestContext.SystemMessageParts);
            messages.Add(new ChatMessage(ChatRole.System, systemContent));
        }
        messages.Add(new ChatMessage(ChatRole.User, processedContent));

        // 7. Create ChatOptions with PromptId for context resolution and system tools
        var chatOptions = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [PromptContextResolver.PromptIdKey] = prompt.Id
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

        // 9. Execute via chat service
        var response = prompt.ProfileId.HasValue
            ? await _chatService.GetChatResponseAsync(prompt.ProfileId.Value, messages, chatOptions, cancellationToken)
            : await _chatService.GetChatResponseAsync(messages, chatOptions, cancellationToken);

        // 10. Map response
        return new AiPromptExecutionResult
        {
            Content = response.Text ?? string.Empty,
            Usage = response.Usage
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
