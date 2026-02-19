using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Request model for updating a test.
/// </summary>
public class UpdateTestRequestModel
{
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
    /// The target being tested (prompt ID/alias or agent ID/alias).
    /// </summary>
    [Required]
    public TestTargetModel Target { get; set; } = new();

    /// <summary>
    /// Test case configuration as JSON string.
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
    public int RunCount { get; set; } = 1;

    /// <summary>
    /// Tags for organizing tests.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];
}
