// DR-4 — Ask decisions over the Management API: real JSON-body formatting for POST decision/ask.
//
// AskDecisionControllerTests exercises the controller directly (bypassing HTTP entirely), so it can't
// see how ASP.NET Core actually deserializes the request body. These tests instead go through the real
// MVC input-formatter pipeline — the same one production traffic uses — to pin that a missing or
// unknown `$type` discriminator on `question` becomes a 400 ProblemDetails, never an unhandled 500. See
// SPEC.md's guarantees for POST decision/ask.
//
// This deliberately doesn't boot a full Umbraco host (IUmbracoBuilder needs a heavier composition
// context than a formatting test warrants). Instead it calls the exact same underlying
// Umbraco.Cms.Api.Common building blocks UmbracoAIUmbracoBuilderExtensions.AddJsonOptions wraps —
// `AddJsonOptions(name, configure)` (which registers Umbraco's own NamedSystemTextJsonInputFormatter)
// plus a `[JsonOptionsName]`-tagged endpoint (as UmbracoAICoreManagementControllerBase applies to every
// AI management controller, including AskDecisionController).
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.AI.Web.Api.Management.Decision.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Decision;

public class AskDecisionRequestFormattingTests : IAsyncLifetime
{
    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AskDecisionRequestFormattingTests).Assembly)
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
            "/decision-formatting-test/ask",
            new StringContent(json, Encoding.UTF8, "application/json"));

    [Fact]
    public async Task MissingDiscriminator_ReturnsBadRequest()
    {
        var response = await PostAsync("""{ "question": { "instructions": "x" } }""");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnknownDiscriminator_ReturnsBadRequest()
    {
        var response = await PostAsync("""{ "question": { "$type": "bogus", "instructions": "x" } }""");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidDiscriminator_ReturnsOk()
    {
        var response = await PostAsync(
            """{ "question": { "$type": "binary", "instructions": "Is this spam?" } }""");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }
}

[ApiController]
[JsonOptionsName(Umbraco.AI.Web.Constants.ManagementApi.ApiName)]
[Route("decision-formatting-test")]
public class DecisionFormattingTestController : ControllerBase
{
    // Mirrors AskDecisionController's [JsonOptionsName]-tagged shape without any of its auth/versioning/
    // capability-gate concerns — those are covered separately by AskDecisionControllerTests and
    // DecisionCapabilityGateFilterTests. This controller only proves discriminator-format handling.
    [HttpPost("ask")]
    public IActionResult Ask([FromBody] AskDecisionRequestModel model) => Ok(model.Question.GetType().Name);
}
