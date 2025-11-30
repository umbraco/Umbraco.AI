using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Service implementation for prompt management operations.
/// </summary>
internal sealed class PromptService : IPromptService
{
    private readonly IPromptRepository _repository;

    public PromptService(IPromptRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public Task<Prompt?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<Prompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<Prompt>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<Prompt>> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        => _repository.GetByProfileAsync(profileId, cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<Prompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, cancellationToken);

    /// <inheritdoc />
    public async Task<Prompt> CreateAsync(
        string alias,
        string name,
        string content,
        string? description = null,
        Guid? profileId = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (await _repository.AliasExistsAsync(alias, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"A prompt with alias '{alias}' already exists.");
        }

        var now = DateTime.UtcNow;
        var prompt = new Prompt
        {
            Id = Guid.NewGuid(),
            Alias = alias,
            Name = name,
            Content = content,
            Description = description,
            ProfileId = profileId,
            Tags = tags?.ToList() ?? [],
            IsActive = true,
            DateCreated = now,
            DateModified = now
        };

        return await _repository.SaveAsync(prompt, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Prompt?> UpdateAsync(
        Guid id,
        string name,
        string content,
        string? description = null,
        Guid? profileId = null,
        IEnumerable<string>? tags = null,
        bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.Name = name;
        existing.Content = content;
        existing.Description = description;
        existing.ProfileId = profileId;
        existing.Tags = tags?.ToList() ?? [];
        existing.IsActive = isActive;
        existing.DateModified = DateTime.UtcNow;

        return await _repository.SaveAsync(existing, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);
}
