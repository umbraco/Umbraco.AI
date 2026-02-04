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
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string SearchPattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ignore case when searching.
    /// </summary>
    [AIField(
        Label = "Ignore Case",
        Description = "Case-insensitive search",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 2)]
    public bool IgnoreCase { get; set; } = true;
}

/// <summary>
/// Grader that validates substring presence.
/// Fast, deterministic validation for content checks.
/// </summary>
[AITestGrader("contains", "Contains", Type = AIGraderType.CodeBased)]
public class ContainsGrader : AITestGraderBase
{
    /// <inheritdoc />
    public override string Description => "Validates that output contains a specific substring or pattern";

    /// <inheritdoc />
    public override Type? ConfigType => typeof(ContainsGraderConfig);

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainsGrader"/> class.
    /// </summary>
    public ContainsGrader(IAIEditableModelSchemaBuilder schemaBuilder)
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
            ? new ContainsGraderConfig()
            : JsonSerializer.Deserialize<ContainsGraderConfig>(graderConfig.ConfigJson)
                ?? new ContainsGraderConfig();

        // Extract actual value from final output
        var actualValue = ExtractContentFromOutput(outcome.FinalOutputJson);

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
