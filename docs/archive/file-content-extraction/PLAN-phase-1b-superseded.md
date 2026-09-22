# File Content Extraction — Phase 1b Implementation Plan

> **Status: Superseded.** This plan was fully executed (all 4 tasks, final review passed), then
> replaced after the fact by a much smaller sync-only implementation: rather than threading an
> async contributor path through every runtime-context call site, `MediaEntityAdapter.FormatForLlm`
> blocks on the three genuinely-async calls directly (safe in this host — no capturing
> `SynchronizationContext` to deadlock on). That cut the change from ~1,200 lines across 29 files
> to ~150 lines across 3 files, and made the `ScopedAIAgent` fix in Task 4 below unnecessary — the
> sync call chain already reached it. See the updated "Phase 1b" section of
> `docs/superpowers/specs/2026-09-03-file-content-extraction-design.md` for the current design.
> The task-by-task plan below is kept as the historical record of what was built and reviewed
> before that change; none of the async infrastructure it describes (`FormatForLlmAsync`,
> `ContributeAsync`, `PopulateAsync`) exists in the codebase anymore.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When a Media item (e.g. a `.csv`) is the "currently open entity" in a Copilot
conversation, the AI should see the file's actual content — not just its name and byte count.

**Architecture:** The bug lives in `MediaEntityAdapter.FormatForLlm`, the only caller of which is
`SerializedEntityContributor` (confirmed: the only call site of `IAIEntityAdapter.FormatForLlm` in
the whole repo). The fix needs to resolve the underlying file and run it through Phase 1a's
file-processing pipeline (`IAIUmbracoMediaResolver` + `IAIFileProcessingHandler`), both of which
are async — but `FormatForLlm` and its caller chain
(`MediaEntityAdapter` → `AIEntityContextHelper` → `SerializedEntityContributor` →
`AIRuntimeContextContributorCollection`) are all synchronous today, and all four are public
extension points. This plan adds an async counterpart to each (`FormatForLlmAsync` /
`ContributeAsync` / `PopulateAsync`) via C# default interface methods that wrap the existing sync
method — every existing/third-party implementation keeps working unchanged — then migrates the
18 internal call sites of the old sync `Populate` to the new async path, and finally gives
`MediaEntityAdapter` a real async override that does the extraction. Tasks are ordered so nothing
user-visible changes until the last task; each earlier task is a safe, independently-verifiable
step.

**Tech Stack:** .NET 10, xUnit, Shouldly, Moq (existing test stack — no new packages).

**Spec:** `docs/superpowers/specs/2026-09-03-file-content-extraction-design.md` (Phase 1b
section — read this first; it documents the full investigation that found the exact bug location
and the reasoning for the additive-interface approach used here).

## Global Constraints

- No new NuGet dependencies.
- **No breaking changes.** Every existing sync method (`Contribute`, `FormatForLlm`, `Populate`)
  keeps its exact current signature and behavior. New async methods are added *alongside* them,
  not instead of them, using C# default interface methods so no existing implementer (built-in or
  third-party) needs to change.
- Reuse Phase 1a's file-processing pipeline as-is (`IAIUmbracoMediaResolver`,
  `IAIFileProcessingHandler`, `AIFileProcessingHandlerCollection`) — no new extraction logic.
- Match this repo's existing error-handling convention: `IAIUmbracoMediaResolver.ResolveAsync`
  already catches its own exceptions and returns `null` instead of throwing (verified in Phase
  1a) — do not add a redundant try/catch around it. `IAIFileProcessingHandler.ProcessAsync` calls
  are NOT wrapped in try/catch anywhere in Phase 1a's code either (e.g.
  `AIFileProcessingChatClient`); match that same level of trust here — don't add speculative
  error handling the rest of the codebase doesn't have.
- Feature-sliced structure: no new folders. Changes land inside the existing `RuntimeContext/`,
  `EntityAdapter/`, and `Media/`-adjacent files already touched by Phase 1a.

---

## File Structure

| File | Responsibility |
|---|---|
| `Umbraco.AI.Core/RuntimeContext/IAIRuntimeContextContributor.cs` (modify) | Add `ContributeAsync` default method |
| `Umbraco.AI.Core/RuntimeContext/AIRuntimeContextContributorCollection.cs` (modify) | Add `PopulateAsync` method |
| 12 files under `Chat/`, `Embeddings/`, `ImageGeneration/`, `InlineChat/`, `SpeechToText/` (modify) | 18 call sites switch from `Populate` to `PopulateAsync` |
| `Umbraco.AI.Core/EntityAdapter/IAIEntityAdapter.cs` (modify) | Add `FormatForLlmAsync` default method |
| `Umbraco.AI.Core/EntityAdapter/IAIEntityContextHelper.cs` (modify) | Add `FormatForLlmAsync` default method |
| `Umbraco.AI.Core/EntityAdapter/AIEntityContextHelper.cs` (modify) | Override `FormatForLlmAsync` to dispatch to the adapter's async method |
| `Umbraco.AI.Core/RuntimeContext/Contributors/SerializedEntityContributor.cs` (modify) | Extract shared `PrepareEntity` helper; add `ContributeAsync` override calling `FormatForLlmAsync` |
| `Umbraco.AI.Core/EntityAdapter/Adapters/MediaEntityAdapter.cs` (modify) | Add `IAIUmbracoMediaResolver`/`AIFileProcessingHandlerCollection` dependencies; override `FormatForLlmAsync` to append extracted file content |

