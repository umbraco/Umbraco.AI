# Backend Tool HITL Approval Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add server-side human-in-the-loop (HITL) approval for destructive *backend* agent tools, so a destructive backend tool pauses for user approve/deny before it executes — reusing the existing AG-UI interrupt/resume plumbing and the already-present `human_approval` client handler.

**Architecture:** Wrap destructive, non-system backend tools in MEAI's `ApprovalRequiredAIFunction` at the agent build site. When the model calls one, `FunctionInvokingChatClient` (FICC) replaces the `FunctionCallContent` with a `ToolApprovalRequestContent` and stops invoking. `AGUIStreamingService` detects that content, emits an AG-UI `human_approval` interrupt, and the run finishes paused. On resume, the AG-UI resume entry is mapped to a `ToolApprovalResponseContent` (instead of the `FunctionResultContent` used for frontend tools); FICC then executes (approved) or skips (denied) the tool. Frontend tools are **unchanged** — they keep their client-side `meta.approval` flow and must NOT be wrapped.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI 10.6.0 (→ 10.7.0 in Task 7), Microsoft.Agents.AI (MAF `ChatClientAgent`), AG-UI protocol SDK (`Umbraco.AI.AGUI`), xUnit + Shouldly + Moq (backend tests), Lit/TypeScript (frontend).

---

## Background: verified current state

These facts were confirmed by reading the code; later tasks depend on them.

- **Frontend tools** (`AIFrontendToolFunction`, `Umbraco.AI.Agent.Core/Chat/AIFrontendToolFunction.cs`): set `FunctionInvokingChatClient.CurrentContext.Terminate = true` so the call is never executed server-side. `AGUIEventEmitter.EmitRunFinished()` then emits an `AGUIRunOutcomeInterrupt` with `Reason = "tool_call"`, `Id = toolCallId` per pending frontend call. Approval for frontend tools is purely client-side (`frontend-tool.executor.ts`, `toolManifest.meta.approval`).
- **Backend tools** (`AIToolFunction<TArgs>`, `Umbraco.AI/src/Umbraco.AI.Core/Tools/AIToolFunction.cs`): execute server-side immediately inside the FICC loop via `IAITool.ExecuteAsync`. **No runtime approval gate exists.** `IsDestructive` is present on `IAITool` but is not consulted on this path.
- **Approval signal already exists:** `IAITool.IsDestructive` and `IAITool.ScopeId` are populated from `[AITool]`/scope (`ContentWriteScope`, `MediaWriteScope` are `IsDestructive = true`). `IAISystemTool`'s doc comment lists "Skip approval workflows" — the model already anticipates this feature. `IAISystemTool` is visible to `Umbraco.AI.Agent.Core` (used in `AIAgentToolHelper.cs:32`).
- **Backend tool → AIFunction build site:** `AIAgentFactory.CreateStandardAgentAsync` at `Umbraco.AI.Agent.Core/Chat/AIAgentFactory.cs:121`: `tools.AddRange(_toolCollection.ToAIFunctions(allowedToolIds, _functionFactory))`. The produced `AIFunction.Name` equals the tool's `Id`.
- **Interrupt emission:** `AGUIEventEmitter.EmitRunFinished()` (`Umbraco.AI.AGUI/Streaming/AGUIEventEmitter.cs:238`) only produces interrupts from `_frontendToolCallIds`, always `Reason = "tool_call"`.
- **Stream loop:** `AGUIStreamingService.StreamCoreAsync` (`Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs:177`) switches on `FunctionCallContent` / `FunctionResultContent` / `ErrorContent` / `TextContent` / default. A `ToolApprovalRequestContent` currently falls into `default` and is silently dropped.
- **Resume mapping:** `AGUIStreamingService.ExtractToolResultsFromResume` (`AGUIStreamingService.cs:322`) unconditionally builds `FunctionResultContent(entry.InterruptId, entry.Payload)` as a `ChatRole.Tool` message. `entry.InterruptId == toolCallId` by construction.
- **`AGUIResumeEntry`** (`Umbraco.AI.AGUI/Models/AGUIResumeEntry.cs`) carries only `InterruptId`, `Status`, `Payload` — **not the interrupt reason.** So resume must recover the interrupt *kind* from the `InterruptId` itself.

### Confirmed MEAI 10.6.0 API surface

- `ApprovalRequiredAIFunction` (type exists in `Microsoft.Extensions.AI` 10.6.0). Per its XML docs: "if a requested function is an `ApprovalRequiredAIFunction`, the `FunctionInvokingChatClient` will not attempt to invoke it directly. Instead, it will replace that `FunctionCallContent` with a `ToolApprovalRequestContent` that wraps the `FunctionCallContent` … The caller is then responsible for responding … by sending a corresponding `ToolApprovalResponseContent` in a subsequent" message.
- **Multi-call caveat (from the same XML docs):** if multiple tools are called in one response and any is approval-required, the others may be treated as approval requests too "even if they were not `ApprovalRequiredAIFunction` instances. If this is a concern, consider … setting `ChatOptions.AllowMultipleToolCalls` to `false`."
- `ToolApprovalRequestContent(string id, ToolCallContent toolCall)`; property `.ToolCall`; method `.CreateResponse(bool approved, string? reason)`.
- `ToolApprovalResponseContent(string id, bool approved, ToolCallContent toolCall)`; properties `.Approved`, `.Reason`, `.ToolCall`.
- `FunctionCallContent` derives from `ToolCallContent`; `ToolCallContent` exposes `.CallId`.

---

## File Structure

**Create:**
- `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/MeaiApprovalRoundTripSpikeTests.cs` — characterization tests pinning FICC approval mechanics (Task 0).
- `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIInterruptKind.cs` — central helper that encodes/decodes the interrupt-kind prefix in an interrupt `Id`.
- Test files alongside each modified type (see tasks).

