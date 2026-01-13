namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Represents the execution status of an AI audit.
/// </summary>
public enum AiAuditStatus
{
    /// <summary>
    /// The AI operation is currently executing.
    /// </summary>
    Running = 0,

    /// <summary>
    /// The AI operation completed successfully.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// The AI operation failed with an error.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// The AI operation was cancelled before completion.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// The AI operation completed with partial success.
    /// </summary>
    PartialSuccess = 4
}
