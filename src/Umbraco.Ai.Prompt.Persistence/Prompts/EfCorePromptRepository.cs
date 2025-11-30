using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Prompt.Persistence.Prompts;

/// <summary>
/// EF Core implementation of <see cref="IPromptRepository"/>.
/// </summary>
internal sealed class EfCorePromptRepository : IPromptRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiPromptDbContext> _scopeProvider;

    public EfCorePromptRepository(IEFCoreScopeProvider<UmbracoAiPromptDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<Prompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return entity is null ? null : PromptEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Prompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();

        return entity is null ? null : PromptEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Prompt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken));

        scope.Complete();

        return entities.Select(PromptEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Prompt>> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking()
                .Where(e => e.ProfileId == profileId)
                .OrderBy(e => e.Name)
                .ToListAsync(cancellationToken));

        scope.Complete();

        return entities.Select(PromptEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<PagedModel<Prompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<PromptEntity> query = db.Prompts.AsNoTracking();

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

        var prompts = result.items.Select(PromptEntityFactory.BuildDomain).ToList();
        return new PagedModel<Prompt>(result.total, prompts);
    }

    /// <inheritdoc />
    public async Task<Prompt> SaveAsync(Prompt prompt, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiPromptDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<Task>(async db =>
        {
            var existing = await db.Prompts.FirstOrDefaultAsync(e => e.Id == prompt.Id, cancellationToken);

            if (existing is null)
            {
                var entity = PromptEntityFactory.BuildEntity(prompt);
                db.Prompts.Add(entity);
            }
            else
            {
                PromptEntityFactory.UpdateEntity(existing, prompt);
            }

            await db.SaveChangesAsync(cancellationToken);
        });

        scope.Complete();

        return prompt;
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
}