Task order: 1 → 2 → 3 → 4, strictly sequential — each task's async method is inert (falls back to
existing sync behavior) until the next task calls it, so nothing observable changes until Task 4.

---

### Task 1: Async contributor pipeline

**Files:**
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/IAIRuntimeContextContributor.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/AIRuntimeContextContributorCollection.cs`
- Modify (18 call sites across 12 files, listed below)
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/RuntimeContext/AIRuntimeContextContributorCollectionTests.cs` (new)

**Interfaces:**
- Produces: `IAIRuntimeContextContributor.ContributeAsync(AIRuntimeContext, CancellationToken = default)` (default method, calls `Contribute`) and `AIRuntimeContextContributorCollection.PopulateAsync(AIRuntimeContext, CancellationToken = default)` — consumed by Task 3.

This task changes nothing observable: `PopulateAsync` just awaits each contributor's
`ContributeAsync`, which for every contributor today (none override it yet) is the default
wrapper calling the unchanged `Contribute`. The 18 call-site migration is mechanical — every one
of the 18 sites is already inside an `async Task` or `async IAsyncEnumerable` method with a
`CancellationToken` already in scope (verified during planning).

- [ ] **Step 1: Write the failing test**

Create `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/RuntimeContext/AIRuntimeContextContributorCollectionTests.cs`:

```csharp
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Tests.Unit.RuntimeContext;

public class AIRuntimeContextContributorCollectionTests
{
    [Fact]
    public async Task PopulateAsync_WithSyncOnlyContributor_InvokesContributeViaDefault()
    {
        // Arrange — a contributor that only implements the original sync Contribute,
        // exactly like every contributor in this codebase today. PopulateAsync must
        // still invoke it via the interface's default ContributeAsync wrapper.
        var contributor = new SyncOnlyContributor();
        var collection = new AIRuntimeContextContributorCollection(() => [contributor]);
        var context = new AIRuntimeContext([]);

        // Act
        await collection.PopulateAsync(context);

        // Assert
        contributor.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task PopulateAsync_WithMultipleContributors_InvokesAllInOrder()
    {
        // Arrange
        var callOrder = new List<int>();
        var first = new SyncOnlyContributor(() => callOrder.Add(1));
        var second = new SyncOnlyContributor(() => callOrder.Add(2));
        var collection = new AIRuntimeContextContributorCollection(() => [first, second]);
        var context = new AIRuntimeContext([]);

        // Act
        await collection.PopulateAsync(context);

        // Assert
        callOrder.ShouldBe([1, 2]);
    }

    [Fact]
    public void Populate_StillWorksUnchanged()
    {
        // Arrange — the original sync method must keep working exactly as before.
        var contributor = new SyncOnlyContributor();
        var collection = new AIRuntimeContextContributorCollection(() => [contributor]);
        var context = new AIRuntimeContext([]);

        // Act
        collection.Populate(context);

        // Assert
        contributor.WasCalled.ShouldBeTrue();
    }

    private sealed class SyncOnlyContributor : IAIRuntimeContextContributor
    {
        private readonly Action? _onContribute;

        public SyncOnlyContributor(Action? onContribute = null)
        {
            _onContribute = onContribute;
        }

        public bool WasCalled { get; private set; }

        public void Contribute(AIRuntimeContext context)
        {
            WasCalled = true;
            _onContribute?.Invoke();
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIRuntimeContextContributorCollectionTests"`
Expected: FAIL to compile — `PopulateAsync` does not exist yet.

- [ ] **Step 3: Add `ContributeAsync` to the interface**

In `Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/IAIRuntimeContextContributor.cs`, add a default
method alongside the existing one (do not remove or change `Contribute`):

