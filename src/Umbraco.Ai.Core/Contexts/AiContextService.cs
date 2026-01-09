namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAiContextService"/>.
/// </summary>
internal sealed class AiContextService : IAiContextService
{
    private readonly IAiContextRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextService"/> class.
    /// </summary>
    /// <param name="repository">The context repository.</param>
    public AiContextService(IAiContextRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public Task<AiContext?> GetContextAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AiContext?> GetContextByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AiContext>> GetContextsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<(IEnumerable<AiContext> Items, int Total)> GetContextsPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(filter, skip, take, cancellationToken);

    /// <inheritdoc />
    public async Task<AiContext> SaveContextAsync(AiContext context, CancellationToken cancellationToken = default)
    {
        // Generate new ID if needed
        if (context.Id == Guid.Empty)
        {
            context.Id = Guid.NewGuid();
        }

        // Generate IDs for new resources
        foreach (var resource in context.Resources.Where(r => r.Id == Guid.Empty))
        {
            resource.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(context.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != context.Id)
        {
            throw new InvalidOperationException($"A context with alias '{context.Alias}' already exists.");
        }

        // Update modified timestamp
        context.DateModified = DateTime.UtcNow;

        return await _repository.SaveAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteContextAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);
}
