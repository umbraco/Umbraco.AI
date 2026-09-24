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

        [Fact(Skip = "Pending T7")]
        public void ReturnsABinaryResponse()
        {
            _response.ShouldBeOfType<AIBinaryDecisionResponse>();
        }

        [Fact(Skip = "Pending T7")]
        public void MapsNoulToTheProbability()
        {
            ((AIBinaryDecisionResponse)_response).Probability.ShouldBe(0.97);
        }

        [Fact(Skip = "Pending T7")]
        public void MapsTheModel()
        {
            _response.ModelId.ShouldBe("jev-latest");
        }

        [Fact(Skip = "Pending T7")]
        public void MapsInputTokens()
        {
            _response.Usage!.InputTokenCount.ShouldBe(42);
        }

        [Fact(Skip = "Pending T7")]
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

        [Fact(Skip = "Pending T7")]
        public void MapsTheChosenKey()
        {
            _response.Choice.ShouldBe("b");
        }

        [Fact(Skip = "Pending T7")]
        public void MapsTheConfidence()
        {
            _response.Confidence.ShouldBe(0.9);
        }

        [Fact(Skip = "Pending T7")]
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

        [Fact(Skip = "Pending T7")]
        public void MapsTheScore()
        {
            _response.Score.ShouldBe(1.8);
        }

        [Fact(Skip = "Pending T7")]
        public void MapsTheNearestLevelLabel()
        {
            _response.Level.ShouldBe("good");
        }

        [Fact(Skip = "Pending T7")]
        public void MapsTheConfidence()
        {
            _response.Confidence.ShouldBe(0.8);
        }

        [Fact(Skip = "Pending T7")]
        public void ReKeysProbabilitiesByLevelLabel()
        {
            _response.Probabilities.ShouldBe(new Dictionary<string, double> { ["poor"] = 0.1, ["good"] = 0.8 });
        }
    }

    public class GivenMalformedJson
    {
        [Fact(Skip = "Pending T7")]
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
        [Fact(Skip = "Pending T7")]
        public async Task ThrowsAJsonException()
        {
            await Should.ThrowAsync<JsonException>(
                () => AskAsync(
                    new AIBinaryDecisionQuestion { Instructions = "Is this spam?" },
                    """{"model":"jev-latest","answers":{"q":{}},"usage":{"input_tokens":1,"output_tokens":1}}"""));
        }
    }
}
