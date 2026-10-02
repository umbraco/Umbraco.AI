#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-1 — Ask typed decisions from C# (profile resolution + the sad paths that don't depend on a
// specific answer shape). The per-kind answer scenarios now live in AskTypedDecisionAnswerTests.

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// Every spec goes through the real <see cref="AIDecisionService"/> + <see cref="AIDecisionClientFactory"/>
/// pipeline (see <see cref="DecisionPipelineHarness"/>); only the provider client is faked.
/// </summary>
public class AskTypedDecisionTests
{
    private static AIDecisionResponse Answered(string id) => new()
    {
        Answers = new Dictionary<string, AIDecisionAnswer> { [id] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 } },
    };

    public class GivenAProfileAlias
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            request => Answered(request.Questions[0].Id!)));

        [Fact]
        public async Task ResolvesTheProfileByThatAlias()
        {
            await _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = "Is this spam?" });

            _harness.ProfileServiceMock.Verify(
                x => x.GetProfileByAliasAsync(DecisionPipelineHarness.ProfileAlias, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    public class GivenNoProfile
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            request => Answered(request.Questions[0].Id!)));

        [Fact]
        public async Task UsesTheDefaultDecisionProfile()
        {
            await _harness.Service.AskAsync(new AIBinaryDecisionQuestion { Instructions = "Is this spam?" });

            _harness.ProfileServiceMock.Verify(
                x => x.GetDefaultProfileAsync(Umbraco.AI.Core.Models.AICapability.Decision, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    // Sad path

    public class GivenAnInvalidQuestion
    {
        private readonly FakeDecisionClient _client = new(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));

        private readonly DecisionPipelineHarness _harness;

        public GivenAnInvalidQuestion() => _harness = new DecisionPipelineHarness(_client);

        [Fact]
        public async Task ThrowsArgumentExceptionNotAProviderException()
        {
            var act = () => _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = " " });

            await Should.ThrowAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task NeverReachesTheProvider()
        {
            await Should.ThrowAsync<ArgumentException>(() => _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = " " }));

            _client.ReceivedRequests.ShouldBeEmpty();
        }
    }

    // EnsureId (an id-less question cloned via AIDecisionQuestion.WithId) must preserve every
    // subclass-specific property, not just Id/Instructions — see AIDecisionQuestion.WithId's remarks.

    public class GivenAnIdLessBinaryQuestion
    {
        private readonly FakeDecisionClient _client = new(request => Answered(request.Questions[0].Id!));
        private readonly DecisionPipelineHarness _harness;

        public GivenAnIdLessBinaryQuestion() => _harness = new DecisionPipelineHarness(_client);

        [Fact]
        public async Task PreservesTrueCriteriaOnTheClonedQuestion()
        {
            await _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = "Is this spam?", TrueCriteria = "Clearly unsolicited." });

            ((AIBinaryDecisionQuestion)_client.ReceivedRequests[0].Request.Questions[0]).TrueCriteria
                .ShouldBe("Clearly unsolicited.");
        }

        [Fact]
        public async Task PreservesFalseCriteriaOnTheClonedQuestion()
        {
            await _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = "Is this spam?", FalseCriteria = "Clearly legitimate." });

            ((AIBinaryDecisionQuestion)_client.ReceivedRequests[0].Request.Questions[0]).FalseCriteria
                .ShouldBe("Clearly legitimate.");
        }
    }

    public class GivenAnIdLessChoiceQuestion
    {
        private readonly FakeDecisionClient _client = new(request => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                [request.Questions[0].Id!] = new AIChoiceDecisionAnswer
                {
                    Choice = "refund",
                    Probabilities = new Dictionary<string, double> { ["refund"] = 0.9, ["rebooking"] = 0.1 },
                },
            },
        });

        private readonly DecisionPipelineHarness _harness;

        public GivenAnIdLessChoiceQuestion() => _harness = new DecisionPipelineHarness(_client);

        [Fact]
        public async Task PreservesOptionsOnTheClonedQuestion()
        {
            var options = new[] { new AIDecisionOption("refund"), new AIDecisionOption("rebooking") };

            await _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIChoiceDecisionQuestion { Instructions = "Pick one", Options = options });

            ((AIChoiceDecisionQuestion)_client.ReceivedRequests[0].Request.Questions[0]).Options.ShouldBe(options);
        }
    }

    public class GivenAnIdLessScoreQuestion
    {
        private readonly FakeDecisionClient _client = new(request => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                [request.Questions[0].Id!] = new AIScoreDecisionAnswer
                {
                    Score = 1,
                    Probabilities = new Dictionary<int, double> { [0] = 0.1, [1] = 0.9 },
                },
            },
        });

        private readonly DecisionPipelineHarness _harness;

        public GivenAnIdLessScoreQuestion() => _harness = new DecisionPipelineHarness(_client);

        [Fact]
        public async Task PreservesLevelsOnTheClonedQuestion()
        {
            var levels = new[] { new AIDecisionScoreLevel("low"), new AIDecisionScoreLevel("high") };

            await _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIScoreDecisionQuestion { Instructions = "Rate it", Levels = levels });

            ((AIScoreDecisionQuestion)_client.ReceivedRequests[0].Request.Questions[0]).Levels.ShouldBe(levels);
        }
    }

    public class GivenAnIdLessCustomQuestionSubclass
    {
        /// <summary>
        /// A third-party question kind this assembly knows nothing about — proves
        /// <see cref="AIDecisionQuestion.WithId"/>'s <c>MemberwiseClone</c> works for any subclass, not
        /// just the three built-in ones.
        /// </summary>
        private sealed class CustomDecisionQuestion : AIDecisionQuestion<AIBinaryDecisionAnswer>
        {
            public string? ExtraProperty { get; init; }
        }

        [Fact]
        public async Task PreservesItsExtraPropertyOnTheClonedQuestion()
        {
            var client = new FakeDecisionClient(request => Answered(request.Questions[0].Id!));
            var harness = new DecisionPipelineHarness(client);

            await harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new CustomDecisionQuestion { Instructions = "Custom?", ExtraProperty = "value" });

            ((CustomDecisionQuestion)client.ReceivedRequests[0].Request.Questions[0]).ExtraProperty
                .ShouldBe("value");
        }
    }
}
