using System.Collections.Concurrent;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// In-memory implementation of <see cref="IAiPromptRepository"/> for testing and fallback scenarios.
/// </summary>
internal sealed class InMemoryAiPromptRepository : IAiPromptRepository
{
    private readonly ConcurrentDictionary<Guid, AiPrompt> _prompts = new();

    /// <inheritdoc />
    public Task<AiPrompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _prompts.TryGetValue(id, out var prompt);
        return Task.FromResult(prompt);
    }

    /// <inheritdoc />
    public Task<AiPrompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var prompt = _prompts.Values.FirstOrDefault(p =>
            p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(prompt);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiPrompt>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AiPrompt>>(_prompts.Values.ToList());

    /// <inheritdoc />
    public Task<IEnumerable<AiPrompt>> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var prompts = _prompts.Values
            .Where(p => p.ProfileId == profileId)
            .ToList();
        return Task.FromResult<IEnumerable<AiPrompt>>(prompts);
    }

    /// <inheritdoc />
    public Task<PagedModel<AiPrompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _prompts.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(p =>
                p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                p.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        if (profileId.HasValue)
        {
            query = query.Where(p => p.ProfileId == profileId.Value);
        }

        var total = query.Count();
        var items = query
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult(new PagedModel<AiPrompt>(total, items));
    }

    /// <inheritdoc />
    public Task<AiPrompt> SaveAsync(AiPrompt prompt, int? userId = null, CancellationToken cancellationToken = default)
    {
        _prompts[prompt.Id] = prompt;
        return Task.FromResult(prompt);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_prompts.TryRemove(id, out _));

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_prompts.ContainsKey(id));

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _prompts.Values.Any(p =>
            p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid promptId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<IEnumerable<AiEntityVersion>>([]);
    }

    /// <inheritdoc />
    public Task<AiPrompt?> GetVersionSnapshotAsync(
        Guid promptId,
        int version,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<AiPrompt?>(null);
    }
}
