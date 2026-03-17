using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AIOpenTelemetryChatMiddlewareTests
{
    [Fact]
    public void Apply_ReturnsWrappedClient()
    {
        // Arrange
        var innerClient = new FakeChatClient();
        var middleware = new AIOpenTelemetryChatMiddleware(NullLoggerFactory.Instance);

        // Act
        var result = middleware.Apply(innerClient);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(innerClient);
    }

    [Fact]
    public async Task Apply_WrappedClient_DelegatesToInnerClient()
    {
        // Arrange
        var innerClient = new FakeChatClient("Hello from inner");
        var middleware = new AIOpenTelemetryChatMiddleware(NullLoggerFactory.Instance);
        var wrappedClient = middleware.Apply(innerClient);

        // Act
        var response = await wrappedClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Hi")]);

        // Assert
        response.ShouldNotBeNull();
        innerClient.ReceivedMessages.Count.ShouldBe(1);
    }
}
