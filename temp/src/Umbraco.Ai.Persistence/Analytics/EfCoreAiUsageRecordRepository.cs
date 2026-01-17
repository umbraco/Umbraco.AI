using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Core.Analytics;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Analytics;

/// <summary>
/// EF Core repository for managing raw AI usage records.
/// </summary>
internal sealed class EfCoreAiUsageRecordRepository : IAiUsageRecordRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;
    private readonly ILogger<EfCoreAiUsageRecordRepository> _logger;

    public EfCoreAiUsageRecordRepository(
        IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider,
        ILogger<EfCoreAiUsageRecordRepository> logger)
    {
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AiUsageRecord record, CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = AiUsageRecordFactory.BuildUsageRecordEntity(record);
            await db.UsageRecords.AddAsync(entity, ct);
            await db.SaveChangesAsync(ct);

            _logger.LogDebug(
                "Saved usage record {RecordId} for profile {ProfileAlias} at {Timestamp}",
                record.Id,
                record.ProfileAlias,
                record.Timestamp);

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageRecord>> GetRecordsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.UsageRecords
                .Where(r => r.Timestamp >= from && r.Timestamp < to)
                .OrderBy(r => r.Timestamp)
                .ToListAsync(ct));

        _logger.LogDebug(
            "Retrieved {Count} usage records for period {From} to {To}",
            entities.Count,
            from,
            to);

        scope.Complete();
        return entities.Select(AiUsageRecordFactory.BuildUsageRecordDomain);
    }

    /// <inheritdoc />
    public async Task DeleteRecordsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var recordsToDelete = await db.UsageRecords
                .Where(r => r.Timestamp >= from && r.Timestamp < to)
                .ToListAsync(ct);

            if (recordsToDelete.Count == 0)
            {
                _logger.LogDebug(
                    "No usage records found to delete for period {From} to {To}",
                    from,
                    to);
                return true;
            }

            db.UsageRecords.RemoveRange(recordsToDelete);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Deleted {Count} usage records for period {From} to {To}",
                recordsToDelete.Count,
                from,
                to);

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastRecordTimestampAsync(CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var lastTimestamp = await scope.ExecuteWithContextAsync(async db =>
            await db.UsageRecords.MaxAsync(r => (DateTime?)r.Timestamp, ct));

        _logger.LogDebug("Last usage record timestamp: {Timestamp}", lastTimestamp);

        scope.Complete();
        return lastTimestamp;
    }
}
