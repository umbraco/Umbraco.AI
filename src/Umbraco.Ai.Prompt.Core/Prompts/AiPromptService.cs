using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
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

    public AiPromptService(
        IAiPromptRepository repository,
        IAiChatService chatService,
        IAiPromptTemplateService templateService)
    {
        _repository = repository;
        _chatService = chatService;
        _templateService = templateService;
    }

    /// <inheritdoc />
    public Task<AiPrompt?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AiPrompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AiPrompt>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<AiPrompt>> GetPagedAsync(
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
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        
        // Generate new ID if needed
        if (prompt.Id == Guid.Empty)
        {
            prompt = new AiPrompt
            {
                Id = Guid.NewGuid(),
                Alias = prompt.Alias,
                Name = prompt.Name,
                Content = prompt.Content,
                Description = prompt.Description,
                ProfileId = prompt.ProfileId,
                Tags = prompt.Tags,
                IsActive = prompt.IsActive,
                DateCreated = prompt.DateCreated,
                DateModified = prompt.DateModified
            };
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
    public async Task<AiPromptExecutionResult> ExecuteAsync(
        Guid promptId,
        AiPromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Get the prompt
        var prompt = await GetAsync(promptId, cancellationToken)
            ?? throw new InvalidOperationException($"Prompt {promptId} not found");

        // 2. Build context for template replacement
        var context = BuildExecutionContext(request);

        // 3. Process template variables
        var processedContent = _templateService.ProcessTemplate(prompt.Content, context);

        // 4. Create chat message
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, processedContent)
        };

        // 5. Execute via chat service
        var response = prompt.ProfileId.HasValue
            ? await _chatService.GetResponseAsync(prompt.ProfileId.Value, messages, cancellationToken: cancellationToken)
            : await _chatService.GetResponseAsync(messages, cancellationToken: cancellationToken);

        // 6. Map response
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
        var context = new Dictionary<string, object?>();

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

        // Add entity context
        if (request.EntityId.HasValue)
        {
            context["entityId"] = request.EntityId.Value.ToString();
        }

        if (!string.IsNullOrEmpty(request.EntityType))
        {
            context["entityType"] = request.EntityType;
        }

        if (!string.IsNullOrEmpty(request.PropertyAlias))
        {
            context["propertyAlias"] = request.PropertyAlias;
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
