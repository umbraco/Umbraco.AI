// Story DE-2 — Grade AI test output with a Decision Judge (AC1-AC21)
//
// Every scenario drives the real DecisionJudgeGrader through its public GradeAsync, with real
// schema-builder/model-resolver infrastructure (so config JSON goes through ResolveConfig exactly
// as in production) and a mocked IAIDecisionService/IAIExperimentalFeatures. The profile used by a
// call is read by invoking the captured Action on a real AIDecisionBuilder and reading its
// internal ProfileId (Umbraco.AI.Core grants InternalsVisibleTo this project).
#pragma warning disable UMBRACOAI_DECISION

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Core.Tests.Graders;

namespace Umbraco.AI.Tests.Unit.Tests.Graders;

public class DecisionJudgeGraderTests
{
    private const string Output = "hello";
    private const string Criteria = "Is polite";
    private const string DecisionFlag = "Umbraco:AI:Experimental:Decision";
    private static readonly Guid GraderConfigId = Guid.NewGuid();

    private sealed class Harness
    {
        public Mock<IAIDecisionService> DecisionService { get; } = new();

        public Mock<IAIExperimentalFeatures> Experimental { get; } = new();

        public DecisionJudgeGrader Grader { get; }

        public AIDecisionQuestion<AIBinaryDecisionResponse>? SentQuestion { get; private set; }

        public Action<AIDecisionBuilder>? SentConfigure { get; private set; }

        public Harness(bool decisionEnabled = true)
        {
            Experimental.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(decisionEnabled);
            Grader = new DecisionJudgeGrader(DecisionService.Object, Experimental.Object, Infrastructure());
        }

        public static IAITestGraderInfrastructure Infrastructure()
            => new AITestGraderInfrastructure(
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

        public Task<AITestGraderResult> GradeAsync(object? config, string? output = Output, CancellationToken cancellationToken = default)
            => Grader.GradeAsync(
                new AITestTranscript { RunId = Guid.NewGuid(), FinalOutput = JsonSerializer.SerializeToElement(output) },
                new AITestOutcome { OutputValue = output },
                new AITestGraderConfig
                {
                    Id = GraderConfigId,
                    GraderTypeId = "decision-judge",
                    Name = "Decision Judge",
                    Config = config is null ? null : JsonSerializer.SerializeToElement(config),
                },
                cancellationToken);

        public AITestGraderResult Grade(object? config, string? output = Output)
            => GradeAsync(config, output).GetAwaiter().GetResult();

        public AIDecisionBuilder ConfiguredBuilder()
        {
            var builder = new AIDecisionBuilder();
            SentConfigure!(builder);
            return builder;
        }
    }

    private static object ConfigWith(double threshold = 0.7, Guid? profileId = null)
        => new { profileId, evaluationCriteria = Criteria, passThreshold = threshold };

    #region Happy path

    public class GivenTheFlagOnAndAGoodAnswer
    {
        private readonly Harness _harness = new Harness().Answers(0.9);
        private readonly AITestGraderResult _result;

        public GivenTheFlagOnAndAGoodAnswer() => _result = _harness.Grade(ConfigWith(threshold: 0.7));

        [Fact]
        public void AsksExactlyOneBinaryQuestion()
            => _harness.DecisionService.Verify(
                s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()),
                Times.Once);

        [Fact]
        public void PutsTheOutputInContext() => _harness.SentQuestion!.Context.ShouldBe(Output);

        [Fact]
        public void PutsTheCriteriaInInstructions() => _harness.SentQuestion!.Instructions.ShouldContain(Criteria);

        [Fact]
        public void Passes() => _result.Passed.ShouldBeTrue();

        [Fact]
        public void HasNoFailureMessage() => _result.FailureMessage.ShouldBeNull();

        [Fact]
        public void ReportsTheOutputAsTheActualValue() => _result.ActualValue.ShouldBe(Output);

        [Fact]
        public void ReportsTheCriteriaAsTheExpectedValue() => _result.ExpectedValue.ShouldBe(Criteria);

