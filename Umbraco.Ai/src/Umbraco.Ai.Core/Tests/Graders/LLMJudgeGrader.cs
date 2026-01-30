using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Model-based grader that uses an LLM to judge outputs against a rubric.
/// Provides flexible, subjective evaluation for complex criteria that are
/// hard to codify (tone, quality, appropriateness, etc.).
/// </summary>
/// <remarks>
/// This grader should be calibrated against human judgment to ensure consistency.
/// See Anthropic's eval framework documentation for best practices.
/// </remarks>
[AiTestGrader("llm-judge", "LLM as Judge")]
public class LLMJudgeGrader : IAiTestGrader
{
    private readonly IAiEditableModelSchemaBuilder _schemaBuilder;
    private readonly IAiChatService _chatService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMJudgeGrader"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder for generating UI configuration.</param>
    /// <param name="chatService">The chat service for calling the judge model.</param>
    public LLMJudgeGrader(
        IAiEditableModelSchemaBuilder schemaBuilder,
        IAiChatService chatService)
    {
        _schemaBuilder = schemaBuilder;
        _chatService = chatService;
    }

    /// <inheritdoc />
    public string Id => "llm-judge";

    /// <inheritdoc />
    public string Name => "LLM as Judge";

    /// <inheritdoc />
    public string Description => "Uses an AI model to grade outputs based on a rubric";

    /// <inheritdoc />
    public GraderType Type => GraderType.ModelBased;

    /// <inheritdoc />
    public Type? ConfigType => typeof(LLMJudgeGraderConfig);

    /// <inheritdoc />
    public AiEditableModelSchema? GetConfigSchema()
    {
        return _schemaBuilder.BuildForType<LLMJudgeGraderConfig>(Id);
    }

    /// <inheritdoc />
    public async Task<AiTestGraderResult> GradeAsync(
        AiTestTranscript transcript,
        AiTestOutcome outcome,
        AiTestGrader graderConfig,
        CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<LLMJudgeGraderConfig>(graderConfig.ConfigJson)
            ?? throw new InvalidOperationException("Failed to deserialize LLMJudgeGraderConfig");

        var actualValue = outcome.OutputValue;

        try
        {
            // Build the prompt for the judge
            var judgePrompt = BuildJudgePrompt(config, actualValue, transcript);

            // Call the judge model
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "You are an expert evaluator. Grade the output based on the provided rubric. Respond ONLY with valid JSON in the format: {\"score\": 1-5, \"reasoning\": \"explanation\"}"),
                new ChatMessage(ChatRole.User, judgePrompt)
            };

            var response = await _chatService.GetChatResponseAsync(
                config.JudgeProfileId,
                messages,
                null,
                cancellationToken);

            // Parse the judge's response (get text from last message)
            var lastMessage = response.Messages.LastOrDefault();
            var responseText = lastMessage?.Text ?? string.Empty;
            var judgeResponse = ParseJudgeResponse(responseText);

            // Determine pass/fail based on passing score
            var passed = judgeResponse.Score >= config.PassingScore;

            var result = new AiTestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = passed,
                Score = (float)judgeResponse.Score / 5.0f, // Normalize to 0-1
                ActualValue = actualValue,
                ExpectedValue = $"Score >= {config.PassingScore}",
                FailureMessage = passed
                    ? null
                    : $"Judge scored {judgeResponse.Score}/5 (below threshold {config.PassingScore}): {judgeResponse.Reasoning}",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    score = judgeResponse.Score,
                    reasoning = judgeResponse.Reasoning,
                    passingScore = config.PassingScore
                })
            };

            return result;
        }
        catch (Exception ex)
        {
            var result = new AiTestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0,
                ActualValue = actualValue,
                ExpectedValue = $"Score >= {config.PassingScore}",
                FailureMessage = $"Failed to get judge evaluation: {ex.Message}"
            };

            return result;
        }
    }

    private static string BuildJudgePrompt(
        LLMJudgeGraderConfig config,
        string output,
        AiTestTranscript transcript)
    {
        var promptParts = new List<string>
        {
            "# Grading Rubric",
            config.Rubric,
            "",
            "# Output to Grade",
            output
        };

        if (config.IncludeTranscript && !string.IsNullOrEmpty(transcript.MessagesJson))
        {
            promptParts.Add("");
            promptParts.Add("# Full Conversation Transcript");
            promptParts.Add(transcript.MessagesJson);
        }

        promptParts.Add("");
        promptParts.Add("Grade the output on a scale of 1-5 based on the rubric.");
        promptParts.Add("Respond with JSON: {\"score\": 1-5, \"reasoning\": \"explanation\"}");

        return string.Join(Environment.NewLine, promptParts);
    }

    private static (int Score, string Reasoning) ParseJudgeResponse(string response)
    {
        try
        {
            // Try to extract JSON from the response (may be wrapped in markdown code blocks)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var judgeResponse = JsonSerializer.Deserialize<JudgeResponse>(jsonString);

                if (judgeResponse != null)
                {
                    return (judgeResponse.Score, judgeResponse.Reasoning ?? "No reasoning provided");
                }
            }

            // Fallback: try to parse the entire response
            var fullResponse = JsonSerializer.Deserialize<JudgeResponse>(response);
            if (fullResponse != null)
            {
                return (fullResponse.Score, fullResponse.Reasoning ?? "No reasoning provided");
            }

            throw new JsonException("Could not parse judge response");
        }
        catch (JsonException)
        {
            // If we can't parse JSON, default to failing score
            return (1, $"Failed to parse judge response: {response}");
        }
    }

    private class JudgeResponse
    {
        public int Score { get; set; }
        public string? Reasoning { get; set; }
    }
}
