namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Configuration options for AI governance and tracing.
/// </summary>
public class AiAuditOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether governance tracing is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days to retain audit records before cleanup.
    /// Default is 14 days.
    /// </summary>
    public int RetentionDays { get; set; } = 14;

    /// <summary>
    /// Gets or sets the detail level for capturing audit information.
    /// Default is FailuresOnly.
    /// </summary>
    public AiAuditDetailLevel DetailLevel { get; set; } = AiAuditDetailLevel.FailuresOnly;

    /// <summary>
    /// Gets or sets a value indicating whether to persist prompt snapshots.
    /// Default is false for privacy reasons.
    /// </summary>
    public bool PersistPrompts { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to persist response snapshots.
    /// Default is false for privacy reasons.
    /// </summary>
    public bool PersistResponses { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to persist detailed failure information.
    /// Default is true.
    /// </summary>
    public bool PersistFailureDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the sampling rate for detailed traces (0.0 to 1.0).
    /// Only applies when DetailLevel is set to Sampled.
    /// Default is 0.1 (10%).
    /// </summary>
    public double SamplingRate { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the list of regex patterns for redacting sensitive data.
    /// </summary>
    public List<string> RedactionPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of allowed model configurations.
    /// Empty list means all models are allowed.
    /// </summary>
    public List<AllowedModelConfig> AllowedModels { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum tokens allowed per request.
    /// Null means no limit.
    /// </summary>
    public int? MaxTokensPerRequest { get; set; }
}

/// <summary>
/// Configuration for allowed AI models.
/// </summary>
public class AllowedModelConfig
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID pattern (supports wildcards).
    /// </summary>
    public string ModelIdPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum tokens allowed for this model.
    /// Null means no specific limit for this model.
    /// </summary>
    public int? MaxTokens { get; set; }
}
