# Chat History Reduction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add opt-in conversation history reduction to the chat pipeline so long agent/chat conversations stay within a bounded size — preventing runaway token cost, latency, and eventual context-window failures — using MEAI's `ReducingChatClient` + reducers as a new chat middleware.

**Architecture:** A new `IAIChatMiddleware` (`AIChatReducingChatMiddleware`) wraps the chat client with MEAI's `ReducingChatClient` via `.UseChatReducer(reducer)`. Strategy is config-driven from `AIOptions.Reduction` and defaults to `None` (no behavior change unless enabled). Ship in two strategies: `MessageCount` (deterministic, no extra LLM cost — delivered first) and `Summarize` (uses a configured profile's `IChatClient` to summarize older turns — delivered second). The middleware is ordered **just inside** function invocation so reduction applies to what the provider sees on every model round-trip, including tool-heavy agent turns.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI 10.6.0 (`ReducingChatClient`, `MessageCountingChatReducer`, `SummarizingChatReducer`, `IChatReducer`), Umbraco collection-builder middleware pattern, xUnit + Shouldly + Moq.

---

## Background: verified current state

- **No reduction exists anywhere** — confirmed by search; conversations are sent to the provider in full every turn.
- **Chat middleware pipeline** is registered in `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs:106-116` via `builder.AIChatMiddleware().Append<...>()`. **Append order = inner→outer**: the first appended (`AIOpenTelemetryChatMiddleware`) is innermost (closest to the provider); the last appended (`AIContextInjectingChatMiddleware`) is outermost (hit first by the caller). `AIFunctionInvokingChatMiddleware` is appended at line 111.
- **Middleware contract** (`Umbraco.AI.Core/Chat/IAIChatMiddleware.cs`): `IChatClient Apply(IChatClient client)`. Canonical example — `AIFunctionInvokingChatMiddleware` (`Chat/Middleware/AIFunctionInvokingChatMiddleware.cs`): `client.AsBuilder().UseFunctionInvocation(_loggerFactory).Build()`.
- **Config** lives in `Umbraco.AI.Core/Models/AIOptions.cs` (bound from `Umbraco:AI`), already holding profile aliases like `ClassifierChatProfileAlias`.
- **Client construction** from a profile: `IAIChatClientFactory.CreateClientAsync(AIProfile, CancellationToken)` (`Chat/AIChatClientFactory.cs`); profile lookup by alias via `IAIProfileService.GetProfileByAliasAsync` (used elsewhere).

### Verified MEAI 10.6.0 API surface

- `IChatReducer.ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken)` → reduced message list.
- `new MessageCountingChatReducer(int targetCount)`.
- `new SummarizingChatReducer(IChatClient chatClient, int targetCount, int? thresholdCount)`; settable `SummarizationPrompt`.
- `new ReducingChatClient(IChatClient innerClient, IChatReducer reducer)`.
- `ReducingChatClientBuilderExtensions.UseChatReducer(ChatClientBuilder builder, IChatReducer reducer, Action<ReducingChatClient>? configure)`.

### Ordering decision

Place the reducer via `InsertBefore<AIFunctionInvokingChatMiddleware>` so it is **more inner** than function invocation (closer to the provider). This means reduction runs on every model round-trip inside the `FunctionInvokingChatClient` loop, bounding even single user turns that trigger many tool calls. Trade-off (documented in Task 3): with `Summarize`, the summarizer may run on multiple round-trips of one turn — mitigated by the reducer's threshold so it only fires when over budget.

## Effect on Copilot / displayed chat history (read before implementing)

This was a specific concern; the behavior must be preserved as an invariant.

**Display is NOT affected.** Verified architecture: the Copilot/agent chat UI owns the transcript **client-side** (`Umbraco.AI.Agent.UI/.../chat/services/run.controller.ts`, the `#messages` `BehaviorSubject`). There is **no server-side transcript store** — agent runs are stateless and the client **replays its full message history to the server every turn** (`sendMessage(this.#messages.value, …)`). The reducer transforms the `IEnumerable<ChatMessage>` handed to the provider **for a single call**: its output is provider-bound, is never streamed back as events, and **must never be emitted as a `MESSAGES_SNAPSHOT`**. So the user keeps seeing the full conversation regardless of reduction.

