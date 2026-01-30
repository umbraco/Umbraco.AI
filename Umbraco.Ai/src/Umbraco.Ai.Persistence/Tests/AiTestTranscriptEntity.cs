namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// EF Core entity representing a test transcript.
/// </summary>
internal class AiTestTranscriptEntity
{
    /// <summary>
    /// Unique identifier for the transcript.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the run.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// The chat history/messages as JSON array.
    /// </summary>
    public string MessagesJson { get; set; } = string.Empty;

    /// <summary>
    /// The tool calls as JSON array.
    /// </summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>
    /// The reasoning steps as JSON array.
    /// </summary>
    public string? ReasoningJson { get; set; }

    /// <summary>
    /// The timing information as JSON.
    /// </summary>
    public string? TimingJson { get; set; }

    /// <summary>
    /// The final output as JSON.
    /// </summary>
    public string FinalOutputJson { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to run.
    /// </summary>
    public AiTestRunEntity Run { get; set; } = null!;
}
