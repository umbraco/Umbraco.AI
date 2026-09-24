#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-1 — Ask typed decisions from C#

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// Every spec goes through the real <see cref="AIDecisionService"/> + <see cref="AIDecisionClientFactory"/>
/// pipeline (see <see cref="DecisionPipelineHarness"/>); only the provider client is faked.
/// </summary>
public class AskTypedDecisionTests
{
    public class GivenABinaryQuestionAndAProviderAnsweringPoint97
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            _ => new AIBinaryDecisionResponse { Probability = 0.97 }));

        private readonly AIBinaryDecisionQuestion _question = new() { Instructions = "Is this spam?" };

        [Fact(Skip = "Pending T5")]
        public async Task ReturnsABinaryResponseWithAnswerTrue()
        {
            AIBinaryDecisionResponse response =
                await _harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, _question);

            response.Answer.ShouldBeTrue();
        }
    }

    public class GivenABinaryQuestionAndAProviderAnsweringPoint2
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            _ => new AIBinaryDecisionResponse { Probability = 0.2 }));

        private readonly AIBinaryDecisionQuestion _question = new() { Instructions = "Is this spam?" };

        [Fact(Skip = "Pending T5")]
        public async Task AnswersFalse()
        {
            var response = await _harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, _question);

            response.Answer.ShouldBeFalse();
        }

        [Fact(Skip = "Pending T5")]
        public async Task ReportsConfidenceInTheAnswerGiven()
        {
            var response = await _harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, _question);

            response.Confidence.ShouldBe(0.8, tolerance: 1e-9);
        }
    }

    public class GivenAChoiceQuestionAndAProviderPickingB
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            _ => new AIChoiceDecisionResponse { Choice = "b", ChoiceConfidence = 0.9 }));

        private readonly AIChoiceDecisionQuestion _question = new()
        {
            Instructions = "Pick one",
            Options = [new AIDecisionOption("a"), new AIDecisionOption("b")],
        };

        [Fact(Skip = "Pending T5")]
        public async Task ReturnsTheChosenKey()
        {
            AIChoiceDecisionResponse response =
                await _harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, _question);

            response.Choice.ShouldBe("b");
        }

        [Fact(Skip = "Pending T5")]
        public async Task ReturnsTheChoiceConfidence()
        {
            var response = await _harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, _question);

            response.Confidence.ShouldBe(0.9);
        }
    }

    public class GivenAScoreQuestionAndAProviderScoring1Point8
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            _ => new AIScoreDecisionResponse { Score = 1.8, Level = "good", ScoreConfidence = 0.8 }));

        private readonly AIScoreDecisionQuestion _question = new()
        {
            Instructions = "Rate it",
            Levels = ["poor", "ok", "good"],
        };

        [Fact(Skip = "Pending T5")]
        public async Task ReturnsTheScore()
        {
            AIScoreDecisionResponse response =
                await _harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, _question);

            response.Score.ShouldBe(1.8);
        }

        [Fact(Skip = "Pending T5")]
        public async Task ReturnsTheLevelLabel()
        {
            var response = await _harness.Service.AskAsync(DecisionPipelineHarness.ProfileAlias, _question);

            response.Level.ShouldBe("good");
        }
    }

    public class GivenAProfileAlias
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            _ => new AIBinaryDecisionResponse { Probability = 0.9 }));

        [Fact(Skip = "Pending T5")]
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
            _ => new AIBinaryDecisionResponse { Probability = 0.9 }));

        [Fact(Skip = "Pending T5")]
        public async Task UsesTheDefaultDecisionProfile()
        {
            await _harness.Service.AskAsync(new AIBinaryDecisionQuestion { Instructions = "Is this spam?" });

            _harness.ProfileServiceMock.Verify(
                x => x.GetDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    // Sad path

    public class GivenAProviderReturningTheWrongResponseType
    {
        private readonly DecisionPipelineHarness _harness = new(new FakeDecisionClient(
            _ => new AIChoiceDecisionResponse { Choice = "a", ChoiceConfidence = 0.9 }));

        [Fact(Skip = "Pending T5")]
        public async Task ThrowsAIProviderException()
        {
            var act = () => _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = "Is this spam?" });

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    public class GivenAnInvalidQuestion
    {
        private readonly FakeDecisionClient _client = new(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));

        private readonly DecisionPipelineHarness _harness;

        public GivenAnInvalidQuestion() => _harness = new DecisionPipelineHarness(_client);

        [Fact(Skip = "Pending T5")]
        public async Task ThrowsArgumentExceptionNotAProviderException()
        {
            var act = () => _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = " " });

            await Should.ThrowAsync<ArgumentException>(act);
        }

        [Fact(Skip = "Pending T5")]
        public async Task NeverReachesTheProvider()
        {
            await Should.ThrowAsync<ArgumentException>(() => _harness.Service.AskAsync(
                DecisionPipelineHarness.ProfileAlias,
                new AIBinaryDecisionQuestion { Instructions = " " }));

            _client.ReceivedRequests.ShouldBeEmpty();
        }
    }
}
