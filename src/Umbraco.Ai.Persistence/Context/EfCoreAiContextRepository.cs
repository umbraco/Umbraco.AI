using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Context;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Context;

/// <summary>
/// EF Core implementation of the AI context repository.
/// </summary>
internal class EfCoreAiContextRepository : IAiContextRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiContextRepository"/>.
    /// </summary>
    public EfCoreAiContextRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<AiContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiContextEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiContextFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AiContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiContextEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();
        return entity is null ? null : AiContextFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiContext>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiContextEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiContextFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiContext> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiContextEntity> query = db.Contexts.Include(c => c.Resources);

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(c =>
                    c.Name.ToLower().Contains(filter.ToLower()) ||
                    c.Alias.ToLower().Contains(filter.ToLower()));
            }

            int total = await query.CountAsync(cancellationToken);

            List<AiContextEntity> items = await query
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiContextFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AiContext> SaveAsync(AiContext context, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiContextEntity? existing = await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Id == context.Id, cancellationToken);

            if (existing is null)
            {
                AiContextEntity newEntity = AiContextFactory.BuildEntity(context);
                db.Contexts.Add(newEntity);
            }
            else
            {
                AiContextFactory.UpdateEntity(existing, context);

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
                        AiContextFactory.UpdateResourceEntity(existingResource, resource);
                    }
                    else
                    {
                        var newResource = AiContextFactory.BuildResourceEntity(resource, context.Id);
                        db.ContextResources.Add(newResource);
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return null;
        });

        scope.Complete();
        return context;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AiContextEntity? entity = await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (entity is null)
            {
                return false;
            }

            // Resources will be cascade deleted due to relationship configuration
            db.Contexts.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
