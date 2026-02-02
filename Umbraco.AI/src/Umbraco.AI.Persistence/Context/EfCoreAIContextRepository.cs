using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Context;

/// <summary>
/// EF Core implementation of the AI context repository.
/// </summary>
internal class EfCoreAiContextRepository : IAiContextRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiContextRepository"/>.
    /// </summary>
    public EfCoreAiContextRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AIContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIContextEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AIContextFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AIContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIContextEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();
        return entity is null ? null : AIContextFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIContext>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIContextEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AIContextFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AIContext> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIContextEntity> query = db.Contexts.Include(c => c.Resources);

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(c =>
                    c.Name.ToLower().Contains(filter.ToLower()) ||
                    c.Alias.ToLower().Contains(filter.ToLower()));
            }

            int total = await query.CountAsync(cancellationToken);

            List<AIContextEntity> items = await query
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AIContextFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AIContext> SaveAsync(AIContext context, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var savedContext = await scope.ExecuteWithContextAsync(async db =>
        {
            AIContextEntity? existing = await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Id == context.Id, cancellationToken);

            if (existing is null)
            {
                // New context - set version and user IDs on domain model before mapping
                context.Version = 1;
                context.DateModified = DateTime.UtcNow;
                context.CreatedByUserId = userId;
                context.ModifiedByUserId = userId;

                AIContextEntity newEntity = AIContextFactory.BuildEntity(context);
                db.Contexts.Add(newEntity);
            }
            else
            {
                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                // Note: Version snapshots are handled by the unified versioning service at the service layer
                context.Version = existing.Version + 1;
                context.DateModified = DateTime.UtcNow;
                context.ModifiedByUserId = userId;

                AIContextFactory.UpdateEntity(existing, context);

                // Handle resources: remove deleted, update existing, add new
                var existingResourceIds = existing.Resources.Select(r => r.Id).ToHashSet();
                var newResourceIds = context.Resources.Select(r => r.Id).ToHashSet();

                // Remove deleted resources
                var toRemove = existing.Resources.Where(r => !newResourceIds.Contains(r.Id)).ToList();
                foreach (var resource in toRemove)
                {
                    db.ContextResources.Remove(resource);
                }

                // Update or add resources
                foreach (var resource in context.Resources)
                {
                    if (existingResourceIds.Contains(resource.Id))
                    {
                        var existingResource = existing.Resources.First(r => r.Id == resource.Id);
                        AIContextFactory.UpdateResourceEntity(existingResource, resource);
                    }
                    else
                    {
                        var newResource = AIContextFactory.BuildResourceEntity(resource, context.Id);
                        db.ContextResources.Add(newResource);
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return context;
        });

        scope.Complete();
        return savedContext;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AIContextEntity? entity = await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (entity is null)
            {
                return false;
            }

            // Resources and versions will be cascade deleted due to relationship configuration
            db.Contexts.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