**Modify:**
- `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Chat/AIAgentFactory.cs` — wrap destructive non-system backend functions; set `AllowMultipleToolCalls = false` when any present (Task 1).
- `Umbraco.AI.Agent/src/Umbraco.AI.AGUI/Streaming/AGUIEventEmitter.cs` — track approval-request ids and emit `human_approval` interrupts (Task 2).
- `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs` — handle `ToolApprovalRequestContent` in the stream (Task 3) and branch resume mapping (Task 4).
- `Umbraco.AI.Agent.UI/.../chat/services/frontend-tool.executor.ts` & `handlers/hitl-interrupt.handler.ts` — verify/align the `human_approval` interrupt shape (Task 5).
- `Directory.Packages.props` — MEAI 10.6.0 → 10.7.0 (Task 7).

**Design boundary:** approval wrapping happens in the **Agent** layer (`AIAgentFactory`), NOT in the Core `AIFunctionFactory`. The Core factory is shared with inline `IAIChatService` chat, which has no interrupt/resume mechanism — wrapping there would pause inline chat with an unhandled `ToolApprovalRequestContent`.

---

### Task 0: Spike — pin MEAI approval round-trip mechanics

**Why first:** the stateless AG-UI resume must reconstruct a `ToolApprovalResponseContent` that FICC correlates with the original request. This task answers, with executable tests, the three unknowns that Tasks 3–4 depend on:
1. On resume, must the *original* `ToolApprovalRequestContent` be present in the replayed message history, or does a standalone `ToolApprovalResponseContent` with a matching id suffice?
2. What `ChatRole` carries the `ToolApprovalResponseContent`?
3. Confirm the multi-call behavior so Task 1's `AllowMultipleToolCalls = false` decision is justified.

**Files:**
- Create: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/MeaiApprovalRoundTripSpikeTests.cs`

- [ ] **Step 1: Write the characterization test**

```csharp
using Microsoft.Extensions.AI;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

public class MeaiApprovalRoundTripSpikeTests
{
    // A fake IChatClient that, on the FIRST call, returns one tool call to "delete_thing",
    // and on the SECOND call (after the approval response is in history) returns a plain
    // text completion. This lets us observe exactly what FICC produces/consumes.
    private sealed class ScriptedChatClient : IChatClient
    {
        private int _calls;
        public ChatClientMetadata Metadata { get; } = new("scripted");
        public bool ToolWasInvoked { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
        {
            _calls++;
            if (_calls == 1)
            {
                var call = new FunctionCallContent("call-1", "delete_thing",
                    new Dictionary<string, object?> { ["id"] = "42" });
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
            }

            // Record whether, by the second model turn, a FunctionResultContent for call-1
            // appeared in history (i.e. FICC executed the tool after approval).
            ToolWasInvoked = messages.Any(m => m.Contents.OfType<FunctionResultContent>()
                .Any(r => r.CallId == "call-1"));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    [Fact]
    public async Task ApprovalRequiredFunction_FirstTurn_ProducesApprovalRequest_AndDoesNotInvoke()
    {
        var invoked = false;
        var inner = AIFunctionFactory.Create(
            (string id) => { invoked = true; return $"deleted {id}"; },
            name: "delete_thing");
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var scripted = new ScriptedChatClient();
        var client = scripted.AsBuilder().UseFunctionInvocation().Build();

        var options = new ChatOptions { Tools = [approvalFn] };
        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "delete thing 42")], options);

        // EXPECT: an approval request surfaced, tool NOT invoked yet.
        var approvalRequest = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>()
            .FirstOrDefault();

        approvalRequest.ShouldNotBeNull();
        approvalRequest!.ToolCall.CallId.ShouldBe("call-1");
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task ApprovalResponse_Approved_CausesToolInvocation()
    {
        var invoked = false;
        var inner = AIFunctionFactory.Create(
            (string id) => { invoked = true; return $"deleted {id}"; },
            name: "delete_thing");
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var scripted = new ScriptedChatClient();
        var client = scripted.AsBuilder().UseFunctionInvocation().Build();
        var options = new ChatOptions { Tools = [approvalFn] };

        // First turn: get the approval request.
        var first = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "delete thing 42")], options);
        var request = first.Messages.SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>().Single();

        // Build the conversation for resume: original user msg + the assistant msg that
        // carried the request + a NEW message carrying the approval response.
        // NOTE: this test exists to discover the REQUIRED shape — adjust the role and whether
        // the request message must be included based on observed pass/fail, and record the
        // finding in this plan's Task 3/4 assumptions.
        var approvalResponse = request.CreateResponse(approved: true, reason: null);

        var history = new List<ChatMessage> { new(ChatRole.User, "delete thing 42") };
        history.AddRange(first.Messages);
        history.Add(new ChatMessage(ChatRole.User, [approvalResponse]));

        var second = await client.GetResponseAsync(history, options);

        invoked.ShouldBeTrue();
        scripted.ToolWasInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task ApprovalResponse_Denied_DoesNotInvoke()
    {
        var invoked = false;
        var inner = AIFunctionFactory.Create(
            (string id) => { invoked = true; return $"deleted {id}"; },
            name: "delete_thing");
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var client = new ScriptedChatClient().AsBuilder().UseFunctionInvocation().Build();
        var options = new ChatOptions { Tools = [approvalFn] };

        var first = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "delete thing 42")], options);
        var request = first.Messages.SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>().Single();

        var denial = request.CreateResponse(approved: false, reason: "user denied");
        var history = new List<ChatMessage> { new(ChatRole.User, "delete thing 42") };
        history.AddRange(first.Messages);
        history.Add(new ChatMessage(ChatRole.User, [denial]));

        await client.GetResponseAsync(history, options);
        invoked.ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Run the spike**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~MeaiApprovalRoundTripSpikeTests"`
