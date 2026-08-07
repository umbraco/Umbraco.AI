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
    /// Read from <see cref="UsageDetails.CachedInputTokenCount"/>, which Microsoft.Extensions.AI defines for
    /// exactly this and which the OpenAI and Anthropic adapters both populate, falling back to
    /// <see cref="UsageDetails.AdditionalCounts"/> under
    /// <see cref="Constants.UsageCounts.CachedInputTokens"/> for a provider whose SDK leaves the property
    /// unset and reports the count there instead.
    /// <para>
    /// Null rather than zero when neither is present, so "not reported" stays distinguishable from "nothing
    /// was cached" — the two look identical once summed otherwise.
    /// </para>
    /// </remarks>
    public static long? GetCachedInputTokenCount(this UsageDetails? usage)
    {
        if (usage is null)
        {
            return null;
        }

        if (usage.CachedInputTokenCount is { } reported)
        {
            return Sanitize(reported);
        }

        if (usage.AdditionalCounts?.TryGetValue(Constants.UsageCounts.CachedInputTokens, out var value) == true)
        {
            return Sanitize(value);
        }

        return null;
    }

    /// <summary>
    /// Drops a negative count, which would be a provider bug.
    /// </summary>
    /// <remarks>
    /// Treated as unreported rather than persisted, since a negative count would corrupt every aggregate it
    /// lands in.
    /// </remarks>
    private static long? Sanitize(long value) => value < 0 ? null : value;
}
