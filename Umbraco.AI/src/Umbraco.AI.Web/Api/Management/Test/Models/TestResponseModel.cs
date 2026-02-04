using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for a test (full details).
/// </summary>
public class TestResponseModel
{
    /// <summary>
    /// The unique identifier of the test.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The alias of the test.
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the test.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this test validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The test type ID (test feature implementation).
    /// </summary>
    [Required]
    public string TestTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The target being tested (prompt ID/alias or agent ID/alias).
    /// </summary>
    [Required]
    public TestTargetModel Target { get; set; } = new();

    /// <summary>
    /// Test case configuration as JSON string.
    /// Structure depends on the test type's TestCaseType.
    /// </summary>
    [Required]
    public string TestCaseJson { get; set; } = string.Empty;

    /// <summary>
    /// List of graders to evaluate test outcomes.
    /// </summary>
    public IReadOnlyList<TestGraderModel> Graders { get; set; } = [];

    /// <summary>
    /// Number of times to run this test for pass@k calculation.
    /// </summary>
    public int RunCount { get; set; }

    /// <summary>
    /// Tags for organizing tests.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// The date and time (in UTC) when the test was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the test was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The current version number of the entity.
    /// </summary>
    public int Version { get; set; }
}

/// <summary>
/// Target specification for a test.
/// </summary>
public class TestTargetModel
{
    /// <summary>
    /// The ID or alias of the target.
    /// </summary>
    [Required]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the TargetId is an alias (true) or ID (false).
    /// </summary>
    public bool IsAlias { get; set; }
}

/// <summary>
/// Grader configuration for a test.
/// </summary>
public class TestGraderModel
{
    /// <summary>
    /// Unique identifier for the grader within the test.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the grader type (implementation) to use.
    /// </summary>
    [Required]
    public string GraderTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Name of this grader instance.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this grader validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Grader-specific configuration as JSON.
    /// </summary>
    public string? ConfigJson { get; set; }

    /// <summary>
    /// Whether to negate the grader result.
    /// </summary>
    public bool Negate { get; set; }

    /// <summary>
    /// Severity level of this grader (Info, Warning, Error).
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Weight for scoring (0-1).
    /// </summary>
    public double Weight { get; set; } = 1.0;
}
