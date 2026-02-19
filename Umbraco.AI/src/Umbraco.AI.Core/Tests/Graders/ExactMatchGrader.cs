using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for exact match grader.
/// </summary>
public class ExactMatchGraderConfig
{
    /// <summary>
    /// The expected value to match against.
    /// </summary>
    [AIField(
        Label = "Expected Value",
        Description = "The exact value to match",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string ExpectedValue { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ignore case when comparing.
    /// </summary>
    [AIField(
        Label = "Ignore Case",
        Description = "Case-insensitive comparison",
        SortOrder = 2)]
    public bool IgnoreCase { get; set; }
}

/// <summary>
/// Grader that validates exact string match.
/// Fast, deterministic validation for exact equality.
/// </summary>
[AITestGrader("exact-match", "Exact Match", Type = AIGraderType.CodeBased)]
public class ExactMatchGrader : AITestGraderBase
{
    /// <inheritdoc />
    public override string Description => "Validates exact string match between actual and expected values";

    /// <inheritdoc />
    public override Type? ConfigType => typeof(ExactMatchGraderConfig);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExactMatchGrader"/> class.
    /// </summary>
    public ExactMatchGrader(IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
    }

    /// <inheritdoc />
    public override Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGrader graderConfig,
        CancellationToken cancellationToken)
    {
        // Deserialize configuration
        var config = string.IsNullOrWhiteSpace(graderConfig.ConfigJson)
            ? new ExactMatchGraderConfig()
            : JsonSerializer.Deserialize<ExactMatchGraderConfig>(graderConfig.ConfigJson)
                ?? new ExactMatchGraderConfig();

        // Extract actual value from final output
        var actualValue = ExtractContentFromOutput(outcome.OutputValue);

        // Apply transformations
        var actual = actualValue.Trim();
        var expected = config.ExpectedValue.Trim();

        // Perform comparison
        var passed = config.IgnoreCase
            ? string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
            : string.Equals(actual, expected, StringComparison.Ordinal);

        return Task.FromResult(new AITestGraderResult
        {
            GraderId = graderConfig.Id,
            Passed = passed,
            Score = passed ? 1.0 : 0.0,
            ActualValue = actualValue,
            ExpectedValue = config.ExpectedValue,
            FailureMessage = passed ? null : $"Expected exact match but got different value"
        });
    }

    private static string ExtractContentFromOutput(string? outputJson)
    {
        if (string.IsNullOrWhiteSpace(outputJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(outputJson);
            if (doc.RootElement.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // If parsing fails, return raw JSON
        }

        return outputJson;
    }
}
