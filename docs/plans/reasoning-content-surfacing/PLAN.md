# Reasoning Content Surfacing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Surface a model's reasoning / chain-of-thought (MEAI `TextReasoningContent`) over AG-UI so the agent chat UI can render it as a distinct, collapsible "thinking" block — instead of silently dropping it (it currently hits the `default` branch in the stream loop).

**Architecture:** Add the standard AG-UI `REASONING_*` events to the `Umbraco.AI.AGUI` SDK. In `AGUIStreamingService.StreamCoreAsync`, detect `TextReasoningContent` and emit `REASONING_MESSAGE_CHUNK` (with `REASONING_START`/`REASONING_END` lifecycle). On the frontend, handle those events and render reasoning in a collapsible block separate from the assistant's answer. Reasoning is display-only and does not enter the answer text stream.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI 10.6.0 (`TextReasoningContent`), AG-UI protocol SDK (`Umbraco.AI.AGUI` — strict spec compliance per its CLAUDE.md), Lit/TypeScript frontend (`Umbraco.AI.Agent.UI`), xUnit + Shouldly.

---

## Background: verified current state & spec

- **Reasoning is dropped today:** `AGUIStreamingService.StreamCoreAsync` (`Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs:182-244`) switches on `FunctionCallContent` / `FunctionResultContent` / `ErrorContent` / `TextContent` / `default`. `TextReasoningContent` falls into `default` and is debug-logged then discarded.
- **Text-message roles can't carry reasoning:** `AGUITextMessageRole` (`Umbraco.AI.AGUI/Models/AGUITextMessageRole.cs`) is restricted by spec to `user`/`assistant`/`system`/`developer`. The existing `AGUIConstants.MessageRoles.Reasoning = "reasoning"` and the `Reasoning` value in the generated `AGUIMessageRoleModel` are for **full message history** (e.g. `MESSAGES_SNAPSHOT`), not streaming text events.
- **AG-UI represents streaming reasoning via dedicated, stable events** (these replaced the deprecated `THINKING_*` events):
  - `REASONING_START` — fields: `messageId`.
  - `REASONING_MESSAGE_START` — fields: `messageId`, `role` (`"reasoning"`).
  - `REASONING_MESSAGE_CONTENT` — fields: `messageId`, `delta`.
  - `REASONING_MESSAGE_END` — fields: `messageId`.
  - `REASONING_MESSAGE_CHUNK` — fields: `messageId`, `delta` (convenience event auto-managing message lifecycle; an empty delta closes the message). Analogous to the `TEXT_MESSAGE_CHUNK` we already use for answers.
  - `REASONING_END` — fields: `messageId`.
  - `REASONING_ENCRYPTED_VALUE` — fields: `subtype` (`"message"`/`"tool-call"`), `entityId`, `encryptedValue`. Carries provider-encrypted reasoning so it can be replayed on later turns. **Out of scope for this plan** (see "Out of scope").
- **AGUI event-adding process** (from `Umbraco.AI.AGUI/CLAUDE.md`): each event implements `IAGUIEvent`/extends `BaseAGUIEvent`, lives in an `Events/<Category>/` folder, gets a constant in `AGUIConstants.EventTypes`, and must have serialization tests. Event type strings are UPPER_SNAKE_CASE.
- **Emitter** (`AGUIEventEmitter`): `EmitTextChunk(delta)` hard-codes `Role = Assistant` and uses `_currentMessageId`. Reasoning needs its own message id lifecycle so reasoning and answer chunks don't collide.

### Design decision

Use the **`REASONING_MESSAGE_CHUNK` convenience event** as the primary emission (mirrors how we already emit answer text via `TextMessageChunkEvent`), bracketed by `REASONING_START` / `REASONING_END` so the UI can show a "thinking…" affordance. This is the minimal standard-compliant surface. We implement the chunk + start/end events; we do **not** implement the granular `REASONING_MESSAGE_START/CONTENT/END` trio (the chunk event subsumes them for our streaming model).

---

## File Structure

**Create (AGUI SDK — `Umbraco.AI.Agent/src/Umbraco.AI.AGUI/Events/Reasoning/`):**
- `ReasoningStartEvent.cs`, `ReasoningMessageChunkEvent.cs`, `ReasoningEndEvent.cs`.

