using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for a test run.
/// </summary>
public class TestRunResponseModel
{
    /// <summary>
    /// The unique identifier of the run.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The test ID this run belongs to.
    /// </summary>
    [Required]
    public Guid TestId { get; set; }

    /// <summary>
    /// The test version when this run was executed.
    /// </summary>
    public int TestVersion { get; set; }

    /// <summary>
    /// The run number (1 to N).
    /// </summary>
    public int RunNumber { get; set; }

    /// <summary>
    /// The profile ID used for this run.
    /// </summary>
    [Required]
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The context IDs used for this run (JSON array).
    /// </summary>
    public string? ContextIdsJson { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the run was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// The user ID who executed the run.
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
    /// The error message if status is Error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The outcome of the run (what the model produced).
    /// </summary>
    public TestOutcomeModel? Outcome { get; set; }

    /// <summary>
    /// The transcript of the run (structured trace).
    /// </summary>
    public TestTranscriptModel? Transcript { get; set; }

    /// <summary>
    /// The grader results for this run.
    /// </summary>
    public IReadOnlyList<TestGraderResultModel> GraderResults { get; set; } = [];

    /// <summary>
    /// The batch ID if this run was part of a batch.
    /// </summary>
    public Guid? BatchId { get; set; }
}

/// <summary>
/// Test outcome model (what the model produced).
/// </summary>
public class TestOutcomeModel
{
    /// <summary>
    /// The type of output (0=Text, 1=Json, 2=Events).
    /// </summary>
    public int OutputType { get; set; }

    /// <summary>
    /// The output value as a string or JSON.
    /// </summary>
    [Required]
    public string OutputValue { get; set; } = string.Empty;

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
}

/// <summary>
/// Test transcript model (structured trace).
/// </summary>
public class TestTranscriptModel
{
    /// <summary>
    /// The transcript ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The run ID this transcript belongs to.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// The messages as JSON array.
    /// </summary>
    [Required]
    public string MessagesJson { get; set; } = string.Empty;

    /// <summary>
    /// The tool calls as JSON array.
    /// </summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>
    /// The reasoning/thinking steps as JSON.
    /// </summary>
    public string? ReasoningJson { get; set; }

    /// <summary>
    /// The timing information as JSON.
    /// </summary>
    public string? TimingJson { get; set; }

    /// <summary>
    /// The final output as JSON.
    /// </summary>
    [Required]
    public string FinalOutputJson { get; set; } = string.Empty;
}

/// <summary>
/// Test grader result model.
/// </summary>
public class TestGraderResultModel
{
    /// <summary>
    /// The grader ID this result belongs to.
    /// </summary>
    public Guid GraderId { get; set; }

    /// <summary>
    /// Whether the grader passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// The score (0-1) for model-based graders.
    /// </summary>
    public float? Score { get; set; }

    /// <summary>
    /// The actual value from the output.
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// The expected value (for comparison graders).
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// The failure message if not passed.
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }
}
