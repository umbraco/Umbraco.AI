namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Represents the execution status of an individual span within an AI trace.
/// </summary>
public enum AiExecutionSpanStatus
{
    /// <summary>
    /// The span is currently executing.
    /// </summary>
    Running = 0,

    /// <summary>
    /// The span completed successfully.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// The span failed with an error.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// The span was skipped.
    /// </summary>
    Skipped = 3
}
