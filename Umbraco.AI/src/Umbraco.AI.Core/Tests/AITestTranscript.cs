using System.Text.Json;

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
    /// Chat messages exchanged during execution.
    /// Structure: [{ role: "system|user|assistant", content: "..." }, ...]
    /// </summary>
    public JsonElement? Messages { get; set; }

    /// <summary>
    /// Tool calls made during execution.
    /// Structure: [{ toolName: "...", arguments: {...}, result: {...} }, ...]
    /// </summary>
    public JsonElement? ToolCalls { get; set; }

    /// <summary>
    /// Reasoning/thinking captured during execution.
    /// Structure depends on the test feature (e.g., agent thinking events).
    /// </summary>
    public JsonElement? Reasoning { get; set; }

    /// <summary>
    /// Timing information for execution steps.
    /// Structure: { totalMs: 1234, steps: [{ name: "...", durationMs: 123 }, ...] }
    /// </summary>
    public JsonElement? Timing { get; set; }

    /// <summary>
    /// The final output from the execution.
    /// Structure depends on the test feature.
    /// </summary>
    public required JsonElement FinalOutput { get; set; }
}
