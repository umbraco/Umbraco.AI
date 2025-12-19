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
    private readonly IAiAgentScopeValidator _scopeValidator;

    public AiAgentService(
        IAiAgentRepository repository,
        IAiChatService chatService,
        IAiAgentScopeValidator scopeValidator)
    {
        _repository = repository;
        _chatService = chatService;
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
    public async Task<AiAgent> SaveAgentAsync(AiAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(agent.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(agent.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(agent.Content);

        // Generate new ID if needed
        if (agent.Id == Guid.Empty)
        {
            agent.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(agent.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != agent.Id)
        {
            throw new InvalidOperationException($"A agent with alias '{agent.Alias}' already exists.");
        }

        // Update timestamp
        agent.DateModified = DateTime.UtcNow;

        return await _repository.SaveAsync(agent, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);
}
