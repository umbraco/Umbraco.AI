using Umbraco.AI.Core.EditableModels;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Attribute to mark AI test grader implementations.
/// Graders validate test outcomes against expectations.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AITestGraderAttribute(string id, string name) : Attribute
{
    /// <summary>
    /// The unique identifier of the grader.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// The display name of the grader.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The type of grader (CodeBased, ModelBased, Human).
    /// </summary>
    public AIGraderType Type { get; set; } = AIGraderType.CodeBased;
}

/// <summary>
/// Defines a test grader that validates test outcomes.
/// </summary>
public interface IAITestGrader : IDiscoverable
{
    /// <summary>
    /// The unique identifier of the grader.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The display name of the grader.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The description of what this grader validates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The type of grader (CodeBased, ModelBased, Human).
    /// </summary>
    AIGraderType Type { get; }

    /// <summary>
    /// The type that represents the grader configuration.
    /// Used to generate UI schemas for grader configuration.
    /// Returns null if no configuration is needed.
    /// </summary>
    Type? ConfigType { get; }

    /// <summary>
    /// Gets the configuration schema that describes the grader settings.
    /// Used by the UI to render the grader configuration form.
    /// Returns null if no configuration is needed.
    /// </summary>
    AIEditableModelSchema? GetConfigSchema();

    /// <summary>
    /// Grades a transcript/outcome against the grader configuration.
    /// Returns score (0-1) and pass/fail judgment.
    /// </summary>
    /// <param name="transcript">The execution transcript.</param>
    /// <param name="outcome">The final outcome.</param>
    /// <param name="graderConfig">The grader configuration from the test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The grading result.</returns>
    Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGraderConfig graderConfig,
        CancellationToken cancellationToken);
}

/// <summary>
/// Type of grader.
/// </summary>
public enum AIGraderType
{
    /// <summary>
    /// Code-based grader (deterministic, fast).
    /// Examples: exact match, regex, JSON schema validation.
    /// </summary>
    CodeBased = 0,

    /// <summary>
    /// Model-based grader (LLM-as-judge, flexible).
    /// Examples: semantic similarity, rubric-based judging.
    /// </summary>
    ModelBased = 1,

    /// <summary>
    /// Human review (manual grading).
    /// Reserved for future use.
    /// </summary>
    Human = 2
}
