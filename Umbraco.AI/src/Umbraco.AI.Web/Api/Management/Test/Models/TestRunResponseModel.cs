using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
    /// The name of the test that was run.
    /// </summary>
    public string? TestName { get; set; }

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
    /// Error information if the run failed with an exception.
    /// </summary>
    public TestRunErrorResponseModel? Error { get; set; }

    /// <summary>
    /// Optional batch ID if this run is part of a batch execution.
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// Whether this run is the baseline run for its test.
    /// </summary>
    public bool IsBaseline { get; set; }

    /// <summary>
    /// The baseline run ID for the test this run belongs to.
    /// Used by the frontend to enable comparison views.
    /// </summary>
    public Guid? BaselineRunId { get; set; }

    /// <summary>
    /// Groups all runs from one test execution (default + all variations).
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// The variation ID within the execution, or null for the default config run.
    /// </summary>
    public Guid? VariationId { get; set; }

    /// <summary>
    /// The variation name for display, or null for default config runs.
    /// </summary>
    public string? VariationName { get; set; }
}

/// <summary>
/// Response model for a test run error.
/// </summary>
public class TestRunErrorResponseModel
{
    /// <summary>
    /// The error message.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The stack trace of the error, if available.
    /// </summary>
    public string? StackTrace { get; set; }
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
    /// The messages exchanged during execution.
    /// </summary>
    public JsonElement? Messages { get; set; }

    /// <summary>
    /// Tool calls made during execution.
    /// </summary>
    public JsonElement? ToolCalls { get; set; }

    /// <summary>
    /// Reasoning steps or intermediate outputs.
    /// </summary>
    public JsonElement? Reasoning { get; set; }

    /// <summary>
    /// Timing information for the execution.
    /// </summary>
    public JsonElement? Timing { get; set; }

    /// <summary>
    /// The final output from the execution.
    /// </summary>
    public JsonElement? FinalOutput { get; set; }
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
    /// Token usage statistics for the execution.
    /// </summary>
    public TestTokenUsageResponseModel? TokenUsage { get; set; }
}

/// <summary>
/// Response model for token usage statistics.
/// </summary>
public class TestTokenUsageResponseModel
{
    /// <summary>
    /// Number of input tokens consumed.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    public int TotalTokens { get; set; }
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
    /// The display name of the grader (from test configuration).
    /// </summary>
    public string? GraderName { get; set; }

    /// <summary>
    /// The grader type identifier (e.g., "exact-match", "llm-judge").
    /// </summary>
    public string? GraderTypeId { get; set; }

    /// <summary>
    /// The grader type category (e.g., "CodeBased", "ModelBased").
    /// </summary>
    public string? GraderType { get; set; }

    /// <summary>
    /// The weight of this grader for aggregate scoring (0-1).
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Whether the grader result is negated (pass becomes fail, fail becomes pass).
    /// </summary>
    public bool Negate { get; set; }

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
    /// Optional metadata from the grader.
    /// </summary>
    public JsonElement? Metadata { get; set; }

    /// <summary>
    /// Severity level (Info, Warning, Error).
    /// </summary>
    public string Severity { get; set; } = "Error";
}