**Modify:**
- `Umbraco.AI.AGUI/AGUIConstants.cs` — add `EventTypes.ReasoningStart`, `ReasoningMessageChunk`, `ReasoningEnd`.
- `Umbraco.AI.AGUI/Streaming/AGUIEventEmitter.cs` — add reasoning emission + reasoning message-id lifecycle.
- `Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs` — add a `TextReasoningContent` case.
- Frontend `Umbraco.AI.Agent.UI/.../chat/` — event handling + a reasoning render block.

**Spike (create, then keep as a guard test):**
- `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/ProviderReasoningContentSpikeTests.cs`.

---

### Task 0: Spike — confirm a provider surfaces `TextReasoningContent`

**Why first:** the entire feature is moot if no configured provider emits reasoning in our setup. Reasoning is provider- and option-dependent (e.g. Anthropic extended thinking must be enabled; OpenAI reasoning models emit reasoning summaries). This task confirms we receive `TextReasoningContent` and documents how to enable it.

**Strong prior (MEAI change #7295):** MEAI added "OpenAI-compatible `reasoning_content` now surfaced as `TextReasoningContent`." Our **DeepSeek, FireworksAI, TogetherAI, HuggingFace, MicrosoftFoundry** providers (and OpenAI itself) all build on `Microsoft.Extensions.AI.OpenAI`, so they should light up. **Test DeepSeek (R1) first** — it emits `reasoning_content` natively with no special option, unlike Anthropic (needs a thinking budget). Two caveats to verify in this spike: (a) this mapping may be version-gated — confirm it on our current MEAI floor, and if it requires 10.7, treat the bump as a prerequisite (Task 1b); (b) a real-provider check belongs in the Task 5 integration test, not just the fake-pipeline test below.

**Files:**
- Create: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/ProviderReasoningContentSpikeTests.cs`

- [ ] **Step 1: Inventory how providers expose reasoning**

Search the provider packages for any existing thinking/reasoning option wiring:
Run: `grep -rniE "thinking|reasoning|ReasoningEffort|budget_tokens|TextReasoningContent" Umbraco.AI.Anthropic/src Umbraco.AI.OpenAI/src Umbraco.AI.Google/src`
Record: which provider(s) can emit reasoning, and what `ChatOptions`/settings toggle it (e.g. an Anthropic thinking budget, an OpenAI `reasoning_effort`). If a provider needs a flag we don't set, note where it would be set (the provider's chat capability `CreateClientAsync`, or `ChatOptions`).

- [ ] **Step 2: Write a spike test that asserts reasoning flows as `TextReasoningContent`**

```csharp
using Microsoft.Extensions.AI;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

public class ProviderReasoningContentSpikeTests
{
    // A fake provider-style client that emits a reasoning chunk then an answer chunk,
    // proving our PIPELINE forwards TextReasoningContent. (Real-provider verification is a
    // manual/integration follow-up recorded in Step 3.)
    private sealed class ReasoningEmittingClient : IChatClient
    {
        public ChatClientMetadata Metadata { get; } = new("reasoning-fake");
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> m, ChatOptions? o = null, CancellationToken c = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new TextReasoningContent("let me think..."), new TextContent("the answer")])));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> m, ChatOptions? o = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken c = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent("let me think...")]);
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("the answer")]);
        }
        public object? GetService(Type t, object? k = null) => null;
        public void Dispose() { }
    }

    [Fact]
    public async Task StreamingResponse_CanCarry_TextReasoningContent()
    {
        var client = new ReasoningEmittingClient();
        var sawReasoning = false;

        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            if (update.Contents.OfType<TextReasoningContent>().Any())
            {
                sawReasoning = true;
            }
        }

        sawReasoning.ShouldBeTrue();
    }
}
```

- [ ] **Step 3: Record findings in this plan**

Fill the assumptions block below with: which real provider emits reasoning, the option that enables it, and whether we must set that option (and where). If NO provider currently surfaces reasoning without extra wiring, add a Task 1b to enable it on at least one provider before the frontend work is worthwhile.

- [ ] **Step 4: Run + commit**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~ProviderReasoningContentSpikeTests"`
Expected: PASS (proves the pipeline can carry it; real-provider proof is the integration test in Task 5).

```bash
git add Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Chat/ProviderReasoningContentSpikeTests.cs Umbraco.AI/docs/internal/plans/2026-06-16-reasoning-content-surfacing.md
git commit -m "test(agent): characterize TextReasoningContent flow through the pipeline"
```

