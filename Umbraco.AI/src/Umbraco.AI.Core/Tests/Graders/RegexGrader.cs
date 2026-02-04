using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for regex grader.
/// </summary>
public class RegexGraderConfig
{
    /// <summary>
    /// The regular expression pattern to match.
    /// </summary>
    [AIField(
        Label = "Regex Pattern",
        Description = "Regular expression pattern to match",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ignore case when matching.
    /// </summary>
    [AIField(
        Label = "Ignore Case",
        Description = "Case-insensitive matching",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 2)]
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Whether to use multiline mode.
    /// </summary>
    [AIField(
        Label = "Multiline",
        Description = "Enable multiline mode (^ and $ match line boundaries)",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 3)]
    public bool Multiline { get; set; }
}

/// <summary>
/// Grader that validates regex pattern matching.
/// Flexible pattern validation for structured outputs.
/// </summary>
[AITestGrader("regex", "Regex Match", Type = AIGraderType.CodeBased)]
public class RegexGrader : AITestGraderBase
{
    /// <inheritdoc />
    public override string Description => "Validates that output matches a regular expression pattern";

    /// <inheritdoc />
    public override Type? ConfigType => typeof(RegexGraderConfig);

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexGrader"/> class.
    /// </summary>
    public RegexGrader(IAIEditableModelSchemaBuilder schemaBuilder)
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
            ? new RegexGraderConfig()
            : JsonSerializer.Deserialize<RegexGraderConfig>(graderConfig.ConfigJson)
                ?? new RegexGraderConfig();

        // Extract actual value from final output
        var actualValue = ExtractContentFromOutput(outcome.FinalOutputJson);

        // Build regex options
        var options = RegexOptions.None;
        if (config.IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }
        if (config.Multiline)
        {
            options |= RegexOptions.Multiline;
        }

        // Perform regex match
        bool passed;
        string? failureMessage = null;
        Match? match = null;

        try
        {
            var regex = new Regex(config.Pattern, options, TimeSpan.FromSeconds(5));
            match = regex.Match(actualValue);
            passed = match.Success;

            if (!passed)
            {
                failureMessage = $"Output did not match regex pattern: {config.Pattern}";
            }
        }
        catch (RegexMatchTimeoutException)
        {
            passed = false;
            failureMessage = "Regex matching timed out after 5 seconds";
        }
        catch (ArgumentException ex)
        {
            passed = false;
            failureMessage = $"Invalid regex pattern: {ex.Message}";
        }

        // Build metadata with match details
        string? metadataJson = null;
        if (match?.Success == true)
        {
            var metadata = new
            {
                matchValue = match.Value,
                matchIndex = match.Index,
                matchLength = match.Length,
                groups = match.Groups.Cast<Group>()
                    .Skip(1) // Skip the full match group
                    .Select(g => new { name = g.Name, value = g.Value })
                    .ToArray()
            };
            metadataJson = JsonSerializer.Serialize(metadata);
        }

        return Task.FromResult(new AITestGraderResult
        {
            GraderId = graderConfig.Id,
            Passed = passed,
            Score = passed ? 1.0 : 0.0,
            ActualValue = actualValue,
            ExpectedValue = config.Pattern,
            FailureMessage = failureMessage,
            MetadataJson = metadataJson
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
