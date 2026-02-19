using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Tests;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// EF Core implementation of the AI test run repository.
/// </summary>
internal class EfCoreAITestRunRepository : IAITestRunRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EfCoreAITestRunRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AITestRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AITestRunEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AITestRunFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AITestRun>> GetByTestIdAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AITestRunEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.ExecutedAt)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AITestRunFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<Umbraco.Cms.Core.Models.PagedModel<AITestRun>> GetPagedByTestIdAsync(
        Guid testId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AITestRunEntity> query = db.TestRuns.Where(r => r.TestId == testId);

            int total = await query.CountAsync(cancellationToken);

            List<AITestRunEntity> items = await query
                .OrderByDescending(r => r.ExecutedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return new Umbraco.Cms.Core.Models.PagedModel<AITestRun>(
            result.total,
            result.items.Select(AITestRunFactory.BuildDomain));
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AITestRun> Items, int Total)> GetPagedAsync(
        Guid? testId = null,
        Guid? batchId = null,
        AITestRunStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AITestRunEntity> query = db.TestRuns;

            // Apply filters
            if (testId.HasValue)
            {
                query = query.Where(r => r.TestId == testId.Value);
            }

            if (batchId.HasValue)
            {
                query = query.Where(r => r.BatchId == batchId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == (int)status.Value);
            }

            int total = await query.CountAsync(cancellationToken);

            List<AITestRunEntity> items = await query
                .OrderByDescending(r => r.ExecutedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AITestRunFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AITestRun?> GetLatestByTestIdAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AITestRunEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.ExecutedAt)
                .FirstOrDefaultAsync(cancellationToken));

        scope.Complete();
        return entity is null ? null : AITestRunFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AITestRun>> GetByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AITestRunEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Where(r => r.BatchId == batchId)
                .OrderByDescending(r => r.ExecutedAt)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AITestRunFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AITestRun> SaveAsync(AITestRun run, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var savedRun = await scope.ExecuteWithContextAsync(async db =>
        {
            AITestRunEntity? existing = await db.TestRuns.FindAsync([run.Id], cancellationToken);

            if (existing is null)
            {
                AITestRunEntity newEntity = AITestRunFactory.BuildEntity(run);
                db.TestRuns.Add(newEntity);
            }
            else
            {
                AITestRunFactory.UpdateEntity(existing, run);
            }

            await db.SaveChangesAsync(cancellationToken);
            return run;
        });

        scope.Complete();
        return savedRun;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AITestRunEntity? entity = await db.TestRuns.FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.TestRuns.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldRunsAsync(Guid testId, int keepCount, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        int deletedCount = await scope.ExecuteWithContextAsync(async db =>
        {
            // Get runs to delete with their transcript IDs (all except the latest keepCount)
            var runsToDelete = await db.TestRuns
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.ExecutedAt)
                .Skip(keepCount)
                .Select(r => new { r.Id, r.TranscriptId })
                .ToListAsync(cancellationToken);

            if (runsToDelete.Count == 0)
            {
                return 0;
            }

            // Extract IDs for bulk operations
            var runIds = runsToDelete.Select(r => r.Id).ToList();
            var transcriptIds = runsToDelete
                .Where(r => r.TranscriptId.HasValue)
                .Select(r => r.TranscriptId!.Value)
                .ToList();

            // Delete associated transcripts first (if any)
            if (transcriptIds.Count > 0)
            {
                await db.TestTranscripts
                    .Where(t => transcriptIds.Contains(t.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // Bulk delete the runs
            await db.TestRuns
                .Where(r => runIds.Contains(r.Id))
                .ExecuteDeleteAsync(cancellationToken);

            return runsToDelete.Count;
        });

        scope.Complete();
        return deletedCount;
    }
}
