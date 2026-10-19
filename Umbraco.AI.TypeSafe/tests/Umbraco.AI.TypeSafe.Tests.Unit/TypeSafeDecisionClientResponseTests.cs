// DR-2 — Connect to TypeSafe AI (AC9, AC10: mapping Jev's answers back to typed answers)
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

// The Level-focused cases (deriving a label from a question's Levels, clamping to the nearest level, the
// legend-disagrees-with-levels case) are gone along with AIScoreDecisionAnswer.Level — Core's rework
// dropped the label-keyed score shape entirely (see SPEC.md "Removed"). The gap-fill (omitted-entry)
// cases moved to TypeSafeDecisionClientGapFillTests.
//
// The adapter must not drop or reject any data Jev reports — Core's DecisionAnswerChecker is the one
// place that rejects a malformed answer (ARCHITECTURE.md "Checks": fill zeros/re-key is the adapter's
// job, rejecting is Core's). So an out-of-range score index or an unoffered choice key is kept, not
// dropped or thrown on — GivenAChoiceAnswerWithAKeyThatIsNotAnOption (which asserted the adapter itself
// threw) is gone; that check now lives only in Core.

using System.Text.Json;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

public class TypeSafeDecisionClientResponseTests
{
    private static async Task<AIDecisionResponse> AskAsync(AIDecisionQuestion question, string answer)
    {
        var client = await TypeSafeTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(answer)));
        return await client.GetResponseAsync(new AIDecisionRequest { State = "text", Questions = [question] });
    }

    private static async Task<AIDecisionAnswer> AnswerAsync(AIDecisionQuestion question, string answer)
        => (await AskAsync(question, answer)).Answers["q"];

    public class GivenANoulAnswer
    {
        private readonly AIDecisionResponse _response = AskAsync(
            new AIBinaryDecisionQuestion { Id = "q", Instructions = "Is this spam?" },
            """{"model":"jev-latest","answers":{"q":{"noul":0.97}},"usage":{"input_tokens":42,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void ReturnsABinaryAnswer()
        {
            _response.Answers["q"].ShouldBeOfType<AIBinaryDecisionAnswer>();
        }

        [Fact]
        public void MapsNoulToTrueProbability()
        {
            ((AIBinaryDecisionAnswer)_response.Answers["q"]).TrueProbability.ShouldBe(0.97);
        }

        [Fact]
        public void MapsTheModel()
        {
            _response.ModelId.ShouldBe("jev-latest");
        }

        [Fact]
        public void MapsInputTokens()
        {
            _response.Usage!.InputTokenCount.ShouldBe(42);
        }

        [Fact]
        public void MapsOutputTokens()
        {
            _response.Usage!.OutputTokenCount.ShouldBe(1);
        }
    }

    public class GivenAChoiceAnswer
    {
        private readonly AIChoiceDecisionAnswer _answer = (AIChoiceDecisionAnswer)AnswerAsync(
            new AIChoiceDecisionQuestion { Id = "q", Instructions = "Which?", Options = [new AIDecisionOption("a"), new AIDecisionOption("b")] },
            """{"model":"jev-latest","answers":{"q":{"choice":"b","probabilities":{"a":0.1,"b":0.9},"confidence":0.9}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void MapsTheChosenKey()
        {
            _answer.Choice.ShouldBe("b");
        }

        [Fact]
        public void MapsTheConfidence()
        {
            _answer.Confidence.ShouldBe(0.9);
        }

        [Fact]
        public void MapsTheProbabilitiesByKey()
        {
            _answer.Probabilities["a"].ShouldBe(0.1);
        }
    }

    public class GivenAChoiceAnswerWithNoConfidence
    {
        private readonly AIChoiceDecisionAnswer _answer = (AIChoiceDecisionAnswer)AnswerAsync(
            new AIChoiceDecisionQuestion { Id = "q", Instructions = "Which?", Options = [new AIDecisionOption("a"), new AIDecisionOption("b")] },
            """{"model":"jev-latest","answers":{"q":{"choice":"b","probabilities":{"a":0.1,"b":0.9}}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void MapsAMissingConfidenceToNull()
        {
            _answer.Confidence.ShouldBeNull();
        }
    }

    public class GivenAChoiceAnswerWithAKeyThatIsNotAnOption
    {
        // "c" was never offered as an option. Core's DecisionAnswerChecker rejects this, not the
        // adapter — the adapter keeps every key Jev reports, exactly as it reported it.
        private readonly AIChoiceDecisionAnswer _answer = (AIChoiceDecisionAnswer)AnswerAsync(
            new AIChoiceDecisionQuestion { Id = "q", Instructions = "Which?", Options = [new AIDecisionOption("a"), new AIDecisionOption("b")] },
            """{"model":"jev-latest","answers":{"q":{"choice":"a","probabilities":{"a":0.3,"b":0.3,"c":0.4},"confidence":0.9}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void KeepsTheExtraChoiceKey()
        {
            _answer.Probabilities["c"].ShouldBe(0.4);
        }
    }

    public class GivenAScoreAnswerWithAProbabilityKeyOutsideTheLevels
    {
        // Key "5" has no matching Levels index (only 0-2 exist). Core's DecisionAnswerChecker rejects
        // this, not the adapter — the adapter keeps every index Jev reports, exactly as it reported it.
        private readonly AIScoreDecisionAnswer _answer = (AIScoreDecisionAnswer)AnswerAsync(
            new AIScoreDecisionQuestion
            {
                Id = "q",
                Instructions = "How good?",
                Levels = [new AIDecisionScoreLevel("poor"), new AIDecisionScoreLevel("ok"), new AIDecisionScoreLevel("good")],
            },
            """{"model":"jev-latest","answers":{"q":{"score":1.0,"probabilities":{"0":0.1,"1":0.1,"2":0.1,"5":0.7},"confidence":0.5}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void KeepsTheOutOfRangeIndex()
        {
            _answer.Probabilities[5].ShouldBe(0.7);
        }
    }

    public class GivenAScoreAnswerWithNoConfidence
    {
        private readonly AIScoreDecisionAnswer _answer = (AIScoreDecisionAnswer)AnswerAsync(
            new AIScoreDecisionQuestion
            {
                Id = "q",
                Instructions = "How good?",
                Levels = [new AIDecisionScoreLevel("poor"), new AIDecisionScoreLevel("ok")],
            },
            """{"model":"jev-latest","answers":{"q":{"score":1.0,"probabilities":{"0":0.5,"1":0.5}}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void MapsAMissingConfidenceToNull()
        {
            _answer.Confidence.ShouldBeNull();
        }
    }

    public class GivenMalformedJson
    {
        [Fact]
        public async Task ThrowsAJsonException()
        {
            // AC15. Classification into a provider failure happens in AIErrorClassifyingDecisionClient
            // (Core, covered by AIDecisionClientFactoryTests); the provider's job is to fail loudly, not
            // return a half-mapped response.
            await Should.ThrowAsync<JsonException>(
                () => AnswerAsync(new AIBinaryDecisionQuestion { Id = "q", Instructions = "Is this spam?" }, "{ not json"));
        }
    }

    public class GivenAnAnswerMissingTheExpectedField
    {
        [Fact]
        public async Task ThrowsAJsonException()
        {
            await Should.ThrowAsync<JsonException>(
                () => AnswerAsync(
                    new AIBinaryDecisionQuestion { Id = "q", Instructions = "Is this spam?" },
                    """{"model":"jev-latest","answers":{"q":{}},"usage":{"input_tokens":1,"output_tokens":1}}"""));
        }
    }
}
