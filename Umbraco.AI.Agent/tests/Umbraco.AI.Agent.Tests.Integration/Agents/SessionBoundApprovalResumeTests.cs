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
using Umbraco.AI.Core.Tools;
using Xunit;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Tests.Integration.Agents;

/// <summary>
/// Reproduces (and proves the fix for) the Copilot Workspace approval-resume crash: a REAL
/// <see cref="ChatClientAgent"/> bound to a <see cref="ChatHistoryProvider"/> that persists messages
/// through an actual JSON serialize/deserialize round trip (like <c>ConversationChatHistoryProvider</c>),
/// driven through the real <see cref="AGUIStreamingService"/> exactly as
/// <c>AIAgentService.StreamAgentAGUIAsync</c> and <c>StreamConversationAGUIController</c> compose it:
/// a FRESH <see cref="AgentSession"/> is created per turn but RESTORED from the prior turn's serialized
/// state (mirroring <c>CreateSessionAsync</c>/<c>DeserializeSessionAsync</c> being called once per HTTP
/// request), and the resume request replays the pending tool call in the client-supplied history exactly
/// as the real frontend does (Task 5 of the HITL approval plan).
/// </summary>
/// <remarks>
/// <para>
/// Two distinct bugs were found here, at two different layers:
/// </para>
/// <list type="number">
///   <item><description>
///   <b>Duplicate tool call on the wire</b> (fixed in application code — see
///   <see cref="AGUIStreamingService"/>'s <c>RemovePersistedApprovalCallsFromClientHistory</c>):
///   <see cref="ChatHistoryProvider"/>'s default <c>InvokingCoreAsync</c> unconditionally concatenates
///   the bound provider's persisted history (which already contains the interrupted turn's tool call,
///   stored verbatim by FICC's own output when the run paused) in front of whatever
///   <see cref="AGUIStreamingService"/> passes to <c>RunStreamingAsync</c>. The client ALSO replays that
///   same turn's pending tool call in the messages it resends on resume (Task 5's
///   onToolCallStart/onToolCallArgsEnd capture — needed so a non-session, stateless resume can correlate
///   it). Left unguarded that produces two copies of the same tool call once concatenated, which a real
///   provider rejects outright (observed live: Anthropic 400 <c>"tool_use ids must be unique"</c>).
///   <see cref="ScriptedApprovalChatClient"/> below asserts the same invariant so this test fails the
///   same way a real provider would.
///   </description></item>
///   <item><description>
///   <b>Approval silently never executes</b> (fixed by restoring session state — see
///   <c>AIAgentService.StreamAgentAGUIAsync</c>'s use of <c>DeserializeSessionAsync</c>/
///   <c>SerializeSessionAsync</c>): Microsoft.Agents.AI 1.14+ ships an
///   <c>ApprovalResponseBindingChatClient</c> decorator that records every model-originated approval
///   request directly in the live <see cref="AgentSession"/>'s <c>StateBag</c> — NOT in chat history —
///   and only honors an inbound approval response tied to a request it recorded. Because Copilot
///   Workspace creates a brand-new <see cref="AgentSession"/> per HTTP request (message-level history is
///   persisted separately, in the conversation store), that recorded state never reached turn 2 under a
///   bare <c>CreateSessionAsync()</c> — the decorator silently dropped the approval as unrecognized, no
///   exception, tool never ran.
///   </description></item>
/// </list>
/// <para>
/// <b>Why the decorator is kept enabled rather than disabled</b> (a real alternative that was tried and
/// reverted): our own resume path already recovers the original request from persisted history and never
/// trusts client-supplied tool-call content, so it seemed like the framework's session-scoped check was
/// redundant defense-in-depth. Disabling it (<c>ChatClientAgentOptions.DisableApprovalResponseBinding</c>)
/// and dropping this test's session round trip in favor of a bare <c>CreateSessionAsync()</c> passed this
/// single-approval test — but broke a chained scenario live (create a page, then immediately publish it,
/// approving each in turn): the SECOND approval failed on the FIRST tool's already-resolved
/// <see cref="ToolApprovalRequestContent"/>, still sitting in replayed history from an earlier turn. Once
/// a <see cref="FunctionCallContent"/> is approved, the framework marks the copy embedded in the response
/// as <see cref="FunctionCallContent.InformationalOnly"/> — "already handled, ignore me" — but the
/// separately-stored ORIGINAL request message never gets that marker, since it's a distinct persisted
/// JSON blob from an earlier turn. Re-derived from scratch on every turn (which is what a bare, unrestored
/// session forces), that stale request has no reachable, still-"live" response to pair against, so FICC
/// reports it unmatched — even though it really was resolved, and executed, turns ago. The decorator's
/// session-scoped bookkeeping sidesteps this class of bug entirely: it tracks resolution by direct
/// record-keeping instead of re-deriving it from replayed message content each time, so a genuinely
/// stale, already-consumed request is never re-examined once removed from its own tracked list. That
/// robustness is worth the extra persisted state; our own resolver (<c>ResolveApprovalToolCalls</c>)
/// stays in place regardless, since it is what supplies the correct request object for
/// <c>CreateResponse()</c> in the first place — the two are complementary, not duplicated, layers.
/// </para>
/// </remarks>
public class SessionBoundApprovalResumeTests
{
    private const string ToolName = "delete_content";
    private const string CallId = "call-1";
    private const string ApprovalInterruptId = "approval:call-1";

