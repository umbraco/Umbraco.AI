namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// EF Core entity representing a test run execution.
/// </summary>
internal class AITestRunEntity
{
    /// <summary>
    /// Unique identifier for this run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the test.
    /// </summary>
    public Guid TestId { get; set; }

    /// <summary>
    /// Version of the test that was run.
    /// </summary>
    public int TestVersion { get; set; }

    /// <summary>
    /// Run number within a batch (1 to RunCount).
    /// </summary>
    public int RunNumber { get; set; } = 1;

    /// <summary>
    /// Profile ID used for this run (nullable).
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Context IDs used for this run, serialized as comma-separated Guids.
    /// </summary>
    public string? ContextIds { get; set; }

    /// <summary>
    /// When this run was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// The key (GUID) of the user who executed this run.
    /// </summary>
    public Guid? ExecutedByUserId { get; set; }

    /// <summary>
    /// Status of the run (Running=0, Passed=1, Failed=2, Error=3).
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Foreign key to the transcript (nullable).
    /// </summary>
    public Guid? TranscriptId { get; set; }

    /// <summary>
    /// Outcome type (Text=0, JSON=1, Events=2).
    /// </summary>
    public int OutcomeType { get; set; }

    /// <summary>
    /// Outcome output value.
    /// </summary>
    public string? OutcomeValue { get; set; }

    /// <summary>
    /// Outcome finish reason.
    /// </summary>
    public string? OutcomeFinishReason { get; set; }

    /// <summary>
    /// Token usage as JSON.
    /// </summary>
    public string? OutcomeTokenUsageJson { get; set; }

    /// <summary>
    /// Grader results serialized as JSON array.
    /// </summary>
    public string? GraderResultsJson { get; set; }

    /// <summary>
    /// Optional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Optional batch ID if part of batch execution.
    /// </summary>
    public Guid? BatchId { get; set; }
}
