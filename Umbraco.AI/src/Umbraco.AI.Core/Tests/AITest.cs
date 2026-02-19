using System.Text.Json;
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
    /// The ID of the target entity being tested (prompt, agent, etc.).
    /// UI uses entity pickers to ensure valid IDs.
    /// </summary>
    public required Guid TestTargetId { get; set; }

    /// <summary>
    /// Test case data as JsonElement.
    /// Stored as JSON in database, deserialized on demand by test features.
    /// </summary>
    public JsonElement? TestCase { get; set; }

    /// <summary>
    /// Success criteria - graders that evaluate the test output.
    /// Multiple graders can be applied to validate different aspects.
    /// </summary>
    public IReadOnlyList<AITestGraderConfig> Graders { get; set; } = Array.Empty<AITestGraderConfig>();

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

    /// <summary>
    /// Gets the test case as a strongly-typed object.
    /// Deserializes the JsonElement to the specified type.
    /// </summary>
    /// <typeparam name="T">The target test case type.</typeparam>
    /// <returns>The test case as the specified type, or null if TestCase is null.</returns>
    public T? GetTestCase<T>() where T : class
    {
        if (TestCase == null)
        {
            return null;
        }

        // Deserialize directly from JsonElement
        return JsonSerializer.Deserialize<T>(TestCase.Value);
    }
}
