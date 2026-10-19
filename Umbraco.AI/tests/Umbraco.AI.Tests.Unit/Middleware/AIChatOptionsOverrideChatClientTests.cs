using Microsoft.Extensions.AI;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AIChatOptionsOverrideChatClientTests
{
    private readonly AIRuntimeContext _runtimeContext = new([]);
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock = new();

    public AIChatOptionsOverrideChatClientTests()
    {
        _contextAccessorMock.Setup(x => x.Context).Returns(_runtimeContext);
    }

    [Fact]
    public async Task GetResponseAsync_WithOverride_KeepsCallerOptionsTheOverrideDoesNotSet()
    {
        // The function-invoking client links a tool result to the previous response through
        // ConversationId. Dropping it makes OpenAI reject the call ("No tool call found").
        _runtimeContext.SetValue(Constants.ContextKeys.ChatOptionsOverride, new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
        });
        var inner = new FakeChatClient();
        var client = CreateClient(inner);
        var factory = (Func<IChatClient, object?>)(_ => null);

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Hello")],
            new ChatOptions
            {
                ConversationId = "resp_123",
                Instructions = "Be brief.",
                AllowMultipleToolCalls = false,
                RawRepresentationFactory = factory,
                Temperature = 0.2f,
            });

        var received = inner.ReceivedOptions[0].ShouldNotBeNull();
        received.ConversationId.ShouldBe("resp_123");
        received.Instructions.ShouldBe("Be brief.");
        received.AllowMultipleToolCalls.ShouldBe(false);
        received.RawRepresentationFactory.ShouldBeSameAs(factory);
        received.Temperature.ShouldBe(0.2f);
        received.ResponseFormat.ShouldBe(ChatResponseFormat.Json);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithOverride_KeepsConversationId()
    {
        _runtimeContext.SetValue(Constants.ContextKeys.ChatOptionsOverride, new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
        });
        var inner = new FakeChatClient(["Hi"]);
        var client = CreateClient(inner);

        await foreach (var _ in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Hello")],
            new ChatOptions { ConversationId = "resp_123" }))
        {
        }

        inner.ReceivedOptions[0].ShouldNotBeNull().ConversationId.ShouldBe("resp_123");
    }

    [Fact]
    public async Task GetResponseAsync_WithOverride_OverrideValuesWinAndCallerOptionsAreNotChanged()
    {
        _runtimeContext.SetValue(Constants.ContextKeys.ChatOptionsOverride, new ChatOptions
        {
            Temperature = 0.9f,
        });
        var inner = new FakeChatClient();
        var client = CreateClient(inner);
        var callerOptions = new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 100 };

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Hello")], callerOptions);

        var received = inner.ReceivedOptions[0].ShouldNotBeNull();
        received.Temperature.ShouldBe(0.9f);
        received.MaxOutputTokens.ShouldBe(100);
        callerOptions.Temperature.ShouldBe(0.2f);
    }

    [Fact]
    public async Task GetResponseAsync_WithoutCallerOptions_UsesACopyOfTheOverride()
    {
        var overrideOptions = new ChatOptions { Temperature = 0.9f };
        _runtimeContext.SetValue(Constants.ContextKeys.ChatOptionsOverride, overrideOptions);
        var inner = new FakeChatClient();
        var client = CreateClient(inner);

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Hello")]);

        var received = inner.ReceivedOptions[0].ShouldNotBeNull();
        received.Temperature.ShouldBe(0.9f);
        received.ShouldNotBeSameAs(overrideOptions);
    }

    private AIChatOptionsOverrideChatClient CreateClient(IChatClient inner)
        => new(inner, _contextAccessorMock.Object);
}
