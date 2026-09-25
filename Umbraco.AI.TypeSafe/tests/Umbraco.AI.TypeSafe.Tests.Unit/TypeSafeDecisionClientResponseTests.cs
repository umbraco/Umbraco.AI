// DR-2 — Connect to TypeSafe AI (AC9, AC10: mapping Jev's answers back to typed responses)
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Text.Json;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

public class TypeSafeDecisionClientResponseTests
{
    private static async Task<AIDecisionResponse> AskAsync(AIDecisionQuestion question, string answer)
    {
        var client = await TypeSafeTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(answer)));
        return await client.AskAsync(question);
    }

    public class GivenANoulAnswer
    {
        private readonly AIDecisionResponse _response = AskAsync(
            new AIBinaryDecisionQuestion { Instructions = "Is this spam?" },
            """{"model":"jev-latest","answers":{"q":{"noul":0.97}},"usage":{"input_tokens":42,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void ReturnsABinaryResponse()
        {
            _response.ShouldBeOfType<AIBinaryDecisionResponse>();
        }

        [Fact]
        public void MapsNoulToTheProbability()
        {
            ((AIBinaryDecisionResponse)_response).Probability.ShouldBe(0.97);
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
        private readonly AIChoiceDecisionResponse _response = (AIChoiceDecisionResponse)AskAsync(
            new AIChoiceDecisionQuestion { Instructions = "Which?", Options = [new AIDecisionOption("a"), new AIDecisionOption("b")] },
            """{"model":"jev-latest","answers":{"q":{"choice":"b","probabilities":{"a":0.1,"b":0.9},"confidence":0.9}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void MapsTheChosenKey()
        {
            _response.Choice.ShouldBe("b");
        }

        [Fact]
        public void MapsTheConfidence()
        {
            _response.Confidence.ShouldBe(0.9);
        }

        [Fact]
        public void MapsTheProbabilitiesByKey()
        {
            _response.Probabilities["a"].ShouldBe(0.1);
        }
    }

    public class GivenAScoreAnswer
    {
        private readonly AIScoreDecisionResponse _response = (AIScoreDecisionResponse)AskAsync(
            new AIScoreDecisionQuestion { Instructions = "How good?", Levels = ["poor", "ok", "good"] },
            """{"model":"jev-latest","answers":{"q":{"score":1.8,"legend":{"0":"poor","1":"ok","2":"good"},"probabilities":{"0":0.1,"2":0.8},"confidence":0.8}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void MapsTheScore()
        {
            _response.Score.ShouldBe(1.8);
        }

        [Fact]
        public void MapsTheNearestLevelLabel()
        {
            _response.Level.ShouldBe("good");
        }

        [Fact]
        public void MapsTheConfidence()
        {
            _response.Confidence.ShouldBe(0.8);
        }

        [Fact]
        public void ReKeysProbabilitiesByLevelLabel()
        {
            _response.Probabilities.ShouldBe(new Dictionary<string, double> { ["poor"] = 0.1, ["good"] = 0.8 });
        }
    }

    public class GivenAScoreAnswerWhereTheLegendDisagreesWithLevels
    {
        // Jev echoes a "legend" alongside "probabilities", but the question's own Levels — sent by us, in
        // this order — are authoritative. A mismatched legend must not override them.
        private readonly AIScoreDecisionResponse _response = (AIScoreDecisionResponse)AskAsync(
            new AIScoreDecisionQuestion { Instructions = "How good?", Levels = ["poor", "ok", "good"] },
            """{"model":"jev-latest","answers":{"q":{"score":2.0,"legend":{"0":"terrible","1":"meh","2":"awesome"},"probabilities":{"2":0.9},"confidence":0.9}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void MapsTheLevelFromLevelsNotTheLegend()
        {
            _response.Level.ShouldBe("good");
        }

        [Fact]
        public void KeysProbabilitiesFromLevelsNotTheLegend()
        {
            _response.Probabilities["good"].ShouldBe(0.9);
        }
    }

    public class GivenAScoreAnswerBelowTheLowestLevel
    {
        // score rounds to -0 before clamping — still below Levels[0]'s index, so it clamps to the lowest level.
        private readonly AIScoreDecisionResponse _response = (AIScoreDecisionResponse)AskAsync(
            new AIScoreDecisionQuestion { Instructions = "How good?", Levels = ["poor", "ok", "good"] },
            """{"model":"jev-latest","answers":{"q":{"score":-0.4,"probabilities":{"0":1.0},"confidence":0.5}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void ClampsToTheLowestLevel()
        {
            _response.Level.ShouldBe("poor");
        }
    }

    public class GivenAScoreAnswerThatRoundsBelowTheLowestLevel
    {
        // score rounds to -1 before clamping — still below Levels[0]'s index, so it clamps to the lowest level.
        private readonly AIScoreDecisionResponse _response = (AIScoreDecisionResponse)AskAsync(
            new AIScoreDecisionQuestion { Instructions = "How good?", Levels = ["poor", "ok", "good"] },
            """{"model":"jev-latest","answers":{"q":{"score":-0.6,"probabilities":{"0":1.0},"confidence":0.5}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void ClampsToTheLowestLevel()
        {
            _response.Level.ShouldBe("poor");
        }
    }

    public class GivenAScoreAnswerAboveTheHighestLevel
    {
        private readonly AIScoreDecisionResponse _response = (AIScoreDecisionResponse)AskAsync(
            new AIScoreDecisionQuestion { Instructions = "How good?", Levels = ["poor", "ok", "good"] },
            """{"model":"jev-latest","answers":{"q":{"score":2.6,"probabilities":{"2":1.0},"confidence":0.5}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void ClampsToTheHighestLevel()
        {
            _response.Level.ShouldBe("good");
        }
    }

    public class GivenAScoreAnswerWithAProbabilityKeyOutsideTheLevels
    {
        // Key "5" has no matching Levels index (only 0-2 exist) and must be dropped, not thrown.
        private readonly AIScoreDecisionResponse _response = (AIScoreDecisionResponse)AskAsync(
            new AIScoreDecisionQuestion { Instructions = "How good?", Levels = ["poor", "ok", "good"] },
            """{"model":"jev-latest","answers":{"q":{"score":1.0,"probabilities":{"0":0.1,"1":0.1,"2":0.1,"5":0.7},"confidence":0.5}},"usage":{"input_tokens":1,"output_tokens":1}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void DropsTheOutOfRangeKey()
        {
            _response.Probabilities.Count.ShouldBe(3);
        }
    }

    public class GivenAChoiceAnswerWithAKeyThatIsNotAnOption
    {
        [Fact]
        public async Task ThrowsAJsonException()
        {
            await Should.ThrowAsync<JsonException>(
                () => AskAsync(
                    new AIChoiceDecisionQuestion { Instructions = "Which?", Options = [new AIDecisionOption("a"), new AIDecisionOption("b")] },
                    """{"model":"jev-latest","answers":{"q":{"choice":"c","probabilities":{"a":0.1,"b":0.9},"confidence":0.9}},"usage":{"input_tokens":1,"output_tokens":1}}"""));
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
                () => AskAsync(new AIBinaryDecisionQuestion { Instructions = "Is this spam?" }, "{ not json"));
        }
    }

    public class GivenAnAnswerMissingTheExpectedField
    {
        [Fact]
        public async Task ThrowsAJsonException()
        {
            await Should.ThrowAsync<JsonException>(
                () => AskAsync(
                    new AIBinaryDecisionQuestion { Instructions = "Is this spam?" },
                    """{"model":"jev-latest","answers":{"q":{}},"usage":{"input_tokens":1,"output_tokens":1}}"""));
        }
    }
}
