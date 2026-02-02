using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Prompt.Persistence.Prompts;

/// <summary>
/// EF Core implementation of <see cref="IAIPromptRepository"/>.
/// </summary>
internal sealed class EfCoreAiPromptRepository : IAIPromptRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIPromptDbContext> _scopeProvider;

    public EfCoreAiPromptRepository(IEFCoreScopeProvider<UmbracoAIPromptDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<Core.Prompts.AIPrompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return entity is null ? null : AIPromptEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Core.Prompts.AIPrompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();

        return entity is null ? null : AIPromptEntityFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Prompts.AIPrompt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken));

        scope.Complete();

        return entities.Select(AIPromptEntityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<PagedModel<Core.Prompts.AIPrompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIPromptEntity> query = db.Prompts.AsNoTracking();

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

        var prompts = result.items.Select(AIPromptEntityFactory.BuildDomain).ToList();
        return new PagedModel<Core.Prompts.AIPrompt>(result.total, prompts);
    }

    /// <inheritdoc />
    public async Task<Core.Prompts.AIPrompt> SaveAsync(Core.Prompts.AIPrompt prompt, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

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

                var entity = AIPromptEntityFactory.BuildEntity(prompt);
                db.Prompts.Add(entity);
            }
            else
            {
                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                prompt.Version = existing.Version + 1;
                prompt.DateModified = DateTime.UtcNow;
                prompt.ModifiedByUserId = userId;

                AIPromptEntityFactory.UpdateEntity(existing, prompt);
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
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

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
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

        var exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Prompts.AnyAsync(e => e.Id == id, cancellationToken));

        scope.Complete();

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIPromptDbContext> scope = _scopeProvider.CreateScope();

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
