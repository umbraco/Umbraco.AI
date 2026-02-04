using Umbraco.AI.Core.EditableModels;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Attribute to mark AI test feature implementations.
/// Test features (harnesses) enable model execution for specific test types (prompt, agent, custom).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AITestFeatureAttribute(string id, string name) : Attribute
{
    /// <summary>
    /// The unique identifier of the test feature.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// The display name of the test feature.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The category of the test feature (e.g., "Built-in", "Custom").
    /// </summary>
    public string Category { get; set; } = "Custom";
}

/// <summary>
/// Defines a test feature (harness) that enables model execution for a specific test type.
/// Test features orchestrate execution: setting up context, running the model, capturing transcripts.
/// </summary>
public interface IAITestFeature : IDiscoverable
{
    /// <summary>
    /// The unique identifier of the test feature.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The display name of the test feature.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The description of what this test feature validates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The category of the test feature (e.g., "Built-in", "Custom").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// The type that represents the test case configuration for this test feature.
    /// Used to generate UI schemas for test case input.
    /// Returns null if no test case configuration is needed.
    /// </summary>
    Type? TestCaseType { get; }

    /// <summary>
    /// Gets the test case schema that describes the configuration needed.
    /// Used by the UI to render the test case configuration form.
    /// Returns null if no test case configuration is needed.
    /// </summary>
    AIEditableModelSchema? GetTestCaseSchema();

    /// <summary>
    /// Executes a single test run and returns the transcript.
    /// The harness enables the model to act - processing inputs, orchestrating tool calls, returning results.
    /// </summary>
    /// <param name="test">The test definition.</param>
    /// <param name="runNumber">The run number (1 to N).</param>
    /// <param name="profileIdOverride">Optional profile override for cross-model comparison.</param>
    /// <param name="contextIdsOverride">Optional context override for cross-context comparison.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcript of the execution.</returns>
    Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        CancellationToken cancellationToken);
}
