#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-14 — Ask several questions in one call from C# (AC1, AC3, AC6-AC10).
//
// ASSUMPTIONS (T29/T30 builders confirm/adjust, keeping each test's behavior and single assertion):
// - IAIDecisionService.GetDecisionResponseAsync(string profileAlias, AIDecisionRequest request,
//   AIDecisionOptions? options = null, CancellationToken ct = default) returns AIDecisionResponse.
// - FakeDecisionClient takes Func<AIDecisionRequest, AIDecisionResponse> and records
//   ReceivedRequests as (AIDecisionRequest Request, AIDecisionOptions? Options).
// AC9/AC10 depend on the answer-completeness check in AIErrorClassifyingDecisionClient (T30).

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// Through the real <see cref="AIDecisionService"/> + <see cref="AIDecisionClientFactory"/> pipeline
/// (see <see cref="DecisionPipelineHarness"/>); only the provider client is faked.
/// </summary>
public class DecisionBatchTests
{
    private static AIDecisionRequest MixedRequest() => new()
    {
        State = "My flight was cancelled. I'm furious. Can I get my money back?",
        Questions =
        [
            new AIBinaryDecisionQuestion { Id = "refund", Instructions = "Does the customer want a refund?" },
            new AIChoiceDecisionQuestion
            {
                Id = "category",
                Instructions = "What is the request about?",
                Options = [new AIDecisionOption("refund"), new AIDecisionOption("rebooking")],
            },
            new AIScoreDecisionQuestion
            {
                Id = "mood",
                Instructions = "How frustrated is the customer?",
                Levels = [new AIDecisionScoreLevel("calm"), new AIDecisionScoreLevel("concerned"), new AIDecisionScoreLevel("angry")],
            },
        ],
    };

    private static readonly AIBinaryDecisionAnswer RefundAnswer = new() { TrueProbability = 0.97 };

    private static readonly AIChoiceDecisionAnswer CategoryAnswer = new()
    {
        Choice = "refund",
        Confidence = 0.8,
        Probabilities = new Dictionary<string, double> { ["refund"] = 0.82, ["rebooking"] = 0.18 },
    };

    private static readonly AIScoreDecisionAnswer MoodAnswer = new()
    {
        Score = 1.3,
        Confidence = 0.7,
        Probabilities = new Dictionary<int, double> { [0] = 0.0, [1] = 0.7, [2] = 0.3 },
    };

    private static AIDecisionResponse AllAnswered() => new()
    {
        Answers = new Dictionary<string, AIDecisionAnswer>
        {
            ["refund"] = RefundAnswer,
            ["category"] = CategoryAnswer,
            ["mood"] = MoodAnswer,
        },
        ModelId = "jev-1.13.0",
    };

    #region Happy path

    public class GivenAMixedThreeQuestionRequest
    {
        private readonly AIDecisionResponse _response;

        public GivenAMixedThreeQuestionRequest()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(_ => AllAnswered()));
            _response = harness.Service.GetDecisionResponseAsync(DecisionPipelineHarness.ProfileAlias, MixedRequest())
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void KeysAnswersByQuestionId()
            => _response.Answers.Keys.ShouldBe(["refund", "category", "mood"], ignoreOrder: true);

        [Fact]
        public void AnswersTheBinaryQuestionWithABinaryAnswer()
            => _response.Answers["refund"].ShouldBeOfType<AIBinaryDecisionAnswer>();

        [Fact]
        public void AnswersTheChoiceQuestionWithAChoiceAnswer()
            => _response.Answers["category"].ShouldBeOfType<AIChoiceDecisionAnswer>();

        [Fact]
        public void AnswersTheScoreQuestionWithAScoreAnswer()
            => _response.Answers["mood"].ShouldBeOfType<AIScoreDecisionAnswer>();

        [Fact]
        public void CarriesTheModelIdForTheWholeCall() => _response.ModelId.ShouldBe("jev-1.13.0");
    }

    public class GivenAMixedRequestSentOnce
    {
        [Fact]
        public async Task MakesOneProviderCall()
        {
            var client = new FakeDecisionClient(_ => AllAnswered());
            var harness = new DecisionPipelineHarness(client);

            await harness.Service.GetDecisionResponseAsync(DecisionPipelineHarness.ProfileAlias, MixedRequest());

            client.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAProfileAlias
    {
        [Fact]
        public async Task ResolvesThatProfile()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(_ => AllAnswered()));

            await harness.Service.GetDecisionResponseAsync(DecisionPipelineHarness.ProfileAlias, MixedRequest());

            harness.ProfileServiceMock.Verify(
                x => x.GetProfileByAliasAsync(DecisionPipelineHarness.ProfileAlias, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    #endregion

    #region Sad path

    public class GivenInvalidBatches
    {
        private static AIDecisionRequest TwoBinaries(string? firstId, string? secondId) => new()
        {
            State = "text",
            Questions =
            [
                new AIBinaryDecisionQuestion { Id = firstId, Instructions = "One?" },
                new AIBinaryDecisionQuestion { Id = secondId, Instructions = "Two?" },
            ],
        };

        public static TheoryData<string, AIDecisionRequest> Requests => new()
        {
            { "duplicate ids", TwoBinaries("q", "q") },
            { "a blank id in a batch", TwoBinaries("q", null) },
            { "no questions", new AIDecisionRequest { State = "text", Questions = [] } },
            {
                "a single question with no id",
                new AIDecisionRequest
                {
                    State = "text",
                    Questions = [new AIBinaryDecisionQuestion { Id = null, Instructions = "One?" }],
                }
            },
        };

        [Theory]
        [MemberData(nameof(Requests))]
        public async Task ThrowsArgumentException(string _, AIDecisionRequest request)
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(_ => AllAnswered()));

            var act = () => harness.Service.GetDecisionResponseAsync(DecisionPipelineHarness.ProfileAlias, request);

            await Should.ThrowAsync<ArgumentException>(act);
        }

        [Theory]
        [MemberData(nameof(Requests))]
        public async Task NeverReachesTheProvider(string _, AIDecisionRequest request)
        {
            var client = new FakeDecisionClient(_ => AllAnswered());
            var harness = new DecisionPipelineHarness(client);

            await Should.ThrowAsync<ArgumentException>(
                () => harness.Service.GetDecisionResponseAsync(DecisionPipelineHarness.ProfileAlias, request));

            client.ReceivedRequests.ShouldBeEmpty();
        }
    }

    public class GivenAProviderThatSkipsAQuestion
    {
        [Fact]
        public async Task ThrowsAIProviderException()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(_ => new AIDecisionResponse
            {
                Answers = new Dictionary<string, AIDecisionAnswer> { ["refund"] = RefundAnswer, ["category"] = CategoryAnswer },
            }));

            var act = () => harness.Service.GetDecisionResponseAsync(DecisionPipelineHarness.ProfileAlias, MixedRequest());

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    public class GivenAProviderThatAnswersAnUnaskedId
    {
        [Fact]
        public async Task ThrowsAIProviderException()
        {
            var harness = new DecisionPipelineHarness(new FakeDecisionClient(_ => new AIDecisionResponse
            {
                Answers = new Dictionary<string, AIDecisionAnswer>
                {
                    ["refund"] = RefundAnswer,
                    ["category"] = CategoryAnswer,
                    ["mood"] = MoodAnswer,
                    ["extra"] = new AIBinaryDecisionAnswer { TrueProbability = 0.5 },
                },
            }));

            var act = () => harness.Service.GetDecisionResponseAsync(DecisionPipelineHarness.ProfileAlias, MixedRequest());

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    #endregion
}
