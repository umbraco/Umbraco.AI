namespace Umbraco.Ai.Persistence.Analytics.Usage;

/// <summary>
/// EF Core entity for raw AI usage records.
/// These records are ephemeral - aggregated into statistics and then deleted.
/// </summary>
internal class AiUsageRecordEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this usage record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the AI operation completed (UTC).
    /// Used for bucketing into hourly/daily aggregations.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the AI capability (Chat, Embedding, etc.) stored as an integer enum value.
    /// </summary>
    public int Capability { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the operation.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name who initiated the operation.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the profile ID used for this operation.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the profile alias.
    /// </summary>
    public string ProfileAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider ID (e.g., "openai", "azure").
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID used for this operation.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feature type that initiated this operation (e.g., "prompt", "agent").
    /// </summary>
    public string? FeatureType { get; set; }

    /// <summary>
    /// Gets or sets the feature ID (prompt or agent ID) that initiated this operation.
    /// </summary>
    public Guid? FeatureId { get; set; }

    /// <summary>
    /// Gets or sets the entity ID this operation was performed on.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the entity type (e.g., "content", "media").
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the number of input tokens consumed.
    /// </summary>
    public long InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens generated.
    /// </summary>
    public long OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens (input + output).
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the duration of the operation in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the status of the operation (Succeeded or Failed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this record was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
