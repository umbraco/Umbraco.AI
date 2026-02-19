using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Defines a test for validating AI prompts, agents, or custom features for consistent outputs.
/// Acts like "unit tests for prompts and agents" - validates that outputs meet expectations even as models evolve.
/// </summary>
public sealed class AITest : IAIVersionableEntity
{
    /// <summary>
    /// The unique identifier of the test.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The alias of the test (unique identifier for lookups).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// The name of the test.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description explaining what this test validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the test feature (harness) to use for execution.
    /// References an IAITestFeature implementation (e.g., "prompt", "agent").
    /// </summary>
    public required string TestFeatureId { get; init; }

    /// <summary>
    /// What to test - the target prompt, agent, or custom feature.
    /// </summary>
    public required AITestTarget Target { get; set; }

    /// <summary>
    /// Test input configuration as JSON - structure depends on TestFeatureId.
    /// Deserialized to the type specified by IAITestFeature.TestCaseType.
    /// </summary>
    public required string TestCaseJson { get; set; }

    /// <summary>
    /// Success criteria - graders that evaluate the test output.
    /// Multiple graders can be applied to validate different aspects.
    /// </summary>
    public IReadOnlyList<AITestGrader> Graders { get; set; } = Array.Empty<AITestGrader>();

    /// <summary>
    /// Number of times to run this test (1 to N).
    /// Multiple runs help measure non-deterministic behavior and calculate pass@k/pass^k metrics.
    /// </summary>
    public int RunCount { get; set; } = 1;

    /// <summary>
    /// Tags for categorization and batch execution.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether this test is active for execution.
    /// Inactive tests are skipped during batch runs.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional baseline run ID for regression detection.
    /// When set, run results are compared against this baseline.
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
