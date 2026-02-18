using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for LLM judge grader.
/// </summary>
public class LLMJudgeGraderConfig
{
    /// <summary>
    /// The profile ID to use for LLM judgment (optional, uses default if not specified).
    /// </summary>
    [AIField(
        Label = "Judge Profile ID",
        Description = "AI profile to use for judgment (leave empty for default)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 1)]
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Evaluation criteria for the judge.
    /// </summary>
    [AIField(
        Label = "Evaluation Criteria",
        Description = "What aspects to evaluate",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 2)]
    public string EvaluationCriteria { get; set; } = "Evaluate the quality, accuracy, and relevance of the response.";

    /// <summary>
    /// Pass threshold (0-1).
    /// </summary>
    [AIField(
        Label = "Pass Threshold",
        Description = "Minimum score to pass (0-1)",
        EditorUiAlias = "Umb.PropertyEditorUi.Slider",
        EditorConfig = "[{\"alias\":\"minValue\",\"value\":0},{\"alias\":\"maxValue\",\"value\":1},{\"alias\":\"step\",\"value\":0.1}]",
        SortOrder = 3)]
    public double PassThreshold { get; set; } = 0.7;
}

/// <summary>
/// Grader that uses an LLM to evaluate test outputs.
/// Provides flexible, rubric-based evaluation for subjective criteria.
/// </summary>
[AITestGrader("llm-judge", "LLM Judge", Type = AIGraderType.ModelBased)]
public class LLMJudgeGrader : AITestGraderBase
{
    private readonly IAIChatService _chatService;

    /// <inheritdoc />
    public override string Description => "Uses an LLM to evaluate test outputs based on criteria";

    /// <inheritdoc />
    public override Type? ConfigType => typeof(LLMJudgeGraderConfig);

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMJudgeGrader"/> class.
    /// </summary>
    public LLMJudgeGrader(
        IAIChatService chatService,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        _chatService = chatService;
    }

    /// <inheritdoc />
    public override async Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGrader graderConfig,
        CancellationToken cancellationToken)
    {
        // Deserialize configuration
        var config = string.IsNullOrWhiteSpace(graderConfig.ConfigJson)
            ? new LLMJudgeGraderConfig()
            : JsonSerializer.Deserialize<LLMJudgeGraderConfig>(graderConfig.ConfigJson)
                ?? new LLMJudgeGraderConfig();

        // Extract actual value from final output
        var actualValue = ExtractContentFromOutput(outcome.OutputValue);

        // Build judgment prompt
        var judgmentPrompt = $$"""
You are an AI test evaluator. Evaluate the following output based on these criteria:

{{config.EvaluationCriteria}}

Output to evaluate:
{{actualValue}}

Provide your evaluation in the following JSON format:
{
  "score": <number between 0 and 1>,
  "reasoning": "<explanation of your evaluation>",
  "strengths": ["<strength 1>", "<strength 2>"],
  "weaknesses": ["<weakness 1>", "<weakness 2>"]
}

Be objective and consistent in your evaluation.
""";

        try
        {
            // Get chat response from LLM judge
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, judgmentPrompt)
            };

            ChatResponse response;
            if (config.ProfileId.HasValue)
            {
                response = await _chatService.GetChatResponseAsync(
                    config.ProfileId.Value,
                    messages,
                    null,
                    cancellationToken);
            }
            else
            {
                response = await _chatService.GetChatResponseAsync(
                    messages,
                    null,
                    cancellationToken);
            }

            // Parse judgment result
            var judgmentText = response.Messages.LastOrDefault()?.Text ?? string.Empty;
            
            // Extract JSON from response (handle markdown code blocks)
            var jsonStart = judgmentText.IndexOf('{');
            var jsonEnd = judgmentText.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = judgmentText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                var score = root.TryGetProperty("score", out var scoreElement)
                    ? scoreElement.GetDouble()
                    : 0.0;

                var reasoning = root.TryGetProperty("reasoning", out var reasoningElement)
                    ? reasoningElement.GetString()
                    : null;

                var passed = score >= config.PassThreshold;

                return new AITestGraderResult
                {
                    GraderId = graderConfig.Id,
                    Passed = passed,
                    Score = score,
                    ActualValue = actualValue,
                    ExpectedValue = config.EvaluationCriteria,
                    FailureMessage = passed ? null : $"Score {score:F2} below threshold {config.PassThreshold:F2}",
                    MetadataJson = JsonSerializer.Serialize(new
                    {
                        reasoning,
                        threshold = config.PassThreshold,
                        fullJudgment = judgmentText
                    })
                };
            }

            // Failed to parse JSON from response
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                ActualValue = actualValue,
                ExpectedValue = config.EvaluationCriteria,
                FailureMessage = "Failed to parse judgment response from LLM",
                MetadataJson = JsonSerializer.Serialize(new { rawResponse = judgmentText })
            };
        }
        catch (Exception ex)
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                ActualValue = actualValue,
                ExpectedValue = config.EvaluationCriteria,
                FailureMessage = $"LLM judge evaluation failed: {ex.Message}"
            };
        }
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
