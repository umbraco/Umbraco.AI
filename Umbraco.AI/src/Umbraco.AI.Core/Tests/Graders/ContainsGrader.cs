using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for contains grader.
/// </summary>
public class ContainsGraderConfig
{
    /// <summary>
    /// The substring or pattern to search for.
    /// </summary>
    [AIField(
        Label = "Search Pattern",
        Description = "The substring to find in the output",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string SearchPattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ignore case when searching.
    /// </summary>
    [AIField(
        Label = "Ignore Case",
        Description = "Case-insensitive search",
        SortOrder = 2)]
    public bool IgnoreCase { get; set; } = true;
}

/// <summary>
/// Grader that validates substring presence.
/// Fast, deterministic validation for content checks.
/// </summary>
[AITestGrader("contains", "Contains", Type = AIGraderType.CodeBased)]
public class ContainsGrader : AITestGraderBase<ContainsGraderConfig>
{
    /// <inheritdoc />
    public override string Description => "Validates that output contains a specific substring or pattern";

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainsGrader"/> class.
    /// </summary>
    public ContainsGrader(IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    { }

    /// <inheritdoc />
    public override Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGraderConfig graderConfig,
        CancellationToken cancellationToken)
    {
        // Deserialize configuration
        var config = graderConfig.Config is not { } configElement
            ? new ContainsGraderConfig()
            : JsonSerializer.Deserialize<ContainsGraderConfig>(configElement)
                ?? new ContainsGraderConfig();

        // Output value is already extracted by the test feature
        var actualValue = outcome.OutputValue ?? string.Empty;

        // Perform substring check
        var comparisonType = config.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var passed = actualValue.Contains(config.SearchPattern, comparisonType);

        return Task.FromResult(new AITestGraderResult
        {
            GraderId = graderConfig.Id,
            Passed = passed,
            Score = passed ? 1.0 : 0.0,
            ActualValue = actualValue,
            ExpectedValue = config.SearchPattern,
            FailureMessage = passed
                ? null
                : $"Expected output to contain '{config.SearchPattern}' but it was not found"
        });
    }

}
