using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Tests;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// EF Core implementation of the AI test repository.
/// </summary>
internal class EfCoreAiTestRepository : IAiTestRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    public EfCoreAiTestRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AiTest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiTestEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Tests
                .Include(t => t.Graders)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiTestFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AiTest?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiTestEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Tests
                .Include(t => t.Graders)
                .FirstOrDefaultAsync(t => t.Alias.ToLower() == alias.ToLower(), cancellationToken));

        scope.Complete();
        return entity is null ? null : AiTestFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiTestEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Tests
                .Include(t => t.Graders)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiTestFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTest>> GetByTagsAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var tagsList = tags.Select(t => t.ToLower()).ToList();

        List<AiTestEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Tests
                .Include(t => t.Graders)
                .Where(t => t.Tags != null && tagsList.Any(tag => t.Tags.ToLower().Contains(tag)))
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiTestFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiTest> Items, int Total)> GetPagedAsync(
        string? filter = null,
        string? testTypeId = null,
        bool? isEnabled = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiTestEntity> query = db.Tests.Include(t => t.Graders);

            // Apply test type filter
            if (!string.IsNullOrEmpty(testTypeId))
            {
                query = query.Where(t => t.TestTypeId == testTypeId);
            }

            // Apply enabled filter
            if (isEnabled.HasValue)
            {
                query = query.Where(t => t.IsEnabled == isEnabled.Value);
            }

            // Apply name/alias filter (case-insensitive contains)
            if (!string.IsNullOrEmpty(filter))
            {
                string lowerFilter = filter.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(lowerFilter) ||
                    t.Alias.ToLower().Contains(lowerFilter));
            }

            int total = await query.CountAsync(cancellationToken);

            List<AiTestEntity> items = await query
                .OrderByDescending(t => t.DateModified)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiTestFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<bool> AliasExistsAsync(
        string alias,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool exists = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiTestEntity> query = db.Tests
                .Where(t => t.Alias.ToLower() == alias.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        });

        scope.Complete();
        return exists;
    }

    /// <inheritdoc />
    public async Task AddAsync(AiTest test, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<int>(async db =>
        {
            AiTestEntity entity = AiTestFactory.BuildEntity(test);
            await db.Tests.AddAsync(entity, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            // Update domain model with generated ID
            test.GetType().GetProperty(nameof(AiTest.Id))!
                .SetValue(test, entity.Id);

            return 0; // Return dummy value
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AiTest test, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<int>(async db =>
        {
            AiTestEntity? existing = await db.Tests
                .Include(t => t.Graders)
                .FirstOrDefaultAsync(t => t.Id == test.Id, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"Test with ID {test.Id} not found");
            }

            // Update test properties
            existing.Alias = test.Alias;
            existing.Name = test.Name;
            existing.Description = test.Description;
            existing.TestTypeId = test.TestTypeId;
            existing.TargetId = test.Target.TargetId;
            existing.TargetIsAlias = test.Target.IsAlias;
            existing.TestCaseJson = test.TestCase.TestCaseJson;
            existing.RunCount = test.RunCount;
            existing.Tags = test.Tags.Any() ? string.Join(',', test.Tags) : null;
            existing.IsEnabled = test.IsEnabled;
            existing.BaselineRunId = test.BaselineRunId;
            existing.Version = test.Version;
            existing.DateModified = test.DateModified;
            existing.ModifiedByUserId = test.ModifiedByUserId;

            // Update graders collection
            db.TestGraders.RemoveRange(existing.Graders);
            existing.Graders = test.Graders.Select(AiTestGraderFactory.BuildEntity).ToList();

            await db.SaveChangesAsync(cancellationToken);
            return 0; // Return dummy value
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<int>(async db =>
        {
            AiTestEntity? entity = await db.Tests.FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                db.Tests.Remove(entity);
                await db.SaveChangesAsync(cancellationToken);
            }
            return 0; // Return dummy value
        });

        scope.Complete();
    }
}