```csharp
namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Contributes to the runtime context.
/// </summary>
public interface IAIRuntimeContextContributor
{
    /// <summary>
    /// Contributes to the runtime context.
    /// </summary>
    /// <param name="context">The runtime context to contribute to.</param>
    void Contribute(AIRuntimeContext context);

    /// <summary>
    /// Contributes to the runtime context asynchronously. The default implementation calls
    /// <see cref="Contribute"/>, so existing implementers work unchanged. Override this when a
    /// contributor needs to do async work (e.g. resolving a file) before it can format its
    /// content.
    /// </summary>
    /// <param name="context">The runtime context to contribute to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ContributeAsync(AIRuntimeContext context, CancellationToken cancellationToken = default)
    {
        Contribute(context);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Add `PopulateAsync` to the collection**

In `Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/AIRuntimeContextContributorCollection.cs`, add a
new method alongside `Populate` (do not remove or change `Populate`):

```csharp
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Collection of runtime context contributors. Loops items and dispatches to handlers.
/// </summary>
public sealed class AIRuntimeContextContributorCollection : BuilderCollectionBase<IAIRuntimeContextContributor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIRuntimeContextContributorCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the contributor instances.</param>
    public AIRuntimeContextContributorCollection(Func<IEnumerable<IAIRuntimeContextContributor>> items)
        : base(items)
    { }

    /// <summary>
    /// Populates a runtime context by invoking all registered contributors in order.
    /// </summary>
    /// <param name="context">The runtime context to populate.</param>
    public void Populate(AIRuntimeContext context)
    {
        foreach (var contributor in this)
        {
            contributor.Contribute(context);
        }
    }

    /// <summary>
    /// Populates a runtime context by invoking all registered contributors in order, awaiting
    /// each one's <see cref="IAIRuntimeContextContributor.ContributeAsync"/>.
    /// </summary>
    /// <param name="context">The runtime context to populate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task PopulateAsync(AIRuntimeContext context, CancellationToken cancellationToken = default)
    {
        foreach (var contributor in this)
        {
            await contributor.ContributeAsync(context, cancellationToken);
        }
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIRuntimeContextContributorCollectionTests"`
Expected: All 3 tests PASS.

- [ ] **Step 6: Migrate the 18 call sites**

In each of the following 12 files, change every occurrence of
`_contributors.Populate(createdScope.Context);` to
`await _contributors.PopulateAsync(createdScope.Context, cancellationToken);`. Every occurrence
is inside a method that is already `async Task<...>` or `async IAsyncEnumerable<...>` with a
`CancellationToken cancellationToken` parameter already in scope — confirm this at each site
before editing; if any site's containing method is not already async with a cancellation token in
scope, STOP and report BLOCKED rather than guessing (this was verified true for all 18 during
planning, but re-verify live since line numbers may have drifted).

| File | Line(s) (at planning time — re-verify live) |
|---|---|
| `Umbraco.AI/src/Umbraco.AI.Core/Chat/AIChatService.cs` | 228, 261 |
| `Umbraco.AI/src/Umbraco.AI.Core/Chat/ScopedProfileChatClient.cs` | 76, 104 |
| `Umbraco.AI/src/Umbraco.AI.Core/Embeddings/AIEmbeddingService.cs` | 180 |
| `Umbraco.AI/src/Umbraco.AI.Core/Embeddings/ScopedInlineEmbeddingGenerator.cs` | 45 |
| `Umbraco.AI/src/Umbraco.AI.Core/Embeddings/ScopedProfileEmbeddingGenerator.cs` | 64 |
| `Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/AIImageGenerationService.cs` | 142, 243 |
| `Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/ScopedInlineImageGenerator.cs` | 60 |
| `Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/ScopedProfileImageGenerator.cs` | 60 |
| `Umbraco.AI/src/Umbraco.AI.Core/InlineChat/ScopedInlineChatClient.cs` | 66, 92 |
| `Umbraco.AI/src/Umbraco.AI.Core/SpeechToText/AISpeechToTextService.cs` | 216, 247 |
| `Umbraco.AI/src/Umbraco.AI.Core/SpeechToText/ScopedInlineSpeechToTextClient.cs` | 69, 95 |
| `Umbraco.AI/src/Umbraco.AI.Core/SpeechToText/ScopedProfileSpeechToTextClient.cs` | 63, 89 |

Example of the exact change (this is the real code at `AIChatService.cs:225-229`, shown so you
know exactly what surrounding code to expect — every other site follows the same shape, just with
different surrounding variable/method names):

```csharp
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }
```

becomes:

```csharp
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(builder.ContextItems ?? []);
                await _contributors.PopulateAsync(createdScope.Context, cancellationToken);
            }
```

- [ ] **Step 7: Run the full unit test suite to confirm no regression**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~Umbraco.AI.Tests.Unit"`
Expected: All tests PASS (this exercises all 18 call sites indirectly via
`AIChatServiceTests`, `AIEmbeddingServiceTests`, `AISpeechToTextServiceTests`,
`AIImageGenerationServiceTests`, and others — a mistake at any of the 18 sites should surface
here).

- [ ] **Step 8: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/IAIRuntimeContextContributor.cs Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/AIRuntimeContextContributorCollection.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/RuntimeContext/AIRuntimeContextContributorCollectionTests.cs Umbraco.AI/src/Umbraco.AI.Core/Chat/AIChatService.cs Umbraco.AI/src/Umbraco.AI.Core/Chat/ScopedProfileChatClient.cs Umbraco.AI/src/Umbraco.AI.Core/Embeddings/AIEmbeddingService.cs Umbraco.AI/src/Umbraco.AI.Core/Embeddings/ScopedInlineEmbeddingGenerator.cs Umbraco.AI/src/Umbraco.AI.Core/Embeddings/ScopedProfileEmbeddingGenerator.cs Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/AIImageGenerationService.cs Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/ScopedInlineImageGenerator.cs Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/ScopedProfileImageGenerator.cs Umbraco.AI/src/Umbraco.AI.Core/InlineChat/ScopedInlineChatClient.cs Umbraco.AI/src/Umbraco.AI.Core/SpeechToText/AISpeechToTextService.cs Umbraco.AI/src/Umbraco.AI.Core/SpeechToText/ScopedInlineSpeechToTextClient.cs Umbraco.AI/src/Umbraco.AI.Core/SpeechToText/ScopedProfileSpeechToTextClient.cs
git commit -m "refactor(core): Add async runtime-context contributor pipeline alongside the sync one"
```

---

### Task 2: Async entity-formatting dispatch

**Files:**
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/IAIEntityAdapter.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/IAIEntityContextHelper.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIEntityContextHelper.cs`
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/EntityAdapter/AIEntityContextHelperTests.cs` (extend existing file)

**Interfaces:**
- Produces: `IAIEntityAdapter.FormatForLlmAsync(AISerializedEntity, CancellationToken = default)` (default method), `IAIEntityContextHelper.FormatForLlmAsync(AISerializedEntity, CancellationToken = default)`, `AIEntityContextHelper.FormatForLlmAsync` (real dispatch to the adapter) — consumed by Task 3.

Like Task 1, this changes nothing observable yet: no adapter overrides `FormatForLlmAsync` until
Task 4, so it always falls back to the existing sync `FormatForLlm`.

- [ ] **Step 1: Write the failing test**

In `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/EntityAdapter/AIEntityContextHelperTests.cs`, add these
two tests (the file already has a `_helper` fixture backed by `defaultAdapterMock` — reuse it,
don't create a new fixture):

```csharp
    [Fact]
    public async Task FormatForLlmAsync_GetsAdapterForEntityType()
    {
        // Arrange
        var data = JsonDocument.Parse("{}").RootElement;
        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Test",
            Data = data
        };

        // Act
        var result = await _helper.FormatForLlmAsync(entity);

        // Assert — the default adapter mock only stubs the sync FormatForLlm; the helper's
        // async path must still reach it via the adapter's own default ContributeAsync wrapper.
        result.ShouldBe("Mocked formatted output");
    }

    [Fact]
    public async Task FormatForLlmAsync_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _helper.FormatForLlmAsync(null!));
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIEntityContextHelperTests"`
Expected: FAIL to compile — `FormatForLlmAsync` does not exist yet on `IAIEntityContextHelper`.

