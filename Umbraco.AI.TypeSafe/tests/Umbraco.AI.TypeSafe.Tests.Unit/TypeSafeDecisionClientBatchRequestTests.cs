// DR-2 — Connect to TypeSafe AI (AC2, AC6, AC7) and DR-14 — Ask several questions in one call (AC5):
// what goes over the wire for an AIDecisionRequest.
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

// Supersedes the "q"-keyed and Context-based cases in TypeSafeDecisionClientRequestTests.

using System.Text.Json;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

/// <summary>Asserts on the captured request body, as TypeSafeDecisionClientRequestTests does.</summary>
public class TypeSafeDecisionClientBatchRequestTests
{
    private const string Usage = "\"usage\":{\"input_tokens\":1,\"output_tokens\":1}";

    private static async Task<(ScriptedHttpMessageHandler Handler, JsonElement Body, AIDecisionResponse Response)> SendAsync(
        AIDecisionRequest request,
        string answer)
    {
        var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(answer));
        var client = await TypeSafeTestHost.CreateClientAsync(handler);

        var response = await client.GetResponseAsync(request);

        return (handler, JsonDocument.Parse(handler.RequestBodies.Single()).RootElement, response);
    }

    #region Happy path

    public class GivenABinaryQuestionWithIdQ1
    {
        private readonly JsonElement _body = SendAsync(
                new AIDecisionRequest { State = "text", Questions = [new AIBinaryDecisionQuestion { Id = "q1", Instructions = "Is it?" }] },
                """{"model":"jev-latest","answers":{"q1":{"noul":0.9}},""" + Usage + "}")
            .GetAwaiter().GetResult().Body;

        [Fact]
        public void KeysTheQuestionByItsId() => _body.GetProperty("questions").TryGetProperty("q1", out _).ShouldBeTrue();
    }

    public class GivenARequestWithState
    {
        private readonly JsonElement _body = SendAsync(
                new AIDecisionRequest
                {
                    State = "Buy cheap watches",
                    Questions = [new AIBinaryDecisionQuestion { Id = "spam", Instructions = "Is this spam?" }],
                },
                """{"model":"jev-latest","answers":{"spam":{"noul":0.9}},""" + Usage + "}")
            .GetAwaiter().GetResult().Body;

        [Fact]
        public void SendsTheRequestStateAsState() => _body.GetProperty("state").GetString().ShouldBe("Buy cheap watches");

        [Fact]
        public void SendsTheQuestionsOwnInstructions()
            => _body.GetProperty("questions").GetProperty("spam").GetProperty("instructions").GetString().ShouldBe("Is this spam?");
    }

    public class GivenARequestWithNoState
    {
        private readonly JsonElement _body = SendAsync(
                new AIDecisionRequest
                {
                    Questions =
                    [
                        new AIBinaryDecisionQuestion { Id = "first", Instructions = "Is the sky blue?" },
                        new AIBinaryDecisionQuestion { Id = "second", Instructions = "Is grass green?" },
                    ],
                },
                """{"model":"jev-latest","answers":{"first":{"noul":0.9},"second":{"noul":0.9}},""" + Usage + "}")
            .GetAwaiter().GetResult().Body;

        [Fact]
        public void FallsBackToTheFirstQuestionsInstructions() => _body.GetProperty("state").GetString().ShouldBe("Is the sky blue?");
    }

    public class GivenAMixedThreeQuestionRequest
    {
        private readonly (ScriptedHttpMessageHandler Handler, JsonElement Body, AIDecisionResponse Response) _sent = SendAsync(
                new AIDecisionRequest
                {
                    State = "My flight was cancelled. Refund please.",
                    Questions =
                    [
                        new AIBinaryDecisionQuestion { Id = "refund", Instructions = "Refund requested?" },
                        new AIChoiceDecisionQuestion
                        {
                            Id = "category",
                            Instructions = "What about?",
                            Options = [new AIDecisionOption("refund"), new AIDecisionOption("rebooking")],
                        },
                        new AIScoreDecisionQuestion
                        {
                            Id = "mood",
                            Instructions = "How frustrated?",
                            Levels = [new AIDecisionScoreLevel("calm"), new AIDecisionScoreLevel("angry")],
                        },
                    ],
                },
                """
                {"model":"jev-latest","answers":{
                  "refund":{"noul":0.97},
                  "category":{"choice":"refund","probabilities":{"refund":0.8,"rebooking":0.2},"confidence":0.8},
                  "mood":{"score":0.7,"probabilities":{"0":0.3,"1":0.7},"confidence":0.7}
                },
                """ + Usage + "}")
            .GetAwaiter().GetResult();

        [Fact]
        public void MakesExactlyOneHttpCall() => _sent.Handler.Attempts.ShouldBe(1);

        [Fact]
        public void SendsAllThreeQuestions()
            => _sent.Body.GetProperty("questions").EnumerateObject().Select(p => p.Name).ShouldBe(["refund", "category", "mood"], ignoreOrder: true);

        [Fact]
        public void ReturnsThreeKeyedAnswers()
            => _sent.Response.Answers.Keys.ShouldBe(["refund", "category", "mood"], ignoreOrder: true);
    }

    public class GivenAQuestionIdWithAQuoteAndABackslash
    {
        // Proves the id round-trips through real JSON escaping both ways: serialized into the request
        // body (handled by System.Text.Json, not hand-rolled), then parsed back out of the response and
        // used to key the answer — not just passed through as a C# string untouched.
        private const string WeirdId = "a\"b\\c";

        private readonly (ScriptedHttpMessageHandler Handler, JsonElement Body, AIDecisionResponse Response) _sent = SendAsync(
                new AIDecisionRequest { Questions = [new AIBinaryDecisionQuestion { Id = WeirdId, Instructions = "Is it?" }] },
                """{"model":"jev-latest","answers":{"a\"b\\c":{"noul":0.9}},""" + Usage + "}")
            .GetAwaiter().GetResult();

        [Fact]
        public void RoundTripsTheIdThroughTheRequestAndTheAnswer()
            => _sent.Response.Answers.ContainsKey(WeirdId).ShouldBeTrue();
    }

    public class GivenAnAnswerForAnIdWeNeverAsked
    {
        // Jev answered an id ("extra") that wasn't in Questions. The adapter must still map it — dropping
        // it here would hide it from Core's DecisionAnswerChecker, whose job is to reject it as an extra.
        private readonly (ScriptedHttpMessageHandler Handler, JsonElement Body, AIDecisionResponse Response) _sent = SendAsync(
                new AIDecisionRequest { Questions = [new AIBinaryDecisionQuestion { Id = "asked", Instructions = "Is it?" }] },
                """{"model":"jev-latest","answers":{"asked":{"noul":0.9},"extra":{"type":"noul","noul":0.5}},""" + Usage + "}")
            .GetAwaiter().GetResult();

        [Fact]
        public void MapsTheUnaskedAnswerInsteadOfDroppingIt()
            => _sent.Response.Answers.ContainsKey("extra").ShouldBeTrue();
    }

    #endregion
}
