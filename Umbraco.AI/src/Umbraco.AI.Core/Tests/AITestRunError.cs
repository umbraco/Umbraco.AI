namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Error information from a failed test run execution.
/// </summary>
public sealed class AITestRunError
{
    /// <summary>
    /// The error message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// The stack trace of the error, if available.
    /// </summary>
    public string? StackTrace { get; set; }
}
