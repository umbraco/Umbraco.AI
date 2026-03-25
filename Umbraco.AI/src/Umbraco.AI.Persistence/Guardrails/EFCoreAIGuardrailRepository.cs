using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Guardrails;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Guardrails;

/// <summary>
/// EF Core implementation of the AI guardrail repository.
/// </summary>
internal class EFCoreAIGuardrailRepository : IAIGuardrailRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EFCoreAIGuardrailRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AIGuardrail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIGuardrailEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Guardrails
                .Include(g => g.Rules)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AIGuardrailFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AIGuardrail?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIGuardrailEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Guardrails
                .Include(g => g.Rules)
                .FirstOrDefaultAsync(g => g.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();
        return entity is null ? null : AIGuardrailFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIGuardrail>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIGuardrailEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Guardrails
                .Include(g => g.Rules)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AIGuardrailFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AIGuardrail> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIGuardrailEntity> query = db.Guardrails.Include(g => g.Rules);

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(g =>
                    g.Name.ToLower().Contains(filter.ToLower()) ||
                    g.Alias.ToLower().Contains(filter.ToLower()));
            }

            int total = await query.CountAsync(cancellationToken);

            List<AIGuardrailEntity> items = await query
                .OrderBy(g => g.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AIGuardrailFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIGuardrail>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIGuardrailEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Guardrails
                .Include(g => g.Rules)
                .Where(g => idList.Contains(g.Id))
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AIGuardrailFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AIGuardrail> SaveAsync(AIGuardrail guardrail, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var savedGuardrail = await scope.ExecuteWithContextAsync(async db =>
        {
            AIGuardrailEntity? existing = await db.Guardrails
                .Include(g => g.Rules)
                .FirstOrDefaultAsync(g => g.Id == guardrail.Id, cancellationToken);

            if (existing is null)
            {
                // New guardrail
                guardrail.Version = 1;
                guardrail.DateModified = DateTime.UtcNow;
                guardrail.CreatedByUserId = userId;
                guardrail.ModifiedByUserId = userId;

                AIGuardrailEntity newEntity = AIGuardrailFactory.BuildEntity(guardrail);
                db.Guardrails.Add(newEntity);
            }
            else
            {
                // Update existing
                guardrail.Version = existing.Version + 1;
                guardrail.DateModified = DateTime.UtcNow;
                guardrail.ModifiedByUserId = userId;

                AIGuardrailFactory.UpdateEntity(existing, guardrail);

                // Handle rules: remove deleted, update existing, add new
                var existingRuleIds = existing.Rules.Select(r => r.Id).ToHashSet();
                var newRuleIds = guardrail.Rules.Select(r => r.Id).ToHashSet();

                // Remove deleted rules
                var toRemove = existing.Rules.Where(r => !newRuleIds.Contains(r.Id)).ToList();
                foreach (var rule in toRemove)
                {
                    db.GuardrailRules.Remove(rule);
                }

                // Update or add rules
                foreach (var rule in guardrail.Rules)
                {
                    if (existingRuleIds.Contains(rule.Id))
                    {
                        var existingRule = existing.Rules.First(r => r.Id == rule.Id);
                        AIGuardrailFactory.UpdateRuleEntity(existingRule, rule);
                    }
                    else
                    {
                        var newRule = AIGuardrailFactory.BuildRuleEntity(rule, guardrail.Id);
                        db.GuardrailRules.Add(newRule);
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return guardrail;
        });

        scope.Complete();
        return savedGuardrail;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AIGuardrailEntity? entity = await db.Guardrails
                .Include(g => g.Rules)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

            if (entity is null)
            {
                return false;
            }

            db.Guardrails.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
