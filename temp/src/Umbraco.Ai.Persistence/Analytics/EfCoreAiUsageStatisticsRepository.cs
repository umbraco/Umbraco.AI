using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Core.Analytics;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Analytics;

/// <summary>
/// EF Core repository for managing aggregated AI usage statistics.
/// </summary>
internal sealed class EfCoreAiUsageStatisticsRepository : IAiUsageStatisticsRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;
    private readonly ILogger<EfCoreAiUsageStatisticsRepository> _logger;

    public EfCoreAiUsageStatisticsRepository(
        IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider,
        ILogger<EfCoreAiUsageStatisticsRepository> logger)
    {
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageStatistics>> GetHourlyByPeriodAsync(
        DateTime from,
        DateTime to,
        AiUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            var query = db.UsageStatisticsHourly
                .Where(s => s.Period >= from && s.Period < to);

            query = ApplyFilter(query, filter);

            return await query
                .OrderBy(s => s.Period)
                .ToListAsync(ct);
        });

        _logger.LogDebug(
            "Retrieved {Count} hourly statistics for period {From} to {To}",
            entities.Count,
            from,
            to);

        scope.Complete();
        return entities.Select(AiUsageRecordFactory.BuildStatisticsDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageStatistics>> GetDailyByPeriodAsync(
        DateTime from,
        DateTime to,
        AiUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            var query = db.UsageStatisticsDaily
                .Where(s => s.Period >= from && s.Period < to);

            query = ApplyFilter(query, filter);

            return await query
                .OrderBy(s => s.Period)
                .ToListAsync(ct);
        });

        _logger.LogDebug(
            "Retrieved {Count} daily statistics for period {From} to {To}",
            entities.Count,
            from,
            to);

        scope.Complete();
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

        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            await db.UsageStatisticsHourly.AddRangeAsync(entities, ct);
            await db.SaveChangesAsync(ct);
            return true;
        });

        _logger.LogInformation(
            "Saved {Count} hourly statistics records",
            entities.Count);

        scope.Complete();
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

        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            await db.UsageStatisticsDaily.AddRangeAsync(entities, ct);
            await db.SaveChangesAsync(ct);
            return true;
        });

        _logger.LogInformation(
            "Saved {Count} daily statistics records",
            entities.Count);

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastAggregatedHourlyPeriodAsync(CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var lastPeriod = await scope.ExecuteWithContextAsync(async db =>
            await db.UsageStatisticsHourly.MaxAsync(s => (DateTime?)s.Period, ct));

        _logger.LogDebug("Last aggregated hourly period: {Period}", lastPeriod);

        scope.Complete();
        return lastPeriod;
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastAggregatedDailyPeriodAsync(CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var lastPeriod = await scope.ExecuteWithContextAsync(async db =>
            await db.UsageStatisticsDaily.MaxAsync(s => (DateTime?)s.Period, ct));

        _logger.LogDebug("Last aggregated daily period: {Period}", lastPeriod);

        scope.Complete();
        return lastPeriod;
    }

    /// <inheritdoc />
    public async Task DeleteHourlyForPeriodAsync(DateTime period, CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var statsToDelete = await db.UsageStatisticsHourly
                .Where(s => s.Period == period)
                .ToListAsync(ct);

            if (statsToDelete.Count == 0)
            {
                _logger.LogDebug("No hourly statistics found to delete for period {Period}", period);
                return true;
            }

            db.UsageStatisticsHourly.RemoveRange(statsToDelete);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Deleted {Count} hourly statistics records for period {Period}",
                statsToDelete.Count,
                period);

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteDailyForPeriodAsync(DateTime period, CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var statsToDelete = await db.UsageStatisticsDaily
                .Where(s => s.Period == period)
                .ToListAsync(ct);

            if (statsToDelete.Count == 0)
            {
                _logger.LogDebug("No daily statistics found to delete for period {Period}", period);
                return true;
            }

            db.UsageStatisticsDaily.RemoveRange(statsToDelete);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Deleted {Count} daily statistics records for period {Period}",
                statsToDelete.Count,
                period);

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteHourlyOlderThanAsync(DateTime olderThan, CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var statsToDelete = await db.UsageStatisticsHourly
                .Where(s => s.Period < olderThan)
                .ToListAsync(ct);

            if (statsToDelete.Count == 0)
            {
                _logger.LogDebug("No hourly statistics found to delete older than {Date}", olderThan);
                return true;
            }

            db.UsageStatisticsHourly.RemoveRange(statsToDelete);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Deleted {Count} hourly statistics records older than {Date}",
                statsToDelete.Count,
                olderThan);

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteDailyOlderThanAsync(DateTime olderThan, CancellationToken ct = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var statsToDelete = await db.UsageStatisticsDaily
                .Where(s => s.Period < olderThan)
                .ToListAsync(ct);

            if (statsToDelete.Count == 0)
            {
                _logger.LogDebug("No daily statistics found to delete older than {Date}", olderThan);
                return true;
            }

            db.UsageStatisticsDaily.RemoveRange(statsToDelete);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Deleted {Count} daily statistics records older than {Date}",
                statsToDelete.Count,
                olderThan);

            return true;
        });

        scope.Complete();
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
