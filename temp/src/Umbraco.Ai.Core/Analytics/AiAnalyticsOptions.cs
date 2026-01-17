namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Configuration options for AI usage analytics.
/// </summary>
public sealed class AiAnalyticsOptions
{
    /// <summary>
    /// Gets or sets whether usage analytics is enabled.
    /// When disabled, no usage records are created and no aggregation jobs run.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the retention period (in days) for hourly aggregated statistics.
    /// Default is 30 days. Valid range: 30-90 days.
    /// </summary>
    public int UsageHourlyRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the retention period (in days) for daily aggregated statistics.
    /// Default is 365 days (one year).
    /// </summary>
    public int UsageDailyRetentionDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets whether to include user ID as a dimension in aggregations.
    /// When true, statistics are broken down by user (privacy consideration).
    /// Default is true.
    /// </summary>
    public bool IncludeUsageUserDimension { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include entity type as a dimension in aggregations.
    /// When true, statistics are broken down by entity type (e.g., "content", "media").
    /// Default is true.
    /// </summary>
    public bool IncludeUsageEntityTypeDimension { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include feature type as a dimension in aggregations.
    /// When true, statistics are broken down by feature type (e.g., "prompt", "agent").
    /// Default is true.
    /// </summary>
    public bool IncludeUsageFeatureTypeDimension { get; set; } = true;
}