- **Invariant + test obligation:** reduction must never surface its reduced/summarized list to the client. Add an assertion (in Task 3 or Task 6) that a reduced run emits **no `MESSAGES_SNAPSHOT` event attributable to reduction**. (The only legitimate snapshot source is the file processor's base64→URL rewrite, which is independent of the reducer.)
- **What changes is the model's context, not the display → "perception gap":** with `MessageCount`, messages still visible on screen can fall outside the model's view ("the model forgot what I can still see"); `Summarize` mitigates by retaining a summary of older turns; `None` (default) has no gap. Call this out in the Task 7 docs.

**Per-call recompute (the caching question).** Because the server is stateless and the client replays the *full* history each turn, **`Summarize` recomputes the summary on every turn** — and, given the inside-FICC ordering above, potentially on every model round-trip within a turn. MEAI's `SummarizingChatReducer` holds no cross-call cache (it can't — it receives a fresh list each call). `MessageCount` has **no** such cost (pure slicing). The caching strategy is therefore a real design decision for the `Summarize` path — see Task 5's "Summarization cost & caching".

---

## File Structure

**Create:**
- `Umbraco.AI/src/Umbraco.AI.Core/Models/AIChatReductionOptions.cs` — config POCO + strategy enum.
- `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AIChatReducingChatMiddleware.cs` — the middleware.
- Test files (see tasks).

**Modify:**
- `Umbraco.AI/src/Umbraco.AI.Core/Models/AIOptions.cs` — add `Reduction` property.
- `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs:106-116` — register the middleware + DI for the options.

---

### Task 1: Reduction config model

**Files:**
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Models/AIChatReductionOptions.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Models/AIOptions.cs`
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Models/AIChatReductionOptionsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Umbraco.AI.Core.Models;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Models;

public class AIChatReductionOptionsTests
{
    [Fact]
    public void Defaults_AreOff_AndSafe()
    {
        var options = new AIOptions();

        options.Reduction.ShouldNotBeNull();
        options.Reduction.Strategy.ShouldBe(AIChatReductionStrategy.None);
        options.Reduction.TargetMessageCount.ShouldBeGreaterThan(0);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIChatReductionOptionsTests"`
Expected: FAIL — `AIOptions.Reduction` / `AIChatReductionStrategy` do not exist.

- [ ] **Step 3: Create the options model**

```csharp
namespace Umbraco.AI.Core.Models;

/// <summary>
/// Strategy for reducing conversation history before it is sent to the model.
/// </summary>
public enum AIChatReductionStrategy
{
    /// <summary>No reduction — the full conversation is sent every turn (default).</summary>
    None = 0,

    /// <summary>Keep only the most recent <see cref="AIChatReductionOptions.TargetMessageCount"/> messages.</summary>
    MessageCount = 1,

    /// <summary>Summarize older messages into a single summary using a chat profile (see
    /// <see cref="AIChatReductionOptions.SummarizationProfileAlias"/>).</summary>
    Summarize = 2,
}

/// <summary>
/// Configuration for chat history reduction. Bound from <c>Umbraco:AI:Reduction</c>.
/// </summary>
public class AIChatReductionOptions
{
    /// <summary>The reduction strategy. Defaults to <see cref="AIChatReductionStrategy.None"/>.</summary>
    public AIChatReductionStrategy Strategy { get; set; } = AIChatReductionStrategy.None;

    /// <summary>
    /// Target number of messages to retain. For <see cref="AIChatReductionStrategy.MessageCount"/>
    /// this is the hard cap; for <see cref="AIChatReductionStrategy.Summarize"/> it is the number
    /// of recent messages kept verbatim alongside the summary.
    /// </summary>
    public int TargetMessageCount { get; set; } = 40;

    /// <summary>
    /// For <see cref="AIChatReductionStrategy.Summarize"/>: only summarize once the conversation
    /// exceeds this many messages. Null lets the reducer use its default behavior.
    /// </summary>
    public int? SummarizationThresholdMessageCount { get; set; }

    /// <summary>
    /// For <see cref="AIChatReductionStrategy.Summarize"/>: the chat profile alias whose client
    /// performs summarization. Falls back to <see cref="AIOptions.ClassifierChatProfileAlias"/>
    /// then the default chat profile when null.
    /// </summary>
    public string? SummarizationProfileAlias { get; set; }
}
```

