using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Usage statistics summed over every row that shares a period or a dimension key, so a dashboard reads a
/// few grouped rows instead of every combination of provider, model, profile, user, capability and feature.
/// </summary>
/// <remarks>
/// Grouped by period when no dimension is asked for: <see cref="Period"/> is then that period and
/// <see cref="Key"/> is null. Grouped by a dimension: <see cref="Period"/> is the latest period the group
/// was seen in, so the latest of several names for one key can be told.
/// </remarks>
internal sealed class AIUsageStatisticsTotal
{
    /// <summary>The period, or the latest period of a dimension group (UTC).</summary>
    public required DateTime Period { get; init; }

    /// <summary>The provider ID, model ID, profile ID or user ID when grouped by a dimension.</summary>
    public string? Key { get; init; }

    /// <summary>The profile alias or user name when grouped by profile or user.</summary>
    public string? Name { get; init; }

    public required int RequestCount { get; init; }

    public int NestedRequestCount { get; init; }

    public required int SuccessCount { get; init; }

    public required int FailureCount { get; init; }

    public required long InputTokens { get; init; }

    public long? CachedInputTokens { get; init; }

    public required long OutputTokens { get; init; }

    public required long TotalTokens { get; init; }

    public required long TotalDurationMs { get; init; }

    /// <summary>
    /// Groups raw usage records the way the repository groups statistics rows, for the records that are
    /// not aggregated yet.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <param name="dimension">The dimension to group by, or null to group by hour.</param>
    public static List<AIUsageStatisticsTotal> FromRecords(
        IEnumerable<AIUsageRecord> records,
        AIUsageBreakdownDimension? dimension)
    {
        Func<AIUsageRecord, string?> key = dimension switch
        {
            AIUsageBreakdownDimension.Provider => r => r.ProviderId,
            AIUsageBreakdownDimension.Model => r => r.ModelId,
            AIUsageBreakdownDimension.Profile => r => r.ProfileId.ToString(),
            AIUsageBreakdownDimension.User => r => r.UserId,
            _ => _ => null,
        };

        Func<AIUsageRecord, string?>? name = dimension switch
        {
            AIUsageBreakdownDimension.Profile => r => r.ProfileAlias,
            AIUsageBreakdownDimension.User => r => r.UserName,
            _ => null,
        };

        return records
            .OrderBy(r => r.Timestamp)
            .GroupBy(r => (Hour: dimension is null ? HourStart(r.Timestamp) : default, Key: key(r)))
            .Select(g => new AIUsageStatisticsTotal
            {
                Period = dimension is null ? g.Key.Hour : HourStart(g.Max(r => r.Timestamp)),
                Key = g.Key.Key,
                Name = name is null ? null : AIUsageStatisticsGrouping.Latest(g, name),
                RequestCount = g.Count(),
                NestedRequestCount = g.Count(r => r.IsNested),
                SuccessCount = g.Count(r => r.Status == AIUsageRecordStatus.Succeeded),
                FailureCount = g.Count(r => r.Status is AIUsageRecordStatus.Failed or AIUsageRecordStatus.Blocked),
                InputTokens = g.Sum(r => r.InputTokens),
                CachedInputTokens = AIUsageTokenAggregation.SumOrNull(g, r => r.CachedInputTokens),
                OutputTokens = g.Sum(r => r.OutputTokens),
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalDurationMs = g.Sum(r => r.DurationMs),
            })
            .ToList();
    }

    private static DateTime HourStart(DateTime timestamp)
        => new(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc);
}
