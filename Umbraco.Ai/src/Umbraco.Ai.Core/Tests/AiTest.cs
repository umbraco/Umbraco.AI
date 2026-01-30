using Umbraco.Ai.Core.Versioning;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Defines a test for AI system validation, including test type, target, test case, graders, and run configuration.
/// </summary>
public sealed class AiTest : IAiVersionableEntity
{
    /// <summary>
    /// The unique identifier of the AI test.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The alias of the AI test.
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// The name of the AI test.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The description of what this test validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The test feature ID that determines how this test is executed (e.g., "prompt", "agent", "custom").
    /// </summary>
    public required string TestTypeId { get; set; }

    /// <summary>
    /// The target configuration specifying what to test (prompt/agent/custom target).
    /// </summary>
    public required AiTestTarget Target { get; set; }

    /// <summary>
    /// The test case configuration specifying inputs and context for the test.
    /// </summary>
    public required AiTestCase TestCase { get; set; }

    /// <summary>
    /// The graders that define success criteria for this test.
    /// </summary>
    public IReadOnlyList<AiTestGrader> Graders { get; set; } = Array.Empty<AiTestGrader>();

    /// <summary>
    /// Number of runs to execute for this test (1 to N). Useful for testing non-deterministic outputs.
    /// </summary>
    public int RunCount { get; set; } = 1;

    /// <summary>
    /// A list of tags associated with the test for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether this test is enabled and should be executed.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The ID of the baseline run to compare against for regression detection.
    /// </summary>
    public Guid? BaselineRunId { get; set; }

    /// <summary>
    /// The current version of the test.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    public int Version { get; internal set; } = 1;

    /// <summary>
    /// The date and time when the test was created.
    /// </summary>
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the test was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who created this test.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this test.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }
}
