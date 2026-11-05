using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Analytics.Usage;

namespace Umbraco.AI.Persistence.Analytics.Usage;

/// <summary>
/// The columns the hourly and daily statistics tables share, so one query sums either.
/// </summary>
internal interface IAIUsageStatisticsEntity
{
    DateTime Period { get; }

    string ProviderId { get; }

    string ModelId { get; }

    Guid ProfileId { get; }

    string? UserId { get; }

    string? UserName { get; }

    string? ProfileAlias { get; }

    int RequestCount { get; }

    int NestedRequestCount { get; }

    int SuccessCount { get; }

    int FailureCount { get; }

    long InputTokens { get; }

    long? CachedInputTokens { get; }

    long OutputTokens { get; }

    long TotalTokens { get; }

    long TotalDurationMs { get; }
}

/// <summary>
/// Sums statistics rows in the database (plain GROUP BY on real columns and SUM, so it runs on SQL Server and
/// SQLite alike).
/// </summary>
internal static class AIUsageStatisticsSummer
{
    public static async Task<IReadOnlyList<AIUsageStatisticsTotal>> SumAsync<TEntity>(
        IQueryable<TEntity> source,
        AIUsageBreakdownDimension? dimension,
        CancellationToken ct)
        where TEntity : class, IAIUsageStatisticsEntity
    {
        var rows = source.AsNoTracking();

        switch (dimension)
        {
            case AIUsageBreakdownDimension.Provider:
                return (await GroupAsync(rows, s => s.ProviderId, ct)).Select(r => ToTotal(r, r.Key, null)).ToList();
            case AIUsageBreakdownDimension.Model:
                return (await GroupAsync(rows, s => s.ModelId, ct)).Select(r => ToTotal(r, r.Key, null)).ToList();
            case AIUsageBreakdownDimension.Profile:
                return (await GroupAsync(rows, s => new { s.ProfileId, s.ProfileAlias }, ct))
                    .Select(r => ToTotal(r, r.Key.ProfileId.ToString(), r.Key.ProfileAlias)).ToList();
            case AIUsageBreakdownDimension.User:
                return (await GroupAsync(rows, s => new { s.UserId, s.UserName }, ct))
                    .Select(r => ToTotal(r, r.Key.UserId, r.Key.UserName)).ToList();
            default:
                return (await GroupAsync(rows, s => s.Period, ct)).Select(r => ToTotal(r, null, null)).ToList();
        }
    }

    private static async Task<List<Sums<TKey>>> GroupAsync<TEntity, TKey>(
        IQueryable<TEntity> rows,
        Expression<Func<TEntity, TKey>> key,
        CancellationToken ct)
        where TEntity : class, IAIUsageStatisticsEntity
        => await rows
            .GroupBy(key)
            .Select(g => new Sums<TKey>
            {
                Key = g.Key,
                Period = g.Max(s => s.Period),
                RequestCount = g.Sum(s => s.RequestCount),
                NestedRequestCount = g.Sum(s => s.NestedRequestCount),
                SuccessCount = g.Sum(s => s.SuccessCount),
                FailureCount = g.Sum(s => s.FailureCount),
                InputTokens = g.Sum(s => s.InputTokens),
                // EF folds a SUM of nothing but NULLs to 0, so count the rows that reported a value to tell "none" from zero.
                CachedRows = g.Count(s => s.CachedInputTokens != null),
                CachedInputTokens = g.Sum(s => s.CachedInputTokens),
                OutputTokens = g.Sum(s => s.OutputTokens),
                TotalTokens = g.Sum(s => s.TotalTokens),
                TotalDurationMs = g.Sum(s => s.TotalDurationMs),
            })
            .ToListAsync(ct);

    private static AIUsageStatisticsTotal ToTotal<TKey>(Sums<TKey> sums, string? key, string? name) => new()
    {
        Period = DateTime.SpecifyKind(sums.Period, DateTimeKind.Utc),
        Key = key,
        Name = name,
        RequestCount = sums.RequestCount,
        NestedRequestCount = sums.NestedRequestCount,
        SuccessCount = sums.SuccessCount,
        FailureCount = sums.FailureCount,
        InputTokens = sums.InputTokens,
        CachedInputTokens = sums.CachedRows > 0 ? sums.CachedInputTokens : null,
        OutputTokens = sums.OutputTokens,
        TotalTokens = sums.TotalTokens,
        TotalDurationMs = sums.TotalDurationMs,
    };

    private sealed class Sums<TKey>
    {
        public TKey Key { get; set; } = default!;

        public DateTime Period { get; set; }

        public int RequestCount { get; set; }

        public int NestedRequestCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public long InputTokens { get; set; }

        public int CachedRows { get; set; }

        public long? CachedInputTokens { get; set; }

        public long OutputTokens { get; set; }

        public long TotalTokens { get; set; }

        public long TotalDurationMs { get; set; }
    }
}
