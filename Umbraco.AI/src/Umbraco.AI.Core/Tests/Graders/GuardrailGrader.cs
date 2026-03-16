using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails.Evaluators;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for the guardrail grader.
/// </summary>
public class GuardrailGraderConfig
{
    /// <summary>
    /// The ID of the guardrail evaluator to run (e.g., "pii", "toxicity", "llm-judge").
    /// </summary>
    [AIField(
        Label = "Evaluator",
        Description = "The guardrail evaluator to run against the test output",
        EditorUiAlias = "Uai.PropertyEditorUi.GuardrailEvaluatorPicker",
        SortOrder = 1)]
    public string EvaluatorId { get; set; } = string.Empty;

    /// <summary>
    /// Evaluator-specific configuration (JSON).
    /// The schema depends on the selected evaluator.
    /// </summary>
    [AIField(
        Label = "Evaluator Config",
        Description = "Evaluator-specific configuration (JSON)",
        EditorUiAlias = "Umb.PropertyEditorUi.CodeEditor",
        EditorConfig = "[{\"alias\":\"language\",\"value\":\"json\"}]",
        SortOrder = 2)]
    public JsonElement? EvaluatorConfig { get; set; }
}

/// <summary>
/// Grader that runs a guardrail evaluator against test output.
/// Validates that content is correctly flagged (or not) by a specific guardrail evaluator.
/// Use with <see cref="AITestGraderConfig.Negate"/> to assert that content should pass (not be flagged).
/// </summary>
[AITestGrader("guardrail", "Guardrail", Type = AIGraderType.CodeBased)]
public class GuardrailGrader : AITestGraderBase<GuardrailGraderConfig>
{
    private readonly AIGuardrailEvaluatorCollection _evaluators;

    /// <inheritdoc />
    public override string Description => "Runs a guardrail evaluator against test output to validate safety compliance";

    /// <summary>
    /// Initializes a new instance of the <see cref="GuardrailGrader"/> class.
    /// </summary>
    public GuardrailGrader(
        AIGuardrailEvaluatorCollection evaluators,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        _evaluators = evaluators;
    }

    /// <inheritdoc />
    public override async Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGraderConfig graderConfig,
        CancellationToken cancellationToken)
    {
        var config = graderConfig.Config is not { } configElement
            ? new GuardrailGraderConfig()
            : configElement.Deserialize<GuardrailGraderConfig>(Constants.DefaultJsonSerializerOptions)
                ?? new GuardrailGraderConfig();

        if (string.IsNullOrWhiteSpace(config.EvaluatorId))
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                FailureMessage = "No evaluator ID specified in guardrail grader configuration"
            };
        }

        var evaluator = _evaluators.GetById(config.EvaluatorId);
        if (evaluator is null)
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                FailureMessage = $"Guardrail evaluator '{config.EvaluatorId}' not found"
            };
        }

        var content = outcome.OutputValue ?? string.Empty;

        // Build conversation history from transcript
        var conversationHistory = DeserializeMessages(transcript.Messages);

        try
        {
            var evaluatorConfig = new AIGuardrailConfig { Config = config.EvaluatorConfig };
            var result = await evaluator.EvaluateAsync(content, conversationHistory, evaluatorConfig, cancellationToken);

            // "Passed" means the evaluator flagged the content (content was caught by the guardrail).
            // Use Negate on the grader config to invert: assert that content should NOT be flagged.
            var passed = result.Flagged;

            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = passed,
                Score = result.Score ?? (passed ? 1.0 : 0.0),
                ActualValue = content,
                ExpectedValue = $"Content should be flagged by '{config.EvaluatorId}' evaluator",
                FailureMessage = passed
                    ? null
                    : $"Evaluator '{config.EvaluatorId}' did not flag the content. Reason: {result.Reason ?? "none"}",
                Metadata = result.Metadata
            };
        }
        catch (Exception ex)
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                ActualValue = content,
                ExpectedValue = $"Content should be flagged by '{config.EvaluatorId}' evaluator",
                FailureMessage = $"Guardrail evaluator '{config.EvaluatorId}' failed: {ex.Message}"
            };
        }
    }

    private static IReadOnlyList<ChatMessage> DeserializeMessages(JsonElement? messagesElement)
    {
        if (messagesElement is null || messagesElement.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var messages = new List<ChatMessage>();
        foreach (var element in messagesElement.Value.EnumerateArray())
        {
            var role = element.TryGetProperty("role", out var roleElement)
                ? roleElement.GetString() ?? "user"
                : "user";
            var text = element.TryGetProperty("text", out var textElement)
                ? textElement.GetString() ?? string.Empty
                : string.Empty;

            messages.Add(new ChatMessage(new ChatRole(role), text));
        }

        return messages;
    }
}
