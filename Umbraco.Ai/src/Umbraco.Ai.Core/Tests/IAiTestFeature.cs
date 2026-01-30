using Umbraco.Ai.Core.EditableModels;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Test feature interface that enables model execution for specific test types.
/// Test features act as harnesses that orchestrate execution, capture transcripts,
/// and return structured traces for grading.
/// </summary>
/// <remarks>
/// Following Anthropic's eval framework, test features separate the execution layer
/// from the grading layer. A test feature knows HOW to execute a test (e.g., run
/// a prompt, execute an agent), while graders judge the outcome.
/// </remarks>
public interface IAiTestFeature : IDiscoverable
{
    /// <summary>
    /// Unique identifier for this test feature (e.g., "prompt", "agent").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name for this test feature.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what this test feature tests.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Category for grouping test features in UI.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// The type that represents the test case configuration for this test feature.
    /// Used to generate UI schemas for test case input.
    /// </summary>
    /// <remarks>
    /// This type should have [AiField] attributes on its properties to define
    /// the configuration form. Return null if no test case configuration is needed.
    /// </remarks>
    Type? TestCaseType { get; }

    /// <summary>
    /// Gets the test case schema that describes the configuration needed.
    /// Used by the UI to render the test case configuration form.
    /// Returns null if no test case configuration is needed.
    /// </summary>
    AiEditableModelSchema? GetTestCaseSchema();

    /// <summary>
    /// Executes a single test run and returns the transcript.
    /// The test feature enables the model to act - processing inputs, orchestrating
    /// tool calls, and returning results. The transcript captures the full trace
    /// for debugging and grading.
    /// </summary>
    /// <param name="test">The test definition containing target and test case configuration.</param>
    /// <param name="runNumber">The run number (1 to N) for this execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A structured transcript with messages, tool calls, reasoning, timing,
    /// and final output.
    /// </returns>
    Task<AiTestTranscript> ExecuteAsync(
        AiTest test,
        int runNumber,
        CancellationToken cancellationToken = default);
}