Expected: PASS. If any test fails, the failure reveals the required shape — iterate on the message `ChatRole` and whether the request message must be replayed.

- [ ] **Step 3: Record findings in this plan**

Edit this file's "Task 3/4 assumptions" block below with the confirmed answers:
- **A. Response role:** the `ChatRole` that carried the passing `ToolApprovalResponseContent` (hypothesis: `ChatRole.User`).
- **B. Request-in-history:** whether `history.AddRange(first.Messages)` was required for the approved test to invoke (hypothesis: yes — FICC correlates against the request).
- **C. Construction without the request object:** whether a freshly built `new ToolApprovalResponseContent(request.ToolCall.CallId, approved, request.ToolCall)` (no `CreateResponse`) also works — this is what the stateless resume path must do.

- [ ] **Step 4: Commit**

```bash
git add Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/MeaiApprovalRoundTripSpikeTests.cs Umbraco.AI.Agent/docs/internal/plans/2026-06-16-backend-tool-hitl-approval.md
git commit -m "test(agent): characterize MEAI ApprovalRequiredAIFunction round-trip"
```

> **Task 3/4 assumptions (filled in by Task 0):**
> - A. Response role: `__________`
> - B. Request must be in replayed history: `__________`
> - C. Stateless construction `new ToolApprovalResponseContent(callId, approved, toolCall)` works: `__________`

---

### Task 1: Wrap destructive backend tools in `ApprovalRequiredAIFunction`

**Files:**
- Modify: `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Chat/AIAgentFactory.cs:113-146`
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/AIAgentFactoryApprovalTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.AI;
using Shouldly;
using Xunit;
// plus existing test usings/fakes for AIAgentFactory construction

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

public class AIAgentFactoryApprovalTests
{
    [Fact]
    public async Task DestructiveBackendTool_IsWrapped_InApprovalRequiredFunction()
    {
        // Arrange: tool collection with one destructive non-system tool ("delete_content",
        // IsDestructive = true) and one safe tool ("get_content", IsDestructive = false),
        // both allowed for the agent. (Use existing tool/agent test builders/fakes.)
        var factory = CreateFactoryWith(destructiveToolId: "delete_content", safeToolId: "get_content");
        var agent = CreateStandardAgentAllowing("delete_content", "get_content");

        // Act
        var msAgent = await factory.CreateAgentAsync(agent);
        var tools = GetChatOptions(msAgent).Tools!;

        // Assert
        tools.Single(t => t.Name == "delete_content").ShouldBeOfType<ApprovalRequiredAIFunction>();
        tools.Single(t => t.Name == "get_content").ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    }

    [Fact]
    public async Task WhenDestructiveToolPresent_AllowMultipleToolCalls_IsFalse()
    {
        var factory = CreateFactoryWith(destructiveToolId: "delete_content", safeToolId: "get_content");
        var agent = CreateStandardAgentAllowing("delete_content", "get_content");

        var msAgent = await factory.CreateAgentAsync(agent);

        GetChatOptions(msAgent).AllowMultipleToolCalls.ShouldBe(false);
    }
}
```

> Use the existing `AIAgentFactory` unit-test setup as the template for `CreateFactoryWith`, `CreateStandardAgentAllowing`, `GetChatOptions` (read the current `AIAgentFactory` tests in `tests/Umbraco.AI.Agent.Tests.Unit/` for the established fakes for `IAIFunctionFactory`, `AIToolCollection`, `IAIChatClientFactory`). `GetChatOptions` reads `ChatClientAgentOptions.ChatOptions` off the returned `ChatClientAgent`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AIAgentFactoryApprovalTests"`
Expected: FAIL — `delete_content` is a plain `AIToolFunction`, not `ApprovalRequiredAIFunction`; `AllowMultipleToolCalls` is null.

- [ ] **Step 3: Implement the wrapping in `CreateStandardAgentAsync`**

Replace the backend-tool build block (`AIAgentFactory.cs:120-121`) and the `ChatOptions` construction (`:142-146`):

```csharp
// STEP 3: Build tool list with ALL allowed backend tools (no context filtering)
var tools = new List<AITool>();

// Identify destructive, non-system backend tools that require runtime approval.
// IAISystemTool is excluded by design (system tools "skip approval workflows").
var approvalToolIds = _toolCollection
    .Where(t => t.IsDestructive && t is not IAISystemTool)
    .Select(t => t.Id)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

foreach (var fn in _toolCollection.ToAIFunctions(allowedToolIds, _functionFactory))
{
    tools.Add(approvalToolIds.Contains(fn.Name)
        ? new ApprovalRequiredAIFunction(fn)
        : fn);
}

var requiresApproval = tools.Any(t => t is ApprovalRequiredAIFunction);
```

Then, in the `ChatOptions` initializer, add `AllowMultipleToolCalls`:

```csharp
var chatOptions = new ChatOptions
{
    Instructions = config?.Instructions,
    Tools = tools,
    // MEAI: when any tool requires approval, a multi-tool response turns ALL calls into
    // approval requests (even non-destructive ones). Disabling multi-call keeps approval
    // scoped to exactly the destructive tool the model chose. See ApprovalRequiredAIFunction docs.
    AllowMultipleToolCalls = requiresApproval ? false : null,
};
```

