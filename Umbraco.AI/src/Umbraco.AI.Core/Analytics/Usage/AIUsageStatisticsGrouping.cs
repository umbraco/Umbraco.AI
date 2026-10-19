using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// The one rule for turning usage records, or finer statistics, into statistics rows. Shared by the
/// aggregation jobs and the dashboard's live view, so the same usage gives the same rows whether it has
/// been aggregated yet or not.
/// </summary>
/// <remarks>
/// Rows are grouped by IDs only. Profile alias and user name are display names that can change over time,
/// so each row takes the latest non-empty one rather than splitting on them.
/// </remarks>
internal static class AIUsageStatisticsGrouping
{
    /// <summary>
    /// Groups raw usage records into statistics, one row per period and dimension.
    /// </summary>
    public static List<AIUsageStatistics> GroupRecords(
        IEnumerable<AIUsageRecord> records,
        Func<DateTime, DateTime> periodStart)
        => records
            .OrderBy(r => r.Timestamp)
            .GroupBy(r => new Key(
                periodStart(r.Timestamp),
                r.ProviderId,
                r.ModelId,
                r.ProfileId,
                r.Capability,
                r.UserId,
                r.EntityType,
                r.FeatureType))
            .Select(g => new AIUsageStatistics
            {
                Id = Guid.NewGuid(),
                Period = g.Key.Period,
                ProviderId = g.Key.ProviderId,
                ModelId = g.Key.ModelId,
                ProfileId = g.Key.ProfileId,
                ProfileAlias = Latest(g, r => r.ProfileAlias),
                Capability = g.Key.Capability,
                UserId = g.Key.UserId,
                UserName = Latest(g, r => r.UserName),
                EntityType = g.Key.EntityType,
                FeatureType = g.Key.FeatureType,
                RequestCount = g.Count(),
                NestedRequestCount = g.Count(r => r.IsNested),
                SuccessCount = g.Count(r => r.Status == AIUsageRecordStatus.Succeeded),
                FailureCount = g.Count(r => r.Status is AIUsageRecordStatus.Failed or AIUsageRecordStatus.Blocked),
                InputTokens = g.Sum(r => r.InputTokens),
                CachedInputTokens = AIUsageTokenAggregation.SumOrNull(g, r => r.CachedInputTokens),
                OutputTokens = g.Sum(r => r.OutputTokens),
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalDurationMs = g.Sum(r => r.DurationMs),
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

    /// <summary>
    /// Re-buckets statistics into coarser periods, e.g. hourly rows into days.
    /// </summary>
    public static List<AIUsageStatistics> GroupStatistics(
        IEnumerable<AIUsageStatistics> statistics,
        Func<DateTime, DateTime> periodStart)
        => statistics
            .OrderBy(s => s.Period)
            .GroupBy(s => new Key(
                periodStart(s.Period),
                s.ProviderId,
                s.ModelId,
                s.ProfileId,
                s.Capability,
                s.UserId,
                s.EntityType,
                s.FeatureType))
            .Select(g => new AIUsageStatistics
            {
                Id = Guid.NewGuid(),
                Period = g.Key.Period,
                ProviderId = g.Key.ProviderId,
                ModelId = g.Key.ModelId,
                ProfileId = g.Key.ProfileId,
                ProfileAlias = Latest(g, s => s.ProfileAlias),
                Capability = g.Key.Capability,
                UserId = g.Key.UserId,
                UserName = Latest(g, s => s.UserName),
                EntityType = g.Key.EntityType,
                FeatureType = g.Key.FeatureType,
                RequestCount = g.Sum(s => s.RequestCount),
                NestedRequestCount = g.Sum(s => s.NestedRequestCount),
                SuccessCount = g.Sum(s => s.SuccessCount),
                FailureCount = g.Sum(s => s.FailureCount),
                InputTokens = g.Sum(s => s.InputTokens),
                CachedInputTokens = AIUsageTokenAggregation.SumOrNull(g, s => s.CachedInputTokens),
                OutputTokens = g.Sum(s => s.OutputTokens),
                TotalTokens = g.Sum(s => s.TotalTokens),
                TotalDurationMs = g.Sum(s => s.TotalDurationMs),
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

    /// <summary>
    /// The latest non-empty display name in a group already ordered oldest first.
    /// </summary>
    internal static string? Latest<T>(IEnumerable<T> ordered, Func<T, string?> name)
        => ordered.Select(name).LastOrDefault(n => !string.IsNullOrEmpty(n));

    private sealed record Key(
        DateTime Period,
        string ProviderId,
        string ModelId,
        Guid ProfileId,
        AICapability Capability,
        string? UserId,
        string? EntityType,
        string? FeatureType);
}
