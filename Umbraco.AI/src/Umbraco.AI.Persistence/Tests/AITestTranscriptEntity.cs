namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// EF Core entity representing a test execution transcript.
/// </summary>
internal class AITestTranscriptEntity
{
    /// <summary>
    /// Unique identifier for this transcript.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the run.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Chat messages as JSON array.
    /// </summary>
    public string? MessagesJson { get; set; }

    /// <summary>
    /// Tool calls as JSON array.
    /// </summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>
    /// Reasoning/thinking as JSON array.
    /// </summary>
    public string? ReasoningJson { get; set; }

    /// <summary>
    /// Timing information as JSON object.
    /// </summary>
    public string? TimingJson { get; set; }

    /// <summary>
    /// Final output as JSON object.
    /// </summary>
    public string FinalOutputJson { get; set; } = string.Empty;
}