Add `using Microsoft.Extensions.AI;` (already present) and ensure `Umbraco.AI.Core.Tools` is imported (it is, line 11).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AIAgentFactoryApprovalTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Chat/AIAgentFactory.cs Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/AIAgentFactoryApprovalTests.cs
git commit -m "feat(agent): wrap destructive backend tools for HITL approval"
```

---

### Task 2: Interrupt-kind encoding + emit `human_approval` interrupts

**Files:**
- Create: `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIInterruptKind.cs`
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIInterruptKindTests.cs`
- Modify: `Umbraco.AI.Agent/src/Umbraco.AI.AGUI/Streaming/AGUIEventEmitter.cs` (add approval tracking + emission)
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIEventEmitterApprovalTests.cs`

**Design:** because `AGUIResumeEntry` carries no reason, the interrupt `Id` itself encodes the kind. Frontend tool-call interrupts keep `Id = toolCallId` (unchanged, no prefix). Approval interrupts use `Id = "approval:" + toolCallId`. Resume (Task 4) routes on the prefix.

- [ ] **Step 1: Write the failing test for the kind helper**

```csharp
using Umbraco.AI.Agent.Core.AGUI;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIInterruptKindTests
{
    [Fact]
    public void Approval_RoundTrips()
    {
        var id = AGUIInterruptKind.ForApproval("call-1");
        id.ShouldBe("approval:call-1");
        AGUIInterruptKind.IsApproval(id).ShouldBeTrue();
        AGUIInterruptKind.GetToolCallId(id).ShouldBe("call-1");
    }

    [Fact]
    public void ToolCall_HasNoPrefix()
    {
        AGUIInterruptKind.IsApproval("call-1").ShouldBeFalse();
        AGUIInterruptKind.GetToolCallId("call-1").ShouldBe("call-1");
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIInterruptKindTests"`
Expected: FAIL — `AGUIInterruptKind` does not exist.

- [ ] **Step 3: Implement the helper**

```csharp
namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Encodes the kind of an AG-UI interrupt into its <c>Id</c> so that the stateless resume
/// path can distinguish a backend approval interrupt from a frontend tool-call interrupt
/// (the resume entry carries no reason). Frontend tool-call interrupts keep the raw
/// tool call id; approval interrupts are prefixed.
/// </summary>
internal static class AGUIInterruptKind
{
    private const string ApprovalPrefix = "approval:";

    public static string ForApproval(string toolCallId) => ApprovalPrefix + toolCallId;

    public static bool IsApproval(string interruptId) =>
        interruptId.StartsWith(ApprovalPrefix, StringComparison.Ordinal);

    public static string GetToolCallId(string interruptId) =>
        IsApproval(interruptId) ? interruptId[ApprovalPrefix.Length..] : interruptId;
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIInterruptKindTests"`
Expected: PASS.

- [ ] **Step 5: Write the failing emitter test**

```csharp
using Microsoft.Extensions.AI;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIEventEmitterApprovalTests
{
    [Fact]
    public void RunFinished_WithApprovalRequest_EmitsHumanApprovalInterrupt()
    {
        var emitter = new AGUIEventEmitter("thread-1", "run-1");

        // Register a pending approval for tool call "call-9".
        emitter.RegisterApprovalRequest(
            toolCallId: "call-9",
            toolName: "delete_content",
            argumentsJson: """{"id":"42"}""");

        var finished = emitter.EmitRunFinished();

        var interrupt = finished.Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>();
        var entry = interrupt.Interrupts.ShouldHaveSingleItem();
        entry.Reason.ShouldBe("human_approval");
        entry.Id.ShouldBe("approval:call-9");
        entry.ToolCallId.ShouldBe("call-9");
    }
}
```

> `RegisterApprovalRequest` lives on `AGUIEventEmitter`, but the `"approval:"` id is produced via the `AGUIInterruptKind` helper from `Umbraco.AI.Agent.Core`. `AGUIEventEmitter` is in the `Umbraco.AI.AGUI` SDK project, which must NOT depend on `Agent.Core`. Therefore inline the same prefix constant in the emitter (keep it a literal `"approval:"`) and rely on `AGUIInterruptKindTests` + a same-constant assertion to guard drift. (Alternative: move `AGUIInterruptKind` into `Umbraco.AI.AGUI` — but the AGUI project is a pure protocol SDK with "no business logic"; a kind-encoding helper is arguably protocol-adjacent. Decide during review; default: literal in emitter, helper in Agent.Core for the resume side.)

- [ ] **Step 6: Run to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIEventEmitterApprovalTests"`
Expected: FAIL — `RegisterApprovalRequest` does not exist; `EmitRunFinished` ignores approvals.

- [ ] **Step 7: Implement approval tracking in `AGUIEventEmitter`**

Add a field near the existing `_frontendToolCallIds` declaration:

```csharp
private readonly List<ApprovalRequestInfo> _approvalRequests = new();

private readonly record struct ApprovalRequestInfo(string ToolCallId, string ToolName, string ArgumentsJson);

/// <summary>
/// Registers a backend tool call that requires human approval before execution.
/// Surfaced as a <c>human_approval</c> interrupt in <see cref="EmitRunFinished"/>.
/// </summary>
public void RegisterApprovalRequest(string toolCallId, string toolName, string argumentsJson)
    => _approvalRequests.Add(new ApprovalRequestInfo(toolCallId, toolName, argumentsJson));

private bool HasApprovalRequests => _approvalRequests.Count > 0;
```

Update `EmitRunFinished` so the outcome combines frontend tool-call interrupts AND approval interrupts:

```csharp
public RunFinishedEvent EmitRunFinished()
{
    var interrupts = new List<AGUIInterruptInfo>();

    // Frontend tool calls (unchanged): Id == toolCallId, reason "tool_call".
    foreach (var toolCallId in _frontendToolCallIds)
    {
        interrupts.Add(new AGUIInterruptInfo
        {
            Id = toolCallId,
            Reason = "tool_call",
            ToolCallId = toolCallId,
        });
    }

    // Backend approval requests: Id == "approval:" + toolCallId, reason "human_approval".
    // Carry tool name + args in metadata so the client can render an informative prompt.
    foreach (var approval in _approvalRequests)
    {
        interrupts.Add(new AGUIInterruptInfo
        {
            Id = "approval:" + approval.ToolCallId,
            Reason = "human_approval",
            ToolCallId = approval.ToolCallId,
            Message = $"The tool \"{approval.ToolName}\" requires your approval to proceed.",
            ResponseSchema = ApprovalResponseSchema,
            Metadata = new Dictionary<string, object?>
            {
                ["toolName"] = approval.ToolName,
                ["arguments"] = approval.ArgumentsJson,
            },
        });
    }

    AGUIRunOutcome outcome = interrupts.Count > 0
        ? new AGUIRunOutcomeInterrupt(interrupts)
        : new AGUIRunOutcomeSuccess();

    return new RunFinishedEvent
    {
        ThreadId = _threadId,
        RunId = _runId,
        Outcome = outcome,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };
}

// Minimal approve/deny schema for the resume payload: { "approved": bool }.
private static readonly object ApprovalResponseSchema = new Dictionary<string, object?>
{
    ["type"] = "object",
    ["properties"] = new Dictionary<string, object?>
    {
        ["approved"] = new Dictionary<string, object?> { ["type"] = "boolean" },
    },
    ["required"] = new[] { "approved" },
};
```

> The existing `HasFrontendToolCalls` property can stay; it's now only one input to the outcome. Keep it for the frontend path's other uses.

- [ ] **Step 8: Run to verify it passes**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIEventEmitterApprovalTests"`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIInterruptKind.cs Umbraco.AI.Agent/src/Umbraco.AI.AGUI/Streaming/AGUIEventEmitter.cs Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/
git commit -m "feat(agent): emit human_approval interrupts for backend tool approval"
```

---

### Task 3: Detect `ToolApprovalRequestContent` in the stream

**Files:**
- Modify: `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs:182-244` (the content switch)
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIStreamingServiceApprovalTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// Arrange a fake MAF agent / chat client whose streamed update contains a
// ToolApprovalRequestContent wrapping FunctionCallContent("call-9","delete_content",{id:42}).
// Drive AGUIStreamingService.StreamAgentAsync and collect events.
[Fact]
public async Task Stream_WithApprovalRequest_FinishesWithHumanApprovalInterrupt()
{
    var events = await CollectEventsForApprovalRequest(
        callId: "call-9", toolName: "delete_content", argsJson: """{"id":"42"}""");

    var finished = events.OfType<RunFinishedEvent>().Single();
    var interrupt = finished.Outcome.ShouldBeOfType<AGUIRunOutcomeInterrupt>();
    var entry = interrupt.Interrupts.Single(i => i.Reason == "human_approval");
    entry.Id.ShouldBe("approval:call-9");
}
```

> Build `CollectEventsForApprovalRequest` from the existing `AGUIStreamingService` test harness (there are existing streaming tests in `tests/Umbraco.AI.Agent.Tests.Unit/AGUI/`; reuse their fake `IAGUIMessageConverter`, `IAGUIFileProcessor`, and the fake agent that yields `ChatResponseUpdate`s).

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIStreamingServiceApprovalTests"`
Expected: FAIL — `ToolApprovalRequestContent` hits the `default` branch; no approval interrupt emitted.

- [ ] **Step 3: Add a case to the content switch in `StreamCoreAsync`**

Insert before the `default:` case (after the `TextContent` case at `AGUIStreamingService.cs:231-233`):

```csharp
case ToolApprovalRequestContent approvalRequest:
    // FICC replaced a destructive backend tool's FunctionCallContent with this. Register it
    // so the run finishes with a human_approval interrupt; also surface the proposed call to
    // the UI as a normal tool-call event so the user sees what they're approving.
    var approvalCall = approvalRequest.ToolCall;
    var argsJson = approvalCall is FunctionCallContent fcc && fcc.Arguments is not null
        ? JsonSerializer.Serialize(fcc.Arguments)
        : "{}";
    var approvalToolName = (approvalCall as FunctionCallContent)?.Name ?? approvalCall.CallId;

    _logger.LogInformation(
        "AGUIStreamingService received ToolApprovalRequestContent for '{ToolName}' (callId={CallId}) on run {RunId}.",
        approvalToolName, approvalCall.CallId, request.RunId);

    emitter.RegisterApprovalRequest(approvalCall.CallId, approvalToolName, argsJson);

    var approvalEvent = emitter.EmitToolCall(
        approvalCall.CallId, approvalToolName,
        (approvalCall as FunctionCallContent)?.Arguments, isFrontendTool: false);
    if (approvalEvent != null)
    {
        yield return approvalEvent;
    }
    break;
```

Add `using System.Text.Json;` if not already present (the file already serializes via the converter; confirm the import).

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIStreamingServiceApprovalTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIStreamingServiceApprovalTests.cs
git commit -m "feat(agent): surface backend tool approval requests over AG-UI"
```

---

### Task 4: Route resume entries to `ToolApprovalResponseContent`

**Files:**
- Modify: `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs:322-347` (`ExtractToolResultsFromResume`)
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIResumeMappingTests.cs`

**Implements per Task 0 findings.** The code below assumes finding **C = yes** (a standalone `ToolApprovalResponseContent` built from the call id correlates). If Task 0 found **B = yes** (the original request must also be in replayed history), add Task 4b below.

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.AI;
using System.Text.Json;
using Umbraco.AI.AGUI.Models;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIResumeMappingTests
{
    [Fact]
    public void ApprovalResume_Approved_ProducesToolApprovalResponseContent()
    {
        var entry = new AGUIResumeEntry
        {
            InterruptId = "approval:call-9",
            Status = AGUIResumeStatus.Resolved,
            Payload = JsonSerializer.SerializeToElement(new { approved = true }),
        };

        var messages = InvokeExtractToolResultsFromResume([entry]);

        var content = messages.ShouldHaveSingleItem()
            .Contents.OfType<ToolApprovalResponseContent>().ShouldHaveSingleItem();
        content.Approved.ShouldBeTrue();
        content.ToolCall.CallId.ShouldBe("call-9");
    }

    [Fact]
    public void ApprovalResume_Denied_ProducesDeniedResponse()
    {
        var entry = new AGUIResumeEntry
        {
            InterruptId = "approval:call-9",
            Status = AGUIResumeStatus.Resolved,
            Payload = JsonSerializer.SerializeToElement(new { approved = false }),
        };

        var messages = InvokeExtractToolResultsFromResume([entry]);
        messages.Single().Contents.OfType<ToolApprovalResponseContent>()
            .Single().Approved.ShouldBeFalse();
    }

    [Fact]
    public void ToolCallResume_StillProducesFunctionResultContent()
    {
        var entry = new AGUIResumeEntry
        {
            InterruptId = "call-1", // no prefix => frontend tool_call interrupt
            Status = AGUIResumeStatus.Resolved,
            Payload = JsonSerializer.SerializeToElement(new { ok = true }),
        };

        var messages = InvokeExtractToolResultsFromResume([entry]);
        messages.Single().Contents.OfType<FunctionResultContent>()
            .Single().CallId.ShouldBe("call-1");
    }
}
```

> `InvokeExtractToolResultsFromResume` exercises the private method — either make `ExtractToolResultsFromResume` `internal` and add `[assembly: InternalsVisibleTo("Umbraco.AI.Agent.Tests.Unit")]` (check whether it's already set in the Agent.Core csproj — the existing tests touching internals will tell you), or test it via the public `StreamAgentAsync` resume path. Prefer `internal` + InternalsVisibleTo for a focused unit test.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIResumeMappingTests"`
Expected: FAIL — every entry currently becomes a `FunctionResultContent`; the approval cases fail.

- [ ] **Step 3: Branch the resume mapping**

Replace the body of `ExtractToolResultsFromResume` (`AGUIStreamingService.cs:322-347`):

```csharp
private List<ChatMessage> ExtractToolResultsFromResume(IReadOnlyList<AGUIResumeEntry> resume)
{
    var results = new List<ChatMessage>();

    foreach (var entry in resume)
    {
        if (entry.Status != AGUIResumeStatus.Resolved)
        {
            continue;
        }

        if (string.IsNullOrEmpty(entry.InterruptId) || !entry.Payload.HasValue)
        {
            _logger.LogWarning(
                "Resume entry {InterruptId} resolved without a payload; skipping",
                entry.InterruptId);
            continue;
        }

        if (AGUIInterruptKind.IsApproval(entry.InterruptId))
        {
            // Backend approval interrupt: payload is { "approved": bool }. Build a
            // ToolApprovalResponseContent correlated by the original tool call id so FICC
            // resumes — executing the tool when approved, skipping it when denied.
            var toolCallId = AGUIInterruptKind.GetToolCallId(entry.InterruptId);
            var approved = entry.Payload.Value.TryGetProperty("approved", out var ap)
                && ap.ValueKind == JsonValueKind.True;

            var responseContent = new ToolApprovalResponseContent(
                toolCallId, approved, new ToolCallContent(toolCallId));

            // Per Task 0 finding A: the role that FICC correlates the response on.
            results.Add(new ChatMessage(ChatRole.User, [responseContent]));
            continue;
        }

        // Frontend tool_call interrupt (unchanged): InterruptId == toolCallId.
        var resultContent = new FunctionResultContent(entry.InterruptId, entry.Payload.Value);
        results.Add(new ChatMessage(ChatRole.Tool, [resultContent]));
    }

    return results;
}
```

Add `using System.Text.Json;` and `using Umbraco.AI.Agent.Core.AGUI;` (same namespace; no import needed). Replace `ChatRole.User` per the role confirmed in Task 0 finding A. Replace `new ToolCallContent(toolCallId)` with the richer construction confirmed in Task 0 finding C if a name/args-bearing `FunctionCallContent` is required for correlation.

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIResumeMappingTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIResumeMappingTests.cs
git commit -m "feat(agent): map approval resume entries to ToolApprovalResponseContent"
```

#### Task 4b (CONDITIONAL — only if Task 0 finding B = yes)

If FICC requires the original `ToolApprovalRequestContent` to be present in the replayed history for correlation, the client's replayed assistant message must round-trip it. Then:
- Modify `IAGUIMessageConverter`'s implementation (`ConvertToChatMessages`) so an AG-UI assistant message that carried an approval tool call is reconstructed with a `ToolApprovalRequestContent` (built from the stored `toolName` + `arguments` metadata the client echoes back).
- Add a unit test in `tests/.../AGUI/` asserting the converted message contains a `ToolApprovalRequestContent` with the matching `CallId`.
- Commit: `feat(agent): round-trip approval requests through message conversion`.

> Locate the converter via `grep -r "IAGUIMessageConverter" Umbraco.AI.Agent/src`. Write the failing test first (same TDD cadence), then implement.

---

### Task 5: Verify/align the frontend `human_approval` interrupt handling

**Files:**
- Read: `Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client/src/chat/services/handlers/hitl-interrupt.handler.ts` (handler for `reason = "human_approval"` — already exists)
- Read/Modify: `Umbraco.AI.Agent.UI/.../chat/services/interrupt.types.ts`, `hitl.context.ts`, `components/hitl-approval.element.ts`
- Read: `Umbraco.AI.Agent.Web.StaticAssets/Client/src/transport/uai-http-agent.ts` (resume payload assembly)

The client already has `UaiHitlInterruptHandler` keyed to `reason = "human_approval"` and an approval UI. This task confirms the server's new interrupt shape matches what the client expects and that resume sends `{ approved: bool }`.

- [ ] **Step 1: Trace the client interrupt→resume path**

Read the four files above and confirm:
- The handler receives an interrupt whose `reason === "human_approval"` and renders the approval element.
- The approval element's approve/deny resolves to a resume payload of `{ approved: true }` / `{ approved: false }`, with the resume entry's `interruptId` set to the server's interrupt `Id` (i.e. `"approval:call-9"`), `status: "Resolved"`.
- The `message`, `toolName`, and `arguments` (from interrupt `Metadata`) are surfaced to the user.

- [ ] **Step 2: Align any mismatch (edit only if needed)**

If the client currently builds a different payload shape (e.g. `{ approve: ... }` or echoes the option `value` string), adjust the approval element / resume assembly so the resolved payload is exactly `{ approved: boolean }` to match `ApprovalResponseSchema` from Task 2. Keep changes minimal and within the existing approval components.

- [ ] **Step 3: Build the frontend to verify it compiles**

Run: `npm run build:agent-ui`
Expected: build succeeds with no type errors.

- [ ] **Step 4: Commit (only if files changed)**

```bash
git add Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client/src/chat/
git commit -m "fix(agent-ui): align human_approval resume payload with backend schema"
```

---

### Task 6: End-to-end integration test (approve + deny)

**Files:**
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Integration/Agents/BackendToolApprovalFlowTests.cs` (create; confirm the integration test project name/path first via `ls Umbraco.AI.Agent/tests`)

- [ ] **Step 1: Write the failing integration test**

```csharp
// Scenario, against a scripted IChatClient registered for a real DI-built agent with one
// destructive backend tool ("delete_content"):
//
// Run 1: user says "delete content 42" -> model calls delete_content ->
//        StreamAgentAsync yields a RunFinishedEvent whose outcome is an interrupt with
//        reason "human_approval", id "approval:<callId>". The tool's executor has NOT run.
//
// Run 2 (resume, approved): same messages + resume entry { interruptId, Resolved, {approved:true} }
//        -> the tool executor RUNS exactly once -> the run finishes successfully.
//
// Run 3 (resume, denied):  resume entry { ..., {approved:false} }
//        -> the tool executor does NOT run -> the run finishes successfully (model is told it was denied).
[Fact] public async Task DestructiveBackendTool_PausesForApproval_ThenExecutesOnApprove() { /* ... */ }
[Fact] public async Task DestructiveBackendTool_Denied_DoesNotExecute() { /* ... */ }
```

> Use a spy `IAITool` whose `ExecuteAsync` increments a counter so "ran exactly once" / "never ran" is assertable. Reuse the integration project's existing DI/composition fixtures.

- [ ] **Step 2: Run to verify it fails, then passes after wiring**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~BackendToolApprovalFlowTests"`
Expected: initially FAIL on the approve path if any wiring gap remains; iterate until PASS. This test is the real proof the four backend tasks compose.

- [ ] **Step 3: Commit**

```bash
git add Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Integration/Agents/BackendToolApprovalFlowTests.cs
git commit -m "test(agent): end-to-end backend tool approval/deny flow"
```

---

### Task 7: Bump MEAI 10.6.0 → 10.7.0 and handle the `InformationalOnly` change

**Files:**
- Modify: `Directory.Packages.props:43,83` (and any product-level override that pins MEAI)
- Test: re-run the approval suites from Tasks 0–6

**Context:** 10.7.0 removed the back-compat path that auto-marked `ToolApprovalResponseContent` as `InformationalOnly`. With approval now in use, verify our resume responses still resume correctly under 10.7.

- [ ] **Step 1: Bump the floors**

In `Directory.Packages.props`, change:
```xml
<PackageVersion Include="Microsoft.Extensions.AI" Version="[10.7.0, 10.999.999)" />
<PackageVersion Include="Microsoft.Extensions.AI.OpenAI" Version="[10.7.0, 10.999.999)" />
```
Then check for product-level overrides: `grep -rl "Microsoft.Extensions.AI" --include=Directory.Packages.props .` and update any that pin a lower floor.

- [ ] **Step 2: Restore + build**

Run: `dotnet build Umbraco.AI.Agent/Umbraco.AI.Agent.slnx`
Expected: succeeds.

- [ ] **Step 3: Re-run the full approval suite**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~Approval"`
Expected: PASS. If the approved-resume path now fails to invoke the tool, the response content needs `InformationalOnly = false` set explicitly — add that in `ExtractToolResultsFromResume` (Task 4) where the `ToolApprovalResponseContent` is built, and re-run. Re-run the Task 0 spike too; update its assumptions if behavior shifted.

- [ ] **Step 4: Commit**

```bash
git add Directory.Packages.props
git commit -m "chore(deps): Bump Microsoft.Extensions.AI to 10.7.0 for tool approval"
```

---

## Scope boundary & open questions: headless / non-interactive approval

This plan implements approval over the **AG-UI streaming** path (interrupt → client approves → resume). It does **not** cover non-interactive callers — a real gap that must be resolved before approval can ever be **default-on** (see `project_v17_alignment_breaking_changes`).

Verified constraints:
- **The non-streaming path has no resume.** `RunAgentAction` (`Umbraco.AI.Automate/Actions/RunAgentAction.cs`) and any non-AG-UI caller invoke `IAIAgentService.RunAgentAsync(...)` — a single, non-streaming call. There is no interrupt/resume there, so a `ToolApprovalRequestContent` produced mid-run has nothing to resolve it: under default-on approval a destructive tool would leave the run incomplete (tool unexecuted) or stalled. **Behavior must be defined explicitly**, not inherited from whatever FICC does by default.
- **No human is present.** `RunAgentAction` runs as the workspace service account; there is no backoffice user or chat surface to approve, and our `Umbraco.AI.Automate` package currently has **no pause/human-task/approval primitive** (grep-confirmed).

Options (decide before default-on; record the outcome here):
1. **Per-execution approval policy** on `AIAgentExecutionOptions` (it already carries `UserGroupIds`/`AdditionalProperties` for headless) — e.g. `ApprovalPolicy { Interactive, AutoDeny, AutoAllowWithAudit }`. AG-UI streaming ⇒ `Interactive`; Automate ⇒ configurable.
2. **Auto-deny (safe default):** the tool is skipped, the model is told approval was denied, the run completes. Preserves automation flow; destructive tools simply don't run headlessly without explicit opt-in.
3. **Auto-allow-with-audit:** approval bypassed but audit-logged. Keeps automation working but defeats the gate — acceptable only behind an explicit per-agent/per-automation opt-in.
4. **Orchestrate with Umbraco Automate (richest — user-proposed).** `RunAgentAction` detects a pending approval and surfaces it to the Automate engine: pause the workflow, raise a human-approval task, resume the agent with the decision when actioned. **Depends on Automate capabilities not yet confirmed** — investigate whether `Umbraco.Automate.Core` supports a *suspended/awaiting* `ActionResult`, workflow pause/resume, and a human-task/approval step. Also requires a **non-AG-UI resume mechanism** (re-invoke the agent with a `ToolApprovalResponseContent` injected — the AG-UI `ExtractToolResultsFromResume` logic is AG-UI-specific; Automate needs an analogous path, or the approval is resolved before re-invocation). If Automate supports this, it's the proper headless story; if not, it's a larger cross-product effort and its own plan.

**Recommendation:** ship this plan (AG-UI/interactive) with an `AIAgentExecutionOptions.ApprovalPolicy` defaulting to **`AutoDeny`** for non-interactive callers — that makes a future default-on safe everywhere without deadlock. Treat the Automate-orchestrated human-task flow (option 4) as a **separate cross-product investigation/plan**, gated on confirming Automate's suspend/resume/human-task support.

## Cross-cutting risks & test obligations

1. **`AIToolReorderingChatClient` interaction.** `Umbraco.AI.Agent.Core/Chat/AIToolReorderingChatClient.cs` reorders so a `Terminate`-setting frontend call is handled correctly when batched with others. Approval-required backend calls are a *second* in-loop halt. Task 1 sets `AllowMultipleToolCalls = false` when destructive tools are present, which sidesteps most batching, but **add a regression test** for a turn that mixes a destructive backend tool and a frontend tool — assert the reordering client still surfaces both correctly (this can live in the Task 3 or Task 6 file). If `AllowMultipleToolCalls = false` makes the model serialize calls, the mixed-turn case may not even arise; document the observed behavior.
2. **Stateless replay.** The whole flow assumes the client replays prior messages + the resume entry. Task 0 finding B determines whether the approval *request* must survive that replay (Task 4b). Do not skip Task 0.
3. **AG-UI compliance** (`Umbraco.AI.AGUI/CLAUDE.md`): MEAI content types stay server-internal; only spec-compliant interrupt reasons (`human_approval`) and event types cross the wire. The new emitter code emits standard `AGUIInterruptInfo`/`RunFinishedEvent` only — no new event types. Keep it that way.
4. **System tools must never require approval** — the `t is not IAISystemTool` guard in Task 1 enforces the documented "system tools skip approval workflows" contract. The Task 1 test should also assert a destructive *system* tool is NOT wrapped (add a third case if a destructive system tool exists in fixtures).
5. **Frontend tools unchanged** — no frontend tool is ever wrapped in `ApprovalRequiredAIFunction`; their approval stays client-side. Task 1's wrapping operates only on `_toolCollection` backend tools, never on `additionalTools`/frontend functions.

---

## Self-Review

- **Spec coverage:** backend approval gate (Tasks 1,3,4,6), interrupt emission (Task 2), resume routing (Task 4), frontend alignment (Task 5), 10.7 bump + InformationalOnly (Task 7), reorder-client risk (risk 1 + tests), system-tool exclusion (Task 1 + risk 4), frontend-tools-unchanged (risk 5). The one genuine unknown (stateless correlation) is de-risked by the Task 0 spike before any production code.
- **Type/name consistency:** `AGUIInterruptKind.ForApproval/IsApproval/GetToolCallId` and the `"approval:"` prefix are used identically in Tasks 2 and 4; `RegisterApprovalRequest(toolCallId, toolName, argumentsJson)` signature matches between Task 2 (definition) and Task 3 (call site); `ApprovalResponseSchema` and the `{ approved: bool }` payload are consistent across Tasks 2, 4, 5. `ToolApprovalResponseContent(string, bool, ToolCallContent)` matches the verified 10.6 ctor.
- **Placeholder scan:** the two intentionally deferred decisions (response `ChatRole`, and whether `ToolCallContent` must be richer) are explicitly gated on Task 0 findings recorded in-plan, not left as vague TODOs. Task 4b is conditional on a named finding. Test-harness helpers (`CreateFactoryWith`, `CollectEventsForApprovalRequest`) are directed to specific existing test files to copy from rather than invented APIs.
