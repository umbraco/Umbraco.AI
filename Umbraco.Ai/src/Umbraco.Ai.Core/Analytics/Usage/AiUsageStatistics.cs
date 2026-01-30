using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Represents aggregated AI usage statistics for a specific time period and dimension combination.
/// </summary>
public sealed class AiUsageStatistics
{
    /// <summary>
    /// Gets the unique identifier for this statistics record.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the time period this statistics record represents (hour start or day start, UTC).
    /// </summary>
    public required DateTime Period { get; init; }

    /// <summary>
    /// Gets the provider ID (e.g., "openai", "azure").
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the model ID.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the profile ID.
    /// </summary>
    public required Guid ProfileId { get; init; }

    /// <summary>
    /// Gets the AI capability (Chat, Embedding, etc.).
    /// </summary>
    public required AiCapability Capability { get; init; }

    /// <summary>
    /// Gets the user ID dimension, if included in aggregation.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the user name for the UserId dimension.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets the profile alias for the ProfileId dimension.
    /// </summary>
    public string? ProfileAlias { get; init; }

    /// <summary>
    /// Gets the entity type dimension, if included in aggregation.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets the feature type dimension, if included in aggregation.
    /// </summary>
    public string? FeatureType { get; init; }

    /// <summary>
    /// Gets the total number of requests in this period/dimension.
    /// </summary>
    public required int RequestCount { get; init; }

    /// <summary>
    /// Gets the number of successful requests.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of failed requests.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the total input tokens consumed.
    /// </summary>
    public required long InputTokens { get; init; }

    /// <summary>
    /// Gets the total output tokens generated.
    /// </summary>
    public required long OutputTokens { get; init; }

    /// <summary>
    /// Gets the total tokens (input + output).
    /// </summary>
    public required long TotalTokens { get; init; }

    /// <summary>
    /// Gets the total duration in milliseconds for all requests.
    /// </summary>
    public required long TotalDurationMs { get; init; }

    /// <summary>
    /// Gets the average duration in milliseconds per request.
    /// </summary>
    public int AverageDurationMs => RequestCount > 0 ? (int)(TotalDurationMs / RequestCount) : 0;

    /// <summary>
    /// Gets the timestamp when this statistics record was created (UTC).
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
