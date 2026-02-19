using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Persistence.Agents;

/// <summary>
/// EF Core implementation of <see cref="IAIAgentRepository"/>.
/// </summary>
internal sealed class EfCoreAIAgentRepository : IAIAgentRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIAgentDbContext> _scopeProvider;

    public EfCoreAIAgentRepository(IEFCoreScopeProvider<UmbracoAIAgentDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<Core.Agents.AIAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return entity is null ? null : AIAgentEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Core.Agents.AIAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();

        return entity is null ? null : AIAgentEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Agents.AIAgent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken));

        scope.Complete();

        return entities.Select(AIAgentEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Core.Agents.AIAgent> Items, int Total)> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIAgentEntity> query = db.Agents.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var lowerFilter = filter.ToLower();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(lowerFilter) ||
                    e.Alias.ToLower().Contains(lowerFilter));
            }

            if (profileId.HasValue)
            {
                query = query.Where(e => e.ProfileId == profileId.Value);
            }

            if (!string.IsNullOrWhiteSpace(surfaceId))
            {
                // Filter agents that have the surfaceId in their SurfaceIds JSON array
                // Using LIKE to search within JSON array (works for both SQL Server and SQLite)
                var surfacePattern = $"\"{surfaceId}\"";
                query = query.Where(e => e.SurfaceIds != null && e.SurfaceIds.Contains(surfacePattern));
            }

            if (isActive.HasValue)
            {
                query = query.Where(e => e.IsActive == isActive.Value);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(e => e.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (total, items);
        });

        scope.Complete();

        return (result.items.Select(AIAgentEntityFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Agents.AIAgent>> GetBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            // Filter agents that have the surfaceId in their SurfaceIds JSON array
            var surfacePattern = $"\"{surfaceId}\"";
            return await db.Agents.AsNoTracking()
                .Where(e => e.SurfaceIds != null && e.SurfaceIds.Contains(surfacePattern))
                .OrderBy(e => e.Name)
                .ToListAsync(cancellationToken);
        });

        scope.Complete();

        return entities.Select(AIAgentEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<Core.Agents.AIAgent> SaveAsync(Core.Agents.AIAgent agent, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var savedAgent = await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Agents.FirstOrDefaultAsync(e => e.Id == agent.Id, cancellationToken);

            if (existing is null)
            {
                // New agent - set version and user IDs on domain model before mapping
                agent.Version = 1;
                agent.DateModified = DateTime.UtcNow;
                agent.CreatedByUserId = userId;
                agent.ModifiedByUserId = userId;

                var entity = AIAgentEntityFactory.BuildEntity(agent);
                db.Agents.Add(entity);
            }
            else
            {
                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                agent.Version = existing.Version + 1;
                agent.DateModified = DateTime.UtcNow;
                agent.ModifiedByUserId = userId;

                AIAgentEntityFactory.UpdateEntity(existing, agent);
            }

            await db.SaveChangesAsync(cancellationToken);
            return agent;
        });

        scope.Complete();

        return savedAgent;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Agents.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.Agents.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();

        return deleted;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AnyAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var exists = await scope.ExecuteWithContextAsync(async db =>
        {
            var lowerAlias = alias.ToLower();
            var query = db.Agents.Where(e => e.Alias.ToLower() == lowerAlias);

            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        });

        scope.Complete();

        return exists;
    }
}
