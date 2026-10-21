// EX-6 — Extract over the Management API (AC1: the SPEC.md request body as real JSON)
//
// Copies AskDecisionRequestFormattingTests: goes through the real MVC input-formatter pipeline (Umbraco's
// NamedSystemTextJsonInputFormatter via AddJsonOptions + [JsonOptionsName]) to pin that the request in
// SPEC.md "Management API surface" binds, `$type` and all, and that a bad discriminator is a 400, never a 500.
#pragma warning disable UMBRACOAI_EXTRACTION // Binds the experimental Extraction request models

using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.AI.Web.Api.Management.Extraction.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Extraction;

public class ExtractRequestFormattingTests : IAsyncLifetime
{
    private const string SpecRequest = """
        {
          "profileIdOrAlias": "cloudflare-clairic",
          "state": "Order A-88412 was delivered on 3 October. I paid 701,10 kr. The mug and teapot were broken.",
          "referenceDate": "2026-10-20",
          "fields": [
            { "$type": "text",   "id": "orderId",  "instructions": "The order number" },
            { "$type": "number", "id": "amount",   "instructions": "Amount paid", "decimals": 2, "unit": "DKK" },
            { "$type": "date",   "id": "deliveredOn", "instructions": "Delivery date" },
            { "$type": "list",   "id": "damaged",  "instructions": "Damaged products", "itemType": "text" }
          ]
        }
        """;

    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ExtractRequestFormattingTests).Assembly)
            .AddJsonOptions(Umbraco.AI.Web.Constants.ManagementApi.ApiName, _ => { });

        _app = builder.Build();
        _app.MapControllers();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
        }
    }

    private async Task<HttpResponseMessage> PostAsync(string json)
        => await _app!.GetTestClient().PostAsync(
            "/extraction-formatting-test/extract",
            new StringContent(json, Encoding.UTF8, "application/json"));

    #region Happy path

    [Fact]
    public async Task SpecRequest_ReturnsOk()
    {
        var response = await PostAsync(SpecRequest);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task SpecRequest_BindsEachFieldToItsTypedModel()
    {
        var response = await PostAsync(SpecRequest);

        // Trim: a string result may go out through the plain-text or the JSON formatter.
        (await response.Content.ReadAsStringAsync()).Trim('"').ShouldBe(
            "TextExtractionFieldModel,NumberExtractionFieldModel,DateExtractionFieldModel,ListExtractionFieldModel");
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task MissingDiscriminator_ReturnsBadRequest()
    {
        var response = await PostAsync("""{ "state": "x", "fields": [ { "id": "a", "instructions": "x" } ] }""");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnknownDiscriminator_ReturnsBadRequest()
    {
        var response = await PostAsync("""{ "state": "x", "fields": [ { "$type": "estimate", "id": "a", "instructions": "x" } ] }""");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion
}

[ApiController]
[JsonOptionsName(Umbraco.AI.Web.Constants.ManagementApi.ApiName)]
[Route("extraction-formatting-test")]
public class ExtractionFormattingTestController : ControllerBase
{
    // Mirrors ExtractController's [JsonOptionsName]-tagged shape without any of its auth/versioning/
    // capability-gate concerns. This controller only proves discriminator-format handling.
    [HttpPost("extract")]
    public IActionResult Extract([FromBody] ExtractRequestModel model)
        => Ok(string.Join(",", model.Fields.Select(f => f.GetType().Name)));
}
