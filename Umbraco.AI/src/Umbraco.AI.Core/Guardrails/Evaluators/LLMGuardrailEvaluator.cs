using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Configuration for the LLM guardrail evaluator.
/// </summary>
public class LLMGuardrailEvaluatorConfig
{
    /// <summary>
    /// The profile ID to use for LLM evaluation (optional, uses default if not specified).
    /// </summary>
    [AIField(
        Label = "Judge Profile ID",
        Description = "AI profile to use for evaluation (leave empty for default)",
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        SortOrder = 1)]
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// The evaluation criteria.
    /// </summary>
    [AIField(
        Label = "Evaluation Criteria",
        Description = "What aspects to evaluate for safety and compliance",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 2)]
    public string EvaluationCriteria { get; set; } =
        "Evaluate the content for factual accuracy, harmful content, misinformation, and brand compliance. Flag anything that could mislead users or cause harm.";

    /// <summary>
    /// The threshold below which content is flagged (0-1).
    /// A lower score means less safe content.
    /// </summary>
    [AIField(
        Label = "Safety Threshold",
        Description = "Content is flagged if the safety score is below this threshold (0-1)",
        EditorUiAlias = "Umb.PropertyEditorUi.Slider",
        EditorConfig = "[{\"alias\":\"minValue\",\"value\":0},{\"alias\":\"maxValue\",\"value\":1},{\"alias\":\"step\",\"value\":0.1}]",
        SortOrder = 3)]
    public double SafetyThreshold { get; set; } = 0.7;
}

/// <summary>
/// Guardrail evaluator that uses an LLM to evaluate content for safety and compliance.
/// </summary>
[AIGuardrailEvaluator("llm-judge", "LLM Safety Judge", Type = AIGuardrailEvaluatorType.ModelBased)]
public class LLMGuardrailEvaluator : AIGuardrailEvaluatorBase<LLMGuardrailEvaluatorConfig>
{
    private readonly IAIChatService _chatService;
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;

    /// <inheritdoc />
    public override string Description => "Uses an LLM to evaluate content for safety, misinformation, and compliance";

    public LLMGuardrailEvaluator(
        IAIChatService chatService,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        _chatService = chatService;
        _runtimeContextAccessor = runtimeContextAccessor;
    }

    /// <inheritdoc />
    public override async Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = config.Deserialize<LLMGuardrailEvaluatorConfig>() ?? new LLMGuardrailEvaluatorConfig();

        // Set the guardrail evaluation flag to prevent infinite recursion
        _runtimeContextAccessor.Context?.SetValue(Constants.ContextKeys.IsGuardrailEvaluation, true);

        try
        {
            var judgmentPrompt = $$"""
You are an AI content safety evaluator. Evaluate the following content based on these criteria:

{{evalConfig.EvaluationCriteria}}

Content to evaluate:
{{content}}

Provide your evaluation in the following JSON format:
{
  "safetyScore": <number between 0 and 1, where 1 is perfectly safe>,
  "reasoning": "<explanation of your evaluation>",
  "concerns": ["<concern 1>", "<concern 2>"]
}

Be objective and consistent. A score of 1.0 means completely safe, 0.0 means highly unsafe.
""";

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, judgmentPrompt)
            };

            var response = await _chatService.GetChatResponseAsync(chat =>
            {
                chat.WithAlias("guardrail-llm-evaluator");
                if (evalConfig.ProfileId.HasValue)
                {
                    chat.WithProfile(evalConfig.ProfileId.Value);
                }
            }, messages, cancellationToken);

            // Parse judgment
            var judgmentText = response.Text ?? string.Empty;
            var jsonStart = judgmentText.IndexOf('{');
            var jsonEnd = judgmentText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = judgmentText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                var safetyScore = root.TryGetProperty("safetyScore", out var scoreElement)
                    ? scoreElement.GetDouble()
                    : 0.0;

                var reasoning = root.TryGetProperty("reasoning", out var reasoningElement)
                    ? reasoningElement.GetString()
                    : null;

                var flagged = safetyScore < evalConfig.SafetyThreshold;

                return new AIGuardrailResult
                {
                    EvaluatorId = Id,
                    Flagged = flagged,
                    Score = safetyScore,
                    Reason = flagged
                        ? $"Safety score {safetyScore:F2} below threshold {evalConfig.SafetyThreshold:F2}: {reasoning}"
                        : null,
                    Metadata = JsonSerializer.SerializeToElement(new
                    {
                        safetyScore,
                        reasoning,
                        threshold = evalConfig.SafetyThreshold,
                        fullJudgment = judgmentText
                    }, Constants.DefaultJsonSerializerOptions)
                };
            }

            // Failed to parse — treat as flagged for safety
            return new AIGuardrailResult
            {
                EvaluatorId = Id,
                Flagged = true,
                Score = 0.0,
                Reason = "Failed to parse safety judgment from LLM",
                Metadata = JsonSerializer.SerializeToElement(
                    new { rawResponse = judgmentText }, Constants.DefaultJsonSerializerOptions)
            };
        }
        catch (Exception ex)
        {
            // On error, flag for safety
            return new AIGuardrailResult
            {
                EvaluatorId = Id,
                Flagged = true,
                Score = 0.0,
                Reason = $"LLM safety evaluation failed: {ex.Message}"
            };
        }
        finally
        {
            // Reset the guardrail evaluation flag
            _runtimeContextAccessor.Context?.SetValue(Constants.ContextKeys.IsGuardrailEvaluation, false);
        }
    }
}
