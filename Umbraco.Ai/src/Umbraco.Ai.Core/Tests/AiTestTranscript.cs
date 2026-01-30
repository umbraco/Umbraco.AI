namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Contains a structured trace of a test execution with messages, tool calls, reasoning, timing, and final output.
/// </summary>
public sealed class AiTestTranscript
{
    /// <summary>
    /// The unique identifier of the transcript.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The ID of the run this transcript belongs to.
    /// </summary>
    public Guid RunId { get; internal set; }

    /// <summary>
    /// The chat history/messages as JSON array.
    /// Structure: array of { role, content, ... } objects.
    /// </summary>
    public required string MessagesJson { get; set; }

    /// <summary>
    /// The tool calls that were made during execution as JSON array.
    /// Structure: array of tool call objects with { toolName, arguments, result, ... }.
    /// </summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>
    /// The reasoning/thinking steps as JSON array.
    /// Structure: array of reasoning objects with { step, content, ... }.
    /// </summary>
    public string? ReasoningJson { get; set; }

    /// <summary>
    /// The timing information for execution steps as JSON.
    /// Structure: { totalMs, steps: [{ name, durationMs }, ...] }.
    /// </summary>
    public string? TimingJson { get; set; }

    /// <summary>
    /// The final output from the test execution as JSON.
    /// Structure depends on test feature, but typically { output, usage, finishReason }.
    /// </summary>
    public required string FinalOutputJson { get; set; }
}