        [Fact]
        public void ReportsTheGraderConfigId() => _result.GraderId.ShouldBe(GraderConfigId);

        [Fact]
        public void PinsTheTrueCriteriaSoItDoesNotDependOnAdminWording()
            => _harness.SentQuestion!.ShouldBeOfType<AIBinaryDecisionQuestion>().TrueCriteria.ShouldNotBeNullOrWhiteSpace();

        [Fact]
        public void PinsTheFalseCriteriaSoItDoesNotDependOnAdminWording()
            => _harness.SentQuestion!.ShouldBeOfType<AIBinaryDecisionQuestion>().FalseCriteria.ShouldNotBeNullOrWhiteSpace();
    }

    public class GivenANullOutput
    {
        private readonly Harness _harness = new Harness().Answers(0.9);

        public GivenANullOutput() => _harness.Grade(ConfigWith(), output: null);

        [Fact]
        public void SendsAnEmptyContext() => _harness.SentQuestion!.Context.ShouldBe(string.Empty);
    }

    public class GivenAConfiguredProfileId
    {
        private static readonly Guid ProfileId = Guid.NewGuid();
        private readonly Harness _harness = new Harness().Answers(0.9);

        public GivenAConfiguredProfileId() => _harness.Grade(ConfigWith(profileId: ProfileId));

        [Fact]
        public void AsksAgainstThatProfile() => _harness.ConfiguredBuilder().ProfileId.ShouldBe(ProfileId);
    }

    public class GivenNoConfiguredProfileId
    {
        private readonly Harness _harness = new Harness().Answers(0.9);

        public GivenNoConfiguredProfileId() => _harness.Grade(ConfigWith(profileId: null));

        [Fact]
        public void NamesNoProfileSoTheDefaultDecisionProfileApplies()
            => _harness.ConfiguredBuilder().ProfileId.ShouldBeNull();
    }

    public class GivenAnAnswerBelowTheThreshold
    {
        private readonly AITestGraderResult _result =
            new Harness().Answers(0.4, modelId: "jev-1").Grade(ConfigWith(threshold: 0.7));

        [Fact]
        public void Fails() => _result.Passed.ShouldBeFalse();

        [Fact]
        public void ScoresTheProbability() => _result.Score.ShouldBe(0.4);

        [Fact]
        public void FailureMessageNamesTheScore() => _result.FailureMessage!.ShouldContain("0.40");

        [Fact]
        public void FailureMessageNamesTheThreshold() => _result.FailureMessage!.ShouldContain("0.70");

        [Fact]
        public void MetadataCarriesTheProbability()
            => _result.Metadata!.Value.GetProperty("probability").GetDouble().ShouldBe(0.4);

        [Fact]
        public void MetadataCarriesTheAnswer()
            => _result.Metadata!.Value.GetProperty("answer").GetBoolean().ShouldBeFalse();

        [Fact]
        public void MetadataCarriesTheConfidence()
            => _result.Metadata!.Value.GetProperty("confidence").GetDouble().ShouldBe(0.6, tolerance: 1e-9);

        [Fact]
        public void MetadataCarriesTheThreshold()
            => _result.Metadata!.Value.GetProperty("threshold").GetDouble().ShouldBe(0.7);

        [Fact]
        public void MetadataCarriesTheModelId()
            => _result.Metadata!.Value.GetProperty("modelId").GetString().ShouldBe("jev-1");
    }

    public class GivenAnAnswerExactlyAtTheThreshold
    {
        [Fact]
        public void Passes()
            => new Harness().Answers(0.7).Grade(ConfigWith(threshold: 0.7)).Passed.ShouldBeTrue();
    }

    public class GivenNoConfig
    {
        [Fact]
        public void DefaultsTheThresholdTo70Percent()
            => new DecisionJudgeGraderConfig().PassThreshold.ShouldBe(0.7);

        [Fact]
        public void DefaultsTheCriteriaToTheLLMJudgeDefault()
            => new DecisionJudgeGraderConfig().EvaluationCriteria.ShouldBe(new LLMJudgeGraderConfig().EvaluationCriteria);

