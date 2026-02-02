namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Defines the granularity period for usage statistics aggregation.
/// </summary>
public enum AIUsagePeriod
{
    /// <summary>
    /// Hourly aggregation - data points represent one hour each.
    /// </summary>
    Hourly,

    /// <summary>
    /// Daily aggregation - data points represent one day each.
    /// </summary>
    Daily
}
