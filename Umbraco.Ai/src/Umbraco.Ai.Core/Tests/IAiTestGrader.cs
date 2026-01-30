using Umbraco.Ai.Core.EditableModels;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Grader interface for evaluating test run outcomes.
/// Graders judge whether a test run succeeded or failed based on specific criteria,
/// and optionally provide a score for model-based grading.
/// </summary>
/// <remarks>
/// Following Anthropic's eval framework, graders operate on the observation layer
/// (transcript + outcome) rather than the execution path. This means we grade WHAT
/// happened, not HOW it happened.
/// </remarks>
public interface IAiTestGrader : IDiscoverable
{
    /// <summary>
    /// Unique identifier for this grader (e.g., "exact-match", "llm-judge").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name for this grader.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what this grader evaluates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The type of grader (code-based, model-based, or human).
    /// </summary>
    GraderType Type { get; }

    /// <summary>
    /// The type that represents the grader configuration.
    /// Used to generate UI schemas for grader configuration.
    /// </summary>
    /// <remarks>
    /// This type should have [AiField] attributes on its properties to define
    /// the configuration form. Return null if no configuration is needed.
    /// </remarks>
    Type? ConfigType { get; }

    /// <summary>
    /// Gets the configuration schema that describes the grader settings.
    /// Used by the UI to render the grader configuration form.
    /// Returns null if no configuration is needed.
    /// </summary>
    AiEditableModelSchema? GetConfigSchema();

    /// <summary>
    /// Grades a transcript/outcome against the grader configuration.
    /// Returns a pass/fail judgment and optionally a score (0-1) for model-based graders.
    /// </summary>
    /// <param name="transcript">The structured transcript from the test run.</param>
    /// <param name="outcome">The final outcome produced by the model.</param>
    /// <param name="graderConfig">The grader configuration from the test definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A grader result with pass/fail status, optional score (0-1), actual vs expected
    /// values, and failure message if applicable.
    /// </returns>
    Task<AiTestGraderResult> GradeAsync(
        AiTestTranscript transcript,
        AiTestOutcome outcome,
        AiTestGrader graderConfig,
        CancellationToken cancellationToken = default);
}
