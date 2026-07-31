namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Helpers for summing token counts that a provider may or may not report.
/// </summary>
internal static class AIUsageTokenAggregation
{
    /// <summary>
    /// Sums an optional count across a group, yielding <c>null</c> when nothing in the group reported one.
    /// </summary>
    /// <remarks>
    /// The nullable <c>Sum</c> overloads fold an all-null sequence to <c>0</c>, which would report "no
    /// provider tracks this" as "this was zero" — indistinguishable once persisted, and misleading in a
    /// dashboard. Kept null so a group only claims a total when at least one record contributed to it.
    /// </remarks>
    internal static long? SumOrNull<T>(IEnumerable<T> source, Func<T, long?> selector)
    {
        long total = 0;
        var reported = false;

        foreach (var item in source)
        {
            if (selector(item) is { } value)
            {
                total += value;
                reported = true;
            }
        }

        return reported ? total : null;
    }
}
