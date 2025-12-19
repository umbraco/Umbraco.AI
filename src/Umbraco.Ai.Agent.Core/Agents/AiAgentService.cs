using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Service implementation for agent management operations.
/// </summary>
internal sealed class AiAgentService : IAiAgentService
{
    private readonly IAiAgentRepository _repository;
    private readonly IAiChatService _chatService;
    private readonly IAiAgentTemplateService _templateService;
    private readonly IAiAgentScopeValidator _scopeValidator;

    public AiAgentService(
        IAiAgentRepository repository,
        IAiChatService chatService,
        IAiAgentTemplateService templateService,
        IAiAgentScopeValidator scopeValidator)
    {
        _repository = repository;
        _chatService = chatService;
        _templateService = templateService;
        _scopeValidator = scopeValidator;
    }

    /// <inheritdoc />
    public Task<AiAgent?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AiAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AiAgent>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<AiAgent>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, cancellationToken);

    /// <inheritdoc />
    public async Task<AiAgent> SavePromptAsync(AiAgent prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);

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
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);

    /// <inheritdoc />
    public async Task<AiAgentExecutionResult> ExecuteAsync(
        Guid promptId,
        AiAgentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Get the prompt
        var prompt = await GetAsync(promptId, cancellationToken)
            ?? throw new InvalidOperationException($"Prompt {promptId} not found");

        // 2. Validate scope - ensure prompt is allowed to run for this context
        var scopeValidation = await _scopeValidator.ValidateAsync(prompt, request, cancellationToken);
        if (!scopeValidation.IsAllowed)
        {
            throw new InvalidOperationException(
                $"Prompt execution denied: {scopeValidation.DenialReason}");
        }

        // 3. Build context for template replacement
        var context = BuildExecutionContext(request);

        // 4. Process template variables
        var processedContent = _templateService.ProcessTemplate(prompt.Content, context);

        // 5. Create chat message
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, processedContent)
        };

        // 6. Execute via chat service
        var response = prompt.ProfileId.HasValue
            ? await _chatService.GetResponseAsync(prompt.ProfileId.Value, messages, cancellationToken: cancellationToken)
            : await _chatService.GetResponseAsync(messages, cancellationToken: cancellationToken);

        // 7. Map response
        return new AiAgentExecutionResult
        {
            Content = response.Text ?? string.Empty,
            Usage = response.Usage
        };
    }

    /// <summary>
    /// Builds the execution context dictionary from the request.
    /// </summary>
    private static Dictionary<string, object?> BuildExecutionContext(AiAgentExecutionRequest request)
    {
        var context = new Dictionary<string, object?>
        {
            ["entityId"] = request.EntityId.ToString(),
            ["entityType"] = request.EntityType,
            ["propertyAlias"] = request.PropertyAlias,
        };

        // Add request-level context if provided
        if (request.Context is not null)
        {
            foreach (var kvp in request.Context)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        // Add local content if provided
        if (request.LocalContent is not null)
        {
            context["localContent"] = request.LocalContent;
        }

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
