// EX-2 — Connect to Cloudflare (AC17-AC21: each field kind maps to its typed answer)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

using System.Globalization;
using System.Text.Json;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;
using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

public class CloudflareExtractionClientTypedAnswerTests
{
    private static async Task<AIExtractionAnswer> ExtractAsync(AIExtractionField field, string valueJson)
    {
        var body = $$$"""{"result":{"answers":{"f":{"value":{{{valueJson}}},"confidence":0.9,"p_null":0.1}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true}""";
        var client = await CloudflareTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(body)));
        var response = await client.GetResponseAsync(new AIExtractionRequest { State = "text", Fields = [field] });
        return response.Answers["f"];
    }

    private static AINumberExtractionField Number => new() { Id = "f", Instructions = "n" };

    public class GivenANumberAnswer
    {
        [Fact]
        public async Task MapsTheValueAsADecimalKeepingItsPrecision()
        {
            ((AINumberExtractionAnswer)await ExtractAsync(Number, "701.10")).Value.ShouldBe(701.10m);
        }
    }

    public class GivenADurationAnswer
    {
        private static AIDurationExtractionField Weeks => new() { Id = "f", Instructions = "d", Unit = AIDurationUnit.Weeks };

        [Fact]
        public async Task CarriesTheUnitTheFieldAskedFor()
        {
            ((AIDurationExtractionAnswer)await ExtractAsync(Weeks, "3")).Unit.ShouldBe(AIDurationUnit.Weeks);
        }

        [Fact]
        public async Task MapsTheValueAsADecimal()
        {
            ((AIDurationExtractionAnswer)await ExtractAsync(Weeks, "2.5")).Value.ShouldBe(2.5m);
        }
    }

    public class GivenADateAnswer
    {
        [Fact]
        public async Task MapsTheValueAsADateOnly()
        {
            var answer = (AIDateExtractionAnswer)await ExtractAsync(new AIDateExtractionField { Id = "f", Instructions = "d" }, "\"2026-10-03\"");

            answer.Value.ShouldBe(new DateOnly(2026, 10, 3));
        }
    }

    public class GivenATimeAnswer
    {
        [Fact]
        public async Task MapsTheValueAsATimeOnly()
        {
            var answer = (AITimeExtractionAnswer)await ExtractAsync(new AITimeExtractionField { Id = "f", Instructions = "t" }, "\"14:30:00\"");

            answer.Value.ShouldBe(new TimeOnly(14, 30));
        }
    }

    public class GivenADateTimeAnswerWithNoOffset
    {
        private readonly Task<AIExtractionAnswer> _answer = ExtractAsync(new AIDateTimeExtractionField { Id = "f", Instructions = "d" }, "\"2026-10-03T14:30:00\"");

        [Fact]
        public async Task KeepsTheWallClockTime()
        {
            ((AIDateTimeExtractionAnswer)await _answer).Value.ShouldBe(new DateTime(2026, 10, 3, 14, 30, 0));
        }

        [Fact]
        public async Task LeavesTheKindUnspecified()
        {
            ((AIDateTimeExtractionAnswer)await _answer).Value!.Value.Kind.ShouldBe(DateTimeKind.Unspecified);
        }
    }

    public class GivenADateTimeAnswerWithAnOffset
    {
        [Fact]
        public async Task ReturnsTheInstantAsUtc()
        {
            var answer = (AIDateTimeExtractionAnswer)await ExtractAsync(
                new AIDateTimeExtractionField { Id = "f", Instructions = "d" }, "\"2026-10-03T14:30:00+02:00\"");

            answer.Value.ShouldBe(new DateTime(2026, 10, 3, 12, 30, 0, DateTimeKind.Utc));
        }
    }

    public class GivenAListOfNumbers
    {
        [Fact]
        public async Task MapsEachItemToInvariantText()
        {
            var field = new AIListExtractionField { Id = "f", Instructions = "l", ItemType = AIExtractionListItemType.Number };

            ((AIListExtractionAnswer)await ExtractAsync(field, "[1.5, 2]")).Values.ShouldBe(["1.5", "2"]);
        }
    }

    public class GivenANullValue
    {
        [Fact]
        public async Task IsNotFound()
        {
            (await ExtractAsync(Number, "null")).IsFound.ShouldBeFalse();
        }

        [Fact]
        public async Task GivesAListAnswerNoValues()
        {
            var answer = (AIListExtractionAnswer)await ExtractAsync(new AIListExtractionField { Id = "f", Instructions = "l" }, "null");

            answer.Values.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GivesAListAnswerNoSpans()
        {
            var answer = (AIListExtractionAnswer)await ExtractAsync(new AIListExtractionField { Id = "f", Instructions = "l" }, "null");

            answer.Spans.Count.ShouldBe(0);
        }
    }

    public class GivenADanishCulture : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        public GivenADanishCulture()
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");
            CultureInfo.CurrentUICulture = new CultureInfo("da-DK");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }

        [Fact]
        public async Task ReadsADecimalNumber()
        {
            ((AINumberExtractionAnswer)await ExtractAsync(Number, "701.10")).Value.ShouldBe(701.10m);
        }

        [Fact]
        public async Task ReadsANumberSentAsAString()
        {
            ((AINumberExtractionAnswer)await ExtractAsync(Number, "\"701.10\"")).Value.ShouldBe(701.10m);
        }

        [Fact]
        public async Task ReadsADateTime()
        {
            var answer = (AIDateTimeExtractionAnswer)await ExtractAsync(
                new AIDateTimeExtractionField { Id = "f", Instructions = "d" }, "\"2026-10-03T14:30:00\"");

            answer.Value.ShouldBe(new DateTime(2026, 10, 3, 14, 30, 0));
        }

        [Fact]
        public async Task WritesAListOfDecimalsAsInvariantText()
        {
            var field = new AIListExtractionField { Id = "f", Instructions = "l", ItemType = AIExtractionListItemType.Number };

            ((AIListExtractionAnswer)await ExtractAsync(field, "[1.5]")).Values.ShouldBe(["1.5"]);
        }
    }

    public class GivenAnAnswerMissingForAField
    {
        private static readonly string Body =
            """{"result":{"answers":{"g":{"value":"x"}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true}""";

        [Fact]
        public async Task ReturnsAResponseWithoutThatKey()
        {
            var client = await CloudflareTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(Body)));

            var response = await client.GetResponseAsync(new AIExtractionRequest { State = "text", Fields = [Number] });

            response.Answers.ContainsKey("f").ShouldBeFalse();
        }
    }

    public class GivenAnAnswerForAFieldNobodyAskedFor
    {
        private static async Task<AIExtractionResponse> ExtractAsync(string answerJson)
        {
            var body = $$$"""{"result":{"answers":{"f":{"value":"1"},"extra":{{{answerJson}}}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true}""";
            var client = await CloudflareTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(body)));
            return await client.GetResponseAsync(new AIExtractionRequest { State = "text", Fields = [Number] });
        }

        [Fact]
        public async Task SurvivesAMalformedNumberValue()
        {
            var response = await ExtractAsync("""{"value":12345678901234567890123456789012345678901234567890}""");

            response.Answers.ContainsKey("extra").ShouldBeTrue();
        }

        [Fact]
        public async Task SurvivesAMalformedSpan()
        {
            var response = await ExtractAsync("""{"value":"a","span":[1]}""");

            response.Answers.ContainsKey("extra").ShouldBeTrue();
        }

        [Fact]
        public async Task SurvivesAnObjectValue()
        {
            var response = await ExtractAsync("""{"value":{"a":1}}""");

            response.Answers.ContainsKey("extra").ShouldBeTrue();
        }

        [Fact]
        public async Task KeepsTheRawAnswer()
        {
            var response = await ExtractAsync("""{"value":{"a":1}}""");

            ((JsonElement)response.Answers["extra"].RawRepresentation!).GetProperty("value").GetProperty("a").GetInt32().ShouldBe(1);
        }
    }

    public class GivenABodyWithoutAnswers
    {
        [Fact]
        public async Task ThrowsAJsonException()
        {
            var client = await CloudflareTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json("""{"result":{}}""")));

            await Should.ThrowAsync<JsonException>(
                () => client.GetResponseAsync(new AIExtractionRequest { State = "t", Fields = [Number] }));
        }
    }
}
