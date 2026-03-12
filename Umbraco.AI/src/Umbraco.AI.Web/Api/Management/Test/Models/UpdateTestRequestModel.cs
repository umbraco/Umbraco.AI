using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
    /// The ID of the target entity being tested (prompt, agent, etc.).
    /// </summary>
    [Required]
    public Guid TestTargetId { get; set; }

    /// <summary>
    /// Optional default profile ID for test execution.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Optional default context IDs for test execution.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIds { get; set; }

    /// <summary>
    /// Test feature configuration as JsonElement.
    /// Structure depends on the test feature's ConfigType.
    /// </summary>
    public JsonElement? TestFeatureConfig { get; set; }

    /// <summary>
    /// List of graders to evaluate test outcomes. At least one grader is required.
    /// </summary>
    [MinLength(1)]
    public IReadOnlyList<TestGraderModel> Graders { get; set; } = [];

    /// <summary>
    /// Named configuration overrides for A/B testing.
    /// </summary>
    public IReadOnlyList<TestVariationModel>? Variations { get; set; }

    /// <summary>
    /// Number of times to run this test for pass@k calculation.
    /// </summary>
    public int RunCount { get; set; } = 1;

    /// <summary>
    /// Tags for organizing tests.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];
}
