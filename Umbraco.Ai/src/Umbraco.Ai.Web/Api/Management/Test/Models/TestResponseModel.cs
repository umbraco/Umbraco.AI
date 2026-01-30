using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for a test.
/// </summary>
public class TestResponseModel
{
    /// <summary>
    /// The unique identifier of the test.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The alias of the test (unique identifier).
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the test.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this test validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The test feature ID (e.g., "prompt", "agent").
    /// </summary>
    [Required]
    public string TestTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The target configuration (what to test).
    /// </summary>
    [Required]
    public TestTargetModel Target { get; set; } = null!;

    /// <summary>
    /// The test case configuration (inputs/context).
    /// </summary>
    [Required]
    public TestCaseModel TestCase { get; set; } = null!;

    /// <summary>
    /// The graders configured for this test.
    /// </summary>
    public IReadOnlyList<TestGraderModel> Graders { get; set; } = [];

    /// <summary>
    /// Number of runs to execute per test (1 to N).
    /// </summary>
    public int RunCount { get; set; }

    /// <summary>
    /// Tags for organizing and filtering tests.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether the test is enabled for execution.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The baseline run ID for regression detection.
    /// </summary>
    public Guid? BaselineRunId { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the test was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the test was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The user ID who created the test.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The user ID who last modified the test.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// The current version number of the test.
    /// </summary>
    public int Version { get; set; }
}

/// <summary>
/// Test target configuration.
/// </summary>
public class TestTargetModel
{
    /// <summary>
    /// The target ID or alias.
    /// </summary>
    [Required]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the TargetId is an alias (true) or Guid (false).
    /// </summary>
    public bool IsAlias { get; set; }
}

/// <summary>
/// Test case configuration (inputs/context).
/// </summary>
public class TestCaseModel
{
    /// <summary>
    /// The test case configuration as JSON.
    /// Structure depends on the test feature (prompt, agent, custom).
    /// </summary>
    [Required]
    public string TestCaseJson { get; set; } = string.Empty;
}

/// <summary>
/// Test grader configuration.
/// </summary>
public class TestGraderModel
{
    /// <summary>
    /// The unique identifier of the grader.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The grader type ID (e.g., "exact-match", "llm-judge").
    /// </summary>
    [Required]
    public string GraderTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the grader.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this grader validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The grader configuration as JSON.
    /// Structure depends on the grader type.
    /// </summary>
    [Required]
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// Whether to negate the grader result (fail becomes pass, pass becomes fail).
    /// </summary>
    public bool Negate { get; set; }

    /// <summary>
    /// The severity level (0=Info, 1=Warning, 2=Error).
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// The weight for scoring (0-1).
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// The sort order for display.
    /// </summary>
    public int SortOrder { get; set; }
}
