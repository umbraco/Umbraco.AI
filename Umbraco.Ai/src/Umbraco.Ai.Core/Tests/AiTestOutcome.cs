namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Represents the final outcome of a test execution - what the model actually produced.
/// </summary>
public sealed class AiTestOutcome
{
    /// <summary>
    /// The type of output produced.
    /// </summary>
    public AiTestOutcomeType OutputType { get; set; }

    /// <summary>
    /// The output value as a string or JSON.
    /// </summary>
    public required string OutputValue { get; set; }

    /// <summary>
    /// The finish reason from the AI model (if applicable).
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// The number of input tokens used.
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// The number of output tokens used.
    /// </summary>
    public int? OutputTokens { get; set; }
}

/// <summary>
/// Types of test outcomes.
/// </summary>
public enum AiTestOutcomeType
{
    /// <summary>
    /// Plain text output.
    /// </summary>
    Text = 0,

    /// <summary>
    /// JSON output.
    /// </summary>
    Json = 1,

    /// <summary>
    /// Stream of events (for agents).
    /// </summary>
    Events = 2
}