Add to `AIOptions` (after `DefaultSpeechToTextProfileAlias`):

```csharp
/// <summary>
/// Conversation history reduction settings. Defaults to no reduction.
/// </summary>
public AIChatReductionOptions Reduction { get; set; } = new();
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIChatReductionOptionsTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/Models/AIChatReductionOptions.cs Umbraco.AI/src/Umbraco.AI.Core/Models/AIOptions.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Models/AIChatReductionOptionsTests.cs
git commit -m "feat(core): add chat history reduction config options"
```

---

### Task 2: `MessageCount` reduction middleware (no-op when disabled)

**Files:**
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AIChatReducingChatMiddleware.cs`
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Chat/Middleware/AIChatReducingChatMiddlewareTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Chat.Middleware;

public class AIChatReducingChatMiddlewareTests
{
    private sealed class PassthroughClient : IChatClient
    {
        public IEnumerable<ChatMessage>? LastSeenMessages { get; private set; }
        public ChatClientMetadata Metadata { get; } = new("passthrough");
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? o = null, CancellationToken c = default)
        {
            LastSeenMessages = messages.ToList();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> m, ChatOptions? o = null, CancellationToken c = default)
            => throw new NotSupportedException();
        public object? GetService(Type t, object? k = null) => null;
        public void Dispose() { }
    }

    private static AIChatReducingChatMiddleware Create(AIChatReductionOptions reduction)
    {
        var options = Options.Create(new AIOptions { Reduction = reduction });
        // Summarizer factory is not needed for MessageCount; pass null/lazy stub (see Task 5).
        return new AIChatReducingChatMiddleware(options, summarizerClientFactory: null, loggerFactory: null);
    }

    [Fact]
    public void Disabled_ReturnsSameClient_NoWrapping()
    {
        var inner = new PassthroughClient();
        var middleware = Create(new AIChatReductionOptions { Strategy = AIChatReductionStrategy.None });

        var result = middleware.Apply(inner);

        result.ShouldBeSameAs(inner);
    }

    [Fact]
    public async Task MessageCount_TrimsToTarget_BeforeProvider()
    {
        var inner = new PassthroughClient();
        var middleware = Create(new AIChatReductionOptions
        {
            Strategy = AIChatReductionStrategy.MessageCount,
            TargetMessageCount = 2,
        });

        var client = middleware.Apply(inner);

        var history = Enumerable.Range(0, 6)
            .Select(i => new ChatMessage(ChatRole.User, $"m{i}")).ToList();

        await client.GetResponseAsync(history);

        // The provider should have seen at most the target number of messages.
        inner.LastSeenMessages!.Count().ShouldBeLessThanOrEqualTo(2);
    }
}
```

> If MEAI's `MessageCountingChatReducer` keeps a system message in addition to the target (so the count can be target+1), relax the assertion to `ShouldBeLessThanOrEqualTo(3)` and document the observed behavior in a comment. This test is also where Task 6's pairing concern first surfaces.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIChatReducingChatMiddlewareTests"`
Expected: FAIL — middleware type does not exist.

- [ ] **Step 3: Implement the middleware (MessageCount path only)**

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Middleware that reduces conversation history before it reaches the provider, using MEAI's
/// <see cref="ReducingChatClient"/>. No-op when <see cref="AIChatReductionOptions.Strategy"/>
/// is <see cref="AIChatReductionStrategy.None"/>.
/// </summary>
public sealed class AIChatReducingChatMiddleware : IAIChatMiddleware
{
    private readonly AIOptions _options;
    private readonly ISummarizerChatClientFactory? _summarizerClientFactory;
    private readonly ILoggerFactory? _loggerFactory;

    /// <param name="options">AI options carrying <see cref="AIOptions.Reduction"/>.</param>
    /// <param name="summarizerClientFactory">Builds the chat client used by the Summarize
    /// strategy. May be null when only MessageCount is used. Added in Task 5.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public AIChatReducingChatMiddleware(
        IOptions<AIOptions> options,
        ISummarizerChatClientFactory? summarizerClientFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        _options = options.Value;
        _summarizerClientFactory = summarizerClientFactory;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        var reduction = _options.Reduction;

