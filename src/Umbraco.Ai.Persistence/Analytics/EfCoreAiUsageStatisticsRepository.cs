using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Core.Analytics;

namespace Umbraco.Ai.Persistence.Analytics;

/// <summary>
/// EF Core repository for managing aggregated AI usage statistics.
/// </summary>
internal sealed class EfCoreAiUsageStatisticsRepository : IAiUsageStatisticsRepository
{
    private readonly UmbracoAiDbContext _context;
    private readonly ILogger<EfCoreAiUsageStatisticsRepository> _logger;

    public EfCoreAiUsageStatisticsRepository(
        UmbracoAiDbContext context,
        ILogger<EfCoreAiUsageStatisticsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageStatistics>> GetHourlyByPeriodAsync(
        DateTime from,
        DateTime to,
        AiUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        var query = _context.UsageStatisticsHourly
            .Where(s => s.Period >= from && s.Period < to);

        query = ApplyFilter(query, filter);

        var entities = await query
            .OrderBy(s => s.Period)
            .ToListAsync(ct);

        _logger.LogDebug(
            "Retrieved {Count} hourly statistics for period {From} to {To}",
            entities.Count,
            from,
            to);

        return entities.Select(AiUsageRecordFactory.BuildStatisticsDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageStatistics>> GetDailyByPeriodAsync(
        DateTime from,
        DateTime to,
        AiUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        var query = _context.UsageStatisticsDaily
            .Where(s => s.Period >= from && s.Period < to);

        query = ApplyFilter(query, filter);

        var entities = await query
            .OrderBy(s => s.Period)
            .ToListAsync(ct);

        _logger.LogDebug(
            "Retrieved {Count} daily statistics for period {From} to {To}",
            entities.Count,
            from,
            to);

        return entities.Select(AiUsageRecordFactory.BuildStatisticsDomain);
    }

    /// <inheritdoc />
    public async Task SaveHourlyBatchAsync(
        IEnumerable<AiUsageStatistics> statistics,
        CancellationToken ct = default)
    {
        var entities = statistics
            .Select(AiUsageRecordFactory.BuildHourlyStatisticsEntity)
            .ToList();

        if (entities.Count == 0)
        {
            _logger.LogDebug("No hourly statistics to save");
            return;
        }

        await _context.UsageStatisticsHourly.AddRangeAsync(entities, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Saved {Count} hourly statistics records",
            entities.Count);
    }

    /// <inheritdoc />
    public async Task SaveDailyBatchAsync(
        IEnumerable<AiUsageStatistics> statistics,
        CancellationToken ct = default)
    {
        var entities = statistics
            .Select(AiUsageRecordFactory.BuildDailyStatisticsEntity)
            .ToList();

        if (entities.Count == 0)
        {
            _logger.LogDebug("No daily statistics to save");
            return;
        }

        await _context.UsageStatisticsDaily.AddRangeAsync(entities, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Saved {Count} daily statistics records",
            entities.Count);
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastAggregatedHourlyPeriodAsync(CancellationToken ct = default)
    {
        var lastPeriod = await _context.UsageStatisticsHourly
            .MaxAsync(s => (DateTime?)s.Period, ct);

        _logger.LogDebug("Last aggregated hourly period: {Period}", lastPeriod);

        return lastPeriod;
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastAggregatedDailyPeriodAsync(CancellationToken ct = default)
    {
        var lastPeriod = await _context.UsageStatisticsDaily
            .MaxAsync(s => (DateTime?)s.Period, ct);

        _logger.LogDebug("Last aggregated daily period: {Period}", lastPeriod);

        return lastPeriod;
    }

    /// <inheritdoc />
    public async Task DeleteHourlyForPeriodAsync(DateTime period, CancellationToken ct = default)
    {
        var statsToDelete = await _context.UsageStatisticsHourly
            .Where(s => s.Period == period)
            .ToListAsync(ct);

        if (statsToDelete.Count == 0)
        {
            _logger.LogDebug("No hourly statistics found to delete for period {Period}", period);
            return;
        }

        _context.UsageStatisticsHourly.RemoveRange(statsToDelete);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} hourly statistics records for period {Period}",
            statsToDelete.Count,
            period);
    }

    /// <inheritdoc />
    public async Task DeleteDailyForPeriodAsync(DateTime period, CancellationToken ct = default)
    {
        var statsToDelete = await _context.UsageStatisticsDaily
            .Where(s => s.Period == period)
            .ToListAsync(ct);

        if (statsToDelete.Count == 0)
        {
            _logger.LogDebug("No daily statistics found to delete for period {Period}", period);
            return;
        }

        _context.UsageStatisticsDaily.RemoveRange(statsToDelete);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} daily statistics records for period {Period}",
            statsToDelete.Count,
            period);
    }

    /// <inheritdoc />
    public async Task DeleteHourlyOlderThanAsync(DateTime olderThan, CancellationToken ct = default)
    {
        var statsToDelete = await _context.UsageStatisticsHourly
            .Where(s => s.Period < olderThan)
            .ToListAsync(ct);

        if (statsToDelete.Count == 0)
        {
            _logger.LogDebug("No hourly statistics found to delete older than {Date}", olderThan);
            return;
        }

        _context.UsageStatisticsHourly.RemoveRange(statsToDelete);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} hourly statistics records older than {Date}",
            statsToDelete.Count,
            olderThan);
    }

    /// <inheritdoc />
    public async Task DeleteDailyOlderThanAsync(DateTime olderThan, CancellationToken ct = default)
    {
        var statsToDelete = await _context.UsageStatisticsDaily
            .Where(s => s.Period < olderThan)
            .ToListAsync(ct);

        if (statsToDelete.Count == 0)
        {
            _logger.LogDebug("No daily statistics found to delete older than {Date}", olderThan);
            return;
        }

        _context.UsageStatisticsDaily.RemoveRange(statsToDelete);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} daily statistics records older than {Date}",
            statsToDelete.Count,
            olderThan);
    }

    /// <summary>
    /// Applies filter criteria to hourly statistics query.
    /// </summary>
    private static IQueryable<AiUsageStatisticsHourlyEntity> ApplyFilter(
        IQueryable<AiUsageStatisticsHourlyEntity> query,
        AiUsageFilter? filter)
    {
        if (filter == null)
            return query;

        if (filter.ProviderId != null)
            query = query.Where(s => s.ProviderId == filter.ProviderId);

        if (filter.ModelId != null)
            query = query.Where(s => s.ModelId == filter.ModelId);

        if (filter.ProfileId != null)
            query = query.Where(s => s.ProfileId == filter.ProfileId.Value);

        if (filter.Capability != null)
            query = query.Where(s => s.Capability == (int)filter.Capability.Value);

        if (filter.UserId != null)
            query = query.Where(s => s.UserId == filter.UserId);

        if (filter.EntityType != null)
            query = query.Where(s => s.EntityType == filter.EntityType);

        if (filter.FeatureType != null)
            query = query.Where(s => s.FeatureType == filter.FeatureType);

        return query;
    }

    /// <summary>
    /// Applies filter criteria to daily statistics query.
    /// </summary>
    private static IQueryable<AiUsageStatisticsDailyEntity> ApplyFilter(
        IQueryable<AiUsageStatisticsDailyEntity> query,
        AiUsageFilter? filter)
    {
        if (filter == null)
            return query;

        if (filter.ProviderId != null)
            query = query.Where(s => s.ProviderId == filter.ProviderId);

        if (filter.ModelId != null)
            query = query.Where(s => s.ModelId == filter.ModelId);

        if (filter.ProfileId != null)
            query = query.Where(s => s.ProfileId == filter.ProfileId.Value);

        if (filter.Capability != null)
            query = query.Where(s => s.Capability == (int)filter.Capability.Value);

        if (filter.UserId != null)
            query = query.Where(s => s.UserId == filter.UserId);

        if (filter.EntityType != null)
            query = query.Where(s => s.EntityType == filter.EntityType);

        if (filter.FeatureType != null)
            query = query.Where(s => s.FeatureType == filter.FeatureType);

        return query;
    }
}
