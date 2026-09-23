#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// DC-2 (AC5, AC6) — sad path. A caller error must be rejected before any inner
/// <see cref="IAIDecisionClient"/> (i.e. any provider) is invoked, mirroring how
/// <c>AIErrorClassifyingSpeechToTextClient</c> wraps every <c>ISpeechToTextClient</c> today.
/// </summary>
public class ValidatingDecisionClientTests
{
    public class GivenAChoiceQuestionWithFewerThanTwoChoices
    {
        private readonly FakeDecisionClient _inner = new();
        private readonly IAIDecisionClient _sut;

        public GivenAChoiceQuestionWithFewerThanTwoChoices()
        {
            _sut = new ValidatingDecisionClient(_inner);
        }

        [Fact]
        public async Task NullChoices_ThrowsArgumentException()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Choice, Prompt = "pick one", Choices = null };

            await Should.ThrowAsync<ArgumentException>(() => _sut.AskAsync(question));
        }

        [Fact]
        public async Task SingleChoice_ThrowsArgumentException()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Choice, Prompt = "pick one", Choices = ["only-option"] };

            await Should.ThrowAsync<ArgumentException>(() => _sut.AskAsync(question));
        }

        [Fact]
        public async Task InvalidChoiceQuestion_NeverReachesTheInnerClient()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Choice, Prompt = "pick one", Choices = ["only-option"] };

            try { await _sut.AskAsync(question); } catch (ArgumentException) { /* expected, asserted elsewhere */ }

            _inner.ReceivedRequests.ShouldBeEmpty();
        }
    }

    public class GivenAnEmptyPrompt
    {
        private readonly FakeDecisionClient _inner = new();
        private readonly IAIDecisionClient _sut;

        public GivenAnEmptyPrompt()
        {
            _sut = new ValidatingDecisionClient(_inner);
        }

        [Fact]
        public async Task WhitespaceOnlyPrompt_ThrowsArgumentException()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "   " };

            await Should.ThrowAsync<ArgumentException>(() => _sut.AskAsync(question));
        }
    }

    public class GivenAValidBinaryQuestion
    {
        private readonly FakeDecisionClient _inner = new();
        private readonly IAIDecisionClient _sut;

        public GivenAValidBinaryQuestion()
        {
            _sut = new ValidatingDecisionClient(_inner);
        }

        [Fact]
        public async Task PassesThroughToTheInnerClient()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this spam?" };

            await _sut.AskAsync(question);

            _inner.ReceivedRequests.ShouldHaveSingleItem();
        }
    }
}
