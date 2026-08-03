using Microsoft.Extensions.AI;
using Umbraco.AI.Core;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for reading provider-reported counts out of <see cref="UsageDetails"/>.
/// </summary>
public static class UsageDetailsExtensions
{
    /// <summary>
    /// Gets the portion of the input tokens the provider reported as served from its prompt cache, or
    /// <c>null</c> when the provider reported nothing.
    /// </summary>
    /// <param name="usage">The usage details, which may be null.</param>
    /// <remarks>
    /// Read from <see cref="UsageDetails.AdditionalCounts"/> under
    /// <see cref="Constants.UsageCounts.CachedInputTokens"/>, which is how a provider hands core a count
    /// Microsoft.Extensions.AI has no property for. Null rather than zero when absent, so "not reported"
    /// stays distinguishable from "nothing was cached" — the two look identical once summed otherwise.
    /// </remarks>
    public static long? GetCachedInputTokenCount(this UsageDetails? usage)
    {
        if (usage?.AdditionalCounts is null
            || !usage.AdditionalCounts.TryGetValue(Constants.UsageCounts.CachedInputTokens, out var value))
        {
            return null;
        }

        // Negative would be a provider bug; treated as unreported rather than persisted, since a negative
        // count would corrupt every aggregate it lands in.
        return value < 0 ? null : value;
    }
}
