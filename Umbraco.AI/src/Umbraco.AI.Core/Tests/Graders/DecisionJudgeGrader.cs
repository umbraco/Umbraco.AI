#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental

using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Serialization;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for the Decision Judge test grader.
/// </summary>
public class DecisionJudgeGraderConfig
{
    /// <summary>
    /// The Decision profile ID to use for grading (optional, uses the default Decision profile
    /// if not specified).
    /// </summary>
    [AIField(
        Label = "Profile",
        Description = "AI Decision profile to use for grading (leave empty for default)",
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        EditorConfig = "[{\"alias\":\"capability\",\"value\":\"Decision\"}]",
        SortOrder = 1)]
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// The evaluation criteria.
    /// </summary>
    [AIField(
        Label = "Evaluation Criteria",
        Description = "What aspects to evaluate",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 2)]
    public string EvaluationCriteria { get; set; } = "Evaluate the quality, accuracy, and relevance of the response.";

    /// <summary>
    /// The threshold above (or equal to) which the output passes (0-1).
    /// </summary>
    [AIField(
        Label = "Pass Threshold",
        Description = "Minimum score to pass (0-1)",
        EditorUiAlias = "Umb.PropertyEditorUi.Slider",
        EditorConfig = "[{\"alias\":\"minVal\",\"value\":0},{\"alias\":\"maxVal\",\"value\":1},{\"alias\":\"step\",\"value\":0.1},{\"alias\":\"initVal1\",\"value\":0.7}]",
        SortOrder = 3)]
    [JsonConverter(typeof(SliderDoubleJsonConverter))]
    public double PassThreshold { get; set; } = 0.7;
}

/// <summary>
/// Test grader that asks a Decision profile a calibrated yes/no question to judge whether test
/// output meets the configured criteria.
/// </summary>
/// <remarks>
/// Hidden from the test grader listing endpoints while the Decision capability is experimental
/// and disabled (see <see cref="AIRequiresCapabilityAttribute"/>). It still runs if a saved test
/// already references it by id, and fails safe via the runtime check in <see cref="GradeAsync"/>
/// when the capability is off, rather than throwing.
/// </remarks>
[AITestGrader("decision-judge", "Decision Judge", Type = AIGraderType.ModelBased)]
[AIRequiresCapability(AICapability.Decision)]
public class DecisionJudgeGrader : AITestGraderBase<DecisionJudgeGraderConfig>
{
    // Fixed, not admin-configurable: the yes/no meaning must not depend on how an admin words their
    // evaluation criteria. See DecisionGuardrailEvaluator for the same reasoning.
    private const string TrueCriteria = "The content meets every criterion.";
    private const string FalseCriteria = "The content fails at least one criterion.";

    private readonly IAIDecisionService _decisionService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <inheritdoc />
    public override string Description => "Uses a calibrated AI decision to judge test outputs against criteria";

    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionJudgeGrader"/> class.
    /// </summary>
    public DecisionJudgeGrader(
        IAIDecisionService decisionService,
        IAIExperimentalFeatures experimentalFeatures,
        IAITestGraderInfrastructure infrastructure)
        : base(infrastructure)
    {
        _decisionService = decisionService;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <inheritdoc />
    public override async Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGraderConfig graderConfig,
        CancellationToken cancellationToken)
    {
        var config = ResolveConfig(graderConfig) ?? new DecisionJudgeGraderConfig();
        var actualValue = outcome.OutputValue ?? string.Empty;

        // Fail safe rather than run without a capability that isn't available. A saved test still
        // resolves this grader by id even while it's hidden from the listing endpoints (see
        // AIRequiresCapabilityAttribute's remarks), so this check is what actually stops it.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0,
                ActualValue = actualValue,
                ExpectedValue = config.EvaluationCriteria,
                FailureMessage = "Decision is turned off (Umbraco:AI:Experimental:Decision), so this grader can't run.",
            };
        }

        try
        {
            var question = new AIBinaryDecisionQuestion
            {
                Instructions =
                    $"Decide whether the content provided as context meets these criteria:\n\n{config.EvaluationCriteria}",
                Context = actualValue,
                TrueCriteria = TrueCriteria,
                FalseCriteria = FalseCriteria,
            };

            var response = await _decisionService.AskAsync(
                b =>
                {
                    b.WithAlias("test-decision-judge-grader");
                    if (config.ProfileId.HasValue)
                    {
                        b.WithProfile(config.ProfileId.Value);
                    }
                },
                question,
                cancellationToken);

            var passed = response.Probability >= config.PassThreshold;

            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = passed,
                Score = response.Probability,
                ActualValue = actualValue,
                ExpectedValue = config.EvaluationCriteria,
                FailureMessage = passed
                    ? null
                    : $"Score {response.Probability:F2} below threshold {config.PassThreshold:F2}",
                Metadata = JsonSerializer.SerializeToElement(new
                {
                    probability = response.Probability,
                    answer = response.Answer,
                    confidence = response.Confidence,
                    threshold = config.PassThreshold,
                    modelId = response.ModelId,
                }, Constants.DefaultJsonSerializerOptions),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A cancelled request isn't a judgment — let it propagate rather than reporting a
            // failed verdict, unlike the error handling below.
            throw;
        }
        catch (Exception ex)
        {
            // On error, fail for safety. The service's own exception messages already say what to
            // fix (e.g. no default Decision profile, or a profile that isn't a Decision profile).
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0,
                ActualValue = actualValue,
                ExpectedValue = config.EvaluationCriteria,
                FailureMessage = $"Decision judge evaluation failed: {ex.Message}",
            };
        }
    }
}
