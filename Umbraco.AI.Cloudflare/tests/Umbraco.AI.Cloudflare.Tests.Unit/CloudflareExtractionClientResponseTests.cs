// EX-2 — Connect to Cloudflare (AC16-AC21: mapping Clairic's answers back to typed answers)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

// Cloudflare's REST API normally wraps results as { result, success, errors, messages }, but the Clairic
// docs never show a REST response, so the client accepts both the wrapped and the bare shape
// (ARCHITECTURE.md decision 5). Rejecting a missing/extra/wrong-type answer is Core's job
// (ExtractionAnswerChecker), not the adapter's, same split as TypeSafe.

using System.Text.Json;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;
using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

public class CloudflareExtractionClientResponseTests
{
    private static readonly AITextExtractionField OrderId = new() { Id = "orderId", Instructions = "The order number" };

    private const string WrappedTextAnswer =
        """{"result":{"answers":{"orderId":{"value":"A-88412","guess":"A-88412","confidence":0.9999,"p_null":0.00001,"span":[6,13]}},"usage":{"input_tokens":612,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""";

    private const string BareTextAnswer =
        """{"answers":{"orderId":{"value":"A-88412","confidence":0.9999,"p_null":0.00001,"span":[6,13]}},"usage":{"input_tokens":612,"output_tokens":0}}""";

