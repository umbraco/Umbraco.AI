using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.Lifecycle;
using Umbraco.AI.AGUI.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Integration.Agents;

/// <summary>
/// End-to-end composition test for the backend tool HITL approval flow. Unlike the unit
/// tests — which mock each seam in isolation — this drives a REAL MAF
/// <see cref="ChatClientAgent"/> (with its built-in function-invocation middleware) over a
/// scripted <see cref="IChatClient"/> through the real <see cref="AGUIStreamingService"/>,
/// proving Tasks 1–4 actually compose: a destructive tool wrapped in
/// <see cref="ApprovalRequiredAIFunction"/> pauses with a <c>human_approval</c> interrupt and,
/// on resume, executes exactly once when approved and never when denied.
/// </summary>
public class BackendToolApprovalFlowTests
{
    private const string ToolName = "delete_content";
    private const string CallId = "call-1";
    private const string ApprovalInterruptId = "approval:call-1";

    private readonly Mock<IAGUIMessageConverter> _converter = new();
    private readonly Mock<IAGUIFileProcessor> _fileProcessor = new();
    private readonly AGUIStreamingService _service;

    public BackendToolApprovalFlowTests()
    {
        _fileProcessor
            .Setup(x => x.ProcessInboundAsync(It.IsAny<IEnumerable<AGUIMessage>?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AGUIMessage>? msgs, string _, CancellationToken _) =>
                new AGUIFileProcessorResult { RewrittenMessages = msgs ?? [], ResolvedMessages = msgs ?? [] });

        _service = new AGUIStreamingService(_converter.Object, _fileProcessor.Object, NullLogger<AGUIStreamingService>.Instance);
    }

    [Fact]
    public async Task DestructiveBackendTool_PausesForApproval_ThenExecutesOnApprove()
    {
        var executions = 0;
        var agent = CreateApprovalAgent(() => executions++);

        // --- Run 1: initial call. The model requests the destructive tool. ---
        // Converter yields just the user turn; FICC turns the tool call into an approval request.
        SetConverterHistory(new ChatMessage(ChatRole.User, "delete content 42"));

        var firstRun = await CollectEvents(agent, CreateRequest());

        var interrupt = firstRun.OfType<RunFinishedEvent>().Single()
            .Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>()
            .Interrupts.Single(i => i.Reason == "human_approval");
        interrupt.Id.ShouldBe(ApprovalInterruptId);
        interrupt.ToolCallId.ShouldBe(CallId);
        executions.ShouldBe(0); // tool has NOT run while awaiting approval

        // --- Run 2: resume, approved. Replay the history (incl. the pending tool call) + resume entry. ---
        SetConverterHistory(
            new ChatMessage(ChatRole.User, "delete content 42"),
            new ChatMessage(ChatRole.Assistant, [PendingToolCall()]));

        var secondRun = await CollectEvents(agent, CreateResumeRequest(approved: true));

        // The wrapped function executed exactly once and the run completed without re-prompting.
        executions.ShouldBe(1);
        secondRun.OfType<RunFinishedEvent>().Single()
            .Outcome.ShouldBeOfType<AGUIRunOutcomeSuccess>();
    }

    [Fact]
    public async Task DestructiveBackendTool_Denied_DoesNotExecute()
    {
        var executions = 0;
        var agent = CreateApprovalAgent(() => executions++);

        // Resume directly with a denial over the replayed pending tool call.
        SetConverterHistory(
            new ChatMessage(ChatRole.User, "delete content 42"),
            new ChatMessage(ChatRole.Assistant, [PendingToolCall()]));

        var run = await CollectEvents(agent, CreateResumeRequest(approved: false));

        // The wrapped function never ran; the run still completes (model is told it was denied).
        executions.ShouldBe(0);
        run.OfType<RunFinishedEvent>().Single()
            .Outcome.ShouldBeOfType<AGUIRunOutcomeSuccess>();
    }

    // ---- Helpers ----

    /// <summary>
    /// Builds a <see cref="ChatClientAgent"/> exactly as <c>AIAgentFactory</c> does for a
    /// destructive backend tool: the inner function (a spy counting executions) wrapped in
    /// <see cref="ApprovalRequiredAIFunction"/>, with <c>AllowMultipleToolCalls = false</c>.
    /// </summary>
    private static ChatClientAgent CreateApprovalAgent(Action onExecute)
    {
        var inner = AIFunctionFactory.Create(
            (string id) => { onExecute(); return $"deleted {id}"; },
            name: ToolName);
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var chatOptions = new ChatOptions
        {
            Tools = [approvalFn],
            AllowMultipleToolCalls = false,
        };

        return new ChatClientAgent(new ScriptedApprovalChatClient(), new ChatClientAgentOptions
        {
            ChatOptions = chatOptions,
        });
    }

    private static FunctionCallContent PendingToolCall() =>
        new(CallId, ToolName, new Dictionary<string, object?> { ["id"] = "42" });

    private void SetConverterHistory(params ChatMessage[] messages) =>
        _converter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns(messages.ToList());

    private static AGUIRunRequest CreateRequest() => new()
    {
        ThreadId = "thread-approval",
        RunId = "run-approval",
        Messages = [new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "delete content 42" }],
    };

    private static AGUIRunRequest CreateResumeRequest(bool approved)
    {
        var request = CreateRequest();
        request.Resume =
        [
            new AGUIResumeEntry
            {
                InterruptId = ApprovalInterruptId,
                Status = AGUIResumeStatus.Resolved,
                Payload = JsonSerializer.SerializeToElement(new { approved }),
            },
        ];
        return request;
    }

    private async Task<List<IAGUIEvent>> CollectEvents(AIAgent agent, AGUIRunRequest request)
    {
        var events = new List<IAGUIEvent>();
        await foreach (var evt in _service.StreamAgentAsync(agent, request, frontendTools: null, CancellationToken.None))
        {
            events.Add(evt);
        }
        return events;
    }

    /// <summary>
    /// Stateless scripted chat client: requests the destructive tool until the conversation
    /// already carries an approval response or a function result (i.e. the tool turn has been
    /// resolved by FICC), after which it returns a closing text completion.
    /// </summary>
    private sealed class ScriptedApprovalChatClient : IChatClient
    {
        public ChatClientMetadata Metadata { get; } = new("scripted-approval");

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
            => throw new NotSupportedException("Agent uses the streaming path.");

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();

            var resolved = messages.Any(m => m.Contents.Any(c =>
                c is ToolApprovalResponseContent or FunctionResultContent));

            if (resolved)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, "done");
                yield break;
            }

            yield return new ChatResponseUpdate(ChatRole.Assistant, new List<AIContent> { PendingToolCall() });
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
