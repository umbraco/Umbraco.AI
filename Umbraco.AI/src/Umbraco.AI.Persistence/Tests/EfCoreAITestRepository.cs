using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Tests;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// EF Core implementation of the AI test repository.
/// </summary>
internal class EfCoreAITestRepository : IAITestRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EfCoreAITestRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AITest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AITestEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Tests.FirstOrDefaultAsync(t => t.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AITestFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AITest?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AITestEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Tests.FirstOrDefaultAsync(
                t => t.Alias.ToLower() == alias.ToLower(),
                cancellationToken));

        scope.Complete();
        return entity is null ? null : AITestFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AITest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AITestEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Tests.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AITestFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AITest> Items, int Total)> GetPagedAsync(
        string? filter = null,
        string? testTypeId = null,
        bool? isActive = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AITestEntity> query = db.Tests;

            // Apply test type filter
            if (!string.IsNullOrEmpty(testTypeId))
            {
                query = query.Where(t => t.TestTypeId == testTypeId);
            }

            // Apply active filter
            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            // Apply name filter (case-insensitive contains)
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(t => t.Name.ToLower().Contains(filter.ToLower()));
            }

            // Get total count before pagination
            int total = await query.CountAsync(cancellationToken);

            // Apply pagination
            List<AITestEntity> items = await query
                .OrderBy(t => t.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AITestFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AITest> SaveAsync(AITest test, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var savedTest = await scope.ExecuteWithContextAsync(async db =>
        {
            AITestEntity? existing = await db.Tests.FindAsync([test.Id], cancellationToken);

            if (existing is null)
            {
                // New test - set version and user IDs
                test.Version = 1;
                test.DateModified = DateTime.UtcNow;
                test.CreatedByUserId = userId;
                test.ModifiedByUserId = userId;

                AITestEntity newEntity = AITestFactory.BuildEntity(test);
                db.Tests.Add(newEntity);
            }
            else
            {
                // Update existing - increment version
                test.Version = existing.Version + 1;
                test.DateModified = DateTime.UtcNow;
                test.ModifiedByUserId = userId;

                AITestFactory.UpdateEntity(existing, test);
            }

            await db.SaveChangesAsync(cancellationToken);
            return test;
        });

        scope.Complete();
        return savedTest;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AITestEntity? entity = await db.Tests.FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.Tests.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
