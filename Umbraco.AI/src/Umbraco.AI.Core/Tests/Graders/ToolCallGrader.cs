using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for tool call grader.
/// </summary>
public class ToolCallGraderConfig
{
    /// <summary>
    /// Expected tool name (or names, comma-separated).
    /// </summary>
    [AIField(
        Label = "Expected Tools",
        Description = "Tool names to validate (comma-separated for multiple)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 1)]
    public string ExpectedTools { get; set; } = string.Empty;

    /// <summary>
    /// Validation mode for tool calls.
    /// </summary>
    [AIField(
        Label = "Validation Mode",
        Description = "How to validate tool calls",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"Any\",\"All\",\"Exact\",\"None\"]}]",
        SortOrder = 2)]
    public string ValidationMode { get; set; } = "Any";

    /// <summary>
    /// Whether to validate tool call order.
    /// </summary>
    [AIField(
        Label = "Validate Order",
        Description = "Whether tool calls must appear in the specified order",
        SortOrder = 3)]
    public bool ValidateOrder { get; set; }
}

/// <summary>
/// Grader that validates tool call presence and sequence.
/// Validates that expected tools were called during execution.
/// </summary>
[AITestGrader("tool-call", "Tool Call Validation", Type = AIGraderType.CodeBased)]
public class ToolCallGrader : AITestGraderBase
{
    /// <inheritdoc />
    public override string Description => "Validates that expected tools were called during execution";

    /// <inheritdoc />
    public override Type? ConfigType => typeof(ToolCallGraderConfig);

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallGrader"/> class.
    /// </summary>
    public ToolCallGrader(IAIEditableModelSchemaBuilder schemaBuilder)
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
            ? new ToolCallGraderConfig()
            : JsonSerializer.Deserialize<ToolCallGraderConfig>(graderConfig.ConfigJson)
                ?? new ToolCallGraderConfig();

        // Parse expected tools
        var expectedTools = config.ExpectedTools
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // Extract actual tool calls from transcript
        var actualTools = ExtractToolCallsFromTranscript(transcript.ToolCallsJson);

        // Validate based on mode
        bool passed;
        string? failureMessage = null;

        switch (config.ValidationMode)
        {
            case "Any":
                passed = expectedTools.Any(expected => actualTools.Any(actual => actual.Equals(expected, StringComparison.OrdinalIgnoreCase)));
                if (!passed)
                {
                    failureMessage = $"Expected at least one of [{string.Join(", ", expectedTools)}] but got [{string.Join(", ", actualTools)}]";
                }
                break;

            case "All":
                var missing = expectedTools.Where(expected => !actualTools.Any(actual => actual.Equals(expected, StringComparison.OrdinalIgnoreCase))).ToList();
                passed = missing.Count == 0;
                if (!passed)
                {
                    failureMessage = $"Missing expected tools: [{string.Join(", ", missing)}]";
                }
                break;

            case "Exact":
                passed = expectedTools.Count == actualTools.Count &&
                         expectedTools.All(expected => actualTools.Any(actual => actual.Equals(expected, StringComparison.OrdinalIgnoreCase)));
                if (!passed)
                {
                    failureMessage = $"Expected exactly [{string.Join(", ", expectedTools)}] but got [{string.Join(", ", actualTools)}]";
                }
                break;

            case "None":
                passed = !actualTools.Any(actual => expectedTools.Any(expected => expected.Equals(actual, StringComparison.OrdinalIgnoreCase)));
                if (!passed)
                {
                    failureMessage = $"Expected none of [{string.Join(", ", expectedTools)}] but found matches";
                }
                break;

            default:
                passed = false;
                failureMessage = $"Unknown validation mode: {config.ValidationMode}";
                break;
        }

        // Additional order validation if required
        if (passed && config.ValidateOrder && config.ValidationMode is "All" or "Exact")
        {
            var orderedActual = actualTools.Where(actual => expectedTools.Any(expected => expected.Equals(actual, StringComparison.OrdinalIgnoreCase))).ToList();
            passed = orderedActual.SequenceEqual(expectedTools, StringComparer.OrdinalIgnoreCase);
            if (!passed)
            {
                failureMessage = $"Tool call order mismatch. Expected: [{string.Join(", ", expectedTools)}], Got: [{string.Join(", ", orderedActual)}]";
            }
        }

        return Task.FromResult(new AITestGraderResult
        {
            GraderId = graderConfig.Id,
            Passed = passed,
            Score = passed ? 1.0 : 0.0,
            ActualValue = string.Join(", ", actualTools),
            ExpectedValue = string.Join(", ", expectedTools),
            FailureMessage = failureMessage
        });
    }

    private static List<string> ExtractToolCallsFromTranscript(string? toolCallsJson)
    {
        var toolNames = new List<string>();

        if (string.IsNullOrWhiteSpace(toolCallsJson))
        {
            return toolNames;
        }

        try
        {
            using var doc = JsonDocument.Parse(toolCallsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var name))
                    {
                        var toolName = name.GetString();
                        if (!string.IsNullOrEmpty(toolName))
                        {
                            toolNames.Add(toolName);
                        }
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, return empty list
        }

        return toolNames;
    }
}
