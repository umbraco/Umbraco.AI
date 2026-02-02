using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agui;
using Umbraco.AI.Agui.Events;
using Umbraco.AI.Agui.Events.Lifecycle;
using Umbraco.AI.Agui.Events.Messages;
using Umbraco.AI.Agui.Events.Tools;
using Umbraco.AI.Agui.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agui;

public class AguiStreamingServiceTests
{
    private readonly Mock<IAguiMessageConverter> _mockConverter;
    private readonly ILogger<AguiStreamingService> _logger;
    private readonly AguiStreamingService _service;

    public AguiStreamingServiceTests()
    {
        _mockConverter = new Mock<IAguiMessageConverter>();
        _logger = NullLogger<AguiStreamingService>.Instance;
        _service = new AguiStreamingService(
            _mockConverter.Object,
            _logger);

        // Default converter setup
        _mockConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AguiMessage>?>()))
            .Returns(new List<ChatMessage>());
    }

    #region Basic Event Flow Tests

    [Fact]
    public async Task StreamAgentAsync_EmitsRunStartedFirst()
    {
        // Arrange
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        events.First().ShouldBeOfType<RunStartedEvent>();
    }

    [Fact]
    public async Task StreamAgentAsync_EmitsRunFinishedLast()
    {
        // Arrange
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        events.Last().ShouldBeOfType<RunFinishedEvent>();
    }

    [Fact]
    public async Task StreamAgentAsync_NoContent_EmitsStartAndFinishOnly()
    {
        // Arrange
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        events.Count.ShouldBe(2);
        events[0].ShouldBeOfType<RunStartedEvent>();
        events[1].ShouldBeOfType<RunFinishedEvent>();
    }

    #endregion

    #region Text Streaming Tests

    [Fact]
    public async Task StreamAgentAsync_WithTextContent_EmitsTextChunks()
    {
        // Arrange
        var updates = CreateTextUpdates("Hello", " ", "World");
        var agent = CreateMockAgent(updates);
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        var textEvents = events.OfType<TextMessageChunkEvent>().ToList();
        textEvents.Count.ShouldBe(3);
        textEvents[0].Delta.ShouldBe("Hello");
        textEvents[1].Delta.ShouldBe(" ");
        textEvents[2].Delta.ShouldBe("World");
    }

    [Fact]
    public async Task StreamAgentAsync_EmptyText_DoesNotEmitTextChunk()
    {
        // Arrange
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, ""),
            new(ChatRole.Assistant, (string?)null)
        };
        var agent = CreateMockAgent(updates.ToAsyncEnumerable());
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        events.OfType<TextMessageChunkEvent>().ShouldBeEmpty();
    }

    #endregion

    #region Tool Call Tests

    [Fact]
    public async Task StreamAgentAsync_WithBackendToolCall_EmitsToolCallEvent()
    {
        // Arrange
        var functionCall = new FunctionCallContent("call-123", "search", new Dictionary<string, object?> { ["query"] = "test" });
        var updates = CreateToolCallUpdates(functionCall);
        var agent = CreateMockAgent(updates);
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request, frontendTools: null);

        // Assert
        var toolCallEvent = events.OfType<ToolCallChunkEvent>().FirstOrDefault();
        toolCallEvent.ShouldNotBeNull();
        toolCallEvent.ToolCallId.ShouldBe("call-123");
        toolCallEvent.ToolCallName.ShouldBe("search");
    }

    [Fact]
    public async Task StreamAgentAsync_WithFrontendToolCall_TracksAsFrontend()
    {
        // Arrange
        var functionCall = new FunctionCallContent("call-frontend", "confirm_action", null);
        var updates = CreateToolCallUpdates(functionCall);
        var agent = CreateMockAgent(updates);
        var request = CreateRequest();
        var frontendTools = new List<AITool> { CreateMockAITool("confirm_action") };

        // Act
        var events = await CollectEvents(agent, request, frontendTools);

        // Assert
        var finishedEvent = events.OfType<RunFinishedEvent>().First();
        finishedEvent.Outcome.ShouldBe(AguiRunOutcome.Interrupt);
        finishedEvent.Interrupt.ShouldNotBeNull();
    }

    [Fact]
    public async Task StreamAgentAsync_WithToolResult_EmitsToolResultEvent()
    {
        // Arrange
        var functionCall = new FunctionCallContent("call-123", "tool", null);
        var functionResult = new FunctionResultContent("call-123", new { data = "result" });
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, new List<AIContent> { functionCall }),
            new(ChatRole.Tool, new List<AIContent> { functionResult })
        };
        var agent = CreateMockAgent(updates.ToAsyncEnumerable());
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        var toolResultEvent = events.OfType<ToolCallResultEvent>().FirstOrDefault();
        toolResultEvent.ShouldNotBeNull();
        toolResultEvent.ToolCallId.ShouldBe("call-123");
    }

    [Fact]
    public async Task StreamAgentAsync_FrontendToolResult_DoesNotEmitToolResultEvent()
    {
        // Arrange
        var functionCall = new FunctionCallContent("call-frontend", "confirm", null);
        var functionResult = new FunctionResultContent("call-frontend", "confirmed");
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, new List<AIContent> { functionCall }),
            new(ChatRole.Tool, new List<AIContent> { functionResult })
        };
        var agent = CreateMockAgent(updates.ToAsyncEnumerable());
        var request = CreateRequest();
        var frontendTools = new List<AITool> { CreateMockAITool("confirm") };

        // Act
        var events = await CollectEvents(agent, request, frontendTools);

        // Assert
        events.OfType<ToolCallResultEvent>().ShouldBeEmpty();
    }

    #endregion

    #region Outcome Tests

    [Fact]
    public async Task StreamAgentAsync_NoFrontendTools_ReturnsSuccessOutcome()
    {
        // Arrange
        var updates = CreateTextUpdates("Hello");
        var agent = CreateMockAgent(updates);
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        var finishedEvent = events.OfType<RunFinishedEvent>().First();
        finishedEvent.Outcome.ShouldBe(AguiRunOutcome.Success);
    }

    [Fact]
    public async Task StreamAgentAsync_WithFrontendTools_ReturnsInterruptOutcome()
    {
        // Arrange
        var functionCall = new FunctionCallContent("call-1", "frontend_tool", null);
        var updates = CreateToolCallUpdates(functionCall);
        var agent = CreateMockAgent(updates);
        var request = CreateRequest();
        var frontendTools = new List<AITool> { CreateMockAITool("frontend_tool") };

        // Act
        var events = await CollectEvents(agent, request, frontendTools);

        // Assert
        var finishedEvent = events.OfType<RunFinishedEvent>().First();
        finishedEvent.Outcome.ShouldBe(AguiRunOutcome.Interrupt);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task StreamAgentAsync_OnError_EmitsErrorAndFinished()
    {
        // Arrange
        var agent = CreateThrowingAgent(new InvalidOperationException("Test error"));
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        var errorEvent = events.OfType<RunErrorEvent>().FirstOrDefault();
        errorEvent.ShouldNotBeNull();
        errorEvent.Message.ShouldBe("Test error");
        errorEvent.Code.ShouldBe("STREAMING_ERROR");

        var finishedEvent = events.OfType<RunFinishedEvent>().First();
        finishedEvent.Outcome.ShouldBe(AguiRunOutcome.Error);
    }

    [Fact]
    public async Task StreamAgentAsync_WhenAgentThrowsCancellation_PropagatesException()
    {
        // Arrange
        var agent = CreateMockAgent(ThrowingCancellationAsyncEnumerable());
        var request = CreateRequest();

        // Act & Assert - cancellation from agent should propagate
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _service.StreamAgentAsync(agent, request, null, CancellationToken.None))
            {
                // Consume
            }
        });
    }

    #endregion

    #region Resume Flow Tests

    [Fact]
    public async Task StreamAgentAsync_WithResume_CallsConverterWithMessages()
    {
        // Arrange
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());
        var resumePayload = JsonSerializer.SerializeToElement(new
        {
            toolResults = new[]
            {
                new { toolCallId = "call-1", result = new { approved = true } }
            }
        });
        var request = new AguiRunRequest
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages = new List<AguiMessage>
            {
                new() { Role = AguiMessageRole.User, Content = "Hello" }
            },
            Resume = new AguiResumeInfo
            {
                InterruptId = "int-123",
                Payload = resumePayload
            }
        };

        // Act
        await CollectEvents(agent, request);

        // Assert
        _mockConverter.Verify(
            x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AguiMessage>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private async Task<List<IAguiEvent>> CollectEvents(
        AIAgent agent,
        AguiRunRequest request,
        IEnumerable<AITool>? frontendTools = null)
    {
        var events = new List<IAguiEvent>();
        await foreach (var evt in _service.StreamAgentAsync(agent, request, frontendTools, CancellationToken.None))
        {
            events.Add(evt);
        }
        return events;
    }

    private static AguiRunRequest CreateRequest(string? threadId = null, string? runId = null)
    {
        return new AguiRunRequest
        {
            ThreadId = threadId ?? "thread-test",
            RunId = runId ?? "run-test",
            Messages = new List<AguiMessage>
            {
                new() { Role = AguiMessageRole.User, Content = "Hello" }
            }
        };
    }

    private static AIAgent CreateMockAgent(IAsyncEnumerable<ChatResponseUpdate> updates)
    {
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(updates);

        return new ChatClientAgent(mockChatClient.Object);
    }

    private static AIAgent CreateThrowingAgent(Exception exception)
    {
        return CreateMockAgent(ThrowingAsyncEnumerable(exception));
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingAsyncEnumerable(Exception exception)
    {
        await Task.Yield();
        throw exception;
#pragma warning disable CS0162 // Unreachable code detected - required for async enumerable
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingCancellationAsyncEnumerable()
    {
        await Task.Yield();
        throw new OperationCanceledException();
#pragma warning disable CS0162 // Unreachable code detected - required for async enumerable
        yield break;
#pragma warning restore CS0162
    }

    private static IAsyncEnumerable<ChatResponseUpdate> CreateTextUpdates(params string[] texts)
    {
        return texts.Select(t => new ChatResponseUpdate(ChatRole.Assistant, t)).ToAsyncEnumerable();
    }

    private static IAsyncEnumerable<ChatResponseUpdate> CreateToolCallUpdates(FunctionCallContent functionCall)
    {
        var update = new ChatResponseUpdate(ChatRole.Assistant, new List<AIContent> { functionCall });
        return new[] { update }.ToAsyncEnumerable();
    }

    private static AITool CreateMockAITool(string name)
    {
        var mockFunction = new Mock<AIFunction>();
        mockFunction.Setup(x => x.Name).Returns(name);
        return mockFunction.Object;
    }

    #endregion
}

internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}
