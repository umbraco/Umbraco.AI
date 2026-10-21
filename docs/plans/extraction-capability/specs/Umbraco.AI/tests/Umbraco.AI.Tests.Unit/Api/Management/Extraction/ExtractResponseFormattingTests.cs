// EX-6 — Extract over the Management API (AC2-AC4, AC7: the real serialized response body)
//
// ExtractControllerTests asserts against the returned IActionResult's .Value, so it can't see whether the
// serialized HTTP body carries each answer's `$type`, or whether field ids survive the API's camelCase
// dictionary key policy. This runs the real ExtractController.Extract(...) result through ASP.NET Core's
// real output-formatter pipeline, copying AskDecisionResponseFormattingTests. Answer keys must come back
// exactly as sent (ExactKeyDictionaryJsonConverter), and `$type` needs the controller's
// OkObjectResult { DeclaredType = typeof(ExtractResponseModel) } (ARCHITECTURE.md decision 7).
#pragma warning disable UMBRACOAI_EXTRACTION // Tests the experimental Extraction controller

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Extraction.Controllers;
using Umbraco.AI.Web.Api.Management.Extraction.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Extraction;

public class ExtractResponseFormattingTests : IAsyncLifetime
{
    private WebApplication? _app;
    private string _json = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ExtractResponseFormattingTests).Assembly)
            .AddJsonOptions(Umbraco.AI.Web.Constants.ManagementApi.ApiName, options =>
            {
                // Mirror the naming policies UmbracoAIUmbracoBuilderExtensions.AddJsonOptions applies to the
                // Management API, so key casing is asserted against what real traffic gets.
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });

        _app = builder.Build();
        _app.MapControllers();
        await _app.StartAsync();

        var response = await _app.GetTestClient().GetAsync("/extraction-response-formatting-test/extract");
        _json = await response.Content.ReadAsStringAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
        }
    }

    // Shouldly's string ShouldContain ignores case by default, which would hide exactly the key-casing bug.

    [Fact(Skip = "Pending")]
    public void TextAnswer_IncludesTheTextDiscriminator()
        => _json.ShouldContain("\"orderId\":{\"$type\":\"text\"", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void NumberAnswer_IncludesTheNumberDiscriminator()
        => _json.ShouldContain("\"amount\":{\"$type\":\"number\"", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void DateAnswer_IncludesTheDateDiscriminator()
        => _json.ShouldContain("\"deliveredOn\":{\"$type\":\"date\"", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void ListAnswer_IncludesTheListDiscriminator()
        => _json.ShouldContain("\"damaged\":{\"$type\":\"list\"", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void MixedCaseFieldId_KeepsItsCasing()
        => _json.ShouldContain("\"deliveredOn\":", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void CapitalisedFieldId_IsNotCamelCased()
        => _json.ShouldContain("\"OrderID\":", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void DateValue_IsAnIsoDate()
        => _json.ShouldContain("\"value\":\"2026-10-03\"", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void Response_IncludesTheModelId()
        => _json.ShouldContain("\"modelId\":\"@cf/cloudflare/clairic\"", Case.Sensitive);

    [Fact(Skip = "Pending")]
    public void Response_IncludesTheUsage()
        => _json.ShouldContain("\"inputTokenCount\":612", Case.Sensitive);
}

// Mirrors ExtractController's [JsonOptionsName]-tagged shape without its auth/versioning/capability-gate
// concerns (covered by ExtractControllerTests and ExtractAuthorizationTests). The action drives the real
// ExtractController.Extract(...) with a mocked IAIExtractionService and returns the exact IActionResult it
// produces, so the assertions above exercise production response construction.
[ApiController]
[JsonOptionsName(Umbraco.AI.Web.Constants.ManagementApi.ApiName)]
[Route("extraction-response-formatting-test")]
public class ExtractionResponseFormattingTestController : ControllerBase
{
    [HttpGet("extract")]
    public Task<IActionResult> Extract()
    {
        var service = new Mock<IAIExtractionService>();
        service
            .Setup(x => x.GetExtractionResponseAsync(
                It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer>
                {
                    ["orderId"] = new AITextExtractionAnswer { Value = "A-88412", Span = new AIExtractionSpan(6, 13), Confidence = 0.9999 },
                    ["OrderID"] = new AITextExtractionAnswer { Value = "A-88412" },
                    ["amount"] = new AINumberExtractionAnswer { Value = 701.10m, Confidence = 0.9999 },
                    ["deliveredOn"] = new AIDateExtractionAnswer { Value = new DateOnly(2026, 10, 3), Confidence = 0.998 },
                    ["damaged"] = new AIListExtractionAnswer { Values = ["mug", "teapot"], Spans = [], Confidence = 0.99 },
                },
                ModelId = "@cf/cloudflare/clairic",
                Usage = new Microsoft.Extensions.AI.UsageDetails { InputTokenCount = 612, OutputTokenCount = 0, TotalTokenCount = 612 },
            });

        var experimentalFeatures = new Mock<IAIExperimentalFeatures>();
        experimentalFeatures.Setup(x => x.IsCapabilityEnabled(AICapability.Extraction)).Returns(true);

        var controller = new ExtractController(service.Object, new Mock<IAIProfileService>().Object, experimentalFeatures.Object);

        return controller.Extract(new ExtractRequestModel
        {
            State = "Order A-88412 was delivered on 3 October. I paid 701,10 kr. The mug and teapot were broken.",
            Fields =
            [
                new TextExtractionFieldModel { Id = "orderId", Instructions = "The order number" },
                new TextExtractionFieldModel { Id = "OrderID", Instructions = "The order number, again" },
                new NumberExtractionFieldModel { Id = "amount", Instructions = "Amount paid" },
                new DateExtractionFieldModel { Id = "deliveredOn", Instructions = "Delivery date" },
                new ListExtractionFieldModel { Id = "damaged", Instructions = "Damaged products", ItemType = "text" },
            ],
        });
    }
}
