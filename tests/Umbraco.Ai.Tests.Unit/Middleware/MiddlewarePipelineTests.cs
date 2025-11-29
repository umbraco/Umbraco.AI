using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Middleware;

public class MiddlewarePipelineTests
{
    #region Middleware application order

    [Fact]
    public void Middleware_AppliedInCollectionOrder()
    {
        // Arrange
        var applicationOrder = new List<string>();

        var middleware1 = new TestMiddleware("First", applicationOrder);
        var middleware2 = new TestMiddleware("Second", applicationOrder);
        var middleware3 = new TestMiddleware("Third", applicationOrder);

        var collection = new AiChatMiddlewareCollection(() => new[]
        {
            middleware1,
            middleware2,
            middleware3
        });

        var baseClient = new FakeChatClient();

        // Act
        var client = baseClient as IChatClient;
        foreach (var middleware in collection)
        {
            client = middleware.Apply(client);
        }

        // Assert
        applicationOrder.Count.ShouldBe(3);
        applicationOrder[0].ShouldBe("First");
        applicationOrder[1].ShouldBe("Second");
        applicationOrder[2].ShouldBe("Third");
    }

    #endregion

    #region Empty middleware collection

    [Fact]
    public void EmptyMiddlewareCollection_ReturnsOriginalClient()
    {
        // Arrange
        var collection = new AiChatMiddlewareCollection(() => Enumerable.Empty<IAiChatMiddleware>());
        var baseClient = new FakeChatClient();

        // Act
        var client = baseClient as IChatClient;
        foreach (var middleware in collection)
        {
            client = middleware.Apply(client);
        }

        // Assert
        client.ShouldBeSameAs(baseClient);
    }

    #endregion

    #region Each middleware wraps previous client

    [Fact]
    public void EachMiddleware_WrapsPreviousClient()
    {
        // Arrange
        var wrappedClients = new List<IChatClient>();

        var middleware1 = new CapturingMiddleware(wrappedClients);
        var middleware2 = new CapturingMiddleware(wrappedClients);

        var collection = new AiChatMiddlewareCollection(() => new[]
        {
            middleware1,
            middleware2
        });

        var baseClient = new FakeChatClient();

        // Act
        var client = baseClient as IChatClient;
        foreach (var middleware in collection)
        {
            client = middleware.Apply(client);
        }

        // Assert
        wrappedClients.Count.ShouldBe(2);
        wrappedClients[0].ShouldBeSameAs(baseClient); // First middleware received base client
        wrappedClients[1].ShouldNotBeSameAs(baseClient); // Second middleware received wrapped client
    }

    #endregion

    #region Middleware can modify requests

    [Fact]
    public async Task Middleware_CanInterceptAndModifyRequests()
    {
        // Arrange
        var modifyingMiddleware = new Mock<IAiChatMiddleware>();
        var wrappedClient = new FakeChatClient("Modified response");

        modifyingMiddleware
            .Setup(m => m.Apply(It.IsAny<IChatClient>()))
            .Returns(wrappedClient);

        var collection = new AiChatMiddlewareCollection(() => new[] { modifyingMiddleware.Object });
        var baseClient = new FakeChatClient("Original response");

        // Act
        var client = baseClient as IChatClient;
        foreach (var middleware in collection)
        {
            client = middleware.Apply(client);
        }

        var response = await client.GetResponseAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        });

        // Assert
        response.Text.ShouldBe("Modified response");
    }

    #endregion

    #region Single middleware

    [Fact]
    public void SingleMiddleware_AppliesCorrectly()
    {
        // Arrange
        var applicationOrder = new List<string>();
        var middleware = new TestMiddleware("Only", applicationOrder);

        var collection = new AiChatMiddlewareCollection(() => new[] { middleware });
        var baseClient = new FakeChatClient();

        // Act
        var client = baseClient as IChatClient;
        foreach (var mw in collection)
        {
            client = mw.Apply(client);
        }

        // Assert
        applicationOrder.Count.ShouldBe(1);
        applicationOrder[0].ShouldBe("Only");
    }

    #endregion

    #region Test helpers

    private class TestMiddleware : IAiChatMiddleware
    {
        private readonly string _name;
        private readonly List<string> _applicationOrder;

        public TestMiddleware(string name, List<string> applicationOrder)
        {
            _name = name;
            _applicationOrder = applicationOrder;
        }

        public IChatClient Apply(IChatClient client)
        {
            _applicationOrder.Add(_name);
            return new WrappingChatClient(client, _name);
        }
    }

    private class CapturingMiddleware : IAiChatMiddleware
    {
        private readonly List<IChatClient> _capturedClients;

        public CapturingMiddleware(List<IChatClient> capturedClients)
        {
            _capturedClients = capturedClients;
        }

        public IChatClient Apply(IChatClient client)
        {
            _capturedClients.Add(client);
            return new WrappingChatClient(client, "Captured");
        }
    }

    private class WrappingChatClient : DelegatingChatClient
    {
        public string MiddlewareName { get; }

        public WrappingChatClient(IChatClient innerClient, string middlewareName)
            : base(innerClient)
        {
            MiddlewareName = middlewareName;
        }
    }

    #endregion
}
