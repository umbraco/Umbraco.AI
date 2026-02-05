namespace Umbraco.AI.Persistence.AuditLog;

/// <summary>
/// EF Core entity for AI audit-log records.
/// </summary>
internal class AIAuditLogEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit-log.
    /// </summary>
    public Guid Id { get; set; }

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
    public string? UserId { get; set; }

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
    /// Gets or sets the AI capability (Chat, Embedding, etc.) stored as an integer enum value.
    /// </summary>
    public int Capability { get; set; }

    /// <summary>
    /// Gets or sets the profile ID.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the profile alias.
    /// </summary>
    public string ProfileAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile version at time of execution.
    /// </summary>
    public int? ProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID.
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
    /// Gets or sets the feature version at time of execution.
    /// </summary>
    public int? FeatureVersion { get; set; }

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
    /// Gets or sets the parent audit-log ID if this audit-log was triggered within another audit-log context.
    /// </summary>
    public Guid? ParentAuditLogId { get; set; }

    /// <summary>
    /// Gets or sets extensible metadata stored as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}