    private readonly Mock<IAGUIMessageConverter> _converter = new();
    private readonly Mock<IAGUIFileProcessor> _fileProcessor = new();
    private readonly AGUIStreamingService _service;

    public SessionBoundApprovalResumeTests()
    {
        _fileProcessor
            .Setup(x => x.ProcessInboundAsync(It.IsAny<IEnumerable<AGUIMessage>?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AGUIMessage>? msgs, string _, CancellationToken _) =>
                new AGUIFileProcessorResult { RewrittenMessages = msgs ?? [], ResolvedMessages = msgs ?? [] });

        _service = new AGUIStreamingService(_converter.Object, _fileProcessor.Object, new AIToolCollection(() => []), NullLogger<AGUIStreamingService>.Instance);
    }

    [Fact]
    public async Task ApprovalResume_WithSessionBoundPersistedHistory_DoesNotThrow_AndExecutesOnApprove()
    {
        var executions = 0;
        var historyProvider = new JsonRoundTrippingChatHistoryProvider();
        var agent = CreateApprovalAgent(() => executions++, historyProvider);

        // --- Turn 1: initial call, persisted via the real ChatHistoryProvider (real JSON round trip). ---
        SetConverterHistory(new ChatMessage(ChatRole.User, "delete content 42"));
        var session1 = await agent.CreateSessionAsync();

        var firstRun = await CollectEvents(agent, CreateRequest(), session1);

        firstRun.OfType<RunFinishedEvent>().Single()
            .Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>()
            .Interrupts.Single(i => i.Reason == "human_approval").Id.ShouldBe(ApprovalInterruptId);
        executions.ShouldBe(0);

        // --- Turn 2: resume, approved. Mirrors production exactly:
        //   - session1's state is serialized and a BRAND NEW session is restored from it
        //     (StreamConversationAGUIController creates one per HTTP call via IAIAgentService ->
        //     context.MafAgent.DeserializeSessionAsync each request when prior state exists) — this is
        //     what carries ApprovalResponseBindingChatClient's own recorded pending-approval state (bug
        //     #2 above) across the request boundary, exactly like AIAgentService.StreamAgentAGUIAsync;
        //   - the client resends the pending tool call as part of its own history (Task 5's
        //     onToolCallStart/onToolCallArgsEnd capture), which AGUIStreamingService may promote;
        //   - pendingApprovalCalls is resolved from the SAME persisted store up front, exactly as
        //     AIAgentService.ResolvePendingApprovalCallsAsync does before streaming starts.
        var serializedState = await agent.SerializeSessionAsync(session1);
        var session2 = await agent.DeserializeSessionAsync(serializedState);
        historyProvider.BindConversation(session2);

        var pendingApprovalCalls = await historyProvider.GetApprovalToolCallsAsync([CallId]);

        SetConverterHistory(
            new ChatMessage(ChatRole.User, "delete content 42"),
            new ChatMessage(ChatRole.Assistant, [PendingToolCall()]));

        var secondRun = await CollectEvents(agent, CreateResumeRequest(approved: true), session2, pendingApprovalCalls);

        executions.ShouldBe(1);
        secondRun.OfType<RunFinishedEvent>().Single()
            .Outcome.ShouldBeOfType<AGUIRunOutcomeSuccess>();
    }