- [ ] **Step 3: Add `FormatForLlmAsync` to `IAIEntityAdapter`**

In `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/IAIEntityAdapter.cs`, add a default method after
the existing `FormatForLlm` member (do not remove or change `FormatForLlm`):

```csharp
    /// <summary>
    /// Formats a serialized entity as a system message for LLM context.
    /// </summary>
    /// <param name="entity">The serialized entity to format.</param>
    /// <returns>Formatted markdown string suitable for LLM consumption.</returns>
    string FormatForLlm(AISerializedEntity entity);

    /// <summary>
    /// Formats a serialized entity as a system message for LLM context, asynchronously. The
    /// default implementation calls <see cref="FormatForLlm"/>, so existing adapters work
    /// unchanged. Override this when formatting needs async work (e.g. resolving a media file).
    /// </summary>
    /// <param name="entity">The serialized entity to format.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Formatted markdown string suitable for LLM consumption.</returns>
    Task<string> FormatForLlmAsync(AISerializedEntity entity, CancellationToken cancellationToken = default)
        => Task.FromResult(FormatForLlm(entity));
```

- [ ] **Step 4: Add `FormatForLlmAsync` to `IAIEntityContextHelper`**

In `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/IAIEntityContextHelper.cs`, add a default method
after the existing `FormatForLlm` member (do not remove or change `FormatForLlm`):

```csharp
    /// <summary>
    /// Formats a serialized entity as a system message for LLM context.
    /// </summary>
    /// <param name="entity">The serialized entity.</param>
    /// <returns>A formatted string describing the entity context.</returns>
    string FormatForLlm(AISerializedEntity entity);

    /// <summary>
    /// Formats a serialized entity as a system message for LLM context, asynchronously. The
    /// default implementation calls <see cref="FormatForLlm"/>.
    /// </summary>
    /// <param name="entity">The serialized entity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A formatted string describing the entity context.</returns>
    Task<string> FormatForLlmAsync(AISerializedEntity entity, CancellationToken cancellationToken = default)
        => Task.FromResult(FormatForLlm(entity));
```

- [ ] **Step 5: Override `FormatForLlmAsync` in `AIEntityContextHelper`**

In `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIEntityContextHelper.cs`, add a new method right
after the existing `FormatForLlm` implementation (do not remove or change `FormatForLlm`):

```csharp
    /// <inheritdoc />
    public string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Get the appropriate adapter for this entity type
        var adapter = _adapters.GetAdapter(entity.EntityType);

        return adapter.FormatForLlm(entity);
    }

    /// <inheritdoc />
    public async Task<string> FormatForLlmAsync(AISerializedEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var adapter = _adapters.GetAdapter(entity.EntityType);

        return await adapter.FormatForLlmAsync(entity, cancellationToken);
    }
```

(The existing `FormatForLlm` method shown above is unchanged — included only so you can see
exactly where the new method goes relative to it.)

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIEntityContextHelperTests"`
Expected: All tests PASS (existing ones plus the 2 new ones).

