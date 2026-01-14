using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Core.Analytics;

namespace Umbraco.Ai.Persistence.Analytics;

/// <summary>
/// EF Core repository for managing raw AI usage records.
/// </summary>
internal sealed class EfCoreAiUsageRecordRepository : IAiUsageRecordRepository
{
    private readonly UmbracoAiDbContext _context;
    private readonly ILogger<EfCoreAiUsageRecordRepository> _logger;

    public EfCoreAiUsageRecordRepository(
        UmbracoAiDbContext context,
        ILogger<EfCoreAiUsageRecordRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AiUsageRecord record, CancellationToken ct = default)
    {
        var entity = AiUsageRecordFactory.BuildUsageRecordEntity(record);

        await _context.UsageRecords.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Saved usage record {RecordId} for profile {ProfileAlias} at {Timestamp}",
            record.Id,
            record.ProfileAlias,
            record.Timestamp);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageRecord>> GetRecordsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var entities = await _context.UsageRecords
            .Where(r => r.Timestamp >= from && r.Timestamp < to)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(ct);

        _logger.LogDebug(
            "Retrieved {Count} usage records for period {From} to {To}",
            entities.Count,
            from,
            to);

        return entities.Select(AiUsageRecordFactory.BuildUsageRecordDomain);
    }

    /// <inheritdoc />
    public async Task DeleteRecordsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var recordsToDelete = await _context.UsageRecords
            .Where(r => r.Timestamp >= from && r.Timestamp < to)
            .ToListAsync(ct);

        if (recordsToDelete.Count == 0)
        {
            _logger.LogDebug(
                "No usage records found to delete for period {From} to {To}",
                from,
                to);
            return;
        }

        _context.UsageRecords.RemoveRange(recordsToDelete);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} usage records for period {From} to {To}",
            recordsToDelete.Count,
            from,
            to);
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastRecordTimestampAsync(CancellationToken ct = default)
    {
        var lastTimestamp = await _context.UsageRecords
            .MaxAsync(r => (DateTime?)r.Timestamp, ct);

        _logger.LogDebug("Last usage record timestamp: {Timestamp}", lastTimestamp);

        return lastTimestamp;
    }
}