    [Fact]
    public async Task ApprovalAbandonedByReload_ThenUnrelatedMessage_DoesNotThrow_AndAutoDeniesTheStaleRequest()
    {
        // Reproduces the reported crash: a browser refresh/close before Approve/Deny leaves the pending
        // ToolApprovalRequestContent dangling in persisted history. The user doesn't come back to resume
        // it — they just type something new. Without the staleApprovalRequests handling, MAF's bound
        // ChatHistoryProvider concatenates that dangling request into this turn too, and
        // FunctionInvokingChatClient throws ("...that have no matching ToolApprovalResponseContent"),
        // bricking every future turn in the conversation.
        var executions = 0;
        var historyProvider = new JsonRoundTrippingChatHistoryProvider();
        var agent = CreateApprovalAgent(() => executions++, historyProvider);

        // --- Turn 1: initial call, pauses on approval (persisted, never resolved). ---
        SetConverterHistory(new ChatMessage(ChatRole.User, "delete content 42"));
        var session1 = await agent.CreateSessionAsync();

        var firstRun = await CollectEvents(agent, CreateRequest(), session1);
        firstRun.OfType<RunFinishedEvent>().Single()
            .Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>();
        executions.ShouldBe(0);

        // --- Simulated reload: session state is restored (as a fresh HTTP request would), but the user
        // sends a brand-new, unrelated message with no Resume entries at all — no click on Approve/Deny. ---
        var serializedState = await agent.SerializeSessionAsync(session1);
        var session2 = await agent.DeserializeSessionAsync(serializedState);
        historyProvider.BindConversation(session2);

        var staleRequests = await historyProvider.GetDanglingApprovalRequestsAsync();
        staleRequests.Count.ShouldBe(1, "sanity check: the abandoned request should still be dangling");

        SetConverterHistory(new ChatMessage(ChatRole.User, "what's today's date?"));

        // Act — must not throw.
        var secondRun = await CollectEvents(agent, CreateRequest(), session2, pendingApprovalCalls: null, staleApprovalRequests: staleRequests);

        // Assert — the conversation completes normally; the abandoned destructive tool never ran.
        secondRun.OfType<RunFinishedEvent>().Single()
            .Outcome.ShouldBeOfType<AGUIRunOutcomeSuccess>();
        executions.ShouldBe(0);

        // The dangling request is now resolved (denied), so a later turn won't hit the same crash again.
        var stillDangling = await historyProvider.GetDanglingApprovalRequestsAsync();
        stillDangling.ShouldBeEmpty();
    }

    // ---- Helpers ----

