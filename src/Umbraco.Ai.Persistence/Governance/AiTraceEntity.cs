namespace Umbraco.Ai.Persistence.Governance;

/// <summary>
/// EF Core entity for AI trace records.
/// </summary>
public class AiTraceEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this trace.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry trace ID (hex string).
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenTelemetry span ID (hex string).
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time of the operation.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the operation.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the error category.
    /// </summary>
    public int? ErrorCategory { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the operation.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user name who initiated the operation.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the entity ID associated with this operation.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the entity type associated with this operation.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile ID.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the profile alias.
    /// </summary>
    public string ProfileAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of input tokens.
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens.
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens.
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the prompt snapshot.
    /// </summary>
    public string? PromptSnapshot { get; set; }

    /// <summary>
    /// Gets or sets the response snapshot.
    /// </summary>
    public string? ResponseSnapshot { get; set; }

    /// <summary>
    /// Gets or sets the detail level.
    /// </summary>
    public int DetailLevel { get; set; }

    /// <summary>
    /// Gets or sets the execution spans associated with this trace.
    /// </summary>
    public ICollection<AiExecutionSpanEntity> Spans { get; set; } = new List<AiExecutionSpanEntity>();
}
