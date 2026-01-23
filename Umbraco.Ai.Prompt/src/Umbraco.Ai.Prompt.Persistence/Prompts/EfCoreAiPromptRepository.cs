using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Prompt.Persistence.Prompts;

/// <summary>
/// EF Core implementation of <see cref="IAiPromptRepository"/>.
/// </summary>
internal sealed class EfCoreAiPromptRepository : IAiPromptRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiPromptDbContext> _scopeProvider;

    public EfCoreAiPromptRepository(IEFCoreScopeProvider<UmbracoAiPromptDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<Core.Prompts.AiPrompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return entity is null ? null : AiPromptEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Core.Prompts.AiPrompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();

        return entity is null ? null : AiPromptEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Prompts.AiPrompt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken));

        scope.Complete();

        return entities.Select(AiPromptEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<PagedModel<Core.Prompts.AiPrompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiPromptEntity> query = db.Prompts.AsNoTracking();

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

        var prompts = result.items.Select(AiPromptEntityFactory.BuildDomain).ToList();
        return new PagedModel<Core.Prompts.AiPrompt>(result.total, prompts);
    }

    /// <inheritdoc />
    public async Task<Core.Prompts.AiPrompt> SaveAsync(Core.Prompts.AiPrompt prompt, int? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var savedPrompt = await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Prompts.FirstOrDefaultAsync(e => e.Id == prompt.Id, cancellationToken);

            if (existing is null)
            {
                // New prompt - set version and user IDs on domain model before mapping
                prompt.Version = 1;
                prompt.DateModified = DateTime.UtcNow;
                prompt.CreatedByUserId = userId;
                prompt.ModifiedByUserId = userId;

                var entity = AiPromptEntityFactory.BuildEntity(prompt);
                db.Prompts.Add(entity);
            }
            else
            {
                // Existing prompt - create version snapshot of current state before updating
                var existingDomain = AiPromptEntityFactory.BuildDomain(existing);
                var versionEntity = new AiPromptVersionEntity
                {
                    Id = Guid.NewGuid(),
                    PromptId = existing.Id,
                    Version = existing.Version,
                    Snapshot = AiPromptEntityFactory.CreateSnapshot(existingDomain),
                    DateCreated = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                db.PromptVersions.Add(versionEntity);

                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                prompt.Version = existing.Version + 1;
                prompt.DateModified = DateTime.UtcNow;
                prompt.ModifiedByUserId = userId;

                AiPromptEntityFactory.UpdateEntity(existing, prompt);
            }

            await db.SaveChangesAsync(cancellationToken);
            return prompt;
        });

        scope.Complete();

        return savedPrompt;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Prompts.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.Prompts.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();

        return deleted;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AnyAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var exists = await scope.ExecuteWithContextAsync(async db =>
        {
            var lowerAlias = alias.ToLower();
            var query = db.Prompts.Where(e => e.Alias.ToLower() == lowerAlias);

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
        Guid promptId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiPromptVersionEntity> query = db.PromptVersions
                .Where(v => v.PromptId == promptId)
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
            EntityId = e.PromptId,
            Version = e.Version,
            Snapshot = e.Snapshot,
            DateCreated = e.DateCreated,
            CreatedByUserId = e.CreatedByUserId,
            ChangeDescription = e.ChangeDescription
        });
    }

    /// <inheritdoc />
    public async Task<Core.Prompts.AiPrompt?> GetVersionSnapshotAsync(
        Guid promptId,
        int version,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.PromptVersions
                .FirstOrDefaultAsync(v => v.PromptId == promptId && v.Version == version, cancellationToken));

        scope.Complete();

        if (entity is null || string.IsNullOrEmpty(entity.Snapshot))
        {
            return null;
        }

        return AiPromptEntityFactory.BuildDomainFromSnapshot(entity.Snapshot);
    }
}
