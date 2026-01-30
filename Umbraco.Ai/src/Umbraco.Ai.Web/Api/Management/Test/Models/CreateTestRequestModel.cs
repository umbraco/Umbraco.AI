using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Test.Models;

/// <summary>
/// Request model for creating a new test.
/// </summary>
public class CreateTestRequestModel
{
    /// <summary>
    /// The alias of the test (must be unique).
    /// </summary>
    [Required]
    public required string Alias { get; init; }

    /// <summary>
    /// The display name of the test.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this test validates.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The test feature ID (e.g., "prompt", "agent").
    /// </summary>
    [Required]
    public required string TestTypeId { get; init; }

    /// <summary>
    /// The target configuration (what to test).
    /// </summary>
    [Required]
    public required TestTargetModel Target { get; init; }

    /// <summary>
    /// The test case configuration (inputs/context).
    /// </summary>
    [Required]
    public required TestCaseModel TestCase { get; init; }

    /// <summary>
    /// The graders configured for this test.
    /// </summary>
    public IReadOnlyList<CreateTestGraderModel> Graders { get; init; } = [];

    /// <summary>
    /// Number of runs to execute per test (1 to N, default: 1).
    /// </summary>
    public int RunCount { get; init; } = 1;

    /// <summary>
    /// Tags for organizing and filtering tests.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether the test is enabled for execution (default: true).
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Request model for creating a test grader.
/// </summary>
public class CreateTestGraderModel
{
    /// <summary>
    /// The grader type ID (e.g., "exact-match", "llm-judge").
    /// </summary>
    [Required]
    public required string GraderTypeId { get; init; }

    /// <summary>
    /// The name of the grader.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this grader validates.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The grader configuration as JSON.
    /// Structure depends on the grader type.
    /// </summary>
    [Required]
    public required string ConfigJson { get; init; }

    /// <summary>
    /// Whether to negate the grader result (default: false).
    /// </summary>
    public bool Negate { get; init; }

    /// <summary>
    /// The severity level (0=Info, 1=Warning, 2=Error, default: 2).
    /// </summary>
    public int Severity { get; init; } = 2;

    /// <summary>
    /// The weight for scoring (0-1, default: 1.0).
    /// </summary>
    public float Weight { get; init; } = 1.0f;

    /// <summary>
    /// The sort order for display (default: 0).
    /// </summary>
    public int SortOrder { get; init; }
}
