using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Conversations.Persistence.Projects;

/// <summary>
/// EF Core implementation of <see cref="IAIProjectRepository"/>. Project resources live in a separate
/// table keyed by <c>ProjectId</c> (no navigation collection), so they are queried and reconciled
/// explicitly rather than via an <c>Include</c>.
/// </summary>
internal sealed class EFCoreAIProjectRepository : IAIProjectRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIConversationsDbContext> _scopeProvider;
    private readonly AIProjectFactory _factory;

    public EFCoreAIProjectRepository(
        IEFCoreScopeProvider<UmbracoAIConversationsDbContext> scopeProvider,
        AIProjectFactory factory)
    {
        _scopeProvider = scopeProvider;
        _factory = factory;
    }

    public async Task<AIProject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Projects.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity is null)
            {
                return ((AIProjectEntity?)null, new List<AIProjectResourceEntity>());
            }

            var resources = await db.ProjectResources.AsNoTracking()
                .Where(r => r.ProjectId == id)
                .ToListAsync(cancellationToken);

            return (entity, resources);
        });
        scope.Complete();

        return result.Item1 is null ? null : _factory.BuildDomain(result.Item1, result.Item2);
    }

    public async Task<(IReadOnlyList<AIProject> Items, int Total)> GetPagedAsync(
        Guid userKey,
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIProjectEntity> query = db.Projects.AsNoTracking().Where(p => p.UserKey == userKey);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s));
            }

            var total = await query.CountAsync(cancellationToken);

            var projects = await query
                .OrderByDescending(p => p.DateModified)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            var projectIds = projects.Select(p => p.Id).ToList();
            var resources = await db.ProjectResources.AsNoTracking()
                .Where(r => projectIds.Contains(r.ProjectId))
                .ToListAsync(cancellationToken);

            return (projects, resources, total);
        });
        scope.Complete();

        var byProject = result.resources.ToLookup(r => r.ProjectId);
        var items = result.projects
            .Select(p => _factory.BuildDomain(p, byProject[p.Id]))
            .ToList();

        return (items, result.total);
    }

    public async Task<AIProject> SaveAsync(AIProject project, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        if (project.Id == Guid.Empty)
        {
            project.Id = Guid.NewGuid();
        }

        foreach (var resource in project.Resources)
        {
            if (resource.Id == Guid.Empty)
            {
                resource.Id = Guid.NewGuid();
            }
        }

        using IEfCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Projects.FirstOrDefaultAsync(e => e.Id == project.Id, cancellationToken);

            if (existing is null)
            {
                project.DateCreated = project.DateCreated == default ? now : project.DateCreated;
                project.DateModified = now;
                project.Version = project.Version <= 0 ? 1 : project.Version;

                db.Projects.Add(_factory.BuildEntity(project));
                foreach (var resource in project.Resources)
                {
                    db.ProjectResources.Add(_factory.BuildResourceEntity(resource, project.Id));
                }
            }
            else
            {
                project.DateModified = now;
                project.Version = existing.Version + 1;
                _factory.UpdateEntity(existing, project);

                var existingResources = await db.ProjectResources
                    .Where(r => r.ProjectId == project.Id)
                    .ToListAsync(cancellationToken);

                var existingById = existingResources.ToDictionary(r => r.Id);
                var incomingIds = project.Resources.Select(r => r.Id).ToHashSet();

                // Remove deleted.
                foreach (var stale in existingResources.Where(r => !incomingIds.Contains(r.Id)))
                {
                    db.ProjectResources.Remove(stale);
                }

                // Add new / update existing.
                foreach (var resource in project.Resources)
                {
                    if (existingById.TryGetValue(resource.Id, out var existingResource))
                    {
                        _factory.UpdateResourceEntity(existingResource, resource);
                    }
                    else
                    {
                        db.ProjectResources.Add(_factory.BuildResourceEntity(resource, project.Id));
                    }
                }
            }

            return await db.SaveChangesAsync(cancellationToken);
        });
        scope.Complete();

        return project;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Projects.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity is not null)
            {
                // Resources cascade-delete. Deletion of a project that still owns conversations is
                // blocked at the service layer (AIProjectDeletingNotification), so the conversation FK's
                // SetNull behaviour is only a DB-level backstop and should not fire in normal operation.
                db.Projects.Remove(entity);
                return await db.SaveChangesAsync(cancellationToken);
            }

            return 0;
        });
        scope.Complete();
    }
}
