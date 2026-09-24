#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-1 — Ask typed decisions from C# (question validation rules)
// Replaces the spike's Decision/ValidatingDecisionClientTests.cs (flat AIDecisionQuestion/Kind shape).

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

public class ValidatingDecisionClientTests
{
    private static AIDecisionOption[] Options(int count)
        => Enumerable.Range(0, count).Select(i => new AIDecisionOption($"o{i}")).ToArray();

    private static string[] Levels(int count)
        => Enumerable.Range(0, count).Select(i => $"l{i}").ToArray();

    public class GivenAValidBinaryQuestion
    {
        private readonly FakeDecisionClient _inner = new(_ => new AIBinaryDecisionResponse { Probability = 0.9 });

        [Fact(Skip = "Pending T2")]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.AskAsync(new AIBinaryDecisionQuestion { Instructions = "Is this spam?" });

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAValidChoiceQuestionWith255Options
    {
        private readonly FakeDecisionClient _inner = new(
            _ => new AIChoiceDecisionResponse { Choice = "o0", ChoiceConfidence = 0.9 });

        [Fact(Skip = "Pending T2")]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.AskAsync(new AIChoiceDecisionQuestion { Instructions = "Pick", Options = Options(255) });

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAValidScoreQuestionWith10Levels
    {
        private readonly FakeDecisionClient _inner = new(
            _ => new AIScoreDecisionResponse { Score = 0, Level = "l0", ScoreConfidence = 0.9 });

        [Fact(Skip = "Pending T2")]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.AskAsync(new AIScoreDecisionQuestion { Instructions = "Rate", Levels = Levels(10) });

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    // Sad path

    public class GivenBlankInstructions
    {
        private readonly FakeDecisionClient _inner = new();

        [Theory(Skip = "Pending T2")]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ThrowsArgumentException(string instructions)
        {
            var client = new ValidatingDecisionClient(_inner);

            await Should.ThrowAsync<ArgumentException>(
                () => client.AskAsync(new AIBinaryDecisionQuestion { Instructions = instructions }));
        }

        [Fact(Skip = "Pending T2")]
        public async Task NeverCallsTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await Should.ThrowAsync<ArgumentException>(
                () => client.AskAsync(new AIBinaryDecisionQuestion { Instructions = " " }));

            _inner.ReceivedRequests.ShouldBeEmpty();
        }
    }

    public class GivenAChoiceQuestionWithTheWrongNumberOfOptions
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Theory(Skip = "Pending T2")]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(256)]
        public async Task ThrowsArgumentException(int count)
        {
            await Should.ThrowAsync<ArgumentException>(
                () => _client.AskAsync(new AIChoiceDecisionQuestion { Instructions = "Pick", Options = Options(count) }));
        }
    }

    public class GivenAChoiceQuestionWithDuplicateKeys
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact(Skip = "Pending T2")]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.AskAsync(new AIChoiceDecisionQuestion
            {
                Instructions = "Pick",
                Options = [new AIDecisionOption("a"), new AIDecisionOption("a")],
            }));
        }
    }

    public class GivenAChoiceQuestionWithABlankKey
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact(Skip = "Pending T2")]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.AskAsync(new AIChoiceDecisionQuestion
            {
                Instructions = "Pick",
                Options = [new AIDecisionOption("a"), new AIDecisionOption(" ")],
            }));
        }
    }

    public class GivenAScoreQuestionWithTheWrongNumberOfLevels
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Theory(Skip = "Pending T2")]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        public async Task ThrowsArgumentException(int count)
        {
            await Should.ThrowAsync<ArgumentException>(
                () => _client.AskAsync(new AIScoreDecisionQuestion { Instructions = "Rate", Levels = Levels(count) }));
        }
    }

    public class GivenAScoreQuestionWithABlankLevel
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact(Skip = "Pending T2")]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(
                () => _client.AskAsync(new AIScoreDecisionQuestion { Instructions = "Rate", Levels = ["low", ""] }));
        }
    }
}
