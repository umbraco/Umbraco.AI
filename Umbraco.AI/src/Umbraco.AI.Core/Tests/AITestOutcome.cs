namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Represents the final state/output of a test execution.
/// This is what the model *actually* produced, which graders evaluate against expectations.
/// </summary>
public sealed class AITestOutcome
{
    /// <summary>
    /// The type of output produced.
    /// </summary>
    public AITestOutputType OutputType { get; set; } = AITestOutputType.Text;

    /// <summary>
    /// The output value as a string.
    /// For Text: the raw text response.
    /// For JSON: the JSON string.
    /// For Events: serialized event stream.
    /// </summary>
    public string? OutputValue { get; set; }

    /// <summary>
    /// The finish reason from the model.
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Token usage statistics (JSON object).
    /// Structure: { inputTokens: 123, outputTokens: 456, totalTokens: 579 }
    /// </summary>
    public string? TokenUsageJson { get; set; }
}

/// <summary>
/// Type of output produced by a test execution.
/// </summary>
public enum AITestOutputType
{
    /// <summary>
    /// Plain text response.
    /// </summary>
    Text = 0,

    /// <summary>
    /// JSON response.
    /// </summary>
    JSON = 1,

    /// <summary>
    /// Event stream (for agent executions).
    /// </summary>
    Events = 2
}
