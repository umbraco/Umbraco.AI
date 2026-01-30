using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Tests;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// EF Core implementation of the AI test run repository.
/// </summary>
internal class EfCoreAiTestRunRepository : IAiTestRunRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    public EfCoreAiTestRunRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AiTestRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiTestRunEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Include(r => r.GraderResults)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiTestRunFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AiTestRun?> GetByIdWithTranscriptAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiTestRunEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Include(r => r.Transcript)
                .Include(r => r.GraderResults)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiTestRunFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTestRun>> GetByTestIdAsync(
        Guid testId,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiTestRunEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Include(r => r.GraderResults)
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.ExecutedAt)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiTestRunFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AiTestRun?> GetLatestByTestIdAsync(
        Guid testId,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiTestRunEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Include(r => r.GraderResults)
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.ExecutedAt)
                .FirstOrDefaultAsync(cancellationToken));

        scope.Complete();
        return entity is null ? null : AiTestRunFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiTestRun> Items, int Total)> GetPagedByTestIdAsync(
        Guid testId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiTestRunEntity> query = db.TestRuns
                .Include(r => r.GraderResults)
                .Where(r => r.TestId == testId);

            int total = await query.CountAsync(cancellationToken);

            List<AiTestRunEntity> items = await query
                .OrderByDescending(r => r.ExecutedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiTestRunFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTestRun>> GetByBatchIdAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiTestRunEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.TestRuns
                .Include(r => r.GraderResults)
                .Where(r => r.BatchId == batchId)
                .OrderBy(r => r.ExecutedAt)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiTestRunFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task AddAsync(AiTestRun run, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<int>(async db =>
        {
            AiTestRunEntity entity = AiTestRunFactory.BuildEntity(run);
            await db.TestRuns.AddAsync(entity, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            // Update domain model with generated ID
            run.GetType().GetProperty(nameof(AiTestRun.Id))!
                .SetValue(run, entity.Id);

            return 0; // Return dummy value
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AiTestRun run, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<int>(async db =>
        {
            AiTestRunEntity? existing = await db.TestRuns
                .Include(r => r.Transcript)
                .Include(r => r.GraderResults)
                .FirstOrDefaultAsync(r => r.Id == run.Id, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"Test run with ID {run.Id} not found");
            }

            // Update run properties
            existing.TestVersion = run.TestVersion;
            existing.RunNumber = run.RunNumber;
            existing.ProfileId = run.ProfileId;
            existing.ContextIdsJson = run.ContextIdsJson;
            existing.ExecutedAt = run.ExecutedAt;
            existing.ExecutedByUserId = run.ExecutedByUserId;
            existing.Status = (int)run.Status;
            existing.DurationMs = run.DurationMs;
            existing.ErrorMessage = run.ErrorMessage;
            existing.MetadataJson = run.MetadataJson;
            existing.BatchId = run.BatchId;

            if (run.Outcome != null)
            {
                existing.OutcomeType = (int)run.Outcome.OutputType;
                existing.OutcomeValue = run.Outcome.OutputValue;
                existing.FinishReason = run.Outcome.FinishReason;
                existing.InputTokens = run.Outcome.InputTokens;
                existing.OutputTokens = run.Outcome.OutputTokens;
            }

            // Update transcript
            if (run.Transcript != null)
            {
                if (existing.Transcript != null)
                {
                    db.TestTranscripts.Remove(existing.Transcript);
                }
                existing.Transcript = AiTestTranscriptFactory.BuildEntity(run.Transcript);
            }

            // Update grader results
            db.TestGraderResults.RemoveRange(existing.GraderResults);
            existing.GraderResults = run.GraderResults
                .Select(AiTestGraderResultFactory.BuildEntity)
                .ToList();

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
            AiTestRunEntity? entity = await db.TestRuns.FindAsync(
                new object[] { id },
                cancellationToken);

            if (entity != null)
            {
                db.TestRuns.Remove(entity);
                await db.SaveChangesAsync(cancellationToken);
            }
            return 0; // Return dummy value
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteOldRunsAsync(
        Guid testId,
        int keepCount,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<int>(async db =>
        {
            // Get IDs of runs to keep (most recent N)
            List<Guid> keepIds = await db.TestRuns
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.ExecutedAt)
                .Take(keepCount)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            // Get runs to delete
            List<AiTestRunEntity> runsToDelete = await db.TestRuns
                .Where(r => r.TestId == testId && !keepIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            if (runsToDelete.Any())
            {
                db.TestRuns.RemoveRange(runsToDelete);
                await db.SaveChangesAsync(cancellationToken);
            }
            return 0; // Return dummy value
        });

        scope.Complete();
    }
}
