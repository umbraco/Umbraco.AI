namespace Umbraco.Ai.Persistence.Analytics;

/// <summary>
/// EF Core entity for daily aggregated AI usage statistics.
/// Aggregated from hourly statistics, not from raw records.
/// </summary>
public class AiUsageStatisticsDailyEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this statistics record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the time period this statistics record represents (day start at midnight UTC).
    /// </summary>
    public DateTime Period { get; set; }

    /// <summary>
    /// Gets or sets the provider ID (e.g., "openai", "azure").
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile ID.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the AI capability (Chat, Embedding, etc.) stored as an integer enum value.
    /// </summary>
    public int Capability { get; set; }

    /// <summary>
    /// Gets or sets the user ID dimension, if included in aggregation.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the entity type dimension, if included in aggregation.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the feature type dimension, if included in aggregation.
    /// </summary>
    public string? FeatureType { get; set; }

    /// <summary>
    /// Gets or sets the total number of requests in this period/dimension.
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the total input tokens consumed.
    /// </summary>
    public long InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total output tokens generated.
    /// </summary>
    public long OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total tokens (input + output).
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the total duration in milliseconds for all requests.
    /// </summary>
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this statistics record was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
