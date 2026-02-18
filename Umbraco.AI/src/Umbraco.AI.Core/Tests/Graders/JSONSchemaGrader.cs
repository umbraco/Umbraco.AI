using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for JSON schema grader.
/// </summary>
public class JSONSchemaGraderConfig
{
    /// <summary>
    /// Expected JSON structure (simplified validation).
    /// For now, validates that output is valid JSON and contains expected keys.
    /// </summary>
    [AIField(
        Label = "Expected JSON Keys",
        Description = "Required JSON keys (comma-separated, dot-notation for nested)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string ExpectedKeys { get; set; } = string.Empty;

    /// <summary>
    /// Whether all keys must be present.
    /// </summary>
    [AIField(
        Label = "Require All Keys",
        Description = "All expected keys must be present",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 2)]
    public bool RequireAllKeys { get; set; } = true;
}

/// <summary>
/// Grader that validates JSON structure.
/// Validates that output is valid JSON with expected structure.
/// Note: This is a simplified JSON validator. Full JSON Schema validation requires external libraries.
/// </summary>
[AITestGrader("json-schema", "JSON Schema Validation", Type = AIGraderType.CodeBased)]
public class JSONSchemaGrader : AITestGraderBase
{
    /// <inheritdoc />
    public override string Description => "Validates that output is valid JSON with expected structure";

    /// <inheritdoc />
    public override Type? ConfigType => typeof(JSONSchemaGraderConfig);

    /// <summary>
    /// Initializes a new instance of the <see cref="JSONSchemaGrader"/> class.
    /// </summary>
    public JSONSchemaGrader(IAIEditableModelSchemaBuilder schemaBuilder)
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
            ? new JSONSchemaGraderConfig()
            : JsonSerializer.Deserialize<JSONSchemaGraderConfig>(graderConfig.ConfigJson)
                ?? new JSONSchemaGraderConfig();

        // Extract actual value from final output
        var actualValue = ExtractContentFromOutput(outcome.OutputValue);

        // Parse expected keys
        var expectedKeys = config.ExpectedKeys
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // Try to parse as JSON
        bool passed;
        string? failureMessage = null;
        List<string>? missingKeys = null;

        try
        {
            using var doc = JsonDocument.Parse(actualValue);
            var root = doc.RootElement;

            // Validate expected keys
            missingKeys = new List<string>();
            foreach (var key in expectedKeys)
            {
                if (!HasProperty(root, key))
                {
                    missingKeys.Add(key);
                }
            }

            passed = config.RequireAllKeys
                ? missingKeys.Count == 0
                : missingKeys.Count < expectedKeys.Count;

            if (!passed)
            {
                if (config.RequireAllKeys)
                {
                    failureMessage = $"Missing required JSON keys: [{string.Join(", ", missingKeys)}]";
                }
                else
                {
                    failureMessage = "No expected JSON keys were found";
                }
            }
        }
        catch (JsonException ex)
        {
            passed = false;
            failureMessage = $"Invalid JSON: {ex.Message}";
        }

        return Task.FromResult(new AITestGraderResult
        {
            GraderId = graderConfig.Id,
            Passed = passed,
            Score = passed ? 1.0 : 0.0,
            ActualValue = actualValue,
            ExpectedValue = config.ExpectedKeys,
            FailureMessage = failureMessage,
            MetadataJson = missingKeys != null && missingKeys.Count > 0
                ? JsonSerializer.Serialize(new { missingKeys })
                : null
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

    private static bool HasProperty(JsonElement element, string propertyPath)
    {
        var parts = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var current = element;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!current.TryGetProperty(part, out var next))
            {
                return false;
            }

            current = next;
        }

        return true;
    }
}
