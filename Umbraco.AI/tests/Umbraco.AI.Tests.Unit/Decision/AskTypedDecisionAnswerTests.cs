#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-1 — Ask typed decisions from C# (AC1-AC4c, AC13): the reworked answer shapes.
//
// ASSUMPTIONS (T29 builder confirms/adjusts, keeping each test's behavior and single assertion):
// - FakeDecisionClient takes Func<AIDecisionRequest, AIDecisionResponse> and records
//   ReceivedRequests as (AIDecisionRequest Request, AIDecisionOptions? Options).
// - IAIDecisionService.AskAsync<TAnswer>(string profileAlias, AIDecisionQuestion<TAnswer> question,
//   string? state = null, AIDecisionOptions? options = null, CancellationToken ct = default)
//   returns AIDecisionResponse<TAnswer>.
// - AskAsync assigns an internal id when Question.Id is null; the fake answers whatever id it was sent.
// Replaces the scenarios in AskTypedDecisionTests that assert the old *DecisionResponse shapes.

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// Every spec goes through the real <see cref="AIDecisionService"/> + <see cref="AIDecisionClientFactory"/>
/// pipeline (see <see cref="DecisionPipelineHarness"/>); only the provider client is faked.
/// </summary>
public class AskTypedDecisionAnswerTests
{
    /// <summary>Answers the request's only question with <paramref name="answer"/>, keyed by whatever id it carries.</summary>
    private static Func<AIDecisionRequest, AIDecisionResponse> AnswerFirst(
        AIDecisionAnswer answer,
        string? modelId = "jev-1.13.0",
        UsageDetails? usage = null)
        => request => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { [request.Questions[0].Id!] = answer },
            ModelId = modelId,
            Usage = usage ?? new UsageDetails { InputTokenCount = 42, OutputTokenCount = 1 },
        };

    private static AIDecisionQuestion<AIScoreDecisionAnswer> Score(params string[] levels) => new AIScoreDecisionQuestion
    {
        Instructions = "Rate it",
        Levels = levels.Select(l => new AIDecisionScoreLevel(l)).ToList(),
    };

    #region Happy path

    public class GivenABinaryQuestionAndAProviderAnsweringPoint97
    {
        private readonly AIDecisionResponse<AIBinaryDecisionAnswer> _response;

        public GivenABinaryQuestionAndAProviderAnsweringPoint97()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(
                AnswerFirst(new AIBinaryDecisionAnswer { TrueProbability = 0.97 })));
            _response = harness.Service.AskAsync(
                    DecisionPipelineHarness.ProfileAlias,
                    new AIBinaryDecisionQuestion { Instructions = "Is this spam?" },
                    state: "Buy cheap watches")
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsTheTrueProbability() => _response.Answer.TrueProbability.ShouldBe(0.97);

        [Fact]
        public void IsTrueAtTheDefaultCutOff() => _response.Answer.IsTrue().ShouldBeTrue();

        [Fact]
        public void CarriesTheModelId() => _response.ModelId.ShouldBe("jev-1.13.0");

        [Fact]
        public void CarriesTheUsage() => _response.Usage!.InputTokenCount.ShouldBe(42);
    }

    public class GivenABinaryQuestionSentWithState
    {
        [Fact]
        public async Task SendsTheStateOnTheRequest()
        {
            var client = new FakeDecisionClient(AnswerFirst(new AIBinaryDecisionAnswer { TrueProbability = 0.9 }));
            var harness = new DecisionPipelineHarness(client);

            await harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = "Is this spam?" },
                state: "Buy cheap watches");

            client.ReceivedRequests.Single().Request.State.ShouldBe("Buy cheap watches");
        }
    }

    public class GivenAProviderAnsweringPoint7
    {
        private readonly AIDecisionResponse<AIBinaryDecisionAnswer> _response;

        public GivenAProviderAnsweringPoint7()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(
                AnswerFirst(new AIBinaryDecisionAnswer { TrueProbability = 0.7 })));
            _response = harness.Service.AskAsync(
                    DecisionPipelineHarness.ProfileAlias,
                    new AIBinaryDecisionQuestion { Instructions = "Is this spam?" })
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void IsTrueAtTheDefaultCutOff() => _response.Answer.IsTrue().ShouldBeTrue();

        [Fact]
        public void IsFalseAtACallerChosenCutOffOf90() => _response.Answer.IsTrue(0.9).ShouldBeFalse();
    }

    public class GivenAChoiceQuestionAndAProviderPickingB
    {
        private readonly AIDecisionResponse<AIChoiceDecisionAnswer> _response;

        public GivenAChoiceQuestionAndAProviderPickingB()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(AnswerFirst(new AIChoiceDecisionAnswer
            {
                Choice = "b",
                Confidence = 0.9,
                Probabilities = new Dictionary<string, double> { ["a"] = 0.1, ["b"] = 0.9 },
            })));
            _response = harness.Service.AskAsync(
                    DecisionPipelineHarness.ProfileAlias,
                    new AIChoiceDecisionQuestion
                    {
                        Instructions = "Pick one",
                        Options = [new AIDecisionOption("a"), new AIDecisionOption("b")],
                    })
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsTheChosenKey() => _response.Answer.Choice.ShouldBe("b");

        [Fact]
        public void ReturnsTheConfidence() => _response.Answer.Confidence.ShouldBe(0.9);

        [Fact]
        public void ReturnsProbabilitiesForExactlyTheOptions()
            => _response.Answer.Probabilities.Keys.ShouldBe(["a", "b"], ignoreOrder: true);
    }

    public class GivenAScoreQuestionAndAProviderScoring1Point8
    {
        private readonly AIDecisionResponse<AIScoreDecisionAnswer> _response;

        public GivenAScoreQuestionAndAProviderScoring1Point8()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(AnswerFirst(new AIScoreDecisionAnswer
            {
                Score = 1.8,
                Confidence = 0.8,
                Probabilities = new Dictionary<int, double> { [0] = 0.05, [1] = 0.1, [2] = 0.85 },
            })));
            _response = harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, Score("poor", "ok", "good"))
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsTheScore() => _response.Answer.Score.ShouldBe(1.8);

        [Fact]
        public void KeysProbabilitiesByLevelPosition()
            => _response.Answer.Probabilities.Keys.ShouldBe([0, 1, 2], ignoreOrder: true);
    }

    public class GivenLevelsSharingTheSameWording
    {
        [Fact]
        public async Task KeepsOneProbabilityPerLevel()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(AnswerFirst(new AIScoreDecisionAnswer
            {
                Score = 1.2,
                Probabilities = new Dictionary<int, double> { [0] = 0.3, [1] = 0.3, [2] = 0.4 },
            })));

            var response = await harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, Score("ok", "ok", "great"));

            response.Answer.Probabilities.Count.ShouldBe(3);
        }
    }

    public class GivenAProviderReturningNoConfidence
    {
        [Fact]
        public async Task LeavesConfidenceNull()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(AnswerFirst(new AIChoiceDecisionAnswer
            {
                Choice = "a",
                Probabilities = new Dictionary<string, double> { ["a"] = 0.6, ["b"] = 0.4 },
            })));

            var response = await harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIChoiceDecisionQuestion
                {
                    Instructions = "Pick one",
                    Options = [new AIDecisionOption("a"), new AIDecisionOption("b")],
                });

            response.Answer.Confidence.ShouldBeNull();
        }
    }

    #endregion

    #region Sad path

    public class GivenAProviderReturningTheWrongAnswerKind
    {
        [Fact]
        public async Task ThrowsAIProviderException()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(AnswerFirst(new AIChoiceDecisionAnswer
            {
                Choice = "a",
                Probabilities = new Dictionary<string, double> { ["a"] = 1.0 },
            })));

            var act = () => harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = "Is this spam?" });

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    #endregion
}
