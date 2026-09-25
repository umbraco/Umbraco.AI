#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Serialization;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Configuration for the Decision Safety Judge guardrail evaluator.
/// </summary>
public class DecisionGuardrailEvaluatorConfig
{
    /// <summary>
    /// The Decision profile ID to use for evaluation (optional, uses the default Decision profile
    /// if not specified).
    /// </summary>
    [AIField(
        Label = "Profile",
        Description = "AI Decision profile to use for evaluation (leave empty for default)",
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        EditorConfig = "[{\"alias\":\"capability\",\"value\":\"Decision\"}]",
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
    /// A lower probability means less safe content.
    /// </summary>
    [AIField(
        Label = "Safety Threshold",
        Description = "Content is flagged if the safety probability is below this threshold (0-1)",
        EditorUiAlias = "Umb.PropertyEditorUi.Slider",
        EditorConfig = "[{\"alias\":\"minVal\",\"value\":0},{\"alias\":\"maxVal\",\"value\":1},{\"alias\":\"step\",\"value\":0.1},{\"alias\":\"initVal1\",\"value\":0.7}]",
        SortOrder = 3)]
    [JsonConverter(typeof(SliderDoubleJsonConverter))]
    public double SafetyThreshold { get; set; } = 0.7;
}

/// <summary>
/// Guardrail evaluator that asks a Decision profile a calibrated yes/no question to judge whether
/// content is safe and compliant.
/// </summary>
/// <remarks>
/// Hidden from the guardrail evaluator listing endpoint while the Decision capability is
/// experimental and disabled (see <see cref="AIRequiresCapabilityAttribute"/>). It still runs if a
/// saved rule already references it by id, and fails safe (flagged) via the runtime check in
/// <see cref="EvaluateAsync"/> when the capability is off, rather than throwing.
/// </remarks>
[AIGuardrailEvaluator("decision-judge", "Decision Safety Judge", Type = AIGuardrailEvaluatorType.ModelBased)]
[AIRequiresCapability(AICapability.Decision)]
public class DecisionGuardrailEvaluator : AIGuardrailEvaluatorBase<DecisionGuardrailEvaluatorConfig>
{
    // Fixed, not admin-configurable: the yes/no meaning must not depend on how an admin words their
    // evaluation criteria. Admin-authored criteria often lean toward describing what's unsafe (e.g.
    // the default "Flag anything that could mislead users or cause harm"), which would otherwise
    // make it ambiguous whether "yes" means safe or unsafe.
    private const string TrueCriteria = "The content meets every criterion and is safe and compliant.";
    private const string FalseCriteria = "The content breaks at least one criterion, or is unsafe or non-compliant.";

    private readonly IAIDecisionService _decisionService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <inheritdoc />
    public override string Description => "Uses a calibrated AI decision to judge content for safety and compliance";

    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionGuardrailEvaluator"/> class.
    /// </summary>
    public DecisionGuardrailEvaluator(
        IAIDecisionService decisionService,
        IAIExperimentalFeatures experimentalFeatures,
        IAIGuardrailEvaluatorInfrastructure infrastructure)
        : base(infrastructure)
    {
        _decisionService = decisionService;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <inheritdoc />
    public override async Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = ResolveConfig(config) ?? new DecisionGuardrailEvaluatorConfig();

        // Fail safe rather than run without a capability that isn't available. A saved rule still
        // resolves this evaluator by id even while it's hidden from the listing endpoint (see
        // AIRequiresCapabilityAttribute's remarks), so this check is what actually stops it.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            return new AIGuardrailResult
            {
                EvaluatorId = Id,
                Flagged = true,
                Score = 0,
                Reason = "Decision is turned off (Umbraco:AI:Experimental:Decision), so this rule can't run.",
            };
        }

        try
        {
            var question = new AIBinaryDecisionQuestion
            {
                Instructions =
                    $"Decide whether the content provided as context is safe and compliant, based on these criteria:\n\n{evalConfig.EvaluationCriteria}",
                Context = content,
                TrueCriteria = TrueCriteria,
                FalseCriteria = FalseCriteria,
            };

            var response = await _decisionService.AskAsync(
                b =>
                {
                    b.WithAlias("guardrail-decision-evaluator");
                    if (evalConfig.ProfileId.HasValue)
                    {
                        b.WithProfile(evalConfig.ProfileId.Value);
                    }
                },
                question,
                cancellationToken);

            var flagged = response.Probability < evalConfig.SafetyThreshold;

            return new AIGuardrailResult
            {
                EvaluatorId = Id,
                Flagged = flagged,
                Score = response.Probability,
                Reason = flagged
                    ? $"Safety probability {response.Probability:F2} below threshold {evalConfig.SafetyThreshold:F2}"
                    : null,
                Metadata = JsonSerializer.SerializeToElement(new
                {
                    probability = response.Probability,
                    answer = response.Answer,
                    confidence = response.Confidence,
                    threshold = evalConfig.SafetyThreshold,
                    modelId = response.ModelId,
                }, Constants.DefaultJsonSerializerOptions),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A cancelled request isn't a judgment — let it propagate rather than reporting a
            // flagged/failed verdict, unlike the error handling below.
            throw;
        }
        catch (Exception ex)
        {
            // On error, flag for safety. The service's own exception messages already say what to
            // fix (e.g. no default Decision profile, or a profile that isn't a Decision profile).
            return new AIGuardrailResult
            {
                EvaluatorId = Id,
                Flagged = true,
                Score = 0,
                Reason = $"Decision safety evaluation failed: {ex.Message}",
            };
        }
    }
}