    private static async Task<AIExtractionResponse> ExtractAsync(AIExtractionField field, string answer)
    {
        var client = await CloudflareTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(answer)));
        return await client.GetResponseAsync(new AIExtractionRequest { State = "Order A-88412 was delivered.", Fields = [field] });
    }

    #region Happy path

    public class GivenAWrappedTextAnswer
    {
        private readonly AIExtractionResponse _response = ExtractAsync(OrderId, WrappedTextAnswer).GetAwaiter().GetResult();

        private AITextExtractionAnswer Answer => (AITextExtractionAnswer)_response.Answers["orderId"];

        [Fact]
        public void ReadsTheAnswersFromResult()
        {
            _response.Answers["orderId"].ShouldBeOfType<AITextExtractionAnswer>();
        }

        [Fact]
        public void MapsValue()
        {
            Answer.Value.ShouldBe("A-88412");
        }

        [Fact]
        public void MapsConfidence()
        {
            Answer.Confidence.ShouldBe(0.9999);
        }

        [Fact]
        public void MapsPNullToNullProbability()
        {
            Answer.NullProbability.ShouldBe(0.00001);
        }

        [Fact]
        public void MapsTheSpan()
        {
            Answer.Span.ShouldNotBeNull();
        }

        [Fact]
        public void MapsTheSpanStart()
        {
            Answer.Span!.Value.Start.ShouldBe(6);
        }

        [Fact]
        public void MapsTheSpanEndAsExclusive()
        {
            Answer.Span!.Value.End.ShouldBe(13);
        }

        [Fact]
        public void MapsInputTokens()
        {
            _response.Usage!.InputTokenCount.ShouldBe(612);
        }

        [Fact]
        public void MapsOutputTokens()
        {
            _response.Usage!.OutputTokenCount.ShouldBe(0);
        }

        [Fact]
        public void KeepsTheGuessInTheRawRepresentation()
        {
            // ASSUMPTION: RawRepresentation holds the provider's answer object as a JsonElement.
            ((JsonElement)Answer.RawRepresentation!).GetProperty("guess").GetString().ShouldBe("A-88412");
        }
    }

    public class GivenABareTextAnswer
    {
        private readonly AIExtractionResponse _response = ExtractAsync(OrderId, BareTextAnswer).GetAwaiter().GetResult();

        [Fact]
        public void ReadsTheAnswersFromTheTopLevel()
        {
            ((AITextExtractionAnswer)_response.Answers["orderId"]).Value.ShouldBe("A-88412");
        }

        [Fact]
        public void ReadsTheUsageFromTheTopLevel()
        {
            _response.Usage!.InputTokenCount.ShouldBe(612);
        }
    }

    public class GivenARealBareResponse
    {
        // Shape captured from a live Clairic call: bare (not wrapped), model without an @cf prefix.
        private readonly AIExtractionResponse _response = ExtractAsync(
            OrderId,
            """{"answers":{"orderId":{"value":"A-88412","p_null":0.0001,"confidence":0.98,"span":[6,13]}},"model":"clairic","usage":{"input_tokens":612,"output_tokens":3}}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void ReadsTheModelFromTheResponse()
        {
            _response.ModelId.ShouldBe("clairic");
        }

        [Fact]
        public void ReadsTheOutputTokens()
        {
            _response.Usage!.OutputTokenCount.ShouldBe(3);
        }

        [Fact]
        public void ReadsTheAnswersFromTheTopLevel()
        {
            ((AITextExtractionAnswer)_response.Answers["orderId"]).Value.ShouldBe("A-88412");
        }
    }

    public class GivenATextAnswerNotFoundWithAGuessedSpan
    {
        // Clairic still sends a span (its guess) when the value is null.
        private readonly AITextExtractionAnswer _answer = (AITextExtractionAnswer)ExtractAsync(
            OrderId,
            """{"answers":{"orderId":{"value":null,"p_null":0.99999964,"confidence":0.787,"span":[6,13]}},"usage":{"input_tokens":1,"output_tokens":0}}""")
            .GetAwaiter().GetResult().Answers["orderId"];

        [Fact]
        public void HasNoValue()
        {
            _answer.Value.ShouldBeNull();
        }

        [Fact]
        public void HasNoSpan()
        {
            _answer.Span.ShouldBeNull();
        }

        [Fact]
        public void KeepsTheGuessedSpanInTheRawRepresentation()
        {
            ((JsonElement)_answer.RawRepresentation!).GetProperty("span").GetArrayLength().ShouldBe(2);
        }
    }

    public class GivenAListAnswerNotFoundWithGuessedSpans
    {
        // The spans shape is assumed by analogy with the live text not-found answer.
        private readonly AIListExtractionAnswer _answer = (AIListExtractionAnswer)ExtractAsync(
            new AIListExtractionField { Id = "damaged", Instructions = "Damaged products", ItemType = AIExtractionListItemType.Text },
            """{"answers":{"damaged":{"value":null,"p_null":0.999,"confidence":0.7,"spans":[[1,4]]}},"usage":{"input_tokens":1,"output_tokens":0}}""")
            .GetAwaiter().GetResult().Answers["damaged"];

        [Fact]
        public void HasNoValues()
        {
            _answer.Values.ShouldBeEmpty();
        }

        [Fact]
        public void HasNoSpans()
        {
            _answer.Spans.ShouldBeEmpty();
        }
    }

    public class GivenRealTypedValueShapes
    {
        private static async Task<AIExtractionAnswer> TypedAsync(AIExtractionField field, string valueJson)
            => (await ExtractAsync(field, $$$"""{"answers":{"f":{"value":{{{valueJson}}},"p_null":0.01,"confidence":0.9}},"model":"clairic","usage":{"input_tokens":1,"output_tokens":0}}""")).Answers["f"];

        [Fact]
        public async Task MapsADateTimeWithMillisecondsAndZToTheRightInstant()
        {
            var answer = (AIDateTimeExtractionAnswer)await TypedAsync(new AIDateTimeExtractionField { Id = "f", Instructions = "x" }, "\"2026-10-24T09:00:00.000Z\"");

            answer.Value.ShouldBe(new DateTime(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task MapsADateTimeWithMillisecondsAndZAsUtcKind()
        {
            var answer = (AIDateTimeExtractionAnswer)await TypedAsync(new AIDateTimeExtractionField { Id = "f", Instructions = "x" }, "\"2026-10-24T09:00:00.000Z\"");

            answer.Value!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        }

        [Fact]
        public async Task MapsATimeWithSeconds()
        {
            var answer = (AITimeExtractionAnswer)await TypedAsync(new AITimeExtractionField { Id = "f", Instructions = "x" }, "\"14:30:00\"");

            answer.Value.ShouldBe(new TimeOnly(14, 30, 0));
        }

        [Fact]
        public async Task MapsADate()
        {
            var answer = (AIDateExtractionAnswer)await TypedAsync(new AIDateExtractionField { Id = "f", Instructions = "x" }, "\"2026-10-03\"");

            answer.Value.ShouldBe(new DateOnly(2026, 10, 3));
        }

        [Fact]
        public async Task MapsANumberSentAsAJsonNumber()
        {
            var answer = (AINumberExtractionAnswer)await TypedAsync(new AINumberExtractionField { Id = "f", Instructions = "x" }, "701.1");

            answer.Value.ShouldBe(701.1m);
        }

        [Fact]
        public async Task MapsADurationSentAsAJsonNumber()
        {
            var answer = (AIDurationExtractionAnswer)await TypedAsync(new AIDurationExtractionField { Id = "f", Instructions = "x", Unit = AIDurationUnit.Days }, "5");

            answer.Value.ShouldBe(5m);
        }
    }

    public class GivenAListAnswerWithSpans
    {
        private readonly AIListExtractionAnswer _answer = (AIListExtractionAnswer)ExtractAsync(
            new AIListExtractionField { Id = "damaged", Instructions = "Damaged products", ItemType = AIExtractionListItemType.Text },
            """{"result":{"answers":{"damaged":{"value":["mug","teapot"],"confidence":0.99,"p_null":0.001,"spans":[[64,67],[72,78]]}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""")
            .GetAwaiter().GetResult().Answers["damaged"];

        [Fact]
        public void MapsTheValues()
        {
            _answer.Values.ShouldBe(["mug", "teapot"]);
        }

        [Fact]
        public void MapsSpansToSpans()
        {
            _answer.Spans.ShouldBe([new AIExtractionSpan(64, 67), new AIExtractionSpan(72, 78)]);
        }
    }

    public class GivenAnAnswerWithNoConfidence
    {
        private readonly AITextExtractionAnswer _answer = (AITextExtractionAnswer)ExtractAsync(
            OrderId,
            """{"result":{"answers":{"orderId":{"value":"A-88412"}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""")
            .GetAwaiter().GetResult().Answers["orderId"];

        [Fact]
        public void MapsAMissingConfidenceToNull()
        {
            _answer.Confidence.ShouldBeNull();
        }

        [Fact]
        public void MapsAMissingPNullToNull()
        {
            _answer.NullProbability.ShouldBeNull();
        }
    }

    #endregion

    #region Sad path

    public class GivenMalformedJson
    {
        [Fact]
        public async Task ThrowsAJsonException()
        {
            // Classification into a provider failure happens in AIErrorClassifyingExtractionClient (Core);
            // the provider's job is to fail loudly, not return a half-mapped response.
            await Should.ThrowAsync<JsonException>(() => ExtractAsync(OrderId, "{ not json"));
        }
    }

    #endregion
}
