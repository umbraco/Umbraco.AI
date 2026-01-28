using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Represents a raw AI usage record captured from a single AI operation.
/// These records are ephemeral - they're aggregated into statistics and then deleted.
/// </summary>
public sealed class AiUsageRecord
{
    /// <summary>
    /// Gets the unique identifier for this usage record.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when the AI operation completed (UTC).
    /// Used for bucketing into hourly/daily aggregations.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the AI capability used (Chat, Embedding, etc.).
    /// </summary>
    public required AiCapability Capability { get; init; }

    /// <summary>
    /// Gets the user ID who initiated the operation, if available.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the user name who initiated the operation, if available.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets the profile ID used for this operation.
    /// </summary>
    public required Guid ProfileId { get; init; }

    /// <summary>
    /// Gets the profile alias.
    /// </summary>
    public required string ProfileAlias { get; init; }

    /// <summary>
    /// Gets the provider ID (e.g., "openai", "azure").
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the model ID used for this operation.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the feature type that initiated this operation (e.g., "prompt", "agent"), if applicable.
    /// </summary>
    public string? FeatureType { get; init; }

    /// <summary>
    /// Gets the feature ID (prompt or agent ID) that initiated this operation, if applicable.
    /// </summary>
    public Guid? FeatureId { get; init; }

    /// <summary>
    /// Gets the entity ID this operation was performed on, if applicable.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets the entity type (e.g., "content", "media"), if applicable.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets the number of input tokens consumed.
    /// </summary>
    public required long InputTokens { get; init; }

    /// <summary>
    /// Gets the number of output tokens generated.
    /// </summary>
    public required long OutputTokens { get; init; }

    /// <summary>
    /// Gets the total number of tokens (input + output).
    /// </summary>
    public required long TotalTokens { get; init; }

    /// <summary>
    /// Gets the duration of the operation in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Gets the status of the operation (Succeeded or Failed).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed, if available.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when this record was created (UTC).
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