        IChatReducer? reducer = reduction.Strategy switch
        {
            AIChatReductionStrategy.MessageCount =>
                new MessageCountingChatReducer(reduction.TargetMessageCount),

            // Summarize is wired in Task 5; until then it degrades to no-op rather than throwing.
            AIChatReductionStrategy.Summarize =>
                BuildSummarizingReducer(reduction),

            _ => null,
        };

        if (reducer is null)
        {
            return client; // None, or Summarize not yet configurable
        }

        return client.AsBuilder()
            .UseChatReducer(reducer)
            .Build();
    }

    // Replaced with a real implementation in Task 5.
    private IChatReducer? BuildSummarizingReducer(AIChatReductionOptions reduction) => null;
}
```

Also create a placeholder factory interface so the constructor compiles (fleshed out in Task 5):

```csharp
// Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/ISummarizerChatClientFactory.cs
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Builds the <see cref="IChatClient"/> used to summarize conversation history for the
/// Summarize reduction strategy. Resolves the configured summarization profile lazily.
/// </summary>
public interface ISummarizerChatClientFactory
{
    /// <summary>Builds (or returns a cached) summarizer client, or null if none can be resolved.</summary>
    IChatClient? GetSummarizerClient();
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIChatReducingChatMiddlewareTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AIChatReducingChatMiddleware.cs Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/ISummarizerChatClientFactory.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Chat/Middleware/AIChatReducingChatMiddlewareTests.cs
git commit -m "feat(core): add message-count chat history reduction middleware"
```

---

### Task 3: Register the middleware in the pipeline (inside function invocation)

**Files:**
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs:106-116`
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Integration/Chat/ChatMiddlewareOrderingTests.cs` (confirm the integration test project path first)

- [ ] **Step 1: Write the failing ordering test**

```csharp
// Resolve AIChatMiddlewareCollection from the configured DI container and assert:
//  - AIChatReducingChatMiddleware is present
//  - it appears BEFORE AIFunctionInvokingChatMiddleware in collection order (i.e. more inner)
[Fact]
public void ReducingMiddleware_IsRegistered_BeforeFunctionInvoking()
{
    var collection = ResolveChatMiddlewareCollection(); // via the integration DI fixture
    var types = collection.Select(m => m.GetType()).ToList();

    types.ShouldContain(typeof(AIChatReducingChatMiddleware));
    types.IndexOf(typeof(AIChatReducingChatMiddleware))
        .ShouldBeLessThan(types.IndexOf(typeof(AIFunctionInvokingChatMiddleware)));
}
```

> Use the existing integration DI fixture that already builds the Umbraco.AI service provider (the current middleware/DI integration tests show the pattern). If there is no such fixture, assert ordering by replicating the `AIChatMiddleware()` builder calls in a unit test instead.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~ChatMiddlewareOrderingTests"`
Expected: FAIL — middleware not registered.

- [ ] **Step 3: Register in `AddUmbracoAI` (or wherever `AIChatMiddleware()` is configured)**

In `UmbracoBuilderExtensions.cs`, change the chat middleware block (lines 106-116) to insert the reducer just before function invocation:

```csharp
builder.AIChatMiddleware()
    .Append<AIOpenTelemetryChatMiddleware>()          // OpenTelemetry tracing + metrics (innermost)
    .Append<AIFileProcessingChatMiddleware>()         // File processing
    .Append<AIChatOptionsOverrideChatMiddleware>()    // ChatOptions override from runtime context
    .Append<AIRuntimeContextInjectingChatMiddleware>()// Multimodal injection
    .Append<AIChatReducingChatMiddleware>()           // History reduction (inside function invocation)
    .Append<AIFunctionInvokingChatMiddleware>()       // Function/tool invocation
    .Append<AIGuardrailChatMiddleware>()
    .Append<AITrackingChatMiddleware>()
    .Append<AIUsageRecordingChatMiddleware>()
    .Append<AIAuditingChatMiddleware>()
    .Append<AIContextInjectingChatMiddleware>();      // Context injection (outermost)
```

> Appending `AIChatReducingChatMiddleware` immediately before `AIFunctionInvokingChatMiddleware` makes it more inner than function invocation, so reduction applies on each provider round-trip. (Equivalent to `InsertBefore<AIFunctionInvokingChatMiddleware, AIChatReducingChatMiddleware>()` if you prefer to keep the original Append list and insert separately.)

Ensure DI can construct the middleware: `IOptions<AIOptions>` is already registered (confirm via the existing `Configure<AIOptions>` / options binding near the top of `AddUmbracoAI`). Register `ISummarizerChatClientFactory` in Task 5; until then the optional constructor param resolves to null (register the middleware itself as transient/singleton consistent with the others — check how the collection instantiates middleware; `OrderedCollectionBuilderBase` resolves types from DI, so add `services.AddTransient<AIChatReducingChatMiddleware>()` if the other middleware are explicitly registered, otherwise rely on the collection's activator).

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~ChatMiddlewareOrderingTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs Umbraco.AI/tests/Umbraco.AI.Tests.Integration/Chat/ChatMiddlewareOrderingTests.cs
git commit -m "feat(core): register chat history reduction middleware in pipeline"
```

---

### Task 4: Tool-call/result pairing safety test (correctness gate)

**Why:** dropping or summarizing old messages can orphan a `FunctionCallContent` from its matching `FunctionResultContent`. Anthropic/OpenAI reject a conversation whose `tool_use` lacks a matching `tool_result` (and vice versa). This task proves whether MEAI's `MessageCountingChatReducer` preserves pairing; if not, we add a guarding reducer.

**Files:**
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Chat/Middleware/ChatReductionPairingTests.cs`
- (Conditional) Create: `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/PairingSafeChatReducer.cs`

- [ ] **Step 1: Write the characterization test**

```csharp
using Microsoft.Extensions.AI;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Chat.Middleware;

public class ChatReductionPairingTests
{
    [Fact]
    public async Task MessageCounting_DoesNotOrphanToolResults()
    {
        // History: user, assistant(tool call c1), tool(result c1), assistant text, user, ...
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "system"),
            new(ChatRole.User, "do a thing"),
            new(ChatRole.Assistant, [new FunctionCallContent("c1", "do_thing", null)]),
            new(ChatRole.Tool, [new FunctionResultContent("c1", "done")]),
            new(ChatRole.Assistant, "thing done"),
            new(ChatRole.User, "another"),
            new(ChatRole.Assistant, "ok"),
        };

        var reducer = new MessageCountingChatReducer(targetCount: 3);
        var reduced = (await reducer.ReduceAsync(messages, CancellationToken.None)).ToList();

        // Assert no FunctionResultContent survives without its matching FunctionCallContent.
        var callIds = reduced.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
            .Select(c => c.CallId).ToHashSet();
        var orphanResults = reduced.SelectMany(m => m.Contents).OfType<FunctionResultContent>()
            .Where(r => !callIds.Contains(r.CallId)).ToList();

        orphanResults.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run the characterization test**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~ChatReductionPairingTests"`
- **If PASS:** MEAI's reducer preserves pairing — record this in the test comment and skip Step 3/4.
- **If FAIL:** MEAI's plain message counting orphans results — proceed to Step 3 to add a pairing-safe wrapper.

- [ ] **Step 3 (conditional): Implement `PairingSafeChatReducer`**

```csharp
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Wraps an inner <see cref="IChatReducer"/> and repairs tool-call/result pairing after reduction:
/// drops any orphaned <see cref="FunctionResultContent"/> whose <see cref="FunctionCallContent"/>
/// was reduced away, so providers don't reject the conversation.
/// </summary>
internal sealed class PairingSafeChatReducer : IChatReducer
{
    private readonly IChatReducer _inner;

    public PairingSafeChatReducer(IChatReducer inner) => _inner = inner;

    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var reduced = (await _inner.ReduceAsync(messages, cancellationToken)).ToList();

        var callIds = reduced.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
            .Select(c => c.CallId).ToHashSet();

        foreach (var message in reduced)
        {
            var orphans = message.Contents.OfType<FunctionResultContent>()
                .Where(r => !callIds.Contains(r.CallId)).ToList();
            foreach (var orphan in orphans)
            {
                message.Contents.Remove(orphan);
            }
        }

        // Drop messages left empty after orphan removal.
        return reduced.Where(m => m.Contents.Count > 0).ToList();
    }
}
```

Then in `AIChatReducingChatMiddleware.Apply`, wrap the chosen reducer: `reducer = new PairingSafeChatReducer(reducer);` before `UseChatReducer`. Add a unit test asserting the wrapper removes the orphan from the failing case.

- [ ] **Step 4 (conditional): Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/PairingSafeChatReducer.cs Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AIChatReducingChatMiddleware.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Chat/Middleware/ChatReductionPairingTests.cs
git commit -m "fix(core): preserve tool-call pairing through history reduction"
```

If Step 2 passed, still commit the characterization test:

```bash
git add Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Chat/Middleware/ChatReductionPairingTests.cs
git commit -m "test(core): characterize tool-call pairing under history reduction"
```

---

### Task 5: `Summarize` strategy with lazy summarizer client

**Files:**
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/SummarizerChatClientFactory.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AIChatReducingChatMiddleware.cs` (`BuildSummarizingReducer`)
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` (register the factory)
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Chat/Middleware/SummarizeReductionTests.cs`

**Design:** `Apply` is synchronous but building a profile's client is async (`CreateClientAsync`). Resolve the summarizer client **lazily** inside `ISummarizerChatClientFactory.GetSummarizerClient()` so the async profile lookup happens off the hot path, cached after first creation. The factory resolves the profile in priority order: `Reduction.SummarizationProfileAlias` → `AIOptions.ClassifierChatProfileAlias` → default chat profile.

> **Summarization cost & caching (design decision — settle before coding this task).** Per "Effect on Copilot" above, the stateless replay model means `Summarize` recomputes per turn (and per FICC round-trip). Options, cheapest-to-build first:
> 1. **No cache + high threshold (recommended first ship).** Accept per-call summarization but set `SummarizationThresholdMessageCount` high enough that most conversations never trigger it, and **add usage telemetry on summarizer calls** (ride the existing `AIUsageRecording*` path). Ship this, measure, and only build a cache if the data shows it's a material cost. Honors YAGNI.
> 2. **Server-side block cache.** Wrap the reducer in a `CachingSummarizingChatReducer` backed by `IMemoryCache`, keyed by a **stable content hash of each summarized *block*** of older messages (not the whole prefix — the whole prefix's hash changes every turn, killing hit rate). Older blocks never change, so their summaries cache cleanly; the final summary composes cached block summaries + recent verbatim. Keeps display untouched, no protocol change. Moderate effort.
> 3. **Client-carried rolling summary.** Server returns the summary (+ a marker of which messages it covers) to the client; the client replays `[summary + messages-since]` next turn, so the server only summarizes *new* messages. Most efficient, but requires an AG-UI protocol addition and careful invariant handling (the summary is carried, not displayed). Biggest lift — defer unless cost demands it.
>
> **Recommendation:** implement Option 1 now (telemetry + threshold), and record Options 2/3 as follow-ups. Do **not** silently ship per-call summarization without the telemetry — that's how a cost surprise hides. If the team prefers, Option 2 is the natural next step.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void Summarize_WithResolvableProfile_WrapsWithReducingClient()
{
    // Arrange: options Strategy=Summarize, TargetMessageCount=2; a stub
    // ISummarizerChatClientFactory.GetSummarizerClient() returns a fake IChatClient.
    var middleware = CreateWithSummarizer(
        new AIChatReductionOptions { Strategy = AIChatReductionStrategy.Summarize, TargetMessageCount = 2 },
        summarizer: new FakeChatClient());

    var result = middleware.Apply(new PassthroughClient());

    // The returned client must differ from the inner (it is now a ReducingChatClient).
    result.ShouldNotBeNull();
    result.GetService(typeof(ReducingChatClient)).ShouldNotBeNull();
}

[Fact]
public void Summarize_WithNoResolvableProfile_DegradesToNoOp()
{
    var middleware = CreateWithSummarizer(
        new AIChatReductionOptions { Strategy = AIChatReductionStrategy.Summarize },
        summarizer: null); // factory yields null

    var inner = new PassthroughClient();
    middleware.Apply(inner).ShouldBeSameAs(inner);
}
```

> `ReducingChatClient` exposes itself via `GetService(typeof(ReducingChatClient))` per MEAI's delegating-client `GetService` convention; if that returns null in practice, assert `result.ShouldNotBeSameAs(inner)` instead.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~SummarizeReductionTests"`
Expected: FAIL — `BuildSummarizingReducer` returns null (Task 2 stub).

- [ ] **Step 3: Implement the factory and `BuildSummarizingReducer`**

```csharp
// SummarizerChatClientFactory.cs
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Core.Chat.Middleware;

internal sealed class SummarizerChatClientFactory : ISummarizerChatClientFactory
{
    private readonly IAIProfileService _profileService;
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly AIOptions _options;
    private readonly object _gate = new();
    private IChatClient? _cached;
    private bool _resolved;

    public SummarizerChatClientFactory(
        IAIProfileService profileService,
        IAIChatClientFactory chatClientFactory,
        IOptions<AIOptions> options)
    {
        _profileService = profileService;
        _chatClientFactory = chatClientFactory;
        _options = options.Value;
    }

    public IChatClient? GetSummarizerClient()
    {
        if (_resolved)
        {
            return _cached;
        }

        lock (_gate)
        {
            if (_resolved)
            {
                return _cached;
            }

            _cached = BuildAsync().GetAwaiter().GetResult();
            _resolved = true;
            return _cached;
        }
    }

    private async Task<IChatClient?> BuildAsync()
    {
        var alias = _options.Reduction.SummarizationProfileAlias
            ?? _options.ClassifierChatProfileAlias;

        AIProfile? profile = alias is not null
            ? await _profileService.GetProfileByAliasAsync(alias)
            : await _profileService.GetDefaultProfileAsync(Models.AICapability.Chat);

        return profile is null ? null : await _chatClientFactory.CreateClientAsync(profile);
    }
}
```

> Confirm `IAIProfileService.GetDefaultProfileAsync(AICapability)` and `GetProfileByAliasAsync(string)` signatures against the current interface; adjust the calls to match. The blocking `GetAwaiter().GetResult()` is acceptable here because it runs once and is cached; if the codebase forbids sync-over-async, instead resolve the client during DI startup via a hosted initializer and inject the resolved client.

In `AIChatReducingChatMiddleware`, replace the Task 2 stub:

```csharp
private IChatReducer? BuildSummarizingReducer(AIChatReductionOptions reduction)
{
    var summarizer = _summarizerClientFactory?.GetSummarizerClient();
    if (summarizer is null)
    {
        // No profile resolvable — degrade to no reduction rather than crash the pipeline.
        return null;
    }

    return new SummarizingChatReducer(
        summarizer,
        reduction.TargetMessageCount,
        reduction.SummarizationThresholdMessageCount);
}
```

Register the factory in `UmbracoBuilderExtensions` near the other chat services:

```csharp
services.AddSingleton<ISummarizerChatClientFactory, SummarizerChatClientFactory>();
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~SummarizeReductionTests"`
Expected: PASS. Also re-run Task 4's pairing test under the Summarize path (add a `[Theory]` case or a sibling test) — summarization that preserves recent messages should keep pairing, but verify.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/SummarizerChatClientFactory.cs Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AIChatReducingChatMiddleware.cs Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Chat/Middleware/SummarizeReductionTests.cs
git commit -m "feat(core): add summarizing chat history reduction strategy"
```

---

### Task 6: Agent integration test (end-to-end bounding)

**Files:**
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Integration/Chat/ChatHistoryReductionFlowTests.cs`

- [ ] **Step 1: Write the failing integration test**

```csharp
// With Reduction { Strategy = MessageCount, TargetMessageCount = 4 } configured in the test
// service provider, and a scripted IChatClient that records how many messages it received:
//  - send a 20-message conversation through IAIChatService (or the agent run path)
//  - assert the scripted provider observed <= 4 (+system) messages
[Fact]
public async Task LongConversation_IsBoundedBeforeProvider() { /* ... */ }
```

> Reuse the integration project's DI fixture and the existing `FakeChatClient`/`FakeChatCapability` (documented in `Umbraco.AI/CLAUDE.md` test utilities) so a real configured pipeline is exercised, not just the middleware in isolation.

- [ ] **Step 2: Run, iterate to green**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~ChatHistoryReductionFlowTests"`
Expected: PASS once configuration flows through the registered middleware.

- [ ] **Step 3: Commit**

```bash
git add Umbraco.AI/tests/Umbraco.AI.Tests.Integration/Chat/ChatHistoryReductionFlowTests.cs
git commit -m "test(core): end-to-end chat history reduction bounding"
```

---

### Task 7: Document the config

**Files:**
- Modify: `Umbraco.AI/CLAUDE.md` (Configuration section) and/or `Umbraco.AI/docs/public/` config reference.

- [ ] **Step 1: Add the config block to docs**

```json
{
  "Umbraco": {
    "AI": {
      "Reduction": {
        "Strategy": "Summarize",            // None | MessageCount | Summarize
        "TargetMessageCount": 40,
        "SummarizationThresholdMessageCount": 60,
        "SummarizationProfileAlias": "summarizer"
      }
    }
  }
}
```

Document that `None` is the default (no behavior change), `MessageCount` is deterministic/free, and `Summarize` requires a resolvable chat profile and incurs summarization LLM cost.

- [ ] **Step 2: Commit**

```bash
git add Umbraco.AI/CLAUDE.md
git commit -m "docs(core): document chat history reduction configuration"
```

---

## Cross-cutting risks & notes

1. **Tool-call pairing (highest risk)** — Task 4 is the gate. If MEAI's reducers orphan tool results, `PairingSafeChatReducer` is mandatory and must also run under the Summarize path. Do not ship reduction (even MessageCount) without Task 4 green.
2. **Reduction inside the FICC loop + stateless per-call recompute** — chosen ordering means the reducer runs on every model round-trip within a turn, and the stateless replay model (see "Effect on Copilot") means `Summarize` also recomputes every *turn*. Together: a long, tool-heavy Copilot session could invoke the summarizer many times. Mitigations, in order: rely on `SummarizationThresholdMessageCount` so it only fires when over budget; add summarizer-call telemetry (Task 5 Option 1); then cache (Task 5 Option 2) or move the summary client-side (Option 3) if telemetry warrants. An alternative ordering (`InsertAfter<AIFunctionInvokingChatMiddleware>`, more outer → reduce once per user turn, not per round-trip) is a one-line change that cuts within-turn recompute but not cross-turn; document whichever is chosen. `MessageCount` is exempt — it's pure slicing with no LLM call.
3. **System message preservation** — reduction must not drop the system/instructions message. Verify MEAI's reducers keep it (the Task 4 fixture includes a system message; assert it survives). If not, the `PairingSafeChatReducer` (or a sibling) must also pin the system message.
4. **Inline chat vs agents** — this middleware is on the shared chat pipeline, so it benefits both `IAIChatService` inline chat and agent runs. That's intended. Confirm no profile-specific override is needed; if a profile wants to opt out, that's a future enhancement (per-profile reduction override), not in scope here.
5. **Sync-over-async in the summarizer factory** — the one-time cached `GetAwaiter().GetResult()` is the pragmatic choice; if house rules forbid it, resolve the summarizer client via a startup initializer instead (noted in Task 5 Step 3).

## Self-Review

- **Spec coverage:** config (Task 1), MessageCount middleware + no-op-when-disabled (Task 2), registration/ordering (Task 3), pairing correctness gate (Task 4), Summarize strategy + caching decision (Task 5), end-to-end bounding (Task 6), docs incl. perception-gap (Task 7). Display-vs-model-context invariant + no-`MESSAGES_SNAPSHOT`-from-reduction obligation covered in "Effect on Copilot".
- **Type/name consistency:** `AIChatReductionStrategy {None,MessageCount,Summarize}`, `AIChatReductionOptions.{Strategy,TargetMessageCount,SummarizationThresholdMessageCount,SummarizationProfileAlias}`, `AIChatReducingChatMiddleware`, `ISummarizerChatClientFactory.GetSummarizerClient()` are used identically across Tasks 1-6. MEAI ctors match the verified signatures: `MessageCountingChatReducer(int)`, `SummarizingChatReducer(IChatClient,int,int?)`, `ReducingChatClient(IChatClient,IChatReducer)`, `.UseChatReducer(reducer, configure)`.
- **Placeholder scan:** the two deferred branches (pairing wrapper in Task 4, summarizer wiring in Task 5) are explicitly staged with a Task-2 stub that degrades to no-op rather than throwing, so every intermediate commit builds and runs. Test-harness helpers point to named existing fixtures/fakes (`FakeChatClient`, integration DI fixture) rather than invented APIs.
