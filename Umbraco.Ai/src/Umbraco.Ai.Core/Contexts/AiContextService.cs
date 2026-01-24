using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAiContextService"/>.
/// </summary>
internal sealed class AiContextService : IAiContextService
{
    private readonly IAiContextRepository _repository;
    private readonly IAiEntityVersionService _versionService;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextService"/> class.
    /// </summary>
    /// <param name="repository">The context repository.</param>
    /// <param name="versionService">The unified versioning service.</param>
    /// <param name="backOfficeSecurityAccessor">The backoffice security accessor for user tracking.</param>
    public AiContextService(
        IAiContextRepository repository,
        IAiEntityVersionService versionService,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
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

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Check if this is an update - if so, create a version snapshot of the current state
        var existing = await _repository.GetByIdAsync(context.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        return await _repository.SaveAsync(context, userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteContextAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Delete version history for this entity
        await _versionService.DeleteVersionsAsync(id, "Context", cancellationToken);

        return await _repository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AiEntityVersion> Items, int Total)> GetContextVersionHistoryAsync(
        Guid contextId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionHistoryAsync(contextId, "Context", skip, take, cancellationToken);

    /// <inheritdoc />
    public Task<AiContext?> GetContextVersionSnapshotAsync(
        Guid contextId,
        int version,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionSnapshotAsync<AiContext>(contextId, version, cancellationToken);

    /// <inheritdoc />
    public async Task<AiContext> RollbackContextAsync(
        Guid contextId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        // Get the current context to ensure it exists
        var currentContext = await _repository.GetByIdAsync(contextId, cancellationToken);
        if (currentContext is null)
        {
            throw new InvalidOperationException($"Context with ID '{contextId}' not found.");
        }

        // Get the snapshot at the target version
        var snapshot = await _versionService.GetVersionSnapshotAsync<AiContext>(contextId, targetVersion, cancellationToken);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Version {targetVersion} not found for context '{contextId}'.");
        }

        // Create a new version by saving the snapshot data
        var rolledBackContext = new AiContext
        {
            Id = contextId,
            Alias = snapshot.Alias,
            Name = snapshot.Name,
            Resources = snapshot.Resources.Select(r => new AiContextResource
            {
                ResourceTypeId = r.ResourceTypeId,
                Name = r.Name,
                Description = r.Description,
                SortOrder = r.SortOrder,
                Data = r.Data,
                InjectionMode = r.InjectionMode
            }).ToList(),
        };

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;
        return await _repository.SaveAsync(rolledBackContext, userId, cancellationToken);
    }
}
