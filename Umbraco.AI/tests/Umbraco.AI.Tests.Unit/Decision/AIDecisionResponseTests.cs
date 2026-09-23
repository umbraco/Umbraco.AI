#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;

namespace Umbraco.AI.Tests.Unit.Decision;

// DC-2 — A typed decision has one consistent shape, whatever kind of question was asked
public class AIDecisionResponseTests
{
    public class GivenABinaryAnswer
    {
        private readonly AIDecisionResponse _response = AIDecisionResponse.ForBinary(true, 0.9);

        [Fact]
        public void KindIsBinary() => _response.Kind.ShouldBe(AIDecisionKind.Binary);

        [Fact]
        public void BinaryAnswerIsSet() => _response.BinaryAnswer.ShouldBe(true);

        [Fact]
        public void SelectedChoiceAndScoreAreNull()
        {
            _response.SelectedChoice.ShouldBeNull();
            _response.Score.ShouldBeNull();
        }

        [Fact]
        public void ConfidenceIsWithinRange() => _response.Confidence.ShouldBeInRange(0.0, 1.0);
    }

    public class GivenAChoiceAnswer
    {
        private readonly AIDecisionResponse _response = AIDecisionResponse.ForChoice("refund policy", 0.75);

        [Fact]
        public void KindIsChoice() => _response.Kind.ShouldBe(AIDecisionKind.Choice);

        [Fact]
        public void SelectedChoiceIsSet() => _response.SelectedChoice.ShouldBe("refund policy");

        [Fact]
        public void BinaryAnswerAndScoreAreNull()
        {
            _response.BinaryAnswer.ShouldBeNull();
            _response.Score.ShouldBeNull();
        }

        [Fact]
        public void ConfidenceIsWithinRange() => _response.Confidence.ShouldBeInRange(0.0, 1.0);
    }

    public class GivenAScoreAnswer
    {
        private readonly AIDecisionResponse _response = AIDecisionResponse.ForScore(0.42, 0.6);

        [Fact]
        public void KindIsScore() => _response.Kind.ShouldBe(AIDecisionKind.Score);

        [Fact]
        public void ScoreIsSet() => _response.Score.ShouldBe(0.42);

        [Fact]
        public void BinaryAnswerAndSelectedChoiceAreNull()
        {
            _response.BinaryAnswer.ShouldBeNull();
            _response.SelectedChoice.ShouldBeNull();
        }

        [Fact]
        public void ConfidenceIsWithinRange() => _response.Confidence.ShouldBeInRange(0.0, 1.0);
    }
}
