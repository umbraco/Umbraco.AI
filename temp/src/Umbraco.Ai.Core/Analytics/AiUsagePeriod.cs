namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Defines the granularity period for usage statistics aggregation.
/// </summary>
public enum AiUsagePeriod
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
