namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Service for querying aggregated AI usage statistics.
/// Public interface - exposed to Management API controllers.
/// </summary>
public interface IAiUsageAnalyticsService
{
    /// <summary>
    /// Gets a summary of usage statistics within a time range.
    /// Combines aggregated stats with live data from the current hour for freshness.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="requestedGranularity">Optional granularity override. If null, auto-selects based on date range.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Summary statistics for the period.</returns>
    Task<AiUsageSummary> GetSummaryAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        AiUsageFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a time series of usage statistics within a time range.
    /// Each point represents one hour or one day depending on granularity.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="requestedGranularity">Optional granularity override. If null, auto-selects based on date range.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Time series data points.</returns>
    Task<IEnumerable<AiUsageTimeSeriesPoint>> GetTimeSeriesAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        AiUsageFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a breakdown of usage statistics by provider.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="requestedGranularity">Optional granularity override. If null, auto-selects based on date range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Usage breakdown by provider.</returns>
    Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByProviderAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a breakdown of usage statistics by model.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="requestedGranularity">Optional granularity override. If null, auto-selects based on date range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Usage breakdown by model.</returns>
    Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByModelAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a breakdown of usage statistics by profile.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="requestedGranularity">Optional granularity override. If null, auto-selects based on date range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Usage breakdown by profile.</returns>
    Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByProfileAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a breakdown of usage statistics by user.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="requestedGranularity">Optional granularity override. If null, auto-selects based on date range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Usage breakdown by user.</returns>
    Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByUserAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default);
}

/// <summary>
/// Summary of usage statistics for a time period.
/// </summary>
public sealed class AiUsageSummary
{
    /// <summary>
    /// Gets the total number of AI requests.
    /// </summary>
    public required int TotalRequests { get; init; }

    /// <summary>
    /// Gets the total number of input tokens consumed.
    /// </summary>
    public required long InputTokens { get; init; }

    /// <summary>
    /// Gets the total number of output tokens generated.
    /// </summary>
    public required long OutputTokens { get; init; }

    /// <summary>
    /// Gets the total number of tokens (input + output).
    /// </summary>
    public required long TotalTokens { get; init; }

    /// <summary>
    /// Gets the number of successful requests.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of failed requests.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the success rate as a decimal (0.0 to 1.0).
    /// </summary>
    public required double SuccessRate { get; init; }

    /// <summary>
    /// Gets the average request duration in milliseconds.
    /// </summary>
    public required int AverageDurationMs { get; init; }
}

/// <summary>
/// A single point in a usage time series.
/// </summary>
public sealed class AiUsageTimeSeriesPoint
{
    /// <summary>
    /// Gets the timestamp for this data point (hour or day start).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the number of requests in this time period.
    /// </summary>
    public required int RequestCount { get; init; }

    /// <summary>
    /// Gets the total tokens consumed in this time period.
    /// </summary>
    public required long TotalTokens { get; init; }

    /// <summary>
    /// Gets the input tokens consumed in this time period.
    /// </summary>
    public required long InputTokens { get; init; }

    /// <summary>
    /// Gets the output tokens consumed in this time period.
    /// </summary>
    public required long OutputTokens { get; init; }

    /// <summary>
    /// Gets the number of successful requests.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of failed requests.
    /// </summary>
    public required int FailureCount { get; init; }
}

/// <summary>
/// Usage breakdown by a specific dimension (provider, model, profile, etc.).
/// </summary>
public sealed class AiUsageBreakdownItem
{
    /// <summary>
    /// Gets the dimension value (e.g., provider name, model ID, profile alias).
    /// </summary>
    public required string Dimension { get; init; }

    /// <summary>
    /// Gets the friendly name for this dimension (e.g., profile alias, user name).
    /// Null for dimensions that don't have friendly names (provider, model).
    /// </summary>
    public string? DimensionName { get; init; }

    /// <summary>
    /// Gets the number of requests for this dimension.
    /// </summary>
    public required int RequestCount { get; init; }

    /// <summary>
    /// Gets the total tokens consumed for this dimension.
    /// </summary>
    public required long TotalTokens { get; init; }

    /// <summary>
    /// Gets the percentage of total requests represented by this dimension.
    /// </summary>
    public required double Percentage { get; init; }
}