- [ ] **Step 7: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/IAIEntityAdapter.cs Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/IAIEntityContextHelper.cs Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIEntityContextHelper.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/EntityAdapter/AIEntityContextHelperTests.cs
git commit -m "refactor(core): Add async entity-formatting dispatch alongside the sync one"
```

---

### Task 3: Wire `SerializedEntityContributor` to the async path

**Files:**
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/Contributors/SerializedEntityContributor.cs`
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/RuntimeContext/Contributors/SerializedEntityContributorTests.cs` (extend existing file)

**Interfaces:**
- Consumes: `IAIEntityContextHelper.FormatForLlmAsync` (Task 2), `IAIRuntimeContextContributor.ContributeAsync` (Task 1, being overridden here).
- Produces: `SerializedEntityContributor.ContributeAsync` — override that Task 4's enhanced `MediaEntityAdapter` will benefit from once it exists; no new symbol other tasks depend on.

This task is a refactor of the existing class to share its entity-extraction logic between the
sync `Contribute` (unchanged behavior) and a new `ContributeAsync` override (which calls
`FormatForLlmAsync` instead of `FormatForLlm`). Still no observable behavior change: the helper's
`FormatForLlmAsync` still just falls back to the sync path until Task 4.

- [ ] **Step 1: Write the failing tests**

In `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/RuntimeContext/Contributors/SerializedEntityContributorTests.cs`,
add these 4 tests (the file already has a `_contextHelperMock`/`_contributor` fixture — reuse it):

```csharp
    [Fact]
    public async Task ContributeAsync_WithValidSerializedEntity_ProcessesEntity()
    {
        // Arrange
        var entityJson = """
            {
                "entityType": "document",
                "unique": "doc-123",
                "name": "Test Document",
                "data": {
                    "contentType": "blogPost",
                    "properties": []
                }
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Description = "Test entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns(new Dictionary<string, object?> { ["test"] = "value" });

        _contextHelperMock
            .Setup(x => x.FormatForLlmAsync(It.IsAny<AISerializedEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Formatted entity context (async)");

        // Act
        await _contributor.ContributeAsync(context);

        // Assert
        context.SystemMessageParts.Count.ShouldBe(1);
        context.SystemMessageParts[0].ShouldBe("Formatted entity context (async)");
        context.Variables.ShouldContainKey("test");
        context.Data.ShouldContainKey(Constants.ContextKeys.EntityType);
        _contextHelperMock.Verify(x => x.FormatForLlm(It.IsAny<AISerializedEntity>()), Times.Never);
    }

    [Fact]
    public async Task ContributeAsync_WithInvalidJson_DoesNotProcess()
    {
        // Arrange
        var contextItem = new AIRequestContextItem
        {
            Description = "Test entity",
            Value = "{ invalid json }"
        };

        var context = new AIRuntimeContext([contextItem]);

        // Act
        await _contributor.ContributeAsync(context);

        // Assert - should silently ignore, same contract as the sync path
        context.SystemMessageParts.Count.ShouldBe(0);
        _contextHelperMock.Verify(x => x.FormatForLlmAsync(It.IsAny<AISerializedEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ContributeAsync_WithEmptyEntityType_DoesNotProcess()
    {
        // Arrange
        var entityJson = """
            {
                "entityType": "",
                "unique": "doc-123",
                "name": "Test",
                "data": {}
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Description = "Test entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        // Act
        await _contributor.ContributeAsync(context);

        // Assert - item must remain unhandled, same contract as the sync path
        context.SystemMessageParts.Count.ShouldBe(0);
        context.RequestContextItems.IsHandled(contextItem).ShouldBeFalse();
    }

    [Fact]
    public async Task ContributeAsync_CallsContextHelperMethods()
    {
        // Arrange
        var entityJson = """
            {
                "entityType": "product",
                "unique": "prod-456",
                "name": "Widget",
                "data": {
                    "sku": "12345"
                }
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Description = "Test entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        var testVariables = new Dictionary<string, object?> { ["sku"] = "12345" };
        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns(testVariables);

        _contextHelperMock
            .Setup(x => x.FormatForLlmAsync(It.IsAny<AISerializedEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Formatted output");

        // Act
        await _contributor.ContributeAsync(context);

        // Assert - verify both methods were called
        _contextHelperMock.Verify(x => x.BuildContextDictionary(It.Is<AISerializedEntity>(e =>
            e.EntityType == "product" &&
            e.Unique == "prod-456" &&
            e.Name == "Widget")), Times.Once);

        _contextHelperMock.Verify(x => x.FormatForLlmAsync(It.Is<AISerializedEntity>(e =>
            e.EntityType == "product"), It.IsAny<CancellationToken>()), Times.Once);

        context.Variables["sku"].ShouldBe("12345");
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~SerializedEntityContributorTests"`
Expected: FAIL to compile — `ContributeAsync` is not yet overridden (it exists only as the
interface default from Task 1, which calls `FormatForLlm`, not `FormatForLlmAsync` — so even if
it compiled, `ContributeAsync_WithValidSerializedEntity_ProcessesEntity`'s
`Times.Never` assertion on `FormatForLlm` would fail). Confirm it fails for the expected reason.

- [ ] **Step 3: Refactor to share entity extraction, add the async override**

Replace the entire contents of
`Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/Contributors/SerializedEntityContributor.cs` with:

```csharp
using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Contributes data from context items that contain serialized entity data.
/// Extracts <see cref="AISerializedEntity"/> and populates template variables.
/// </summary>
internal sealed class SerializedEntityContributor : IAIRuntimeContextContributor
{
    private readonly JsonSerializerOptions _jsonOptions = new(Constants.DefaultJsonSerializerOptions)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IAIEntityContextHelper _contextHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedEntityContributor"/> class.
    /// </summary>
    /// <param name="contextHelper">The entity context helper for formatting.</param>
    public SerializedEntityContributor(IAIEntityContextHelper contextHelper)
    {
        _contextHelper = contextHelper;
    }

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        AIRequestContextItem? matchedItem = null;
        context.RequestContextItems.Handle(IsSerializedEntity, item => matchedItem = item);

        if (matchedItem is null)
        {
            return;
        }

        var entity = PrepareEntity(matchedItem, context);
        if (entity is null)
        {
            return;
        }

        var systemMessage = _contextHelper.FormatForLlm(entity);
        context.SystemMessageParts.Add(systemMessage);
    }

    /// <inheritdoc />
    public async Task ContributeAsync(AIRuntimeContext context, CancellationToken cancellationToken = default)
    {
        AIRequestContextItem? matchedItem = null;
        context.RequestContextItems.Handle(IsSerializedEntity, item => matchedItem = item);

        if (matchedItem is null)
        {
            return;
        }

        var entity = PrepareEntity(matchedItem, context);
        if (entity is null)
        {
            return;
        }

        var systemMessage = await _contextHelper.FormatForLlmAsync(entity, cancellationToken);
        context.SystemMessageParts.Add(systemMessage);
    }

    private bool IsSerializedEntity(AIRequestContextItem item)
    {
        // Check if the value contains entity structure by looking for required fields.
        // Required: entityType (non-empty string), unique (non-empty string), data (object).
        // name is optional — mock entities may not have one.
        // We validate values (not just presence) so mismatched items fall through to
        // other contributors instead of being silently swallowed by Handle's eager-mark.
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && HasNonEmptyString(value, "entityType")
                && HasNonEmptyString(value, "unique")
                && value.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasNonEmptyString(JsonElement obj, string propertyName)
        => obj.TryGetProperty(propertyName, out var element)
           && element.ValueKind == JsonValueKind.String
           && !string.IsNullOrEmpty(element.GetString());

    /// <summary>
    /// Deserializes the context item's JSON into an <see cref="AISerializedEntity"/>, stores
    /// derived values (entity id, parent id, entity type) into the runtime context's data bag,
    /// and builds template variables from it. Returns <c>null</c> (silently) on any
    /// deserialization failure — the item was already validated as JSON-shaped-like-an-entity
    /// by <see cref="IsSerializedEntity"/>, so failures here are unexpected edge cases, not the
    /// common path.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT call <see cref="IAIEntityContextHelper.FormatForLlm"/> or
    /// <see cref="IAIEntityContextHelper.FormatForLlmAsync"/> — callers do that themselves so
    /// each can use the sync or async path as appropriate.
    /// </remarks>
    private AISerializedEntity? PrepareEntity(AIRequestContextItem item, AIRuntimeContext context)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return null;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            var entity = DeserializeEntity(value);
            if (entity is null)
            {
                return null;
            }

            // Store in data bag
            context.SetValue(Constants.ContextKeys.SerializedEntity, entity);

            // Extract entity ID as Guid if possible
            if (Guid.TryParse(entity.Unique, out var entityId))
            {
                context.SetValue(Constants.ContextKeys.EntityId, entityId);
            }

            // Extract parent entity ID as Guid if available (for new entities)
            if (!string.IsNullOrEmpty(entity.ParentUnique) && Guid.TryParse(entity.ParentUnique, out var parentEntityId))
            {
                context.SetValue(Constants.ContextKeys.ParentEntityId, parentEntityId);
            }

            // Store entity type
            context.SetValue(Constants.ContextKeys.EntityType, entity.EntityType);

            // Build template variables from entity
            // When an element is present (e.g., block), prefix entity variables with "entity."
            // so they don't collide with element variables. When no element, keep unprefixed.
            var hasElement = context.Data.ContainsKey(Constants.ContextKeys.SerializedElement);
            var variables = _contextHelper.BuildContextDictionary(entity);
            foreach (var (varKey, varValue) in variables)
            {
                if (hasElement)
                {
                    context.Variables[$"entity.{varKey}"] = varValue;
                }
                else
                {
                    context.Variables[varKey] = varValue;
                }
            }

            return entity;
        }
        catch
        {
            // Silently ignore deserialization errors - item wasn't actually an entity
            return null;
        }
    }

    private static AISerializedEntity? DeserializeEntity(JsonElement element)
    {
        // Thorough value validation (called after lightweight IsSerializedEntity check).
        // Required: entityType, unique, data. name is optional (empty string allowed).
        try
        {
            var entityType = element.GetProperty("entityType").GetString();
            var unique = element.GetProperty("unique").GetString();

            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(unique))
            {
                return null;
            }

            // Extract data field (required)
            if (!element.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Extract name (optional — defaults to empty string)
            string name = string.Empty;
            if (element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                name = nameElement.GetString() ?? string.Empty;
            }

            // Extract parentUnique (optional)
            string? parentUnique = null;
            if (element.TryGetProperty("parentUnique", out var parentUniqueElement))
            {
                parentUnique = parentUniqueElement.GetString();
            }

            // Extract active culture/segment (optional). Frontend adapters emit
            // these on multi-variant entities so the helper can pick matching
            // property values.
            string? culture = null;
            if (element.TryGetProperty("culture", out var cultureElement) && cultureElement.ValueKind == JsonValueKind.String)
            {
                culture = cultureElement.GetString();
            }

            string? segment = null;
            if (element.TryGetProperty("segment", out var segmentElement) && segmentElement.ValueKind == JsonValueKind.String)
            {
                segment = segmentElement.GetString();
            }

            return new AISerializedEntity
            {
                EntityType = entityType,
                Unique = unique,
                Name = name,
                ParentUnique = parentUnique,
                Culture = culture,
                Segment = segment,
                Data = dataElement.Clone() // Clone to avoid referencing original document
            };
        }
        catch
        {
            return null;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~SerializedEntityContributorTests"`
Expected: All tests PASS — the original 12 sync tests (unchanged, still exercising `Contribute`)
plus the 4 new async ones.

- [ ] **Step 5: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/Contributors/SerializedEntityContributor.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/RuntimeContext/Contributors/SerializedEntityContributorTests.cs
git commit -m "refactor(core): Wire SerializedEntityContributor to the async formatting path"
```

---

### Task 4: Extract file content in `MediaEntityAdapter`

**Files:**
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/Adapters/MediaEntityAdapter.cs`
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/EntityAdapter/Adapters/MediaEntityAdapterTests.cs` (new)

**Interfaces:**
- Consumes: `IAIUmbracoMediaResolver.ResolveAsync(object?, string?, CancellationToken)` (existing,
  from Phase 1a), `IAIFileProcessingHandler.CanHandleAsync`/`ProcessAsync` (existing, from Phase
  1a), `AIFileProcessingHandlerCollection` (existing, from Phase 1a — iterate it directly, same
  pattern as `AIFileProcessingChatClient.FindHandlerAsync`).
- Produces: `MediaEntityAdapter.FormatForLlmAsync` override — this is the task that makes the bug
  fix observable. Nothing else depends on it; it's the end of the chain.

This is the only task in this plan with user-visible behavior change: everything in Tasks 1-3 was
inert plumbing until this point.

- [ ] **Step 1: Write the failing tests**

Create `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/EntityAdapter/Adapters/MediaEntityAdapterTests.cs`:

```csharp
using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.EntityAdapter.Adapters;
using Umbraco.AI.Core.FileProcessing;
using Umbraco.AI.Core.Media;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Tests.Unit.EntityAdapter.Adapters;

public class MediaEntityAdapterTests
{
    private readonly Mock<IMediaTypeService> _mediaTypeServiceMock = new();
    private readonly Mock<IPublishedContentTypeCache> _typeCacheMock = new();
    private readonly Mock<IPropertyEditorSchemaService> _schemaServiceMock = new();
    private readonly Mock<IAIUmbracoMediaResolver> _mediaResolverMock = new();

    private MediaEntityAdapter CreateAdapter(params IAIFileProcessingHandler[] handlers)
    {
        var collection = new AIFileProcessingHandlerCollection(() => handlers);
        return new MediaEntityAdapter(
            _mediaTypeServiceMock.Object,
            _typeCacheMock.Object,
            _schemaServiceMock.Object,
            _mediaResolverMock.Object,
            collection);
    }

    private static AISerializedEntity CreateEntity(string name = "report.csv")
        => new()
        {
            EntityType = "media",
            Unique = "11111111-1111-1111-1111-111111111111",
            Name = name,
            Data = JsonDocument.Parse("{}").RootElement,
        };

    [Fact]
    public async Task FormatForLlmAsync_WithTextExtractableMedia_AppendsExtractedContent()
    {
        // Arrange
        var entity = CreateEntity();
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = "a,b\n1,2"u8.ToArray(), MediaType = "text/csv" });

        var handler = new FakeHandler("text/csv", "a,b\n1,2");
        var adapter = CreateAdapter(handler);

        // Act
        var result = await adapter.FormatForLlmAsync(entity);

        // Assert
        result.ShouldContain("a,b\n1,2");
        result.ShouldContain(entity.Unique); // still includes the existing metadata line
    }

    [Fact]
    public async Task FormatForLlmAsync_WhenMediaCannotBeResolved_FallsBackToMetadataOnly()
    {
        // Arrange
        var entity = CreateEntity();
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIMediaContent?)null);

        var adapter = CreateAdapter();

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);
        var syncResult = adapter.FormatForLlm(entity);

        // Assert
        asyncResult.ShouldBe(syncResult);
    }

    [Fact]
    public async Task FormatForLlmAsync_WhenNoHandlerMatchesMediaType_FallsBackToMetadataOnly()
    {
        // Arrange — e.g. a real image, which has no text-extraction handler
        var entity = CreateEntity(name: "photo.png");
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = [1, 2, 3], MediaType = "image/png" });

        var adapter = CreateAdapter(); // no handlers registered

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);
        var syncResult = adapter.FormatForLlm(entity);

        // Assert
        asyncResult.ShouldBe(syncResult);
    }

    [Fact]
    public void FormatForLlm_SyncPath_DoesNotTouchTheMediaResolver()
    {
        // Arrange — the original sync method must remain exactly as before: no file I/O.
        var entity = CreateEntity();
        var adapter = CreateAdapter();

        // Act
        adapter.FormatForLlm(entity);

        // Assert
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class FakeHandler : IAIFileProcessingHandler
    {
        private readonly string _mimeType;
        private readonly string _content;

        public FakeHandler(string mimeType, string content)
        {
            _mimeType = mimeType;
            _content = content;
        }

        public Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(mimeType, _mimeType, StringComparison.OrdinalIgnoreCase));

        public Task<AIFileProcessingResult> ProcessAsync(
            ReadOnlyMemory<byte> data, string mimeType, string? filename,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AIFileProcessingResult(_content, false));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~MediaEntityAdapterTests"`
Expected: FAIL to compile — `MediaEntityAdapter`'s constructor doesn't yet accept
`IAIUmbracoMediaResolver`/`AIFileProcessingHandlerCollection`, and `FormatForLlmAsync` isn't
overridden yet.

- [ ] **Step 3: Implement the override**

In `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/Adapters/MediaEntityAdapter.cs`, add the two new
constructor dependencies and the `FormatForLlmAsync` override. The full file becomes:

```csharp
using Umbraco.AI.Core.FileProcessing;
using Umbraco.AI.Core.Media;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.EntityAdapter.Adapters;

/// <summary>
/// Adapter for Umbraco CMS media entities.
/// Delegates formatting to the same CMS property-based logic as documents.
/// Provides media type sub-types.
/// </summary>
internal sealed class MediaEntityAdapter : AIEntityAdapterBase
{
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;
    private readonly IAIUmbracoMediaResolver _mediaResolver;
    private readonly AIFileProcessingHandlerCollection _fileProcessingHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaEntityAdapter"/> class.
    /// </summary>
    public MediaEntityAdapter(
        IMediaTypeService mediaTypeService,
        IPublishedContentTypeCache publishedContentTypeCache,
        IPropertyEditorSchemaService propertyEditorSchemaService,
        IAIUmbracoMediaResolver mediaResolver,
        AIFileProcessingHandlerCollection fileProcessingHandlers)
    {
        _mediaTypeService = mediaTypeService;
        _publishedContentTypeCache = publishedContentTypeCache;
        _propertyEditorSchemaService = propertyEditorSchemaService;
        _mediaResolver = mediaResolver;
        _fileProcessingHandlers = fileProcessingHandlers;
    }

    /// <inheritdoc />
    public override string? EntityType => "media";

    /// <inheritdoc />
    public override string Name => "Media";

    /// <inheritdoc />
    public override string? Icon => "icon-picture";

    /// <inheritdoc />
    public override bool HasSubTypes => true;

    /// <inheritdoc />
    public override string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return CmsEntityFormatHelper.FormatCmsEntity(
            entity,
            _publishedContentTypeCache,
            _propertyEditorSchemaService,
            PublishedItemType.Media);
    }

    /// <summary>
    /// Formats a media entity for LLM consumption, appending extracted file text when the
    /// underlying file is a supported format — reusing the same file-processing handler
    /// pipeline that already services chat attachments (see
    /// <c>Umbraco.AI.Core.FileProcessing.AIFileProcessingChatClient</c>). Falls back to the
    /// metadata-only format from <see cref="FormatForLlm"/> when the media can't be resolved or
    /// no handler matches its MIME type.
    /// </summary>
    public override async Task<string> FormatForLlmAsync(AISerializedEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var baseline = FormatForLlm(entity);

        var media = await _mediaResolver.ResolveAsync(entity.Unique, cancellationToken: cancellationToken);
        if (media is null)
        {
            return baseline;
        }

        IAIFileProcessingHandler? handler = null;
        foreach (var candidate in _fileProcessingHandlers)
        {
            if (await candidate.CanHandleAsync(media.MediaType, cancellationToken))
            {
                handler = candidate;
                break;
            }
        }

        if (handler is null)
        {
            return baseline;
        }

        var result = await handler.ProcessAsync(media.Data, media.MediaType, entity.Name, cancellationToken);
        if (string.IsNullOrWhiteSpace(result.Content))
        {
            return baseline;
        }

        return $"{baseline}\n\n{result.Content}";
    }

    /// <inheritdoc />
    public override Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default)
    {
        var mediaTypes = _mediaTypeService.GetAll()
            .Where(x => !x.IsElement)
            .Select(mt => new AIEntitySubType
            {
                Alias = mt.Alias,
                Name = mt.Name ?? mt.Alias,
                Icon = mt.Icon,
                Description = mt.Description,
                Unique = mt.Key.ToString()
            })
            .OrderBy(mt => mt.Name);

        return Task.FromResult<IEnumerable<AIEntitySubType>>(mediaTypes);
    }
}
```

Note: `FormatForLlmAsync` is declared `override` here — unlike the interface-level default
methods in Tasks 1-2 (which are plain, non-`override` methods on classes that only implement the
interface), `MediaEntityAdapter` extends `AIEntityAdapterBase`, which does NOT declare its own
`FormatForLlmAsync` (it only has the abstract sync `FormatForLlm`), so `FormatForLlmAsync` here is
a new method satisfying/replacing the interface's default for this class — if the compiler
rejects the `override` keyword (because there's no virtual member of that name in the base
class), remove `override` and leave it as a plain method; either way the resulting dispatch
behavior through `IAIEntityAdapter`-typed callers is identical. Verify which one compiles and use
that.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~MediaEntityAdapterTests"`
Expected: All tests PASS.

- [ ] **Step 5: Run the full unit test suite to confirm no regression**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~Umbraco.AI.Tests.Unit"`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/Adapters/MediaEntityAdapter.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/EntityAdapter/Adapters/MediaEntityAdapterTests.cs
git commit -m "feat(core): Extract file content when a Media entity is the active Copilot context"
```

---

## Post-plan verification (manual, not a task)

Reproduce the original bug report in the running demo site: open a `.csv` Media item, open
Copilot, and ask what's in it. The AI should now describe the file's actual rows, not just its
name and byte count.

## Self-Review Notes

- **Spec coverage:** the spec's Phase 1b design (5 numbered points) maps directly to these 4
  tasks — points 3-4 (`IAIRuntimeContextContributor`/`AIRuntimeContextContributorCollection`) are
  Task 1, points 1-2 (`IAIEntityAdapter`/`IAIEntityContextHelper`) are Task 2, point 5 (call-site
  migration) is folded into Task 1 since it's the same pipeline. `SerializedEntityContributor`
  wiring (implied by point 3's "only `SerializedEntityContributor` overrides it") is Task 3. The
  actual extraction logic is Task 4.
- **No breaking changes anywhere:** every task adds new members; no existing signature changes.
  Verified by re-reading each modified file's diff shape above — `Contribute`, `FormatForLlm`,
  and `Populate` are reproduced unchanged in every task that touches their containing file.
- **Placeholder scan:** no TBD/TODO; every step has complete, runnable code.
- **Type consistency:** `FormatForLlmAsync(AISerializedEntity, CancellationToken = default)` and
  `ContributeAsync(AIRuntimeContext, CancellationToken = default)` signatures are identical
  everywhere they're declared, implemented, or called across all 4 tasks.