        [Fact]
        public void AsksWithTheDefaultCriteria()
        {
            var harness = new Harness().Answers(0.9);
            harness.Grade(config: null);

            harness.SentQuestion!.Instructions.ShouldContain(new LLMJudgeGraderConfig().EvaluationCriteria);
        }
    }

    public class GivenTheConfigSchema
    {
        private readonly IReadOnlyList<AIEditableModelField> _fields =
            new Harness().Grader.GetConfigSchema()!.Fields;

        [Fact]
        public void HasProfileCriteriaAndThresholdInThatOrder()
            => _fields.OrderBy(f => f.SortOrder).Select(f => f.PropertyName)
                .ShouldBe(["ProfileId", "EvaluationCriteria", "PassThreshold"]);

        [Fact]
        public void LabelsTheFields()
            => _fields.OrderBy(f => f.SortOrder).Select(f => f.Label)
                .ShouldBe(["Profile", "Evaluation Criteria", "Pass Threshold"]);

        [Fact]
        public void UsesProfilePickerTextAreaAndSliderEditors()
            => _fields.OrderBy(f => f.SortOrder).Select(f => f.EditorUiAlias)
                .ShouldBe(["Uai.PropertyEditorUi.ProfilePicker", "Umb.PropertyEditorUi.TextArea", "Umb.PropertyEditorUi.Slider"]);

        [Fact]
        public void LimitsTheProfilePickerToDecisionProfiles()
            => ((JsonElement)_fields.Single(f => f.PropertyName == "ProfileId").EditorConfig!)
                .EnumerateArray()
                .Single(e => e.GetProperty("alias").GetString() == "capability")
                .GetProperty("value").GetString()
                .ShouldBe("Decision");
    }

    public class GivenTheGraderCollection
    {
        [Fact]
        public void IsDiscoveredUnderTheDecisionJudgeId()
            => typeof(DecisionJudgeGrader).GetCustomAttribute<AITestGraderAttribute>()!.Id.ShouldBe("decision-judge");

        [Fact]
        public void ContainsItAsModelBased()
            => new AITestGraderCollection(() => [new Harness().Grader])
                .GetByType(AIGraderType.ModelBased)
                .ShouldContain(g => g.Id == "decision-judge");
    }

    #endregion

    #region Sad path

    public class GivenTheFlagOff
    {
        private readonly Harness _harness = new Harness(decisionEnabled: false).Answers(0.9);
        private readonly AITestGraderResult _result;

        public GivenTheFlagOff() => _result = _harness.Grade(ConfigWith());

        [Fact]
        public void Fails() => _result.Passed.ShouldBeFalse();

        [Fact]
        public void ScoresZero() => _result.Score.ShouldBe(0);

        [Fact]
        public void MakesNoDecisionCall() => _harness.DecisionService.Invocations.ShouldBeEmpty();

        [Fact]
        public void FailureMessageSaysDecisionIsTurnedOff() => _result.FailureMessage!.ShouldContain("Decision is turned off");

        [Fact]
        public void FailureMessageNamesTheFlag() => _result.FailureMessage!.ShouldContain(DecisionFlag);
    }

    public class GivenTheDecisionCallThrows
    {
        private readonly AITestGraderResult _result =
            new Harness().Throws(new InvalidOperationException("No default Decision profile")).Grade(ConfigWith());

        [Fact]
        public void Fails() => _result.Passed.ShouldBeFalse();

        [Fact]
        public void ScoresZero() => _result.Score.ShouldBe(0);

        [Fact]
        public void FailureMessageCarriesTheErrorMessage() => _result.FailureMessage!.ShouldContain("No default Decision profile");
    }

    public class GivenTheCallerCancels
    {
        [Fact]
        public async Task PropagatesTheCancellation()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var harness = new Harness().Throws(new OperationCanceledException(cts.Token));

            await Should.ThrowAsync<OperationCanceledException>(() => harness.GradeAsync(ConfigWith(), cancellationToken: cts.Token));
        }
    }

    #endregion
}
