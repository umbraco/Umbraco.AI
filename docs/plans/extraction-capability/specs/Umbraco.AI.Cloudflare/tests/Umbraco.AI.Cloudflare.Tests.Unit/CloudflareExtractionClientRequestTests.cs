// EX-2 — Connect to Cloudflare (AC3, AC10-AC15: what goes over the wire)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

// Asserts on the captured request body, not on what we handed the client, like the TypeSafe request
// specs. The Clairic REST shape below ("fields" keyed by id, each with "type" and "instructions") follows
// SPEC.md "Cloudflare provider"; the wire task (T9) confirms it against the real API and fixes these
// specs if Cloudflare's shape differs.

using System.Text.Json;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;
using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

public class CloudflareExtractionClientRequestTests
{
    private const string TextAnswer =
        """{"result":{"answers":{"orderId":{"value":"A-88412","confidence":0.99,"p_null":0.001,"span":[6,13]}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""";

    private const string NumberAnswer =
        """{"result":{"answers":{"amount":{"value":701.10,"confidence":0.99,"p_null":0.001}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""";

    private const string ListAnswer =
        """{"result":{"answers":{"dates":{"value":["2026-10-03"],"confidence":0.99,"p_null":0.001,"spans":[[0,10]]}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""";

    private const string DurationAnswer =
        """{"result":{"answers":{"stay":{"value":3,"confidence":0.99,"p_null":0.001}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""";

    private const string State = "Order A-88412 was delivered on 3 October. I paid 701,10 kr.";

    private static async Task<(ScriptedHttpMessageHandler Handler, JsonElement Body)> SendAsync(
        AIExtractionField field,
        string answer,
        DateOnly? referenceDate = null)
    {
        var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(answer));
        var client = await CloudflareTestHost.CreateClientAsync(handler);

        await client.GetResponseAsync(new AIExtractionRequest { State = State, Fields = [field], ReferenceDate = referenceDate });

        return (handler, JsonDocument.Parse(handler.RequestBodies.Single()).RootElement);
    }

    private static JsonElement Field(JsonElement body, string id) => body.GetProperty("fields").GetProperty(id);

    public class GivenATextFieldWithNoEndpointSet
    {
        private readonly (ScriptedHttpMessageHandler Handler, JsonElement Body) _sent =
            SendAsync(new AITextExtractionField { Id = "orderId", Instructions = "The order number" }, TextAnswer)
                .GetAwaiter().GetResult();

        [Fact(Skip = "Pending")]
        public void PostsToTheClairicRunEndpointOnTheDefaultCloudflareApi()
        {
            _sent.Handler.Requests.Single().RequestUri.ShouldBe(
                new Uri("https://api.cloudflare.com/client/v4/accounts/test-account/ai/run/@cf/cloudflare/clairic"));
        }

        [Fact(Skip = "Pending")]
        public void UsesPost()
        {
            _sent.Handler.Requests.Single().Method.ShouldBe(HttpMethod.Post);
        }

        [Fact(Skip = "Pending")]
        public void SendsTheApiTokenAsABearerToken()
        {
            _sent.Handler.Requests.Single().Headers.Authorization!.ToString().ShouldBe("Bearer test-api-token");
        }

        [Fact(Skip = "Pending")]
        public void SendsTheTextType()
        {
            Field(_sent.Body, "orderId").GetProperty("type").GetString().ShouldBe("text");
        }

        [Fact(Skip = "Pending")]
        public void SendsTheState()
        {
            _sent.Body.GetProperty("state").GetString().ShouldBe(State);
        }

        [Fact(Skip = "Pending")]
        public void SendsNoImages()
        {
            _sent.Body.TryGetProperty("images", out _).ShouldBeFalse();
        }

        [Fact(Skip = "Pending")]
        public void OmitsTheReferenceDateWhenNoneIsGiven()
        {
            _sent.Body.TryGetProperty("reference_date", out _).ShouldBeFalse();
        }
    }

    public class GivenAReferenceDate
    {
        private readonly JsonElement _body =
            SendAsync(
                new AITextExtractionField { Id = "orderId", Instructions = "The order number" },
                TextAnswer,
                new DateOnly(2026, 10, 20)).GetAwaiter().GetResult().Body;

        [Fact(Skip = "Pending")]
        public void SendsItAsAnIsoReferenceDate()
        {
            _body.GetProperty("reference_date").GetString().ShouldBe("2026-10-20");
        }
    }

    public class GivenANumberFieldWithEveryOption
    {
        private readonly JsonElement _field;

        public GivenANumberFieldWithEveryOption()
        {
            var field = new AINumberExtractionField
            {
                Id = "amount",
                Instructions = "Amount paid",
                Decimals = 2,
                Integer = true,
                Unit = "DKK",
            };
            _field = Field(SendAsync(field, NumberAnswer).GetAwaiter().GetResult().Body, "amount");
        }

        [Fact(Skip = "Pending")]
        public void SendsTheNumberType()
        {
            _field.GetProperty("type").GetString().ShouldBe("number");
        }

        [Fact(Skip = "Pending")]
        public void SendsTheInstructions()
        {
            _field.GetProperty("instructions").GetString().ShouldBe("Amount paid");
        }

        [Fact(Skip = "Pending")]
        public void SendsTheDecimals()
        {
            _field.GetProperty("decimals").GetInt32().ShouldBe(2);
        }

        [Fact(Skip = "Pending")]
        public void SendsTheWholeNumberFlagAsInteger()
        {
            _field.GetProperty("integer").GetBoolean().ShouldBeTrue();
        }

        [Fact(Skip = "Pending")]
        public void SendsTheUnit()
        {
            _field.GetProperty("unit").GetString().ShouldBe("DKK");
        }
    }

    public class GivenAListFieldOfDates
    {
        private readonly JsonElement _field = Field(
            SendAsync(
                new AIListExtractionField { Id = "dates", Instructions = "Every date mentioned", ItemType = AIExtractionListItemType.Date },
                ListAnswer).GetAwaiter().GetResult().Body,
            "dates");

        [Fact(Skip = "Pending")]
        public void SendsTheListType()
        {
            _field.GetProperty("type").GetString().ShouldBe("list");
        }

        [Fact(Skip = "Pending")]
        public void SendsTheItemTypeUnderItems()
        {
            _field.GetProperty("items").GetProperty("type").GetString().ShouldBe("date");
        }
    }

    public class GivenADurationFieldInDays
    {
        private readonly JsonElement _field = Field(
            SendAsync(
                new AIDurationExtractionField { Id = "stay", Instructions = "Length of stay", Unit = AIDurationUnit.Days },
                DurationAnswer).GetAwaiter().GetResult().Body,
            "stay");

        [Fact(Skip = "Pending")]
        public void SendsTheDurationType()
        {
            _field.GetProperty("type").GetString().ShouldBe("duration");
        }

        [Fact(Skip = "Pending")]
        public void SendsTheUnitInLowerCase()
        {
            _field.GetProperty("unit").GetString().ShouldBe("days");
        }
    }
}
