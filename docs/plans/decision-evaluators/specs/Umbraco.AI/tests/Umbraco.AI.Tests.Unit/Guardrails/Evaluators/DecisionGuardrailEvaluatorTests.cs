// Story DE-1 — Guard AI output with a Decision Safety Judge (AC1-AC24)
//
// STAGED SPEC (task T2). Every scenario drives the real DecisionGuardrailEvaluator through its
// public EvaluateAsync, with real schema-builder/model-resolver infrastructure (so config JSON goes
// through ResolveConfig exactly as in production) and a mocked IAIDecisionService/IAIExperimentalFeatures.
//
// Assumed production surface (ARCHITECTURE "Extension points" / "Runtime behavior"):
//   - ctor DecisionGuardrailEvaluator(IAIDecisionService, IAIExperimentalFeatures,
//     IAIGuardrailEvaluatorInfrastructure). If the builder orders the parameters differently, fix
//     Harness's constructor call only.
//   - DecisionGuardrailEvaluatorConfig { Guid? ProfileId; string EvaluationCriteria; double SafetyThreshold }.
//   - The Decision call goes through
//     IAIDecisionService.AskAsync<AIBinaryDecisionResponse>(Action<AIDecisionBuilder>, question, ct).
//     The profile used is read by invoking the captured Action on a real AIDecisionBuilder and
//     reading its internal ProfileId (Umbraco.AI.Core grants InternalsVisibleTo this project).
//   - The flag is checked with IAIExperimentalFeatures.IsCapabilityEnabled(AICapability.Decision).
#pragma warning disable UMBRACOAI_DECISION

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Tests.Unit.Guardrails.Evaluators;

public class DecisionGuardrailEvaluatorTests
{
    private const string Content = "some content";
    private const string Criteria = "No profanity";
    private const string DecisionFlag = "Umbraco:AI:Experimental:Decision";

    private sealed class Harness
    {
        public Mock<IAIDecisionService> DecisionService { get; } = new();

        public Mock<IAIExperimentalFeatures> Experimental { get; } = new();

        public DecisionGuardrailEvaluator Evaluator { get; }

        public AIDecisionQuestion<AIBinaryDecisionResponse>? SentQuestion { get; private set; }

        public Action<AIDecisionBuilder>? SentConfigure { get; private set; }

        public Harness(bool decisionEnabled = true)
        {
            Experimental.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(decisionEnabled);
            Evaluator = new DecisionGuardrailEvaluator(DecisionService.Object, Experimental.Object, Infrastructure());
        }

        public static IAIGuardrailEvaluatorInfrastructure Infrastructure()
            => new AIGuardrailEvaluatorInfrastructure(
                new AIEditableModelSchemaBuilder(),
                new AIEditableModelResolver(new ConfigurationBuilder().Build()));

        public Harness Answers(double probability, string? modelId = null)
        {
            DecisionService
                .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
                // Callback generics must match AskAsync<TResponse>'s declared parameter types.
                .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIBinaryDecisionResponse>, CancellationToken>((b, q, _) =>
                {
                    SentConfigure = b;
                    SentQuestion = q;
                })
                .ReturnsAsync(new AIBinaryDecisionResponse { Probability = probability, ModelId = modelId });
            return this;
        }

        public Harness Throws(Exception exception)
        {
            DecisionService
                .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            return this;
        }

        public Task<AIGuardrailResult> EvaluateAsync(object? config, CancellationToken cancellationToken = default)
            => Evaluator.EvaluateAsync(
                Content,
                Array.Empty<ChatMessage>(),
                new AIGuardrailConfig { Config = config is null ? null : JsonSerializer.SerializeToElement(config) },
                cancellationToken);

        public AIGuardrailResult Evaluate(object? config) => EvaluateAsync(config).GetAwaiter().GetResult();

        public AIDecisionBuilder ConfiguredBuilder()
        {
            var builder = new AIDecisionBuilder();
            SentConfigure!(builder);
            return builder;
        }
    }

    private static object ConfigWith(double threshold = 0.7, Guid? profileId = null)
        => new { profileId, evaluationCriteria = Criteria, safetyThreshold = threshold };

    #region Happy path

    public class GivenTheFlagOnAndASafeAnswer
    {
        private readonly Harness _harness = new Harness().Answers(0.9);
        private readonly AIGuardrailResult _result;

        public GivenTheFlagOnAndASafeAnswer() => _result = _harness.Evaluate(ConfigWith(threshold: 0.7));