> **Assumptions (filled in by Task 0):**
> - Provider(s) that emit reasoning: `__________`
> - Option that enables it + where set: `__________`
> - Extra enablement task needed (1b)? `__________`

---

### Task 1: Add `REASONING_*` AG-UI events

**Files:**
- Create: `Umbraco.AI.AGUI/Events/Reasoning/ReasoningStartEvent.cs`, `ReasoningMessageChunkEvent.cs`, `ReasoningEndEvent.cs`
- Modify: `Umbraco.AI.AGUI/AGUIConstants.cs`
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/ReasoningEventSerializationTests.cs`

- [ ] **Step 1: Write the failing serialization tests**

```csharp
using Umbraco.AI.AGUI;
using Umbraco.AI.AGUI.Events.Reasoning;
using Umbraco.AI.AGUI.Streaming;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class ReasoningEventSerializationTests
{
    private readonly AGUIEventSerializer _serializer = new();

    [Fact]
    public void ReasoningStart_Serializes()
    {
        var evt = new ReasoningStartEvent { MessageId = "r1", Timestamp = 0 };
        evt.Type.ShouldBe(AGUIConstants.EventTypes.ReasoningStart);
        _serializer.Serialize(evt).ShouldContain("\"messageId\":\"r1\"");
    }

    [Fact]
    public void ReasoningMessageChunk_Serializes_WithDelta()
    {
        var evt = new ReasoningMessageChunkEvent { MessageId = "r1", Delta = "thinking", Timestamp = 0 };
        evt.Type.ShouldBe(AGUIConstants.EventTypes.ReasoningMessageChunk);
        var json = _serializer.Serialize(evt);
        json.ShouldContain("\"delta\":\"thinking\"");
        json.ShouldContain("\"messageId\":\"r1\"");
    }

    [Fact]
    public void ReasoningEnd_Serializes()
    {
        var evt = new ReasoningEndEvent { MessageId = "r1", Timestamp = 0 };
        evt.Type.ShouldBe(AGUIConstants.EventTypes.ReasoningEnd);
        _serializer.Serialize(evt).ShouldContain("\"messageId\":\"r1\"");
    }
}
```

> Match the exact serializer API and field casing used by the existing event tests in `tests/.../AGUI/` (the AGUI CLAUDE.md shows `AGUIEventSerializer.Serialize`). Confirm whether `Timestamp` is required on `BaseAGUIEvent`.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~ReasoningEventSerializationTests"`
Expected: FAIL — events and constants don't exist.

- [ ] **Step 3: Add the constants**

In `AGUIConstants.EventTypes`, add a Reasoning section (UPPER_SNAKE_CASE per spec):

```csharp
        // Reasoning events
        public const string ReasoningStart = "REASONING_START";
        public const string ReasoningMessageChunk = "REASONING_MESSAGE_CHUNK";
        public const string ReasoningEnd = "REASONING_END";
```

- [ ] **Step 4: Create the event records**

```csharp
// Events/Reasoning/ReasoningStartEvent.cs
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Reasoning;

/// <summary>
/// Signals that the agent is beginning a reasoning process.
/// </summary>
/// <remarks>AG-UI spec: https://docs.ag-ui.com/concepts/events (Reasoning events).</remarks>
public sealed record ReasoningStartEvent : BaseAGUIEvent
{
    /// <inheritdoc />
    public override string Type => AGUIConstants.EventTypes.ReasoningStart;

    /// <summary>The reasoning message identifier.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}
```

```csharp
// Events/Reasoning/ReasoningMessageChunkEvent.cs
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Reasoning;

/// <summary>
/// Delivers an incremental reasoning content chunk. An empty delta closes the message
/// (convenience event auto-managing the reasoning message lifecycle).
/// </summary>
/// <remarks>AG-UI spec: https://docs.ag-ui.com/concepts/events (Reasoning events).</remarks>
public sealed record ReasoningMessageChunkEvent : BaseAGUIEvent
{
    /// <inheritdoc />
    public override string Type => AGUIConstants.EventTypes.ReasoningMessageChunk;

    /// <summary>The reasoning message identifier.</summary>
    [JsonPropertyName("messageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; init; }

    /// <summary>The reasoning content delta.</summary>
    [JsonPropertyName("delta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Delta { get; init; }
}
```

