namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// EF Core entity representing a test run.
/// </summary>
internal class AiTestRunEntity
{
    /// <summary>
    /// Unique identifier for the run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the test.
    /// </summary>
    public Guid TestId { get; set; }

    /// <summary>
    /// The version of the test that was executed.
    /// </summary>
    public int TestVersion { get; set; }

    /// <summary>
    /// The run number (1 to N).
    /// </summary>
    public int RunNumber { get; set; }

    /// <summary>
    /// The profile ID used for this run.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The context IDs used for this run (JSON array).
    /// </summary>
    public string? ContextIdsJson { get; set; }

    /// <summary>
    /// When the run was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// The key (GUID) of the user who executed this run.
    /// </summary>
    public Guid? ExecutedByUserId { get; set; }

    /// <summary>
    /// The duration of the run in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// The status of the run (0=Passed, 1=Failed, 2=Error).
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Error message if status is Error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The outcome type (0=Text, 1=Json, 2=Events).
    /// </summary>
    public int OutcomeType { get; set; }

    /// <summary>
    /// The outcome value as string or JSON.
    /// </summary>
    public string OutcomeValue { get; set; } = string.Empty;

    /// <summary>
    /// The finish reason from the AI model.
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

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Optional batch ID if part of batch execution.
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// Navigation property to test.
    /// </summary>
    public AiTestEntity Test { get; set; } = null!;

    /// <summary>
    /// Navigation property to transcript.
    /// </summary>
    public AiTestTranscriptEntity? Transcript { get; set; }

    /// <summary>
    /// Navigation property to grader results.
    /// </summary>
    public ICollection<AiTestGraderResultEntity> GraderResults { get; set; } = new List<AiTestGraderResultEntity>();
}
