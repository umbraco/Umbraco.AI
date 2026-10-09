#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-1 — Ask typed decisions from C# (question validation rules), DR-14 (batch validation rules)
// Replaces the spike's Decision/ValidatingDecisionClientTests.cs (flat AIDecisionQuestion/Kind shape).

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

public class ValidatingDecisionClientTests
{
    private static AIDecisionOption[] Options(int count)
        => Enumerable.Range(0, count).Select(i => new AIDecisionOption($"o{i}")).ToArray();

    private static AIDecisionScoreLevel[] Levels(int count)
        => Enumerable.Range(0, count).Select(i => new AIDecisionScoreLevel($"l{i}")).ToArray();

    private static AIDecisionRequest Request(AIDecisionQuestion question) => new() { State = "text", Questions = [question] };

    public class GivenAValidBinaryQuestion
    {
        private readonly FakeDecisionClient _inner = new();

        [Fact]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.GetResponseAsync(Request(new AIBinaryDecisionQuestion { Id = "q", Instructions = "Is this spam?" }));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAValidChoiceQuestionWith2Options
    {
        private readonly FakeDecisionClient _inner = new();

        [Fact]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.GetResponseAsync(Request(new AIChoiceDecisionQuestion { Id = "q", Instructions = "Pick", Options = Options(2) }));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAValidChoiceQuestionWith255Options
    {
        private readonly FakeDecisionClient _inner = new();

        [Fact]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.GetResponseAsync(Request(new AIChoiceDecisionQuestion { Id = "q", Instructions = "Pick", Options = Options(255) }));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAChoiceQuestionWithKeysDifferingOnlyByCase
    {
        private readonly FakeDecisionClient _inner = new();

        [Fact]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.GetResponseAsync(Request(new AIChoiceDecisionQuestion
            {
                Id = "q",
                Instructions = "Pick",
                Options = [new AIDecisionOption("a"), new AIDecisionOption("A")],
            }));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAValidScoreQuestionWith2Levels
    {
        private readonly FakeDecisionClient _inner = new();

        [Fact]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.GetResponseAsync(Request(new AIScoreDecisionQuestion { Id = "q", Instructions = "Rate", Levels = Levels(2) }));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAValidScoreQuestionWith10Levels
    {
        private readonly FakeDecisionClient _inner = new();

        [Fact]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.GetResponseAsync(Request(new AIScoreDecisionQuestion { Id = "q", Instructions = "Rate", Levels = Levels(10) }));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenATwoQuestionBatchWithUniqueNonBlankIds
    {
        private readonly FakeDecisionClient _inner = new();

        [Fact]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingDecisionClient(_inner);

            await client.GetResponseAsync(new AIDecisionRequest
            {
                State = "text",
                Questions =
                [
                    new AIBinaryDecisionQuestion { Id = "a", Instructions = "One?" },
                    new AIBinaryDecisionQuestion { Id = "b", Instructions = "Two?" },
                ],
            });

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    // Sad path

    public class GivenAnEmptyBatch
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(
                () => _client.GetResponseAsync(new AIDecisionRequest { State = "text", Questions = [] }));
        }
    }

    public class GivenATwoQuestionBatchWithDuplicateIds
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.GetResponseAsync(new AIDecisionRequest
            {
                State = "text",
                Questions =
                [
                    new AIBinaryDecisionQuestion { Id = "q", Instructions = "One?" },
                    new AIBinaryDecisionQuestion { Id = "q", Instructions = "Two?" },
                ],
            }));
        }
    }

    public class GivenATwoQuestionBatchWithABlankId
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.GetResponseAsync(new AIDecisionRequest
            {
                State = "text",
                Questions =
                [
                    new AIBinaryDecisionQuestion { Id = "q", Instructions = "One?" },
                    new AIBinaryDecisionQuestion { Id = null, Instructions = "Two?" },
                ],
            }));
        }
    }

    public class GivenBlankInstructions
    {
        private readonly FakeDecisionClient _inner = new();
        private readonly Exception? _exception;

        public GivenBlankInstructions()
        {
            var client = new ValidatingDecisionClient(_inner);

            _exception = Record.ExceptionAsync(
                () => client.GetResponseAsync(Request(new AIBinaryDecisionQuestion { Id = "q", Instructions = " " }))).GetAwaiter().GetResult();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ThrowsArgumentException(string instructions)
        {
            var client = new ValidatingDecisionClient(_inner);

            await Should.ThrowAsync<ArgumentException>(
                () => client.GetResponseAsync(Request(new AIBinaryDecisionQuestion { Id = "q", Instructions = instructions })));
        }

        [Fact]
        public void ThrowsArgumentExceptionForTheConstructorCall() => _exception.ShouldBeOfType<ArgumentException>();

        [Fact]
        public void NeverCallsTheInnerClient() => _inner.ReceivedRequests.ShouldBeEmpty();
    }

    public class GivenAChoiceQuestionWithNullOptions
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.GetResponseAsync(Request(new AIChoiceDecisionQuestion
            {
                Id = "q",
                Instructions = "Pick",
                Options = null!,
            })));
        }
    }

    public class GivenAChoiceQuestionWithANullOption
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.GetResponseAsync(Request(new AIChoiceDecisionQuestion
            {
                Id = "q",
                Instructions = "Pick",
                Options = [new AIDecisionOption("a"), null!],
            })));
        }
    }

    public class GivenAChoiceQuestionWithTheWrongNumberOfOptions
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(256)]
        public async Task ThrowsArgumentException(int count)
        {
            await Should.ThrowAsync<ArgumentException>(
                () => _client.GetResponseAsync(Request(new AIChoiceDecisionQuestion { Id = "q", Instructions = "Pick", Options = Options(count) })));
        }
    }

    public class GivenAChoiceQuestionWithDuplicateKeys
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.GetResponseAsync(Request(new AIChoiceDecisionQuestion
            {
                Id = "q",
                Instructions = "Pick",
                Options = [new AIDecisionOption("a"), new AIDecisionOption("a")],
            })));
        }
    }

    public class GivenAChoiceQuestionWithABlankKey
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.GetResponseAsync(Request(new AIChoiceDecisionQuestion
            {
                Id = "q",
                Instructions = "Pick",
                Options = [new AIDecisionOption("a"), new AIDecisionOption(" ")],
            })));
        }
    }

    public class GivenAScoreQuestionWithNullLevels
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(
                () => _client.GetResponseAsync(Request(new AIScoreDecisionQuestion { Id = "q", Instructions = "Rate", Levels = null! })));
        }
    }

    public class GivenAScoreQuestionWithTheWrongNumberOfLevels
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        public async Task ThrowsArgumentException(int count)
        {
            await Should.ThrowAsync<ArgumentException>(
                () => _client.GetResponseAsync(Request(new AIScoreDecisionQuestion { Id = "q", Instructions = "Rate", Levels = Levels(count) })));
        }
    }

    public class GivenAScoreQuestionWithABlankLevel
    {
        private readonly ValidatingDecisionClient _client = new(new FakeDecisionClient());

        [Fact]
        public async Task ThrowsArgumentException()
        {
            await Should.ThrowAsync<ArgumentException>(() => _client.GetResponseAsync(Request(new AIScoreDecisionQuestion
            {
                Id = "q",
                Instructions = "Rate",
                Levels = [new AIDecisionScoreLevel("low"), new AIDecisionScoreLevel("")],
            })));
        }
    }
}
