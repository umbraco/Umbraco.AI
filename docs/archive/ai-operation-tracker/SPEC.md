# Design: Capability-agnostic `IAIOperationTracker`

**Issue:** [#195](https://github.com/umbraco/Umbraco.AI/issues/195)
**Date:** 2026-07-03
**Status:** Draft — awaiting review
**Scope:** `Umbraco.AI` core package only. Pure refactor; no observable change to emitted telemetry/audit/usage data (with one deliberate exception, see §7).

---

## 1. Problem

The image-generation slice consolidated usage-analytics + audit recording into one component,
`AIImageGenerationTracker`, shared by both the tracking middleware and the escape-hatch helper
(`IAIImageGenerationService.InvokeWithTrackingAsync`). It proved the pattern: extract runtime-context
dimensions → build record → `factory.Create` → queue, with independent `Enabled` gating and
fire-and-forget usage semantics — implemented once.

The same orchestration is still duplicated across **Chat, Embedding and SpeechToText**, each shipping
**three** pipeline wrappers:

- a capture-only tracking client (`AITrackingChatClient` / `AITrackingEmbeddingGenerator` /
  `AITrackingSpeechToTextClient`) exposing `Last*` properties,
- a usage-recording client (`AIUsageRecording*`),
- an auditing client (`AIAuditing*`).

Image-gen has **one** tracking middleware. This design generalizes that one-capability solution so the
recording orchestration lives in exactly one place for all four capabilities.

## 2. Goal / non-goals

**Goal:** a single internal `AIOperationTracker : IAIOperationTracker` in Core that all capabilities and
the image escape-hatch route through. Collapse each capability's three wrappers into one tracker-backed
client. Delete `AIImageGenerationTracker`.

**Non-goals:** no change to the public escape-hatch surface beyond what's unavoidable; no change to
middleware ordering; no new emitted data (except §7); no cross-version backport in this plan (flagged
separately in §11).

## 3. Key finding — the real coupling is the result hand-off, not the record-building

The context extractors are **already capability-agnostic**:

- `AIUsageContext.ExtractFromRuntimeContext(AICapability, AIRuntimeContext, modelId?)`
- `AIAuditContext.ExtractFromRuntimeContext(AICapability, AIRuntimeContext, prompt, modelId?)`

So the tracker needs **no** per-capability extraction logic. What differs per capability is only:

1. **How the operation result flows back.** Image returns it via a value (`AITrackedImageResult<T>`);
   chat/embedding/STT stash it in a separate upstream tracking wrapper (`LastUsageDetails`,
   `LastResponseMessages`, `LastEmbeddings`, `LastTranscriptionText`) that the downstream recording
   wrapper reads via `GetService<AITracking*>()`. **Generalizing means every capability returns its
   result by value** (image-style), which lets the `Last*` capture wrappers be deleted.

2. **Streaming.** Chat and STT aggregate usage/response *after* the stream completes. `yield` cannot sit
   in a `try/catch`, so the current code drives the enumerator manually. This cannot be forced into a
   `Func<CancellationToken, Task<T>>` shape.

3. **Per-capability recording policy** — see the faithfulness table in §4.

## 4. Faithfulness table (behavior that MUST be preserved byte-for-byte)

| Aspect | Chat | Embedding | SpeechToText | ImageGeneration |
|---|---|---|---|---|
| Activity enrichment (`AIActivityEnricher.EnrichCurrentActivity`) | ✅ yes | ✅ yes | ✅ yes | ❌ **no (today)** |
| Audit metadata (`LogKeys`) source | RuntimeContext | `options.AdditionalProperties` | RuntimeContext | none (null) |
| Audit **prompt** `Data` | chat messages | input values | `BuildPromptData(options)` | caller `promptData` |
| Audit **response** `Data` | `response.Messages` (agg. for stream) | `LastEmbeddings` | `response.Text` / agg. | `"{N} image(s)"` |
| Audit **response** `Usage` | `response.Usage` | `LastUsageDetails` | none | none |
| Analytics **usage** source | `LastUsageDetails` | `LastUsageDetails` | none (duration only) | provider `Usage` |
| Skip usage record when no `UsageDetails`? | ✅ skip | ✅ skip | ❌ record (duration) | ❌ record |
| Fire-and-forget usage; awaited audit; `CancellationToken.None` for status | ✅ | ✅ | ✅ | ✅ |

Two consequences drive the API shape:

- **Audit-response `Usage` diverges from analytics `Usage` only for image** (image records analytics
  usage but writes no usage into the audit response). So the two cannot be a single field.
- **Metadata source differs** (embedding reads the options bag, others read the runtime context), so
  metadata must be supplied by the caller, not derived inside the tracker.

## 5. Proposed architecture

New root-level `Observability/` folder in `Umbraco.AI.Core` (shared infrastructure spanning both
`Analytics/` and `AuditLog/`; consistent with the "shared code at root" convention in CLAUDE.md).

### 5.1 The tracker (internal)

```csharp
namespace Umbraco.AI.Core.Observability;

internal interface IAIOperationTracker
{
    // Convenience for non-streaming operations (image, and the non-streaming path of every capability).
    Task<AITrackedOperationResult<TResult>> TrackAsync<TResult>(
        AIOperationDescriptor descriptor,
        Func<CancellationToken, Task<AITrackedOperationResult<TResult>>> operation,
        CancellationToken cancellationToken);

    // Primitive for streaming: begin the audit+timer, hand back a scope the caller completes/fails.
    Task<AIOperationScope> BeginAsync(AIOperationDescriptor descriptor, CancellationToken cancellationToken);
}
```

```csharp
// Passed in BEFORE the operation runs (audit start needs prompt + metadata up front).
internal sealed class AIOperationDescriptor
{
    public required AICapability Capability { get; init; }
    public object? PromptData { get; init; }                       // audit prompt Data
    public IReadOnlyDictionary<string, string>? Metadata { get; init; } // caller-extracted LogKeys
    public bool RecordUsageWhenEmpty { get; init; }                // STT/image = true; chat/embedding = false
}

// Returned by the operation delegate AFTER it runs.
internal sealed class AITrackedOperationResult<TResult>
{
    public required TResult Result { get; init; }
    public UsageDetails? Usage { get; init; }                      // analytics usage
    public AIAuditResponse? AuditResponse { get; init; }           // fully-formed audit-complete payload
}

// Disposable scope for streaming: carries auditLog + auditScope + stopwatch.
internal sealed class AIOperationScope : IDisposable
{
    // Records audit-complete (awaited, CancellationToken.None) + fire-and-forget usage.
    public Task CompleteAsync(UsageDetails? usage, AIAuditResponse? auditResponse);
    // Records audit-failure (awaited, CancellationToken.None) + fire-and-forget failed usage.
    public Task FailAsync(Exception exception);
    public void Dispose(); // disposes the AIAuditScope
}
```

`TrackAsync` is implemented in terms of `BeginAsync` + `CompleteAsync`/`FailAsync`, so the
fire-and-forget-usage / awaited-audit / `CancellationToken.None` semantics live in exactly one place.
The tracker owns: `Enabled` gating (audit and analytics independently), context extraction,
`AIAuditScope.Begin`/dispose, parenting via `AIAuditScope.Current`, `Activity.Current.TraceId` capture,
and `AIActivityEnricher.EnrichCurrentActivity` (see §7).

**Why `AITrackedOperationResult` is internal and separate from the public `AITrackedImageResult<T>`:**
the internal wrapper needs precise audit control (separate `AuditResponse`), while the public escape-hatch
wrapper stays simple. The image service maps between them (§5.3). This keeps the public API churn to zero.

### 5.2 Per-capability collapse (three wrappers → one)

Each capability keeps **one** tracker-backed client that handles both streaming and non-streaming:

- **Non-streaming:** call `tracker.TrackAsync(descriptor, ct => { var r = await inner...; return new AITrackedOperationResult{...}; }, ct)`.
- **Streaming:** `var scope = await tracker.BeginAsync(descriptor, ct);` then enumerate manually (the
  existing `WrapStreamWithErrorCapture` / manual-enumerator glue stays, because it's about
  `IAsyncEnumerable` mechanics), aggregate usage/response, then `scope.CompleteAsync(...)` /
  `scope.FailAsync(ex)` and `scope.Dispose()`.

Deleted after the collapse: `AIUsageRecording{Chat,Embedding,SpeechToText}*`,
`AIAuditing{Chat,Embedding,SpeechToText}*`, and the `Last*` capture wrappers
(`AITrackingChatClient`, `AITrackingEmbeddingGenerator`, `AITrackingSpeechToTextClient`) — their
aggregation logic folds into the single tracker-backed client where still needed for streaming.

### 5.3 Image path

- Delete `AIImageGenerationTracker`.
- `AITrackingImageGenerationClient` and `IAIImageGenerationService.InvokeWithTrackingAsync` route through
  `IAIOperationTracker`.
- **Keep the public `AITrackedImageResult<T> { Result, Usage, ImageCount }`** as the escape-hatch surface
  (it is `[Experimental]`; leaving it unchanged avoids breaking escape-hatch callers). The image service
  converts it to the internal `AITrackedOperationResult` — building `AIAuditResponse { Data = "{ImageCount} image(s)" }`
  with **no** usage, exactly as today.

### 5.4 DI + middleware ordering

- Register `IAIOperationTracker` → `AIOperationTracker` as a **singleton** (replacing the
  `AIImageGenerationTracker` singleton registration).
- Remove the `AIUsageRecording*Middleware` and `AIAuditing*Middleware` registrations from the chat,
  embedding and STT pipelines. Each pipeline keeps OpenTelemetry (innermost) + a single tracking
  middleware occupying the span the three wrappers previously occupied.
- **Preserve ordering exactly** (OpenTelemetry innermost so Activity/TraceId is available; audit-start
  happens at the same relative point as today). Verified by existing audit tests asserting TraceId.

## 6. Data flow (chat non-streaming, as an example)

```
AITrackingChatClient.GetResponseAsync
  └─ tracker.TrackAsync(descriptor{Chat, promptData=messages, metadata=LogKeys, RecordUsageWhenEmpty=false}, op)
       ├─ BeginAsync: gate on Enabled; ExtractFromRuntimeContext(Chat); factory.Create(parentId); AIAuditScope.Begin;
       │              TraceId = Activity.Current; EnrichCurrentActivity; QueueStartAuditLogAsync
       ├─ op(ct):  response = inner.GetResponseAsync(...);
       │           return { Result=response, Usage=response.Usage,
       │                    AuditResponse={ Data=response.Messages, Usage=response.Usage } }
       └─ CompleteAsync: QueueCompleteAuditLogAsync(auditResponse, CancellationToken.None);
                         if (Usage != null || RecordUsageWhenEmpty) fire-and-forget QueueRecordUsageAsync
```

## 7. The one deliberate behavioral delta — image Activity enrichment

Chat, Embedding and STT all call `AIActivityEnricher.EnrichCurrentActivity`; the image tracker does not.
Centralizing enrichment in the tracker gives the image path enrichment it lacks today. This is a small,
arguably-correct consistency improvement but **is** a change to image telemetry, so it violates strict
"no observable change."

**Recommendation:** enrich uniformly in the tracker (image gains parity), call it out explicitly in the
changelog. **Alternative:** gate enrichment behind a descriptor flag so image stays un-enriched
(strict purity, minor asymmetry retained). **Decision needed from reviewer.** Default in this doc:
uniform enrichment.

## 8. Testing

- **Existing behavior tests must pass unchanged** where they assert emitted audit/usage *data and
  semantics* (routed through the new single client). This is the primary safety net.
- **Tests that reference the deleted classes by name** (`AIUsageRecordingChatClient`,
  `AIAuditing*`, etc.) cannot pass unchanged because the classes are gone — they must be migrated to
  target the single tracker-backed client. Flagged: the issue's AC says "all existing middleware tests
  pass unchanged"; that holds for *behavior* assertions, not for tests coupled to the old class shapes.
- **New `AIOperationTracker` unit tests:** Enabled-gating (audit/analytics independent), fire-and-forget
  usage, awaited audit, `CancellationToken.None` on status, `RecordUsageWhenEmpty` policy, parent via
  `AIAuditScope.Current`, TraceId capture, exception path (fail audit + failed usage + rethrow).

## 9. Risks

1. **Silent telemetry drift** — the whole value is "no data change." Mitigation: the faithfulness table
   (§4) is the test checklist; lean on existing data-assertion tests.
2. **Ordering/TraceId timing** — collapsing three wrappers into one must not move the audit-start point
   relative to OpenTelemetry. Mitigation: preserve pipeline position; verify via TraceId tests.
3. **Streaming edge cases** (guardrail block mid-stream, client disconnect) — the manual-enumerator glue
   is subtle. Mitigation: keep that glue per-capability; only the recording calls move into the scope.
4. **Public API** — `InvokeWithTrackingAsync` signature. Mitigation: keep `AITrackedImageResult<T>`.

## 10. Acceptance criteria (from #195)

- [ ] One `IAIOperationTracker` implementation; no per-capability duplication of extract→record→queue.
- [ ] Chat / Embedding / SpeechToText / ImageGeneration usage + audit all routed through it.
- [ ] `AIImageGenerationTracker` deleted; escape-hatch helper uses the generic tracker.
- [ ] Behavior tests pass; class-coupled tests migrated; tracker unit tests added.
- [ ] No change to emitted telemetry/audit/usage data (except the §7 image-enrichment decision).

## 11. Open decisions for reviewer

1. **§7 image Activity enrichment:** uniform (recommended) vs. strictly preserved?
2. **Folder name:** `Observability/` (recommended) vs. under `Analytics/`?
3. **Backport to `v17/dev`?** Pure internal refactor — optional. Flag per CLAUDE.md sync policy.
4. **Tracker API shape:** `AITrackedOperationResult<T>` wrapper (recommended) vs. selector-delegate
   descriptor. This doc assumes the wrapper. (The original AskUserQuestion on this went unanswered.)
