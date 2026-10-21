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

        [Fact(Skip = "Pending")]
        public void ReadsTheAnswersFromResult()
        {
            _response.Answers["orderId"].ShouldBeOfType<AITextExtractionAnswer>();
        }

        [Fact(Skip = "Pending")]
        public void MapsValue()
        {
            Answer.Value.ShouldBe("A-88412");
        }

        [Fact(Skip = "Pending")]
        public void MapsConfidence()
        {
            Answer.Confidence.ShouldBe(0.9999);
        }

        [Fact(Skip = "Pending")]
        public void MapsPNullToNullProbability()
        {
            Answer.NullProbability.ShouldBe(0.00001);
        }

        [Fact(Skip = "Pending")]
        public void MapsTheSpan()
        {
            Answer.Span.ShouldNotBeNull();
        }

        [Fact(Skip = "Pending")]
        public void MapsTheSpanStart()
        {
            Answer.Span!.Start.ShouldBe(6);
        }

        [Fact(Skip = "Pending")]
        public void MapsTheSpanEndAsExclusive()
        {
            Answer.Span!.End.ShouldBe(13);
        }

        [Fact(Skip = "Pending")]
        public void MapsInputTokens()
        {
            _response.Usage!.InputTokenCount.ShouldBe(612);
        }

        [Fact(Skip = "Pending")]
        public void MapsOutputTokens()
        {
            _response.Usage!.OutputTokenCount.ShouldBe(0);
        }

        [Fact(Skip = "Pending")]
        public void KeepsTheGuessInTheRawRepresentation()
        {
            // ASSUMPTION: RawRepresentation holds the provider's answer object as a JsonElement.
            ((JsonElement)Answer.RawRepresentation!).GetProperty("guess").GetString().ShouldBe("A-88412");
        }
    }

    public class GivenABareTextAnswer
    {
        private readonly AIExtractionResponse _response = ExtractAsync(OrderId, BareTextAnswer).GetAwaiter().GetResult();

        [Fact(Skip = "Pending")]
        public void ReadsTheAnswersFromTheTopLevel()
        {
            ((AITextExtractionAnswer)_response.Answers["orderId"]).Value.ShouldBe("A-88412");
        }

        [Fact(Skip = "Pending")]
        public void ReadsTheUsageFromTheTopLevel()
        {
            _response.Usage!.InputTokenCount.ShouldBe(612);
        }
    }

    public class GivenAListAnswerWithSpans
    {
        private readonly AIListExtractionAnswer _answer = (AIListExtractionAnswer)ExtractAsync(
            new AIListExtractionField { Id = "damaged", Instructions = "Damaged products", ItemType = AIExtractionListItemType.Text },
            """{"result":{"answers":{"damaged":{"value":["mug","teapot"],"confidence":0.99,"p_null":0.001,"spans":[[64,67],[72,78]]}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""")
            .GetAwaiter().GetResult().Answers["damaged"];

        [Fact(Skip = "Pending")]
        public void MapsTheValues()
        {
            _answer.Values.ShouldBe(["mug", "teapot"]);
        }

        [Fact(Skip = "Pending")]
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

        [Fact(Skip = "Pending")]
        public void MapsAMissingConfidenceToNull()
        {
            _answer.Confidence.ShouldBeNull();
        }

        [Fact(Skip = "Pending")]
        public void MapsAMissingPNullToNull()
        {
            _answer.NullProbability.ShouldBeNull();
        }
    }

    #endregion

    #region Sad path

    public class GivenMalformedJson
    {
        [Fact(Skip = "Pending")]
        public async Task ThrowsAJsonException()
        {
            // Classification into a provider failure happens in AIErrorClassifyingExtractionClient (Core);
            // the provider's job is to fail loudly, not return a half-mapped response.
            await Should.ThrowAsync<JsonException>(() => ExtractAsync(OrderId, "{ not json"));
        }
    }

    #endregion
}
