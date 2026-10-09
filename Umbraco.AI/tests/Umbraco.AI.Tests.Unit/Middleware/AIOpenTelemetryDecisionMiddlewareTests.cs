#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

// DC-3 — per-kind "gen_ai.request.kind" / question-count batch tagging now lives in
// AIOpenTelemetryDecisionBatchTests, which asserts through the real GetResponseAsync -> Activity
// pipeline. This class keeps only the one assertion that isn't about tag content.
public class AIOpenTelemetryDecisionMiddlewareTests
{
    [Fact]
    public void Apply_ReturnsWrappedClient()
    {
        // Arrange
        var innerClient = new FakeDecisionClient();
        var middleware = new AIOpenTelemetryDecisionMiddleware();

        // Act
        var result = middleware.Apply(innerClient);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(innerClient);
    }
}
