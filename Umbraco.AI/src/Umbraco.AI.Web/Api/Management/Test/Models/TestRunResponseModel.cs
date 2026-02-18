using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for a test run result.
/// </summary>
public class TestRunResponseModel
{
    /// <summary>
    /// The unique identifier of the test run.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the test that was run.
    /// </summary>
    [Required]
    public Guid TestId { get; set; }

    /// <summary>
    /// The version of the test at the time of execution.
    /// </summary>
    public int TestVersion { get; set; }

    /// <summary>
    /// The run number for this execution.
    /// </summary>
    public int RunNumber { get; set; }

    /// <summary>
    /// The profile ID used for this run.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// The context IDs used for this run.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// The date and time (in UTC) when the run was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// The user ID who executed this run (if applicable).
    /// </summary>
    public Guid? ExecutedByUserId { get; set; }

    /// <summary>
    /// The status of the run (Running, Passed, Failed, Error).
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Duration of the test execution in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// The ID of the transcript for this run.
    /// </summary>
    public Guid? TranscriptId { get; set; }

    /// <summary>
    /// The outcome of the test execution.
    /// </summary>
    public TestOutcomeResponseModel? Outcome { get; set; }

    /// <summary>
    /// Results from each grader.
    /// </summary>
    public IReadOnlyList<TestGraderResultResponseModel> GraderResults { get; set; } = [];

    /// <summary>
    /// Optional metadata for the run (JSON).
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Optional batch ID if this run is part of a batch execution.
    /// </summary>
    public Guid? BatchId { get; set; }
}

/// <summary>
/// Response model for a test transcript.
/// </summary>
public class TestTranscriptResponseModel
{
    /// <summary>
    /// The unique identifier of the transcript.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The messages exchanged during execution (JSON).
    /// </summary>
    public string? MessagesJson { get; set; }

    /// <summary>
    /// Tool calls made during execution (JSON).
    /// </summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>
    /// Reasoning steps or intermediate outputs (JSON).
    /// </summary>
    public string? ReasoningJson { get; set; }

    /// <summary>
    /// Timing information for the execution (JSON).
    /// </summary>
    public string? TimingJson { get; set; }

    /// <summary>
    /// The final output from the execution (JSON).
    /// </summary>
    public string? FinalOutputJson { get; set; }
}

/// <summary>
/// Response model for a test outcome.
/// </summary>
public class TestOutcomeResponseModel
{
    /// <summary>
    /// The type of output produced by the test.
    /// </summary>
    [Required]
    public string OutputType { get; set; } = string.Empty;

    /// <summary>
    /// The actual output value from the test execution.
    /// </summary>
    public string? OutputValue { get; set; }

    /// <summary>
    /// The reason the execution finished.
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Token usage information (JSON).
    /// </summary>
    public string? TokenUsageJson { get; set; }
}

/// <summary>
/// Response model for a grader result.
/// </summary>
public class TestGraderResultResponseModel
{
    /// <summary>
    /// The ID of the grader that produced this result.
    /// </summary>
    [Required]
    public Guid GraderId { get; set; }

    /// <summary>
    /// Whether the grader passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Score from the grader (0-1).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// The actual value that was evaluated.
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// The expected value (if applicable).
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// Failure message explaining why the grader failed.
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// Optional metadata from the grader (JSON).
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Severity level (Info, Warning, Error).
    /// </summary>
    public string Severity { get; set; } = "Error";
}
