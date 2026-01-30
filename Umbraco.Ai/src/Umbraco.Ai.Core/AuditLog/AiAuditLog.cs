using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Represents a governance audit-log record for an AI operation, capturing execution details,
/// user context, and outcomes for audit-log and observability purposes.
/// </summary>
public sealed class AiAuditLog
{
    /// <summary>
    /// Gets the unique identifier for this audit-log in the local database.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Gets the start time of the AI operation.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets or sets the end time of the AI operation.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets the duration of the AI operation, if completed.
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// Gets or sets the execution status of this audit-log.
    /// </summary>
    public AiAuditLogStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error category, if the operation failed.
    /// </summary>
    public AiAuditLogErrorCategory? ErrorCategory { get; set; }

    /// <summary>
    /// Gets or sets the error message, if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the user ID who initiated the AI operation.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the user name who initiated the AI operation.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets the entity ID that this AI operation was performed on, if applicable.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets the entity type that this AI operation was performed on, if applicable.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets the AI capability being executed (Chat, Embedding, etc.).
    /// </summary>
    public AiCapability Capability { get; init; }

    /// <summary>
    /// Gets the profile ID used for this operation.
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Gets the profile alias used for this operation.
    /// </summary>
    public string ProfileAlias { get; init; } = string.Empty;

    /// <summary>
    /// Gets the version of the profile at time of execution.
    /// </summary>
    public int? ProfileVersion { get; init; }

    /// <summary>
    /// Gets the provider ID (e.g., "openai", "azure") used for this operation.
    /// </summary>
    public string ProviderId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the model ID used for this operation.
    /// </summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the feature type that initiated this operation (e.g., "prompt", "agent").
    /// Null for direct API calls or when not applicable.
    /// </summary>
    public string? FeatureType { get; init; }

    /// <summary>
    /// Gets the feature ID (prompt or agent ID) that initiated this operation.
    /// Null for direct API calls or when not applicable.
    /// </summary>
    public Guid? FeatureId { get; init; }

    /// <summary>
    /// Gets the version of the feature (prompt/agent) at time of execution.
    /// Null for direct API calls or when not applicable.
    /// </summary>
    public int? FeatureVersion { get; init; }

    /// <summary>
    /// Gets or sets the number of input tokens consumed.
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens generated.
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens consumed.
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Gets the prompt snapshot (if configured to persist).
    /// </summary>
    public string? PromptSnapshot { get; set; }

    /// <summary>
    /// Gets the response snapshot (if configured to persist).
    /// </summary>
    public string? ResponseSnapshot { get; set; }

    /// <summary>
    /// Gets the detail level configured for this audit-log.
    /// </summary>
    public AiAuditLogDetailLevel DetailLevel { get; init; }

    /// <summary>
    /// Gets the parent audit-log ID if this audit-log was triggered within another audit-log context (e.g., agent calling another agent).
    /// </summary>
    public Guid? ParentAuditLogId { get; internal set; }

    /// <summary>
    /// Gets extensible metadata for feature-specific context (e.g., AgentRunId, ThreadId, ConversationId).
    /// Stored as a JSON dictionary in the database.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
