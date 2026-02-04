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
    /// The date and time (in UTC) when the run started.
    /// </summary>
    public DateTime DateStarted { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the run completed.
    /// </summary>
    public DateTime? DateCompleted { get; set; }

    /// <summary>
    /// The status of the run (Running, Completed, Failed).
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total number of executions.
    /// </summary>
    public int TotalRuns { get; set; }

    /// <summary>
    /// Number of executions that passed.
    /// </summary>
    public int PassedRuns { get; set; }

    /// <summary>
    /// Number of executions that failed.
    /// </summary>
    public int FailedRuns { get; set; }

    /// <summary>
    /// pass@k score (proportion that passed).
    /// </summary>
    public double PassAtK { get; set; }

    /// <summary>
    /// Average score across all runs.
    /// </summary>
    public double AverageScore { get; set; }

    /// <summary>
    /// Optional profile ID override used for this run.
    /// </summary>
    public Guid? ProfileIdOverride { get; set; }

    /// <summary>
    /// Optional context IDs override used for this run.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIdsOverride { get; set; }

    /// <summary>
    /// Error message if the run failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Transcripts for each execution.
    /// </summary>
    public IReadOnlyList<TestTranscriptResponseModel> Transcripts { get; set; } = [];

    /// <summary>
    /// Outcomes for each execution.
    /// </summary>
    public IReadOnlyList<TestOutcomeResponseModel> Outcomes { get; set; } = [];
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
    /// The unique identifier of the outcome.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The run number (1-indexed).
    /// </summary>
    public int RunNumber { get; set; }

    /// <summary>
    /// Whether this execution passed all graders.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Aggregate weighted score from all graders.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Results from each grader.
    /// </summary>
    public IReadOnlyList<TestGraderResultResponseModel> GraderResults { get; set; } = [];
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