```csharp
// Events/Reasoning/ReasoningEndEvent.cs
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Reasoning;

/// <summary>
/// Signals completion of the reasoning process.
/// </summary>
/// <remarks>AG-UI spec: https://docs.ag-ui.com/concepts/events (Reasoning events).</remarks>
public sealed record ReasoningEndEvent : BaseAGUIEvent
{
    /// <inheritdoc />
    public override string Type => AGUIConstants.EventTypes.ReasoningEnd;

    /// <summary>The reasoning message identifier.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}
```

> Mirror the exact base-class members and JSON conventions of an existing event (e.g. `Events/Messages/TextMessageChunkEvent.cs`) — including whether `Timestamp` is inherited/required and how `Type` is declared.

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~ReasoningEventSerializationTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add Umbraco.AI.Agent/src/Umbraco.AI.AGUI/Events/Reasoning/ Umbraco.AI.Agent/src/Umbraco.AI.AGUI/AGUIConstants.cs Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/ReasoningEventSerializationTests.cs
git commit -m "feat(agent): add AG-UI reasoning events to the protocol SDK"
```

---

### Task 2: Emitter support for reasoning

**Files:**
- Modify: `Umbraco.AI.AGUI/Streaming/AGUIEventEmitter.cs`
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIEventEmitterReasoningTests.cs`

**Design:** reasoning gets its own message-id lifecycle, independent of `_currentMessageId` (the answer stream), so a "thinking" block and the answer render as separate UI blocks. `EmitReasoningChunk` lazily opens a reasoning message (assigning a new id) and is bracketed by explicit start/end helpers.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void EmitReasoningChunk_UsesAReasoningMessageId_DistinctFromAnswer()
{
    var emitter = new AGUIEventEmitter("t", "r");

    var answer = emitter.EmitTextChunk("answer");
    var start = emitter.EmitReasoningStart();
    var chunk = emitter.EmitReasoningChunk("thinking");
    var end = emitter.EmitReasoningEnd();

    start.MessageId.ShouldNotBeNullOrEmpty();
    chunk.MessageId.ShouldBe(start.MessageId);
    end.MessageId.ShouldBe(start.MessageId);
    chunk.MessageId.ShouldNotBe(answer.MessageId); // reasoning != answer block
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIEventEmitterReasoningTests"`
Expected: FAIL — methods don't exist.

- [ ] **Step 3: Implement emitter methods**

Add a reasoning message-id field near `_currentMessageId`:

```csharp
    private string? _currentReasoningMessageId;
```

Add methods (place near `EmitTextChunk`):

```csharp
/// <summary>Emits a <see cref="ReasoningStartEvent"/>, opening a new reasoning block.</summary>
public ReasoningStartEvent EmitReasoningStart()
{
    _currentReasoningMessageId = Guid.NewGuid().ToString();
    return new ReasoningStartEvent
    {
        MessageId = _currentReasoningMessageId,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };
}

/// <summary>Emits a <see cref="ReasoningMessageChunkEvent"/> for streaming reasoning content.</summary>
public ReasoningMessageChunkEvent EmitReasoningChunk(string delta)
{
    // Defensive: open a reasoning message if a chunk arrives before an explicit start.
    _currentReasoningMessageId ??= Guid.NewGuid().ToString();
    return new ReasoningMessageChunkEvent
    {
        MessageId = _currentReasoningMessageId,
        Delta = delta,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };
}

/// <summary>Emits a <see cref="ReasoningEndEvent"/>, closing the current reasoning block.</summary>
public ReasoningEndEvent EmitReasoningEnd()
{
    var id = _currentReasoningMessageId ?? Guid.NewGuid().ToString();
    _currentReasoningMessageId = null;

    // Start a fresh answer message id so post-reasoning answer text is its own UI block.
    RegenerateMessageId();

    return new ReasoningEndEvent
    {
        MessageId = id,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };
}
```

Add `using Umbraco.AI.AGUI.Events.Reasoning;` to the emitter.

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIEventEmitterReasoningTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI.Agent/src/Umbraco.AI.AGUI/Streaming/AGUIEventEmitter.cs Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIEventEmitterReasoningTests.cs
git commit -m "feat(agent): add reasoning event emission to AGUIEventEmitter"
```

---

### Task 3: Emit reasoning from the stream

**Files:**
- Modify: `Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs` (the content switch, ~line 231 before `default:`)
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIStreamingServiceReasoningTests.cs`

**Design:** the model interleaves reasoning then answer. Open a reasoning block on the first `TextReasoningContent`, emit chunks, and close it when the first non-reasoning content (answer text or tool call) arrives. Track an "in reasoning" flag local to the stream loop.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task Stream_WithReasoningThenText_EmitsReasoningEventsThenAnswer()
{
    // Fake agent yields: update[TextReasoningContent "think"], then update with Text "answer".
    var events = await CollectEventsFor(
        reasoning: "think", answer: "answer");

    var types = events.Select(e => e.Type).ToList();
    types.ShouldContain(AGUIConstants.EventTypes.ReasoningStart);
    types.ShouldContain(AGUIConstants.EventTypes.ReasoningMessageChunk);
    types.ShouldContain(AGUIConstants.EventTypes.ReasoningEnd);

    // Reasoning end must come before the answer text chunk.
    types.IndexOf(AGUIConstants.EventTypes.ReasoningEnd)
        .ShouldBeLessThan(types.IndexOf(AGUIConstants.EventTypes.TextMessageChunk));
}
```

> Build `CollectEventsFor` from the existing `AGUIStreamingService` test harness (reuse the fake agent/`ChatResponseUpdate` source used by current streaming tests).

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIStreamingServiceReasoningTests"`
Expected: FAIL — `TextReasoningContent` is dropped; no reasoning events.

- [ ] **Step 3: Add the `TextReasoningContent` case + close-on-other-content**

Add a stream-local flag at the top of the `await foreach` body scope in `StreamCoreAsync` (declare before the loop):

```csharp
var reasoningOpen = false;
```

Add a case before `case TextContent:`:

```csharp
case TextReasoningContent reasoning:
    if (!reasoningOpen)
    {
        reasoningOpen = true;
        yield return emitter.EmitReasoningStart();
    }
    if (!string.IsNullOrEmpty(reasoning.Text))
    {
        yield return emitter.EmitReasoningChunk(reasoning.Text);
    }
    break;
```

Close the reasoning block when answer text or a tool call arrives. In the `FunctionCallContent` case and just before emitting answer text (the `if (!string.IsNullOrEmpty(update.Text))` block at ~line 248), add:

```csharp
if (reasoningOpen)
{
    reasoningOpen = false;
    yield return emitter.EmitReasoningEnd();
}
```

> Factor this into a small local function `IAGUIEvent? CloseReasoningIfOpen()` returning the end event (or null) to avoid duplicating the guard; `yield return` it where non-null. Also close any still-open reasoning block after the `await foreach` completes (a turn that is reasoning-only).

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~AGUIStreamingServiceReasoningTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/AGUI/AGUIStreamingService.cs Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/AGUI/AGUIStreamingServiceReasoningTests.cs
git commit -m "feat(agent): emit AG-UI reasoning events from the stream"
```

---

### Task 4: Frontend — render reasoning as a collapsible block

**Files:**
- Read first: `Umbraco.AI.Agent.UI/.../chat/services/run.controller.ts` (event dispatch), `chat/components/message.element.ts`, `chat/components/agent-status.element.ts`, `Umbraco.AI.Agent.Web.StaticAssets/Client/src/transport/uai-http-agent.ts` (does `@ag-ui/client` already parse REASONING_* events?)
- Modify: the run controller / message model to capture reasoning, and a component to render it.

- [ ] **Step 1: Determine whether the transport already parses REASONING_* events**

Read `uai-http-agent.ts` and the `@ag-ui/client` version in use. If the client lib already surfaces reasoning events, hook the run controller's event handling. If not, parse the raw SSE `REASONING_START`/`REASONING_MESSAGE_CHUNK`/`REASONING_END` event types directly (mirror how `TEXT_MESSAGE_CHUNK` is handled).

- [ ] **Step 2: Accumulate reasoning onto the message model**

In `run.controller.ts`, on `REASONING_MESSAGE_CHUNK` append `delta` to a `reasoning` string on the in-flight assistant message (separate from `content`); on `REASONING_START` set agent status to "thinking" and open the block; on `REASONING_END` mark the reasoning complete. Keep reasoning out of the `content` (answer) field.

- [ ] **Step 3: Render the collapsible block**

In `message.element.ts` (or a new `reasoning-block.element.ts` used by it), when the message has `reasoning` text, render a collapsible `<details>`-style block labelled "Reasoning"/"Thinking", collapsed by default, above the answer content. Reuse existing chat styles.

- [ ] **Step 4: Build to verify**

Run: `npm run build:agent-ui`
Expected: builds clean. Manually verify against a reasoning-capable provider/profile (per Task 0 finding) on the demo site: the thinking block streams, collapses, and the answer renders separately.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client/src/chat/
git commit -m "feat(agent-ui): render streaming model reasoning as a collapsible block"
```

---

### Task 5: Integration test (reasoning → answer ordering)

**Files:**
- Test: `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Integration/AGUI/ReasoningStreamFlowTests.cs`

- [ ] **Step 1: Write the failing integration test**

```csharp
// Drive the full StreamAgentAsync path with a scripted agent/chat client that emits
// TextReasoningContent then TextContent, and assert the SSE event sequence contains
// REASONING_START -> REASONING_MESSAGE_CHUNK(s) -> REASONING_END -> TEXT_MESSAGE_CHUNK,
// and that the answer text never appears inside a reasoning event.
[Fact] public async Task FullRun_SeparatesReasoningFromAnswer() { /* ... */ }
```

- [ ] **Step 2: Run, iterate to green**

Run: `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx --filter "FullyQualifiedName~ReasoningStreamFlowTests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Integration/AGUI/ReasoningStreamFlowTests.cs
git commit -m "test(agent): end-to-end reasoning/answer stream separation"
```

---

## Out of scope (follow-ups)

- **`REASONING_ENCRYPTED_VALUE` + multi-turn reasoning replay.** Some providers (e.g. Anthropic interleaved thinking with tool use) require the encrypted reasoning block to be replayed on subsequent turns for tool-use continuity. Our AG-UI runs are stateless (client replays history), so preserving encrypted reasoning across the round-trip is a separate, provider-specific effort. If Task 0 finds a target provider needs this for tool-using reasoning, log a follow-up work item — do not expand this plan.
- **Reasoning in non-agent inline chat** (`IAIChatService`). This plan is agent/AG-UI only. Inline chat has no streaming UI contract for reasoning; out of scope.
- **Persisting reasoning into stored conversation history** (the `Reasoning` message role on `MESSAGES_SNAPSHOT`). Display-only streaming is the goal here; persistence is a follow-up.

## Cross-cutting risks & notes

1. **Provider enablement (gating risk)** — Task 0 decides feasibility. If no provider surfaces reasoning without extra option wiring, the frontend work has nothing to show; do Task 0 (and any 1b enablement) before Task 4.
2. **AG-UI compliance** (`Umbraco.AI.AGUI/CLAUDE.md`) — reasoning events must match the spec's field names/casing exactly; serialization tests (Task 1) are the guard. We deliberately use the standard `REASONING_*` events, not a `CustomEvent`, because reasoning is a first-class AG-UI concept.
3. **Block separation** — reasoning must never leak into the answer `content` (it would corrupt the rendered answer and any downstream consumers). Tasks 3 and 5 assert the ordering and separation.
4. **Ordering with tool calls** — a turn may reason, then call a tool, then reason again. The close-on-other-content logic (Task 3) plus the per-block message ids (Task 2) must handle multiple reasoning blocks per turn; add a test case for reason→tool→reason if Task 0's provider does interleaved thinking.

## Self-Review

- **Spec coverage:** provider feasibility (Task 0), AG-UI events (Task 1), emitter (Task 2), stream emission + block separation (Task 3), frontend rendering (Task 4), end-to-end ordering (Task 5). Encrypted-reasoning replay and persistence explicitly deferred with rationale.
- **Type/name consistency:** `EventTypes.ReasoningStart/ReasoningMessageChunk/ReasoningEnd`, `ReasoningStartEvent`/`ReasoningMessageChunkEvent`/`ReasoningEndEvent`, and emitter methods `EmitReasoningStart/EmitReasoningChunk/EmitReasoningEnd` are used identically across Tasks 1-5. Event field names (`messageId`, `delta`) match the spec listed in Background.
- **Placeholder scan:** the frontend task (Task 4) is intentionally investigation-led (transport-lib-dependent) but each step states the concrete decision/output; backend tasks carry full code. Test-harness helpers point at named existing harnesses rather than invented APIs. The one true unknown (provider reasoning surfacing) is a gated spike, not a hidden assumption.
