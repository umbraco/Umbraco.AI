using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Connections;

/// <summary>
/// EF Core implementation of the AI connection repository.
/// </summary>
internal class EfCoreAIConnectionRepository : IAIConnectionRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;
    private readonly IAIConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAIConnectionRepository"/>.
    /// </summary>
    public EfCoreAIConnectionRepository(
        IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider,
        IAIConnectionFactory connectionFactory)
    {
        _scopeProvider = scopeProvider;
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<AIConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIConnectionEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.FirstOrDefaultAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : _connectionFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AIConnection?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIConnectionEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.FirstOrDefaultAsync(
                c => c.Alias.ToLower() == alias.ToLower(),
                cancellationToken));

        scope.Complete();
        return entity is null ? null : _connectionFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIConnection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIConnectionEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(_connectionFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIConnection>> GetByProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIConnectionEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections
                .Where(c => c.ProviderId == providerId)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(_connectionFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AIConnection> Items, int Total)> GetPagedAsync(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIConnectionEntity> query = db.Connections;

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
            List<AIConnectionEntity> items = await query
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(_connectionFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AIConnection> SaveAsync(AIConnection connection, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var savedConnection = await scope.ExecuteWithContextAsync(async db =>
        {
            AIConnectionEntity? existing = await db.Connections.FindAsync([connection.Id], cancellationToken);

            if (existing is null)
            {
                // New connection - set version and user IDs on domain model before mapping
                connection.Version = 1;
                connection.DateModified = DateTime.UtcNow;
                connection.CreatedByUserId = userId;
                connection.ModifiedByUserId = userId;

                AIConnectionEntity newEntity = _connectionFactory.BuildEntity(connection);
                db.Connections.Add(newEntity);
            }
            else
            {
                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                // Note: Version snapshots are handled by the unified versioning service at the service layer
                connection.Version = existing.Version + 1;
                connection.DateModified = DateTime.UtcNow;
                connection.ModifiedByUserId = userId;

                _connectionFactory.UpdateEntity(existing, connection);
            }

            await db.SaveChangesAsync(cancellationToken);
            return connection;
        });

        scope.Complete();
        return savedConnection;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AIConnectionEntity? entity = await db.Connections.FindAsync([id], cancellationToken);
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
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.AnyAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return exists;
    }
}
