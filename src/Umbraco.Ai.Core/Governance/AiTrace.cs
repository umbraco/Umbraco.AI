namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Represents a governance trace record for an AI operation, capturing execution details,
/// user context, and outcomes for audit and observability purposes.
/// </summary>
public sealed class AiTrace
{
    /// <summary>
    /// Gets the unique identifier for this trace in the local database.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Gets the OpenTelemetry trace ID (hex string) for correlation with external observability systems.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the OpenTelemetry span ID (hex string) for the root span of this trace.
    /// </summary>
    public string SpanId { get; init; } = string.Empty;

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
    /// Gets or sets the execution status of this trace.
    /// </summary>
    public AiTraceStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error category, if the operation failed.
    /// </summary>
    public AiTraceErrorCategory? ErrorCategory { get; set; }

    /// <summary>
    /// Gets or sets the error message, if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the user ID who initiated the AI operation.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

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
    /// Gets the type of operation performed (e.g., "chat", "embedding", "content-generation").
    /// </summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the profile ID used for this operation.
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Gets the profile alias used for this operation.
    /// </summary>
    public string ProfileAlias { get; init; } = string.Empty;

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
    /// Gets the detail level configured for this trace.
    /// </summary>
    public AiTraceDetailLevel DetailLevel { get; init; }

    /// <summary>
    /// Gets the execution spans associated with this trace.
    /// </summary>
    public IReadOnlyList<AiExecutionSpan> Spans { get; internal set; } = Array.Empty<AiExecutionSpan>();
}
