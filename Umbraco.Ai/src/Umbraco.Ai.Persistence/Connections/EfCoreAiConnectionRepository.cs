using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Connections;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Connections;

/// <summary>
/// EF Core implementation of the AI connection repository.
/// </summary>
internal class EfCoreAiConnectionRepository : IAiConnectionRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiConnectionRepository"/>.
    /// </summary>
    public EfCoreAiConnectionRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<AiConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiConnectionEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.FirstOrDefaultAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiConnectionFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AiConnection?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiConnectionEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.FirstOrDefaultAsync(
                c => c.Alias.ToLower() == alias.ToLower(),
                cancellationToken));

        scope.Complete();
        return entity is null ? null : AiConnectionFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiConnection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiConnectionEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiConnectionFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiConnection>> GetByProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiConnectionEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections
                .Where(c => c.ProviderId == providerId)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiConnectionFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiConnection> Items, int Total)> GetPagedAsync(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiConnectionEntity> query = db.Connections;

            // Apply provider filter
            if (!string.IsNullOrEmpty(providerId))
            {
                query = query.Where(c => c.ProviderId == providerId);
            }

            // Apply name filter (case-insensitive contains)
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(c => c.Name.ToLower().Contains(filter.ToLower()));
            }

            // Get total count before pagination
            int total = await query.CountAsync(cancellationToken);

            // Apply pagination and get items
            List<AiConnectionEntity> items = await query
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiConnectionFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AiConnection> SaveAsync(AiConnection connection, int? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiConnectionEntity? existing = await db.Connections.FindAsync([connection.Id], cancellationToken);

            if (existing is null)
            {
                // New connection - set user IDs on domain model before mapping
                connection.CreatedByUserId = userId;
                connection.ModifiedByUserId = userId;

                AiConnectionEntity newEntity = AiConnectionFactory.BuildEntity(connection);
                db.Connections.Add(newEntity);
            }
            else
            {
                // Existing connection - set ModifiedByUserId on domain model
                connection.ModifiedByUserId = userId;

                AiConnectionFactory.UpdateEntity(existing, connection);
            }

            await db.SaveChangesAsync(cancellationToken);
            return null;
        });

        scope.Complete();
        return connection;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AiConnectionEntity? entity = await db.Connections.FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.Connections.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.AnyAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return exists;
    }
}