    private static ChatClientAgent CreateApprovalAgent(Action onExecute, ChatHistoryProvider historyProvider)
    {
        var inner = AIFunctionFactory.Create(
            (string id) => { onExecute(); return $"deleted {id}"; },
            name: ToolName);
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        return new ChatClientAgent(new ScriptedApprovalChatClient(), new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions { Tools = [approvalFn], AllowMultipleToolCalls = false },
            ChatHistoryProvider = historyProvider,
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

    private async Task<List<IAGUIEvent>> CollectEvents(
        MsAIAgent agent,
        AGUIRunRequest request,
        AgentSession session,
        IReadOnlyDictionary<string, ToolApprovalRequestContent>? pendingApprovalCalls = null,
        IReadOnlyList<ToolApprovalRequestContent>? staleApprovalRequests = null)
    {
        var events = new List<IAGUIEvent>();
        await foreach (var evt in _service.StreamAgentAsync(agent, request, frontendTools: null, session, pendingApprovalCalls, staleApprovalRequests, cancellationToken: CancellationToken.None))
        {
            events.Add(evt);
        }
        return events;
    }

    /// <summary>
    /// Stateless scripted chat client identical in spirit to <c>BackendToolApprovalFlowTests</c>'s: requests
    /// the destructive tool until the conversation carries an approval response or function result.
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

            // Real providers reject a repeated tool_use id outright — observed live as Anthropic 400
            // "tool_use ids must be unique" when the client's replayed pending call and the persisted
            // history's copy of the same turn both reach the wire. Assert the same invariant here so
            // this test fails the way a real provider does if either copy leaks through unstripped.
            var duplicateCallId = messages
                .SelectMany(m => m.Contents)
                .OfType<FunctionCallContent>()
                .GroupBy(c => c.CallId)
                .FirstOrDefault(g => g.Count() > 1)?.Key;
            if (duplicateCallId is not null)
            {
                throw new InvalidOperationException(
                    $"Duplicate tool_use id '{duplicateCallId}' in request messages — a real provider would reject this.");
            }

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

    /// <summary>
    /// Minimal stand-in for <c>ConversationChatHistoryProvider</c> that persists every message through a
    /// REAL <see cref="JsonSerializer"/> serialize/then-deserialize round trip (using
    /// <see cref="AIJsonUtilities.DefaultOptions"/>, same as the product code), so this test exercises the
    /// same "does the polymorphic AIContent type survive storage" concern the product provider has,
    /// rather than just reusing in-memory objects across turns. All sessions share one conversation
    /// (single-conversation test); production scopes this per <c>conversationId</c> via session state.
    /// </summary>
    private sealed class JsonRoundTrippingChatHistoryProvider : ChatHistoryProvider
    {
        private readonly List<string> _persistedMessageJson = [];

        // No-op: production binds a session to a conversation id via session state; this fake always
        // targets its single in-memory store, so binding is just a marker call for readability at the
        // call site (mirroring ConversationChatHistoryProvider.BindConversation's call shape).
        public void BindConversation(AgentSession session) { }

        protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
            InvokingContext context, CancellationToken cancellationToken = default)
        {
            IEnumerable<ChatMessage> messages = _persistedMessageJson
                .Select(json => JsonSerializer.Deserialize<ChatMessage>(json, AIJsonUtilities.DefaultOptions)!)
                .ToList();
            return new ValueTask<IEnumerable<ChatMessage>>(messages);
        }

        protected override ValueTask StoreChatHistoryAsync(
            InvokedContext context, CancellationToken cancellationToken = default)
        {
            foreach (var message in context.RequestMessages.Concat(context.ResponseMessages ?? []))
            {
                _persistedMessageJson.Add(JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions));
            }
            return default;
        }

        /// <summary>Mirrors <c>ConversationChatHistoryProvider.GetApprovalToolCallsAsync</c>.</summary>
        public ValueTask<IReadOnlyDictionary<string, ToolApprovalRequestContent>> GetApprovalToolCallsAsync(
            IReadOnlyCollection<string> callIds)
        {
            var wanted = callIds.ToHashSet(StringComparer.Ordinal);
            var result = new Dictionary<string, ToolApprovalRequestContent>(StringComparer.Ordinal);

            foreach (var json in _persistedMessageJson)
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(json, AIJsonUtilities.DefaultOptions);
                foreach (var content in message?.Contents ?? [])
                {
                    if (content is ToolApprovalRequestContent request && wanted.Contains(request.ToolCall.CallId))
                    {
                        result[request.ToolCall.CallId] = request;
                    }
                }
            }

            return new ValueTask<IReadOnlyDictionary<string, ToolApprovalRequestContent>>(result);
        }

        /// <summary>Mirrors <c>ConversationChatHistoryProvider.GetDanglingApprovalRequestsAsync</c>.</summary>
        public ValueTask<IReadOnlyList<ToolApprovalRequestContent>> GetDanglingApprovalRequestsAsync()
        {
            var requests = new Dictionary<string, ToolApprovalRequestContent>(StringComparer.Ordinal);
            var respondedRequestIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var json in _persistedMessageJson)
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(json, AIJsonUtilities.DefaultOptions);
                foreach (var content in message?.Contents ?? [])
                {
                    switch (content)
                    {
                        case ToolApprovalRequestContent request:
                            requests[request.RequestId] = request;
                            break;
                        case ToolApprovalResponseContent response:
                            respondedRequestIds.Add(response.RequestId);
                            break;
                    }
                }
            }

            IReadOnlyList<ToolApprovalRequestContent> dangling = requests
                .Where(kvp => !respondedRequestIds.Contains(kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();
            return new ValueTask<IReadOnlyList<ToolApprovalRequestContent>>(dangling);
        }
    }
}
