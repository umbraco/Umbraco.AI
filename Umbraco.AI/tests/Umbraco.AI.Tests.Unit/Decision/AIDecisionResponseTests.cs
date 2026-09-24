#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;

namespace Umbraco.AI.Tests.Unit.Decision;

// DR-1 — each response type's computed properties (Answer/Confidence) are derived correctly from the
// raw model output, whatever kind of question was asked.
public class AIDecisionResponseTests
{
    public class GivenABinaryResponseWithHighProbability
    {
        private readonly AIBinaryDecisionResponse _response = new() { Probability = 0.9 };

        [Fact]
        public void AnswerIsTrue() => _response.Answer.ShouldBeTrue();

        [Fact]
        public void ConfidenceEqualsProbability() => _response.Confidence.ShouldBe(0.9);
    }

    public class GivenABinaryResponseWithLowProbability
    {
        private readonly AIBinaryDecisionResponse _response = new() { Probability = 0.1 };

        [Fact]
        public void AnswerIsFalse() => _response.Answer.ShouldBeFalse();

        [Fact]
        public void ConfidenceIsOneMinusProbability() => _response.Confidence.ShouldBe(0.9);
    }

    public class GivenABinaryResponseWithBoundaryProbability
    {
        private readonly AIBinaryDecisionResponse _response = new() { Probability = 0.5 };

        [Fact]
        public void AnswerIsTrue() => _response.Answer.ShouldBeTrue();

        [Fact]
        public void ConfidenceEqualsProbability() => _response.Confidence.ShouldBe(0.5);
    }

    public class GivenAChoiceResponse
    {
        private readonly AIChoiceDecisionResponse _response = new() { Choice = "refund-policy", ChoiceConfidence = 0.75 };

        [Fact]
        public void ChoiceIsSet() => _response.Choice.ShouldBe("refund-policy");

        [Fact]
        public void ConfidenceEqualsChoiceConfidence() => _response.Confidence.ShouldBe(0.75);
    }

    public class GivenAScoreResponse
    {
        private readonly AIScoreDecisionResponse _response = new() { Score = 1.8, Level = "good", ScoreConfidence = 0.6 };

        [Fact]
        public void ScoreIsSet() => _response.Score.ShouldBe(1.8);

        [Fact]
        public void LevelIsSet() => _response.Level.ShouldBe("good");

        [Fact]
        public void ConfidenceEqualsScoreConfidence() => _response.Confidence.ShouldBe(0.6);
    }
}
