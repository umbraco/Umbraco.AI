using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.Lifecycle;
using Umbraco.AI.AGUI.Events.Messages;
using Umbraco.AI.AGUI.Events.State;
using Umbraco.AI.AGUI.Events.Tools;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Tools;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIStreamingServiceTests
{
    private readonly Mock<IAGUIMessageConverter> _mockConverter;
    private readonly Mock<IAGUIFileProcessor> _mockFileProcessor;
    private readonly ILogger<AGUIStreamingService> _logger;
    private readonly AGUIStreamingService _service;

    public AGUIStreamingServiceTests()
    {
        _mockConverter = new Mock<IAGUIMessageConverter>();
        _mockFileProcessor = new Mock<IAGUIFileProcessor>();
        _logger = NullLogger<AGUIStreamingService>.Instance;
        _service = new AGUIStreamingService(
            _mockConverter.Object,
            _mockFileProcessor.Object,
            new AIToolCollection(() => []),
            _logger);

        // Default converter setup
        _mockConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns(new List<ChatMessage>());

        // Default file processor setup (pass-through)
        _mockFileProcessor
            .Setup(x => x.ProcessInboundAsync(It.IsAny<IEnumerable<AGUIMessage>?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AGUIMessage>? msgs, string _, CancellationToken _) =>
                new AGUIFileProcessorResult
                {
                    RewrittenMessages = msgs ?? [],
                    ResolvedMessages = msgs ?? []
                });
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

        // Assert (RunStarted + RunFinished — no MessagesSnapshot when no files were rewritten)
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
    public async Task StreamAgentAsync_WithErrorContent_SurfacesAsTextChunkAndKeepsRunGoing()
    {
        // Arrange — a stream that emits ErrorContent mid-response (e.g. content
        // filter, transient provider error). MEAI documents ErrorContent as
        // non-fatal, so the run should continue and the user should see the
        // error inline rather than the bare '[unknown:ErrorContent]' the chat
        // trace renders for unrecognised AIContent.
        var errorContent = new ErrorContent("Content was filtered.")
        {
            ErrorCode = "content_filter",
            Details = "Detected restricted material.",
        };
        var updates = new[]
        {
            new ChatResponseUpdate(ChatRole.Assistant, "Before. "),
            new ChatResponseUpdate(ChatRole.Assistant, new List<AIContent> { errorContent }),
            new ChatResponseUpdate(ChatRole.Assistant, "After."),
        };

        var agent = CreateMockAgent(updates.ToAsyncEnumerable());
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert: text chunks include the surrounding text and a marker for the error
        var textChunks = events.OfType<TextMessageChunkEvent>().Select(e => e.Delta).ToList();
        textChunks.ShouldContain(d => d == "Before. ");
        textChunks.ShouldContain(d => d == "After.");
        textChunks.ShouldContain(d => d.Contains("content_filter") && d.Contains("Content was filtered."));

        // Run completes normally (no RunErrorEvent — ErrorContent is non-fatal)
        events.OfType<RunErrorEvent>().ShouldBeEmpty();
        events.Last().ShouldBeOfType<RunFinishedEvent>();
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
        var interruptOutcome = finishedEvent.Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>();
        interruptOutcome.Interrupts.ShouldNotBeEmpty();
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

    [Fact]
    public async Task StreamAgentAsync_WithToolApprovalRequest_EmitsToolCallAndRegistersApprovalInterrupt()
    {
        // Arrange — stream a ToolApprovalRequestContent (what FICC emits for ApprovalRequiredAIFunction)
        var functionCall = new FunctionCallContent("call-del", "delete_thing",
            new Dictionary<string, object?> { ["id"] = "42" });
        var approvalRequest = new ToolApprovalRequestContent("call-del", functionCall);
        var update = new ChatResponseUpdate(ChatRole.Assistant, new List<AIContent> { approvalRequest });
        var agent = CreateMockAgent(new[] { update }.ToAsyncEnumerable());
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert — tool call event emitted so the frontend shows the pending call
        var toolCallEvent = events.OfType<ToolCallChunkEvent>().FirstOrDefault();
        toolCallEvent.ShouldNotBeNull();
        toolCallEvent!.ToolCallId.ShouldBe("call-del");
        toolCallEvent.ToolCallName.ShouldBe("delete_thing");

        // Assert — RunFinished carries a human_approval interrupt
        var finishedEvent = events.OfType<RunFinishedEvent>().First();
        var interruptOutcome = finishedEvent.Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>();
        interruptOutcome.Interrupts.Count.ShouldBe(1);
        var interrupt = interruptOutcome.Interrupts[0];
        interrupt.Id.ShouldBe("approval:call-del");
        interrupt.Reason.ShouldBe("human_approval");
        interrupt.ToolCallId.ShouldBe("call-del");

        // Assert — no matching tool in the (empty) collection, so falls back to a generic
        // title/message built from the tool name and raw arguments.
        interrupt.Metadata.ShouldNotBeNull();
        interrupt.Metadata!["title"].ShouldBe("delete_thing");
        interrupt.Message.ShouldContain("id");
        interrupt.Message.ShouldContain("42");
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
        finishedEvent.Outcome.ShouldBeOfType<AGUIRunOutcomeSuccess>();
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
        finishedEvent.Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task StreamAgentAsync_OnError_EmitsClassifiedErrorAndFinished()
    {
        // Arrange — unrecognised exception type falls through to the Unknown category.
        var agent = CreateThrowingAgent(new InvalidOperationException("internal-only: Test error"));
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert
        // Per AG-UI spec a run terminates with EITHER RunFinished OR RunError — never both.
        var errorEvent = events.OfType<RunErrorEvent>().FirstOrDefault();
        errorEvent.ShouldNotBeNull();
        // Raw exception text must not be surfaced to users.
        errorEvent.Message.ShouldNotContain("internal-only");
        // Code is the AIProviderErrorCategory name.
        errorEvent.Code.ShouldBe("Unknown");

        events.OfType<RunFinishedEvent>().ShouldBeEmpty();
    }

    [Fact]
    public async Task StreamAgentAsync_OnClassifiedProviderError_EmitsCategoryAndUserMessage()
    {
        // Arrange — provider SDK failures arrive pre-classified as AIProviderException
        // (the chat client is wrapped by the error-classifying decorator in the factory).
        var info = new AIProviderErrorInfo(
            AIProviderErrorCategory.Transient,
            "The AI service is briefly overloaded. Please try again in a few seconds.",
            "overloaded_error",
            "SSE error returned from server: '{...overloaded_error...}'");
        var agent = CreateThrowingAgent(new AIProviderException(info));
        var request = CreateRequest();

        // Act
        var events = await CollectEvents(agent, request);

        // Assert — the classified user message and category code reach the frontend; the raw
        // envelope text does not.
        var errorEvent = events.OfType<RunErrorEvent>().FirstOrDefault();
        errorEvent.ShouldNotBeNull();
        errorEvent.Message.ShouldBe(info.UserMessage);
        errorEvent.Message.ShouldNotContain("SSE error");
        errorEvent.Code.ShouldBe("Transient");

        events.OfType<RunFinishedEvent>().ShouldBeEmpty();
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
        var resumePayload = JsonSerializer.SerializeToElement(new { approved = true });
        var request = new AGUIRunRequest
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages = new List<AGUIMessage>
            {
                new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "Hello" }
            },
            Resume = new List<AGUIResumeEntry>
            {
                new()
                {
                    InterruptId = "call-1",
                    Status = AGUIResumeStatus.Resolved,
                    Payload = resumePayload
                }
            }
        };

        // Act
        await CollectEvents(agent, request);

        // Assert
        _mockConverter.Verify(
            x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAgentAsync_WithApprovalResume_Approved_PromotesHistoryAndAddsApprovalResponse()
    {
        // Arrange: converter returns an assistant message with a FunctionCallContent (approval-pending)
        var pendingToolCall = new FunctionCallContent("call-del", "delete_thing",
            new Dictionary<string, object?> { ["id"] = "42" });
        var assistantHistory = new ChatMessage(ChatRole.Assistant, new List<AIContent> { pendingToolCall });
        // Hold reference to the list so we can inspect it after the service runs
        var converterReturnList = new List<ChatMessage> { new(ChatRole.User, "delete content 42"), assistantHistory };
        _mockConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns(converterReturnList);
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var request = new AGUIRunRequest
        {
            ThreadId = "thread-1", RunId = "run-1",
            Messages = [new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "Delete content 42" }],
            Resume = [new()
            {
                InterruptId = "approval:call-del",
                Status = AGUIResumeStatus.Resolved,
                Payload = JsonSerializer.SerializeToElement(new { approved = true })
            }]
        };

        // Act
        await CollectEvents(agent, request);

        // Assert: list passed to RunStreamingAsync has 3 entries:
        // [0] original user message, [1] promoted assistant, [2] ToolApprovalResponseContent
        // (MAF normalises ToolApprovalRequestContent → FunctionCallContent when forwarding to the
        // model client, so we verify against the list WE built, not what the model client sees.)
        converterReturnList.Count.ShouldBe(3);

        // [1] FunctionCallContent → ToolApprovalRequestContent (spike Finding B: FICC needs it)
        var promotedContent = converterReturnList[1].Contents!
            .OfType<ToolApprovalRequestContent>()
            .FirstOrDefault();
        promotedContent.ShouldNotBeNull("assistant FunctionCallContent should be promoted to ToolApprovalRequestContent");
        promotedContent!.ToolCall.CallId.ShouldBe("call-del");

        // [2] ToolApprovalResponseContent on ChatRole.User (approved)
        converterReturnList[2].Role.ShouldBe(ChatRole.User);
        var approvalResponse = converterReturnList[2].Contents!
            .OfType<ToolApprovalResponseContent>()
            .FirstOrDefault();
        approvalResponse.ShouldNotBeNull("resume should produce a ToolApprovalResponseContent");
        approvalResponse!.Approved.ShouldBeTrue();
        approvalResponse.ToolCall.CallId.ShouldBe("call-del");
    }

    [Fact]
    public async Task StreamAgentAsync_WithApprovalResume_Denied_AddesDeniedApprovalResponse()
    {
        var pendingToolCall = new FunctionCallContent("call-x", "delete_thing", null);
        var assistantHistory = new ChatMessage(ChatRole.Assistant, new List<AIContent> { pendingToolCall });
        var converterReturnList = new List<ChatMessage> { assistantHistory };
        _mockConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns(converterReturnList);
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var request = new AGUIRunRequest
        {
            ThreadId = "t1", RunId = "r1",
            Messages = [new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "deny" }],
            Resume = [new()
            {
                InterruptId = "approval:call-x",
                Status = AGUIResumeStatus.Resolved,
                Payload = JsonSerializer.SerializeToElement(new { approved = false })
            }]
        };

        await CollectEvents(agent, request);

        // [0] original assistant message, [1] ToolApprovalResponseContent (denied)
        converterReturnList.Count.ShouldBe(2);
        converterReturnList[1].Role.ShouldBe(ChatRole.User);
        var approvalResponse = converterReturnList[1].Contents!
            .OfType<ToolApprovalResponseContent>()
            .FirstOrDefault();
        approvalResponse.ShouldNotBeNull();
        approvalResponse!.Approved.ShouldBeFalse();
        approvalResponse.ToolCall.CallId.ShouldBe("call-x");
    }

    [Fact]
    public async Task StreamAgentAsync_WithToolCallResume_StillProducesFunctionResultContent()
    {
        // Regular frontend tool_call resume (unchanged behavior)
        var converterReturnList = new List<ChatMessage>();
        _mockConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns(converterReturnList);
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var request = new AGUIRunRequest
        {
            ThreadId = "t1", RunId = "r1",
            Messages = [new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "hi" }],
            Resume = [new()
            {
                InterruptId = "call-fe-1",  // no "approval:" prefix
                Status = AGUIResumeStatus.Resolved,
                Payload = JsonSerializer.SerializeToElement(new { ok = true })
            }]
        };

        await CollectEvents(agent, request);

        // Should produce a Tool-role message with FunctionResultContent (not ToolApprovalResponseContent)
        converterReturnList.Count.ShouldBe(1);
        converterReturnList[0].Role.ShouldBe(ChatRole.Tool);
        var resultContent = converterReturnList[0].Contents!
            .OfType<FunctionResultContent>()
            .FirstOrDefault();
        resultContent.ShouldNotBeNull();
        resultContent!.CallId.ShouldBe("call-fe-1");
        // No ToolApprovalResponseContent for a plain tool_call interrupt
        converterReturnList[0].Contents!.OfType<ToolApprovalResponseContent>().ShouldBeEmpty();
    }

    [Fact]
    public async Task StreamAgentAsync_WithApprovalResume_UncorrelatedCallId_SkipsInsteadOfSynthesisingEmptyCall()
    {
        // Arrange — the resume references an approval callId that is in NEITHER the client-supplied
        // history NOR persisted history (a stale/prior-run entry). Previously this synthesised an empty
        // FunctionCallContent, which FICC would then try (and fail) to invoke. It must now be skipped (B2).
        var converterReturnList = new List<ChatMessage> { new(ChatRole.User, "hi") };
        _mockConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns(converterReturnList);
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var request = new AGUIRunRequest
        {
            ThreadId = "t1", RunId = "r1",
            Messages = [new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "x" }],
            Resume = [new()
            {
                InterruptId = "approval:call-ghost",
                Status = AGUIResumeStatus.Resolved,
                Payload = JsonSerializer.SerializeToElement(new { approved = true })
            }]
        };

        // Act
        await CollectEvents(agent, request);

        // Assert — nothing appended; no (empty) ToolApprovalResponseContent synthesised.
        converterReturnList.Count.ShouldBe(1);
        converterReturnList
            .SelectMany(m => m.Contents ?? [])
            .OfType<ToolApprovalResponseContent>()
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task StreamAgentAsync_WithApprovalResume_CallOnlyInPersistedHistory_UsesRealToolCall()
    {
        // Arrange — resume-after-reload: the client history no longer holds the pending call (only the
        // new turn), so it is recovered from persisted history via pendingApprovalCalls (B2).
        var converterReturnList = new List<ChatMessage> { new(ChatRole.User, "approve please") };
        _mockConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns(converterReturnList);
        var agent = CreateMockAgent(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var realCall = new FunctionCallContent("call-del", "delete_thing",
            new Dictionary<string, object?> { ["id"] = "42" });
        var realRequest = new ToolApprovalRequestContent("ficc_call-del", realCall);
        var pending = new Dictionary<string, ToolApprovalRequestContent>(StringComparer.Ordinal) { ["call-del"] = realRequest };

        var request = new AGUIRunRequest
        {
            ThreadId = "t1", RunId = "r1",
            Messages = [new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "ok" }],
            Resume = [new()
            {
                InterruptId = "approval:call-del",
                Status = AGUIResumeStatus.Resolved,
                Payload = JsonSerializer.SerializeToElement(new { approved = true })
            }]
        };

        // Act
        await CollectEvents(agent, request, pending);

        // Assert — a ToolApprovalResponseContent is appended carrying the REAL call (from persisted history),
        // not an empty synthesised one, so FICC can execute the approved function.
        var approvalResponse = converterReturnList
            .SelectMany(m => m.Contents ?? [])
            .OfType<ToolApprovalResponseContent>()
            .FirstOrDefault();
        approvalResponse.ShouldNotBeNull();
        approvalResponse!.Approved.ShouldBeTrue();
        approvalResponse.ToolCall.CallId.ShouldBe("call-del");
        approvalResponse.ToolCall.ShouldBeSameAs(realCall);
    }

    #endregion

    #region Helper Methods

    private async Task<List<IAGUIEvent>> CollectEvents(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AITool>? frontendTools = null)
    {
        var events = new List<IAGUIEvent>();
        await foreach (var evt in _service.StreamAgentAsync(agent, request, frontendTools, CancellationToken.None))
        {
            events.Add(evt);
        }
        return events;
    }

    private async Task<List<IAGUIEvent>> CollectEvents(
        AIAgent agent,
        AGUIRunRequest request,
        IReadOnlyDictionary<string, ToolApprovalRequestContent> pendingApprovalCalls)
    {
        var events = new List<IAGUIEvent>();
        await foreach (var evt in _service.StreamAgentAsync(agent, request, null, session: null, pendingApprovalCalls, CancellationToken.None))
        {
            events.Add(evt);
        }
        return events;
    }

    private static AGUIRunRequest CreateRequest(string? threadId = null, string? runId = null)
    {
        return new AGUIRunRequest
        {
            ThreadId = threadId ?? "thread-test",
            RunId = runId ?? "run-test",
            Messages = new List<AGUIMessage>
            {
                new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "Hello" }
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
