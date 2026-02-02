using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using AIPropertyChange = Umbraco.Ai.Core.EntityAdapter.AIPropertyChange;
using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Service implementation for prompt management operations.
/// </summary>
internal sealed class AIPromptService : IAIPromptService
{
    private readonly IAIPromptRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IAIChatService _chatService;
    private readonly IAIPromptTemplateService _templateService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AIToolCollection _tools;
    private readonly IAIFunctionFactory _functionFactory;
    private readonly IAIRuntimeContextScopeProvider _runtimeContextScopeProvider;
    private readonly AIRuntimeContextContributorCollection _contextContributors;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AIPromptService(
        IAIPromptRepository repository,
        IAIEntityVersionService versionService,
        IAIChatService chatService,
        IAIPromptTemplateService templateService,
        IServiceScopeFactory serviceScopeFactory,
        AIToolCollection tools,
        IAIFunctionFactory functionFactory,
        IAIRuntimeContextScopeProvider runtimeContextScopeProvider,
        AIRuntimeContextContributorCollection contextContributors,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _chatService = chatService;
        _templateService = templateService;
        _serviceScopeFactory = serviceScopeFactory;
        _tools = tools;
        _functionFactory = functionFactory;
        _runtimeContextScopeProvider = runtimeContextScopeProvider;
        _contextContributors = contextContributors;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AIPrompt?> GetPromptAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AIPrompt?> GetPromptByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIPrompt>> GetPromptsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<AIPrompt>> GetPromptsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, cancellationToken);

    /// <inheritdoc />
    public async Task<AIPrompt> SavePromptAsync(AIPrompt prompt, CancellationToken cancellationToken = default)
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

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(prompt.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        return await _repository.SaveAsync(prompt, userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeletePromptAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> PromptAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);

    /// <inheritdoc />
    public async Task<AIPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Get the prompt
        var prompt = await GetPromptAsync(promptId, cancellationToken)
            ?? throw new InvalidOperationException($"Prompt {promptId} not found");

        // 2. Validate scope - ensure prompt is allowed to run for this context
        // Use a scope to resolve the scoped IAIPromptScopeValidator
        using var scope = _serviceScopeFactory.CreateScope();
        var scopeValidator = scope.ServiceProvider.GetRequiredService<IAIPromptScopeValidator>();
        var scopeValidation = await scopeValidator.ValidateAsync(prompt, request, cancellationToken);
        if (!scopeValidation.IsAllowed)
        {
            throw new InvalidOperationException(
                $"Prompt execution denied: {scopeValidation.DenialReason}");
        }

        // Create a runtime context scope for this execution
        using var runtimeContextScope = _runtimeContextScopeProvider.CreateScope(request.Context ?? []);
        var runtimeContext = runtimeContextScope.Context;
        
        // 3. Process context items through registered contributors
        _contextContributors.Populate(runtimeContext);

        // If no EntityId was set by contributors, use request.EntityId as fallback
        if (!runtimeContext.Data.ContainsKey(CoreConstants.ContextKeys.EntityId) && request.EntityId != Guid.Empty)
        {
            runtimeContext.SetValue(CoreConstants.ContextKeys.EntityId, request.EntityId);
        }

        // Set prompt metadata in runtime context for auditing and telemetry
        runtimeContext.SetValue(Constants.MetadataKeys.PromptId, prompt.Id);
        runtimeContext.SetValue(Constants.MetadataKeys.PromptAlias, prompt.Alias);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureType, "prompt");
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureId, prompt.Id);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureAlias, prompt.Alias);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureVersion, prompt.Version);

        // 4. Build template context from basic request info + processor results
        var templateContext = BuildExecutionContext(request);
        foreach (var (key, value) in runtimeContext.Variables)
        {
            templateContext[key] = value;
        }
        templateContext["currentValue"] = templateContext.TryGetValue(request.PropertyAlias, out var propValue)
            ? propValue
            : null;

        // 5. Process template variables (returns multimodal content list)
        var contents = await _templateService.ProcessTemplateAsync(prompt.Instructions, templateContext, cancellationToken);

        // 6. Build chat messages with multimodal content
        List<ChatMessage> messages = [new(ChatRole.User, contents.ToList())];

        // 7. Inject system message from context processors (only if IncludeEntityContext is enabled)
        if (prompt.IncludeEntityContext && runtimeContext?.SystemMessageParts.Count > 0)
        {
            var contextContent = string.Join("\n\n", runtimeContext.SystemMessageParts);

            // Insert new system message at the beginning
            messages.Insert(0, new ChatMessage(ChatRole.System, contextContent));
        }

        // 8. Create ChatOptions with PromptId for context resolution, feature tracking, and system tools
        var chatOptions = new ChatOptions
        {
            Tools = _tools.ToSystemToolFunctions(_functionFactory).Cast<AITool>().ToList(),
            ToolMode = ChatToolMode.Auto
        };

        // 10. Execute via chat service
        var response = prompt.ProfileId.HasValue
            ? await _chatService.GetChatResponseAsync(prompt.ProfileId.Value, messages, chatOptions, cancellationToken)
            : await _chatService.GetChatResponseAsync(messages, chatOptions, cancellationToken);

        // 11. Map response
        return new AIPromptExecutionResult
        {
            Content = response.Text ?? string.Empty,
            Usage = response.Usage,
            PropertyChanges = [
                new AIPropertyChange
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
    private static Dictionary<string, object?> BuildExecutionContext(AIPromptExecutionRequest request)
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
