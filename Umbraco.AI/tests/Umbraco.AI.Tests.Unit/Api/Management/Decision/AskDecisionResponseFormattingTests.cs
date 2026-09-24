// DR-4 — Ask decisions over the Management API: real MVC *output* formatting for POST decision/ask.
//
// AskDecisionControllerTests exercises the controller directly and asserts against the returned
// IActionResult's .Value (a strongly-typed DecisionResponseModel), so it can't see whether the
// serialized HTTP *response body* actually carries the `$type` discriminator SPEC.md promises. Its
// SerializesWithTheBinaryDiscriminator test gets close, but calls JsonSerializer.Serialize<DecisionResponseModel>
// directly against the already-cast base type — that's exactly the polymorphic contract ASP.NET Core's
// own output formatter does NOT get for free from an OkObjectResult, because OkObjectResult's constructor
// takes `object? value` and erases the compile-time DecisionResponseModel type. A regression here (e.g.
// reverting to plain `Ok(mapped)`) would still pass that test while breaking real HTTP traffic.
//
// This test instead runs the actual AskDecisionController.Ask(...) result through ASP.NET Core's real
// output-formatter pipeline — the same NamedSystemTextJsonOutputFormatter wiring
// UmbracoAIUmbracoBuilderExtensions.AddJsonOptions registers via [JsonOptionsName], as
// AskDecisionRequestFormattingTests already does for the *input* side. The controller instance is real;
// only its service dependencies are mocked, exactly as AskDecisionControllerTests does.
#pragma warning disable UMBRACOAI_DECISION // Tests the experimental Decision controller

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Decision.Controllers;
using Umbraco.AI.Web.Api.Management.Decision.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Decision;

public class AskDecisionResponseFormattingTests : IAsyncLifetime
{
    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AskDecisionResponseFormattingTests).Assembly)
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

    private async Task<string> GetJsonAsync(string route)
    {
        var response = await _app!.GetTestClient().GetAsync(route);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task BinaryResponse_IncludesTheBinaryDiscriminator()
        => (await GetJsonAsync("/decision-response-formatting-test/binary")).ShouldContain("\"$type\":\"binary\"");

    [Fact]
    public async Task ChoiceResponse_IncludesTheChoiceDiscriminator()
        => (await GetJsonAsync("/decision-response-formatting-test/choice")).ShouldContain("\"$type\":\"choice\"");

    [Fact]
    public async Task ScoreResponse_IncludesTheScoreDiscriminator()
        => (await GetJsonAsync("/decision-response-formatting-test/score")).ShouldContain("\"$type\":\"score\"");
}

// Mirrors AskDecisionController's [JsonOptionsName]-tagged shape (see AskDecisionRequestFormattingTests'
// DecisionFormattingTestController) without its auth/versioning/capability-gate concerns, which are
// covered separately by AskDecisionControllerTests and DecisionCapabilityGateFilterTests. Each action
// drives the real AskDecisionController.Ask(...) with a mocked IAIDecisionService and returns the exact
// IActionResult it produces, so the assertions above exercise production response construction.
[ApiController]
[JsonOptionsName(Umbraco.AI.Web.Constants.ManagementApi.ApiName)]
[Route("decision-response-formatting-test")]
public class DecisionResponseFormattingTestController : ControllerBase
{
    [HttpGet("binary")]
    public Task<IActionResult> Binary()
    {
        var decisionService = new Mock<IAIDecisionService>();
        decisionService
            .Setup(x => x.AskAsync(
                It.IsAny<Action<AIDecisionBuilder>>(),
                It.IsAny<AIDecisionQuestion<AIBinaryDecisionResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIBinaryDecisionResponse { Probability = 0.99, ModelId = "jev-1.13.0" });

        return AskAsync(
            decisionService,
            new BinaryDecisionQuestionModel { Instructions = "Is this spam?", Context = "Buy cheap watches" });
    }

    [HttpGet("choice")]
    public Task<IActionResult> Choice()
    {
        var decisionService = new Mock<IAIDecisionService>();
        decisionService
            .Setup(x => x.AskAsync(
                It.IsAny<Action<AIDecisionBuilder>>(),
                It.IsAny<AIDecisionQuestion<AIChoiceDecisionResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIChoiceDecisionResponse
            {
                Choice = "seo",
                ChoiceConfidence = 0.91,
                Probabilities = new Dictionary<string, double> { ["seo"] = 0.91, ["other"] = 0.09 },
            });

        return AskAsync(
            decisionService,
            new ChoiceDecisionQuestionModel
            {
                Instructions = "Which topic?",
                Options = [new DecisionOptionModel { Key = "seo" }, new DecisionOptionModel { Key = "other" }],
            });
    }

    [HttpGet("score")]
    public Task<IActionResult> Score()
    {
        var decisionService = new Mock<IAIDecisionService>();
        decisionService
            .Setup(x => x.AskAsync(
                It.IsAny<Action<AIDecisionBuilder>>(),
                It.IsAny<AIDecisionQuestion<AIScoreDecisionResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIScoreDecisionResponse
            {
                Score = 1.8,
                Level = "good",
                ScoreConfidence = 0.8,
                Probabilities = new Dictionary<string, double> { ["poor"] = 0.05, ["ok"] = 0.15, ["good"] = 0.8 },
            });

        return AskAsync(
            decisionService,
            new ScoreDecisionQuestionModel { Instructions = "How good?", Levels = ["poor", "ok", "good"] });
    }

    private static Task<IActionResult> AskAsync(Mock<IAIDecisionService> decisionService, DecisionQuestionModel question)
    {
        var experimentalFeatures = new Mock<IAIExperimentalFeatures>();
        experimentalFeatures.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(true);

        var controller = new AskDecisionController(
            decisionService.Object,
            new Mock<IAIProfileService>().Object,
            experimentalFeatures.Object);

        return controller.Ask(new AskDecisionRequestModel { Question = question });
    }
}
