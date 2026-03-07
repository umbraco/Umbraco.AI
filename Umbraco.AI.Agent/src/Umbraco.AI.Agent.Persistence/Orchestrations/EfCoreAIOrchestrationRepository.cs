using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Persistence.Orchestrations;

/// <summary>
/// EF Core implementation of <see cref="IAIOrchestrationRepository"/>.
/// </summary>
internal sealed class EfCoreAIOrchestrationRepository : IAIOrchestrationRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIAgentDbContext> _scopeProvider;

    public EfCoreAIOrchestrationRepository(IEFCoreScopeProvider<UmbracoAIAgentDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AIOrchestration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Orchestrations.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return entity is null ? null : AIOrchestrationEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AIOrchestration?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Orchestrations.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();

        return entity is null ? null : AIOrchestrationEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIOrchestration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Orchestrations.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken));

        scope.Complete();

        return entities.Select(AIOrchestrationEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<PagedModel<AIOrchestration>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIOrchestrationEntity> query = db.Orchestrations.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var lowerFilter = filter.ToLower();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(lowerFilter) ||
                    e.Alias.ToLower().Contains(lowerFilter));
            }

            if (!string.IsNullOrWhiteSpace(surfaceId))
            {
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

        var orchestrations = result.items.Select(AIOrchestrationEntityFactory.BuildDomain).ToList();
        return new PagedModel<AIOrchestration>(result.total, orchestrations);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIOrchestration>> GetBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            var surfacePattern = $"\"{surfaceId}\"";
            return await db.Orchestrations.AsNoTracking()
                .Where(e => e.SurfaceIds != null && e.SurfaceIds.Contains(surfacePattern))
                .OrderBy(e => e.Name)
                .ToListAsync(cancellationToken);
        });

        scope.Complete();

        return entities.Select(AIOrchestrationEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AIOrchestration> SaveAsync(AIOrchestration orchestration, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var savedOrchestration = await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Orchestrations.FirstOrDefaultAsync(e => e.Id == orchestration.Id, cancellationToken);

            if (existing is null)
            {
                orchestration.Version = 1;
                orchestration.DateModified = DateTime.UtcNow;
                orchestration.CreatedByUserId = userId;
                orchestration.ModifiedByUserId = userId;

                var entity = AIOrchestrationEntityFactory.BuildEntity(orchestration);
                db.Orchestrations.Add(entity);
            }
            else
            {
                orchestration.Version = existing.Version + 1;
                orchestration.DateModified = DateTime.UtcNow;
                orchestration.ModifiedByUserId = userId;

                AIOrchestrationEntityFactory.UpdateEntity(existing, orchestration);
            }

            await db.SaveChangesAsync(cancellationToken);
            return orchestration;
        });

        scope.Complete();

        return savedOrchestration;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Orchestrations.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.Orchestrations.Remove(entity);
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
            await db.Orchestrations.AnyAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIAgentDbContext> scope = _scopeProvider.CreateScope();

        var exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Orchestrations.AnyAsync(e => e.ProfileId == profileId, cancellationToken));

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
            var query = db.Orchestrations.Where(e => e.Alias.ToLower() == lowerAlias);

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
