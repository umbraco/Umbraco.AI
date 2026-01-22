using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Core;
using Umbraco.Ai.Core.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Agent.Persistence.Agents;

/// <summary>
/// EF Core implementation of <see cref="IAiAgentRepository"/>.
/// </summary>
internal sealed class EfCoreAiAgentRepository : IAiAgentRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiAgentDbContext> _scopeProvider;

    public EfCoreAiAgentRepository(IEFCoreScopeProvider<UmbracoAiAgentDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<Core.Agents.AiAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return entity is null ? null : AiAgentEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Core.Agents.AiAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();

        return entity is null ? null : AiAgentEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Agents.AiAgent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken));

        scope.Complete();

        return entities.Select(AiAgentEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<PagedModel<Core.Agents.AiAgent>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiAgentEntity> query = db.Agents.AsNoTracking();

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

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(e => e.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (total, items);
        });

        scope.Complete();

        var Agents = result.items.Select(AiAgentEntityFactory.BuildDomain).ToList();
        return new PagedModel<Core.Agents.AiAgent>(result.total, Agents);
    }

    /// <inheritdoc />
    public async Task<Core.Agents.AiAgent> SaveAsync(Core.Agents.AiAgent agent, int? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

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

                var entity = AiAgentEntityFactory.BuildEntity(agent);
                db.Agents.Add(entity);
            }
            else
            {
                // Existing agent - create version snapshot of current state before updating
                var existingDomain = AiAgentEntityFactory.BuildDomain(existing);
                var versionEntity = new AiAgentVersionEntity
                {
                    Id = Guid.NewGuid(),
                    AgentId = existing.Id,
                    Version = existing.Version,
                    Snapshot = JsonSerializer.Serialize(existingDomain, Constants.DefaultJsonSerializerOptions),
                    DateCreated = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                db.AgentVersions.Add(versionEntity);

                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                agent.Version = existing.Version + 1;
                agent.DateModified = DateTime.UtcNow;
                agent.ModifiedByUserId = userId;

                AiAgentEntityFactory.UpdateEntity(existing, agent);
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
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

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
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

        var exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Agents.AnyAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

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

    /// <inheritdoc />
    public async Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid agentId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiAgentVersionEntity> query = db.AgentVersions
                .Where(v => v.AgentId == agentId)
                .OrderByDescending(v => v.Version);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return await query.ToListAsync(cancellationToken);
        });

        scope.Complete();

        return entities.Select(e => new AiEntityVersion
        {
            Id = e.Id,
            EntityId = e.AgentId,
            Version = e.Version,
            Snapshot = e.Snapshot,
            DateCreated = e.DateCreated,
            CreatedByUserId = e.CreatedByUserId,
            ChangeDescription = e.ChangeDescription
        });
    }

    /// <inheritdoc />
    public async Task<Core.Agents.AiAgent?> GetVersionSnapshotAsync(
        Guid agentId,
        int version,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiAgentDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.AgentVersions
                .FirstOrDefaultAsync(v => v.AgentId == agentId && v.Version == version, cancellationToken));

        scope.Complete();

        if (entity is null || string.IsNullOrEmpty(entity.Snapshot))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Core.Agents.AiAgent>(entity.Snapshot, Constants.DefaultJsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }
}
