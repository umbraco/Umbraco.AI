using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Middleware;

public class AiTrackingChatClientTests
{
    #region GetResponseAsync

    [Fact]
    public async Task GetResponseAsync_WithTextResponse_CapturesLastResponseMessages()
    {
        // Arrange
        var responseMessage = new ChatMessage(ChatRole.Assistant, "Hello, world!");
        var fakeClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(responseMessage)));

        var trackingClient = new AiTrackingChatClient(fakeClient);

        // Act
        await trackingClient.GetResponseAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "Hi")
        });

        // Assert
        trackingClient.LastResponseMessages.ShouldNotBeNull();
        trackingClient.LastResponseMessages.Count.ShouldBe(1);
        trackingClient.LastResponseMessages[0].Role.ShouldBe(ChatRole.Assistant);
        trackingClient.LastResponseMessages[0].Text.ShouldBe("Hello, world!");
    }

    [Fact]
    public async Task GetResponseAsync_WithToolCalls_CapturesResponseMessagesWithToolCalls()
    {
        // Arrange
        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "get_weather",
            arguments: new Dictionary<string, object?> { ["city"] = "London" });

        var responseMessage = new ChatMessage(ChatRole.Assistant, new List<AIContent>
        {
            new TextContent("Let me check the weather."),
            functionCall
        });

        var fakeClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(responseMessage)));

        var trackingClient = new AiTrackingChatClient(fakeClient);

        // Act
        await trackingClient.GetResponseAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "What's the weather?")
        });

        // Assert
        trackingClient.LastResponseMessages.ShouldNotBeNull();
        trackingClient.LastResponseMessages.Count.ShouldBe(1);

        var message = trackingClient.LastResponseMessages[0];
        message.Role.ShouldBe(ChatRole.Assistant);

        // Verify tool calls are captured
        var contents = message.Contents.ToList();
        contents.Count.ShouldBe(2);

        var textContent = contents[0].ShouldBeOfType<TextContent>();
        textContent.Text.ShouldBe("Let me check the weather.");

        var toolCall = contents[1].ShouldBeOfType<FunctionCallContent>();
        toolCall.CallId.ShouldBe("tc_001");
        toolCall.Name.ShouldBe("get_weather");
        toolCall.Arguments.ShouldNotBeNull();
        toolCall.Arguments!.ShouldContainKeyAndValue("city", "London");
    }

    [Fact]
    public async Task GetResponseAsync_WithMultipleToolCalls_CapturesAll()
    {
        // Arrange
        var call1 = new FunctionCallContent(
            callId: "tc_001",
            name: "get_weather",
            arguments: new Dictionary<string, object?> { ["city"] = "London" });

        var call2 = new FunctionCallContent(
            callId: "tc_002",
            name: "get_time",
            arguments: new Dictionary<string, object?> { ["timezone"] = "UTC" });

        var responseMessage = new ChatMessage(ChatRole.Assistant, new List<AIContent>
        {
            call1,
            call2
        });

        var fakeClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(responseMessage)));

        var trackingClient = new AiTrackingChatClient(fakeClient);

        // Act
        await trackingClient.GetResponseAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "Check weather and time")
        });

        // Assert
        trackingClient.LastResponseMessages.ShouldNotBeNull();
        trackingClient.LastResponseMessages.Count.ShouldBe(1);

        var functionCalls = trackingClient.LastResponseMessages[0].Contents
            .OfType<FunctionCallContent>()
            .ToList();

        functionCalls.Count.ShouldBe(2);
        functionCalls[0].Name.ShouldBe("get_weather");
        functionCalls[1].Name.ShouldBe("get_time");
    }

    [Fact]
    public async Task GetResponseAsync_WithUsageDetails_CapturesUsage()
    {
        // Arrange
        var responseMessage = new ChatMessage(ChatRole.Assistant, "Response");
        var usage = new UsageDetails
        {
            InputTokenCount = 10,
            OutputTokenCount = 5,
            TotalTokenCount = 15
        };

        var fakeClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(responseMessage) { Usage = usage }));

        var trackingClient = new AiTrackingChatClient(fakeClient);

        // Act
        await trackingClient.GetResponseAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "Hi")
        });

        // Assert
        trackingClient.LastUsageDetails.ShouldNotBeNull();
        trackingClient.LastUsageDetails.InputTokenCount.ShouldBe(10);
        trackingClient.LastUsageDetails.OutputTokenCount.ShouldBe(5);
        trackingClient.LastUsageDetails.TotalTokenCount.ShouldBe(15);
    }

    #endregion

    #region GetStreamingResponseAsync

    [Fact]
    public async Task GetStreamingResponseAsync_WithTextUpdates_CapturesAggregatedResponseMessages()
    {
        // Arrange
        var fakeClient = new FakeChatClient("Hello world");
        var trackingClient = new AiTrackingChatClient(fakeClient);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in trackingClient.GetStreamingResponseAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "Hi")
        }))
        {
            updates.Add(update);
        }

        // Assert
        updates.Count.ShouldBeGreaterThan(0);
        trackingClient.LastResponseMessages.ShouldNotBeNull();
        trackingClient.LastResponseMessages.Count.ShouldBeGreaterThan(0);
        trackingClient.LastResponseMessages[0].Role.ShouldBe(ChatRole.Assistant);
        // The aggregated text should contain the streamed content
        trackingClient.LastResponseMessages[0].Text.ShouldContain("Hello");
    }

    [Fact]
    public async Task GetStreamingResponseAsync_YieldsUpdatesImmediately()
    {
        // Arrange
        var yieldTimes = new List<DateTime>();
        var fakeClient = new FakeChatClient("Word1 Word2 Word3");
        var trackingClient = new AiTrackingChatClient(fakeClient);

        // Act
        await foreach (var update in trackingClient.GetStreamingResponseAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "Hi")
        }))
        {
            yieldTimes.Add(DateTime.UtcNow);
        }

        // Assert - updates should have been yielded (not buffered until end)
        yieldTimes.Count.ShouldBeGreaterThan(1);

        // The times should be spread out (not all at the same time)
        // FakeChatClient adds a 1ms delay between each word
        var totalTimeSpan = (yieldTimes.Last() - yieldTimes.First()).TotalMilliseconds;
        totalTimeSpan.ShouldBeGreaterThan(0);
    }

    #endregion

    #region GetService

    [Fact]
    public void GetService_ReturnsTrackingClient()
    {
        // Arrange
        var fakeClient = new FakeChatClient();
        var trackingClient = new AiTrackingChatClient(fakeClient);

        // Act
        var service = trackingClient.GetService<AiTrackingChatClient>();

        // Assert
        service.ShouldBe(trackingClient);
    }

    #endregion
}
