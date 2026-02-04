namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Structured trace of a test execution.
/// Captures messages, tool calls, reasoning, timing, and final output for analysis and debugging.
/// </summary>
public sealed class AITestTranscript
{
    /// <summary>
    /// Unique identifier for this transcript.
    /// </summary>
    public Guid Id { get; internal set; } = Guid.NewGuid();

    /// <summary>
    /// The run ID this transcript belongs to.
    /// </summary>
    public required Guid RunId { get; set; }

    /// <summary>
    /// Chat messages exchanged during execution (JSON array).
    /// Structure: [{ role: "system|user|assistant", content: "..." }, ...]
    /// </summary>
    public string? MessagesJson { get; set; }

    /// <summary>
    /// Tool calls made during execution (JSON array).
    /// Structure: [{ toolName: "...", arguments: {...}, result: {...} }, ...]
    /// </summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>
    /// Reasoning/thinking captured during execution (JSON array).
    /// Structure depends on the test feature (e.g., agent thinking events).
    /// </summary>
    public string? ReasoningJson { get; set; }

    /// <summary>
    /// Timing information for execution steps (JSON object).
    /// Structure: { totalMs: 1234, steps: [{ name: "...", durationMs: 123 }, ...] }
    /// </summary>
    public string? TimingJson { get; set; }

    /// <summary>
    /// The final output from the execution (JSON object).
    /// Structure depends on the test feature.
    /// </summary>
    public required string FinalOutputJson { get; set; }
}