        [Fact(Skip = "Pending T2")]
        public void AsksExactlyOneBinaryQuestion()
            => _harness.DecisionService.Verify(
                s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()),
                Times.Once);

        [Fact(Skip = "Pending T2")]
        public void PutsTheContentInContext() => _harness.SentQuestion!.Context.ShouldBe(Content);

        [Fact(Skip = "Pending T2")]
        public void PutsTheCriteriaInInstructions() => _harness.SentQuestion!.Instructions.ShouldContain(Criteria);

        [Fact(Skip = "Pending T2")]
        public void DoesNotFlag() => _result.Flagged.ShouldBeFalse();

        [Fact(Skip = "Pending T2")]
        public void HasNoReason() => _result.Reason.ShouldBeNull();

        [Fact(Skip = "Pending T2")]
        public void ReportsTheDecisionJudgeEvaluatorId() => _result.EvaluatorId.ShouldBe("decision-judge");
    }

    public class GivenAConfiguredProfileId
    {
        private static readonly Guid ProfileId = Guid.NewGuid();
        private readonly Harness _harness = new Harness().Answers(0.9);

        public GivenAConfiguredProfileId() => _harness.Evaluate(ConfigWith(profileId: ProfileId));

        [Fact(Skip = "Pending T2")]
        public void AsksAgainstThatProfile() => _harness.ConfiguredBuilder().ProfileId.ShouldBe(ProfileId);
    }

    public class GivenNoConfiguredProfileId
    {
        private readonly Harness _harness = new Harness().Answers(0.9);

        public GivenNoConfiguredProfileId() => _harness.Evaluate(ConfigWith(profileId: null));

        [Fact(Skip = "Pending T2")]
        public void NamesNoProfileSoTheDefaultDecisionProfileApplies()
            => _harness.ConfiguredBuilder().ProfileId.ShouldBeNull();
    }

    public class GivenAnAnswerBelowTheThreshold
    {
        private readonly AIGuardrailResult _result =
            new Harness().Answers(0.4, modelId: "jev-1").Evaluate(ConfigWith(threshold: 0.7));

        [Fact(Skip = "Pending T2")]
        public void Flags() => _result.Flagged.ShouldBeTrue();

        [Fact(Skip = "Pending T2")]
        public void ScoresTheProbability() => _result.Score.ShouldBe(0.4);

        [Fact(Skip = "Pending T2")]
        public void ReasonNamesTheProbability() => _result.Reason.ShouldContain("0.40");

        [Fact(Skip = "Pending T2")]
        public void ReasonNamesTheThreshold() => _result.Reason.ShouldContain("0.70");

        [Fact(Skip = "Pending T2")]
        public void MetadataCarriesTheProbability()
            => _result.Metadata!.Value.GetProperty("probability").GetDouble().ShouldBe(0.4);

        [Fact(Skip = "Pending T2")]
        public void MetadataCarriesTheAnswer()
            => _result.Metadata!.Value.GetProperty("answer").GetBoolean().ShouldBeFalse();

        [Fact(Skip = "Pending T2")]
        public void MetadataCarriesTheConfidence()
            => _result.Metadata!.Value.GetProperty("confidence").GetDouble().ShouldBe(0.6, tolerance: 1e-9);

        [Fact(Skip = "Pending T2")]
        public void MetadataCarriesTheThreshold()
            => _result.Metadata!.Value.GetProperty("threshold").GetDouble().ShouldBe(0.7);

        [Fact(Skip = "Pending T2")]
        public void MetadataCarriesTheModelId()
            => _result.Metadata!.Value.GetProperty("modelId").GetString().ShouldBe("jev-1");
    }

    public class GivenAnAnswerExactlyAtTheThreshold
    {
        [Fact(Skip = "Pending T2")]
        public void DoesNotFlag()
            => new Harness().Answers(0.7).Evaluate(ConfigWith(threshold: 0.7)).Flagged.ShouldBeFalse();
    }

    public class GivenNoConfig
    {
        [Fact(Skip = "Pending T2")]
        public void DefaultsTheThresholdTo70Percent()
            => new DecisionGuardrailEvaluatorConfig().SafetyThreshold.ShouldBe(0.7);

        [Fact(Skip = "Pending T2")]
        public void DefaultsTheCriteriaToTheLLMSafetyJudgeDefault()
            => new DecisionGuardrailEvaluatorConfig().EvaluationCriteria.ShouldBe(new LLMGuardrailEvaluatorConfig().EvaluationCriteria);

        [Fact(Skip = "Pending T2")]
        public void AsksWithTheDefaultCriteria()
        {
            var harness = new Harness().Answers(0.9);
            harness.Evaluate(config: null);

            harness.SentQuestion!.Instructions.ShouldContain(new LLMGuardrailEvaluatorConfig().EvaluationCriteria);
        }
    }

    public class GivenTheConfigSchema
    {
        private readonly IReadOnlyList<AIEditableModelField> _fields =
            new Harness().Evaluator.GetConfigSchema()!.Fields;

        [Fact(Skip = "Pending T2")]
        public void HasProfileCriteriaAndThresholdInThatOrder()
            => _fields.OrderBy(f => f.SortOrder).Select(f => f.PropertyName)
                .ShouldBe(["ProfileId", "EvaluationCriteria", "SafetyThreshold"]);

        [Fact(Skip = "Pending T2")]
        public void LabelsTheFields()
            => _fields.OrderBy(f => f.SortOrder).Select(f => f.Label)
                .ShouldBe(["Profile", "Evaluation Criteria", "Safety Threshold"]);

        [Fact(Skip = "Pending T2")]
        public void UsesProfilePickerTextAreaAndSliderEditors()
            => _fields.OrderBy(f => f.SortOrder).Select(f => f.EditorUiAlias)
                .ShouldBe(["Uai.PropertyEditorUi.ProfilePicker", "Umb.PropertyEditorUi.TextArea", "Umb.PropertyEditorUi.Slider"]);

        [Fact(Skip = "Pending T2")]
        public void LimitsTheProfilePickerToDecisionProfiles()
            => ((JsonElement)_fields.Single(f => f.PropertyName == "ProfileId").EditorConfig!)
                .EnumerateArray()
                .Single(e => e.GetProperty("alias").GetString() == "capability")
                .GetProperty("value").GetString()
                .ShouldBe("Decision");
    }

    public class GivenTheEvaluatorCollection
    {
        [Fact(Skip = "Pending T2")]
        public void IsDiscoveredUnderTheDecisionJudgeId()
            => typeof(DecisionGuardrailEvaluator).GetCustomAttribute<AIGuardrailEvaluatorAttribute>()!.Id.ShouldBe("decision-judge");

        [Fact(Skip = "Pending T2")]
        public void ContainsItAsModelBased()
            => new AIGuardrailEvaluatorCollection(() => [new Harness().Evaluator])
                .GetByType(AIGuardrailEvaluatorType.ModelBased)
                .ShouldContain(e => e.Id == "decision-judge");
    }

    #endregion

    #region Sad path

    public class GivenTheFlagOff
    {
        private readonly Harness _harness = new Harness(decisionEnabled: false).Answers(0.9);
        private readonly AIGuardrailResult _result;

        public GivenTheFlagOff() => _result = _harness.Evaluate(ConfigWith());

        [Fact(Skip = "Pending T2")]
        public void Flags() => _result.Flagged.ShouldBeTrue();

        [Fact(Skip = "Pending T2")]
        public void MakesNoDecisionCall() => _harness.DecisionService.Invocations.ShouldBeEmpty();

        [Fact(Skip = "Pending T2")]
        public void ReasonSaysDecisionIsTurnedOff() => _result.Reason.ShouldContain("Decision is turned off");

        [Fact(Skip = "Pending T2")]
        public void ReasonNamesTheFlag() => _result.Reason.ShouldContain(DecisionFlag);

        [Fact(Skip = "Pending T2")]
        public void ScoresZero() => _result.Score.ShouldBe(0);
    }

    public class GivenTheDecisionCallThrows
    {
        private readonly AIGuardrailResult _result =
            new Harness().Throws(new InvalidOperationException("No default Decision profile")).Evaluate(ConfigWith());

        [Fact(Skip = "Pending T2")]
        public void Flags() => _result.Flagged.ShouldBeTrue();

        [Fact(Skip = "Pending T2")]
        public void ReasonCarriesTheErrorMessage() => _result.Reason.ShouldContain("No default Decision profile");

        [Fact(Skip = "Pending T2")]
        public void ScoresZero() => _result.Score.ShouldBe(0);
    }

    public class GivenTheCallerCancels
    {
        [Fact(Skip = "Pending T2")]
        public async Task PropagatesTheCancellation()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var harness = new Harness().Throws(new OperationCanceledException(cts.Token));

            await Should.ThrowAsync<OperationCanceledException>(() => harness.EvaluateAsync(ConfigWith(), cts.Token));
        }
    }

    #endregion
}
