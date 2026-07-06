# Capability-agnostic `IAIOperationTracker` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract one capability-agnostic `IAIOperationTracker` from `AIImageGenerationTracker` and route Chat / Embedding / SpeechToText / ImageGeneration usage + audit recording through it, collapsing each capability's three pipeline wrappers into one.

**Architecture:** A new internal `AIOperationTracker` in `Umbraco.AI.Core/Observability/` owns the extract → record → queue orchestration (independent `Enabled` gating, `AIAuditScope`, TraceId capture, Activity enrichment, fire-and-forget usage, awaited audit with `CancellationToken.None`). Each capability keeps a single tracker-backed client that returns its result *by value* (`AITrackedOperationResult<T>`); the old `Last*` capture wrappers and the separate usage/audit wrappers are deleted. Streaming uses a `BeginAsync` → `CompleteAsync`/`FailAsync` scope primitive so the recording semantics stay in one place while each capability keeps its own `IAsyncEnumerable` glue.

**Tech Stack:** .NET 10 (`net10.0`), Microsoft.Extensions.AI, xUnit + Shouldly + Moq, Umbraco `OrderedCollectionBuilder` middleware.

**Spec:** `docs/superpowers/specs/2026-07-03-ai-operation-tracker-design.md`. **Issue:** [#195](https://github.com/umbraco/Umbraco.AI/issues/195).

## Global Constraints

- **Pure refactor — no observable change to emitted usage/audit/telemetry data**, except the one deliberate delta below.
- **Deliberate delta (§7 of spec):** the generic tracker enriches `Activity.Current` for *all* capabilities; today image generation does not. This gives image parity. Must be called out in the commit/changelog. (If the reviewer rejects this, gate enrichment behind `AIOperationDescriptor.EnrichActivity`, defaulting false only for image.)
- **Preserve per-capability behavior exactly** per the faithfulness table in the spec §4:
  - Audit-response `Usage` is populated for chat/embedding, **null for STT and image**.
  - Analytics usage: chat/embedding use provider `UsageDetails`; STT records duration only; image uses provider `Usage`.
  - **Skip usage recording when `UsageDetails` is null** for chat + embedding; **record anyway** for STT + image (`RecordUsageWhenEmpty = true`).
  - Audit metadata (`LogKeys`) source: chat/STT from `AIRuntimeContext`; embedding from `options.AdditionalProperties`; image = none.
  - Fire-and-forget usage; awaited audit; **`CancellationToken.None`** for all status persistence.
  - Activity enrichment is **not** gated on audit `Enabled` (it has a runtime-context fallback path).
- **Middleware ordering unchanged:** OpenTelemetry stays innermost; the single tracking client occupies the pipeline span the three old wrappers occupied.
- **Naming:** async methods `[Action][Entity]Async`; extension methods in `Umbraco.AI.Extensions`; new types under namespace `Umbraco.AI.Core.Observability`.
- **Commit style:** Conventional Commits, sentence-case subject, scope `core`. End commit messages with the `Co-Authored-By` trailer.
- **Experimental guards:** image + STT types need `#pragma warning disable MEAI001`; image also `UMBRACOAI_IMAGEGEN`.

---

## Task 0: Worktree setup (Phase 0 — mandatory per CLAUDE.local.md)

**Files:** none (environment only).

- [ ] **Step 1:** Call `EnterWorktree` with name `ai-operation-tracker`. (The `WorktreeCreate` hook branches `v18/feature/ai-operation-tracker` from `v18/dev`, copies `.worktreeinclude`, switches cwd.)
- [ ] **Step 2:** Verify: `pwd && git branch --show-current` → path under `.claude/worktrees/`, branch `v18/feature/ai-operation-tracker`.
- [ ] **Step 3:** `npm install` if the frontend is touched (it is not here — skip unless a build needs it).
- [ ] **Step 4:** Update tracking task #1 description with the worktree absolute path + branch.
- [ ] **Step 5:** Baseline build/test to confirm a clean start:

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx`
Expected: PASS (all green before any change).

---

## Task 1: `Observability/` core types + `AIOperationTracker`

Foundation. No capability wired yet; this task delivers the tracker and its unit tests in isolation.

**Files:**
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Observability/IAIOperationTracker.cs`
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Observability/AIOperationDescriptor.cs`
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Observability/AITrackedOperationResult.cs`
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Observability/AIOperationScope.cs`
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Observability/AIOperationTracker.cs`
- Create: `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/AIAuditMetadata.cs` (shared LogKeys extractor)
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` (register singleton)
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Observability/AIOperationTrackerTests.cs`

**Interfaces:**
- Consumes: `IAIRuntimeContextAccessor`, `IAIAuditLogService` (`QueueStartAuditLogAsync`, `QueueCompleteAuditLogAsync`, `QueueRecordAuditLogFailureAsync`), `IAIAuditLogFactory.Create(AIAuditContext, IDictionary<string,string>? metadata, Guid? parentId)`, `IOptionsMonitor<AIAuditLogOptions>`, `IAIUsageRecordingService.QueueRecordUsageAsync`, `IAIUsageRecordFactory.Create(AIUsageRecordContext, AIUsageRecordResult)`, `IOptionsMonitor<AIAnalyticsOptions>`, `AIUsageContext.ExtractFromRuntimeContext(AICapability, AIRuntimeContext)`, `AIAuditContext.ExtractFromRuntimeContext(AICapability, AIRuntimeContext, object? prompt)`, `AIUsageRecordContext.FromUsageContext(...)`, `AIAuditScope.Begin/Current/AuditLogId`, `AIActivityEnricher.EnrichCurrentActivity(AIAuditLog?, IAIRuntimeContextAccessor)`, `AIAuditResponse { Data, Usage }`, `AIAuditPrompt { Data, Capability }`.
- Produces:
  - `IAIOperationTracker.TrackAsync<TResult>(AIOperationDescriptor, Func<CancellationToken, Task<AITrackedOperationResult<TResult>>>, CancellationToken) : Task<AITrackedOperationResult<TResult>>`
  - `IAIOperationTracker.BeginAsync(AIOperationDescriptor, CancellationToken) : Task<AIOperationScope>`
  - `AIOperationDescriptor { AICapability Capability; object? PromptData; IReadOnlyDictionary<string,string>? Metadata; bool RecordUsageWhenEmpty }`
  - `AITrackedOperationResult<TResult> { TResult Result; UsageDetails? Usage; AIAuditResponse? AuditResponse }`
  - `AIOperationScope.CompleteAsync(UsageDetails?, AIAuditResponse?) : Task` / `FailAsync(Exception) : Task` / `Dispose()`
  - `AIAuditMetadata.ExtractFromRuntimeContext(AIRuntimeContext?) : IReadOnlyDictionary<string,string>?`

- [ ] **Step 1: Create the descriptor and result types.**

`Observability/AIOperationDescriptor.cs`:
```csharp
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Describes a trackable AI operation. Supplied to <see cref="IAIOperationTracker"/> before the
/// operation runs (audit start needs the prompt + metadata up front).
/// </summary>
internal sealed class AIOperationDescriptor
{
    /// <summary>The capability being tracked (drives context extraction).</summary>
    public required AICapability Capability { get; init; }

    /// <summary>Prompt/input descriptor captured for the audit entry.</summary>
    public object? PromptData { get; init; }

    /// <summary>Optional audit metadata (LogKeys), pre-extracted by the caller from its own source.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// When true, a usage record is written even if no <c>UsageDetails</c> are available
    /// (duration/status only). Chat/Embedding = false; SpeechToText/ImageGeneration = true.
    /// </summary>
    public bool RecordUsageWhenEmpty { get; init; }
}
```

`Observability/AITrackedOperationResult.cs`:
```csharp
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Internal outcome wrapper returned by a tracked operation, carrying everything the tracker needs
/// to record analytics and audit entries. Analytics usage and audit-response usage are separate
/// because they diverge for image generation (analytics records usage; audit response does not).
/// </summary>
internal sealed class AITrackedOperationResult<TResult>
{
    public required TResult Result { get; init; }

    /// <summary>Usage recorded to analytics (nullable; STT has none).</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>The fully-formed audit-complete payload (Data + optional Usage); null skips nothing but writes null data.</summary>
    public AIAuditResponse? AuditResponse { get; init; }
}
```

- [ ] **Step 2: Create the shared metadata extractor.**

`AuditLog/AIAuditMetadata.cs` (de-duplicates the identical chat + STT LogKeys extraction):
```csharp
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Extracts audit metadata (declared via <see cref="Constants.ContextKeys.LogKeys"/>) from the
/// ambient runtime context. Used by capabilities whose LogKeys live in the runtime context
/// (chat, speech-to-text). Embedding reads its LogKeys from the options bag instead.
/// </summary>
internal static class AIAuditMetadata
{
    public static IReadOnlyDictionary<string, string>? ExtractFromRuntimeContext(AIRuntimeContext? context)
    {
        if (context?.TryGetValue<string[]>(Constants.ContextKeys.LogKeys, out var logKeys) != true)
        {
            return null;
        }

        return logKeys!.ToDictionary(
            key => key,
            key => context!.GetValue<object?>(key)?.ToString() ?? string.Empty);
    }
}
```

- [ ] **Step 3: Write the tracker interface.**

`Observability/IAIOperationTracker.cs`:
```csharp
namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Capability-agnostic recorder of usage analytics + audit entries around an AI operation.
/// The single source of truth for the extract → record → queue orchestration.
/// </summary>
internal interface IAIOperationTracker
{
    /// <summary>Runs <paramref name="operation"/> with audit + usage recording (non-streaming path).</summary>
    Task<AITrackedOperationResult<TResult>> TrackAsync<TResult>(
        AIOperationDescriptor descriptor,
        Func<CancellationToken, Task<AITrackedOperationResult<TResult>>> operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Starts audit + timing and returns a scope the caller completes/fails. For streaming operations
    /// where the result is only known after enumeration.
    /// </summary>
    Task<AIOperationScope> BeginAsync(AIOperationDescriptor descriptor, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Write the failing tracker unit tests.**

`tests/Umbraco.AI.Tests.Unit/Observability/AIOperationTrackerTests.cs` — cover the faithfulness rules. (Full test bodies; use Moq + Shouldly per repo convention. `runtimeContext` is a real `AIRuntimeContext` with ProfileId/Alias set so extraction succeeds.)
```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Observability;

public class AIOperationTrackerTests
{
    // Test 1: successful op queues start + complete audit and one usage record.
    [Fact]
    public async Task TrackAsync_OnSuccess_QueuesStartCompleteAudit_AndUsage() { /* arrange mocks, act, assert QueueStart/Complete + QueueRecordUsage called once */ }

    // Test 2: exception path queues audit failure + a failed usage record, then rethrows.
    [Fact]
    public async Task TrackAsync_OnException_QueuesAuditFailure_AndFailedUsage_AndRethrows() { }

    // Test 3: audit disabled => no audit queue calls, usage still recorded.
    [Fact]
    public async Task TrackAsync_AuditDisabled_SkipsAudit_ButRecordsUsage() { }

    // Test 4: analytics disabled => no usage record, audit still queued.
    [Fact]
    public async Task TrackAsync_AnalyticsDisabled_SkipsUsage_ButQueuesAudit() { }

    // Test 5: RecordUsageWhenEmpty=false + null Usage => NO usage record queued.
    [Fact]
    public async Task TrackAsync_NullUsage_WithRecordWhenEmptyFalse_SkipsUsage() { }

    // Test 6: RecordUsageWhenEmpty=true + null Usage => usage record queued (duration only).
    [Fact]
    public async Task TrackAsync_NullUsage_WithRecordWhenEmptyTrue_RecordsUsage() { }

    // Test 7: complete/failure audit uses CancellationToken.None even if the passed token is cancelled.
    [Fact]
    public async Task TrackAsync_UsesCancellationTokenNone_ForStatusPersistence() { }

    // Test 8: audit log is created with parentId = AIAuditScope.Current when nested.
    [Fact]
    public async Task TrackAsync_NestedScope_ParentsAuditLog() { }

    // Test 9: BeginAsync + CompleteAsync mirrors TrackAsync success behavior.
    [Fact]
    public async Task BeginThenComplete_QueuesStartCompleteAudit_AndUsage() { }

    // Test 10: usage recording exception is swallowed (does not throw out of TrackAsync).
    [Fact]
    public async Task TrackAsync_UsageRecordingThrows_DoesNotPropagate() { }
}
```
> Implementer note: fill each body following the AAA pattern in `Umbraco.AI/CLAUDE.md`. Because usage is fire-and-forget (`_ = ...`), tests assert on the mock with a short `Mock.Verify` retry or make `QueueRecordUsageAsync` completion awaitable via a `TaskCompletionSource` the test waits on.

- [ ] **Step 5: Run tests to verify they fail.**

Run: `dotnet test Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Umbraco.AI.Tests.Unit.csproj --filter FullyQualifiedName~AIOperationTrackerTests`
Expected: FAIL — `AIOperationTracker` / `AIOperationScope` not defined.

- [ ] **Step 6: Write `AIOperationScope`.**

`Observability/AIOperationScope.cs` — carries the audit state + stopwatch and centralizes the record calls:
```csharp
using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// A tracking scope for a single AI operation. Created by <see cref="AIOperationTracker.BeginAsync"/>.
/// Completing or failing the scope queues the audit status (awaited, on <see cref="CancellationToken.None"/>)
/// and fire-and-forgets the usage record. Dispose ends the ambient <see cref="AIAuditScope"/>.
/// </summary>
internal sealed class AIOperationScope : IDisposable
{
    private readonly AIOperationTracker _tracker;
    private readonly AIOperationDescriptor _descriptor;
    private readonly AIAuditScope? _auditScope;
    private readonly AIAuditLog? _auditLog;
    private readonly AIAuditPrompt? _auditPrompt;
    private readonly Stopwatch _stopwatch;
    private readonly CancellationToken _cancellationToken;

    internal AIOperationScope(
        AIOperationTracker tracker,
        AIOperationDescriptor descriptor,
        AIAuditScope? auditScope,
        AIAuditLog? auditLog,
        AIAuditPrompt? auditPrompt,
        CancellationToken cancellationToken)
    {
        _tracker = tracker;
        _descriptor = descriptor;
        _auditScope = auditScope;
        _auditLog = auditLog;
        _auditPrompt = auditPrompt;
        _cancellationToken = cancellationToken;
        _stopwatch = Stopwatch.StartNew();
    }

    public async Task CompleteAsync(UsageDetails? usage, AIAuditResponse? auditResponse)
    {
        _stopwatch.Stop();

        if (_auditLog is not null)
        {
            await _tracker.AuditLogService.QueueCompleteAuditLogAsync(
                _auditLog, _auditPrompt, auditResponse, CancellationToken.None);
        }

        _ = _tracker.RecordUsageAsync(
            _descriptor, usage, _stopwatch.ElapsedMilliseconds, succeeded: true, errorMessage: null, _cancellationToken);
    }

    public async Task FailAsync(Exception exception)
    {
        _stopwatch.Stop();

        if (_auditLog is not null)
        {
            await _tracker.AuditLogService.QueueRecordAuditLogFailureAsync(
                _auditLog, _auditPrompt, exception, CancellationToken.None);
        }

        _ = _tracker.RecordUsageAsync(
            _descriptor, usage: null, _stopwatch.ElapsedMilliseconds, succeeded: false, errorMessage: exception.Message, _cancellationToken);
    }

    public void Dispose() => _auditScope?.Dispose();
}
```

- [ ] **Step 7: Write `AIOperationTracker`.**

`Observability/AIOperationTracker.cs` — generalizes `AIImageGenerationTracker` verbatim, parameterised by capability + descriptor:
```csharp
using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.AuditLog.Middleware;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Observability;

/// <inheritdoc cref="IAIOperationTracker" />
internal sealed class AIOperationTracker : IAIOperationTracker
{
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIUsageRecordingService _usageRecordingService;
    private readonly IAIUsageRecordFactory _usageRecordFactory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _analyticsOptions;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;
    private readonly ILogger<AIOperationTracker> _logger;

    internal IAIAuditLogService AuditLogService { get; }

    public AIOperationTracker(
        IAIRuntimeContextAccessor contextAccessor,
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions,
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory usageRecordFactory,
        IOptionsMonitor<AIAnalyticsOptions> analyticsOptions,
        ILogger<AIOperationTracker> logger)
    {
        _contextAccessor = contextAccessor;
        AuditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
        _usageRecordingService = usageRecordingService;
        _usageRecordFactory = usageRecordFactory;
        _analyticsOptions = analyticsOptions;
        _logger = logger;
    }

    public async Task<AITrackedOperationResult<TResult>> TrackAsync<TResult>(
        AIOperationDescriptor descriptor,
        Func<CancellationToken, Task<AITrackedOperationResult<TResult>>> operation,
        CancellationToken cancellationToken)
    {
        var scope = await BeginAsync(descriptor, cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await scope.CompleteAsync(result.Usage, result.AuditResponse);
            return result;
        }
        catch (Exception ex)
        {
            await scope.FailAsync(ex);
            throw;
        }
        finally
        {
            scope.Dispose();
        }
    }

    public async Task<AIOperationScope> BeginAsync(AIOperationDescriptor descriptor, CancellationToken cancellationToken)
    {
        AIAuditScope? auditScope = null;
        AIAuditLog? auditLog = null;
        AIAuditPrompt? auditPrompt = null;

        if (_auditLogOptions.CurrentValue.Enabled && _contextAccessor.Context is not null)
        {
            var auditContext = AIAuditContext.ExtractFromRuntimeContext(
                descriptor.Capability, _contextAccessor.Context, descriptor.PromptData);

            var metadata = descriptor.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value);

            auditLog = _auditLogFactory.Create(auditContext, metadata, parentId: AIAuditScope.Current?.AuditLogId);
            auditScope = AIAuditScope.Begin(auditLog.Id);
            auditLog.TraceId = Activity.Current?.TraceId.ToString();

            await AuditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);

            auditPrompt = new AIAuditPrompt { Data = descriptor.PromptData, Capability = descriptor.Capability };
        }

        // Enrich ambient Activity regardless of audit toggle (falls back to runtime context).
        AIActivityEnricher.EnrichCurrentActivity(auditLog, _contextAccessor);

        return new AIOperationScope(this, descriptor, auditScope, auditLog, auditPrompt, cancellationToken);
    }

    internal async Task RecordUsageAsync(
        AIOperationDescriptor descriptor, UsageDetails? usage, long durationMs,
        bool succeeded, string? errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            if (!_analyticsOptions.CurrentValue.Enabled || _contextAccessor.Context is null)
            {
                return;
            }

            if (usage is null && !descriptor.RecordUsageWhenEmpty)
            {
                return; // chat/embedding: no token counts => nothing to record
            }

            var usageContext = AIUsageContext.ExtractFromRuntimeContext(descriptor.Capability, _contextAccessor.Context);
            var recordContext = AIUsageRecordContext.FromUsageContext(usageContext);
            var result = new AIUsageRecordResult
            {
                Usage = usage,
                DurationMs = durationMs,
                Succeeded = succeeded,
                ErrorMessage = errorMessage,
            };

            var record = _usageRecordFactory.Create(recordContext, result);
            await _usageRecordingService.QueueRecordUsageAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record AI usage for {Capability}", descriptor.Capability);
        }
    }
}
```
> Faithfulness check vs. `AIImageGenerationTracker`: same StartAudit sequence (Enabled+context gate, ExtractFromRuntimeContext, factory.Create with parentId, AIAuditScope.Begin, TraceId, QueueStart), same complete/fail with `CancellationToken.None`, same fire-and-forget usage swallowing errors. New: `AIActivityEnricher` call (§7 delta for image), the `RecordUsageWhenEmpty` gate, caller-supplied metadata.

- [ ] **Step 8: Register the tracker as a singleton.**

Modify `Configuration/UmbracoBuilderExtensions.cs` — replace line 230 (`services.AddSingleton<AIImageGenerationTracker>();`) with:
```csharp
        // Capability-agnostic usage + audit recorder (chat / embedding / speech-to-text / image),
        // shared by every tracking middleware and the image escape-hatch helper.
        services.AddSingleton<IAIOperationTracker, AIOperationTracker>();
```
Add `using Umbraco.AI.Core.Observability;` to the file's usings.

- [ ] **Step 9: Fill in and run the tests to green.**

Run: `dotnet test Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Umbraco.AI.Tests.Unit.csproj --filter FullyQualifiedName~AIOperationTrackerTests`
Expected: PASS (all 10).

- [ ] **Step 10: Commit.**
```bash
git add Umbraco.AI/src/Umbraco.AI.Core/Observability Umbraco.AI/src/Umbraco.AI.Core/AuditLog/AIAuditMetadata.cs Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Observability
git commit -m "$(cat <<'EOF'
feat(core): Add capability-agnostic IAIOperationTracker

Introduce Observability/AIOperationTracker as the single source of truth for
usage + audit recording, generalized from AIImageGenerationTracker. No
capability is wired to it yet.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Route ImageGeneration through the tracker; delete `AIImageGenerationTracker`

Image goes first: it already returns its result by value, so it validates the tracker end-to-end with the least risk. Keeps the public `AITrackedImageResult<T>` surface unchanged.

**Files:**
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/AITrackingImageGenerationClient.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/AITrackingImageGenerationMiddleware.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/AIImageGenerationService.cs:122-165` (`InvokeWithTrackingAsync`)
- Delete: `Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/AIImageGenerationTracker.cs`
- Test: existing image tests under `tests/Umbraco.AI.Tests.Unit/` (run unchanged).

**Interfaces:**
- Consumes: `IAIOperationTracker` (Task 1). `AITrackedImageResult<TResult> { Result, Usage, ImageCount }` stays public/`[Experimental]`.
- Produces: no new public surface. `InvokeWithTrackingAsync` signature unchanged.

- [ ] **Step 1: Repoint the middleware + client at `IAIOperationTracker`.**

`AITrackingImageGenerationMiddleware.cs` — inject `IAIOperationTracker`:
```csharp
    private readonly IAIOperationTracker _tracker;

    public AITrackingImageGenerationMiddleware(IAIOperationTracker tracker) => _tracker = tracker;

    public IImageGenerator Apply(IImageGenerator generator)
        => new AITrackingImageGenerationClient(generator, _tracker);
```
(add `using Umbraco.AI.Core.Observability;`)

`AITrackingImageGenerationClient.cs` — swap the tracker type and build the internal result. `BuildPromptData` stays. The audit response mirrors today (`"{count} image(s)"`, no usage); `RecordUsageWhenEmpty = true` (image records regardless):
```csharp
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;

// (pragmas unchanged)

internal sealed class AITrackingImageGenerationClient : AIBoundImageGeneratorBase
{
    private readonly IAIOperationTracker _tracker;

    public AITrackingImageGenerationClient(IImageGenerator innerGenerator, IAIOperationTracker tracker)
        : base(innerGenerator) => _tracker = tracker;

    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request, ImageGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var descriptor = new AIOperationDescriptor
        {
            Capability = AICapability.ImageGeneration,
            PromptData = BuildPromptData(request, options),
            Metadata = null,
            RecordUsageWhenEmpty = true,
        };

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var response = await base.GenerateAsync(request, options, token);
                var imageCount = response.Contents?.Count(c => c is DataContent or UriContent) ?? 0;
                return new AITrackedOperationResult<ImageGenerationResponse>
                {
                    Result = response,
                    Usage = response.Usage,
                    AuditResponse = new AIAuditResponse { Data = $"{imageCount} image(s)" },
                };
            },
            cancellationToken);

        return tracked.Result;
    }

    private static object BuildPromptData(ImageGenerationRequest request, ImageGenerationOptions? options) => /* unchanged */;
}
```

- [ ] **Step 2: Repoint `InvokeWithTrackingAsync`.**

In `AIImageGenerationService.cs`, change the injected field from `AIImageGenerationTracker` to `IAIOperationTracker`, and convert the public `AITrackedImageResult<TResult>` returned by the caller's delegate into the internal result:
```csharp
        var descriptor = new AIOperationDescriptor
        {
            Capability = AICapability.ImageGeneration,
            PromptData = promptData,
            Metadata = null,
            RecordUsageWhenEmpty = true,
        };

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var r = await operation(generator, token);   // r is AITrackedImageResult<TResult> (public)
                return new AITrackedOperationResult<TResult>
                {
                    Result = r.Result,
                    Usage = r.Usage,
                    AuditResponse = new AIAuditResponse { Data = $"{r.ImageCount ?? 0} image(s)" },
                };
            },
            ct);

        return new AITrackedImageResult<TResult> { Result = tracked.Result, Usage = /* preserve */, ImageCount = /* preserve */ };
```
> Implementer: read the current `InvokeWithTrackingAsync` body (lines 122-165) and preserve the surrounding scope/contributor/`PopulateProfileMetadata` logic verbatim — only the `_tracker.TrackAsync` call and result mapping change. Return the caller's original `AITrackedImageResult` values (capture them before mapping).

- [ ] **Step 3: Delete `AIImageGenerationTracker.cs`.**
```bash
git rm Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/AIImageGenerationTracker.cs
```

- [ ] **Step 4: Build + run image tests.**

Run: `dotnet test Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Umbraco.AI.Tests.Unit.csproj --filter FullyQualifiedName~Image`
Expected: PASS. Fix any test that referenced `AIImageGenerationTracker` by name (retarget to `IAIOperationTracker` behavior).

- [ ] **Step 5: Commit.**
```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(core): Route image generation through IAIOperationTracker

Delete AIImageGenerationTracker; the tracking middleware and the
InvokeWithTrackingAsync escape hatch now use the generic tracker. Public
AITrackedImageResult<T> is unchanged. Image now enriches the ambient
Activity for parity with the other capabilities.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Collapse Chat into one tracker-backed client

**Files:**
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AITrackingChatClient.cs` (becomes the single tracker-backed client)
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AITrackingChatMiddleware.cs` (inject tracker)
- Delete: `Analytics/Usage/Middleware/AIUsageRecordingChatClient.cs` + `AIUsageRecordingChatMiddleware.cs`
- Delete: `AuditLog/Middleware/AIAuditingChatClient.cs` + `AIAuditingChatMiddleware.cs`
- Modify: `Configuration/UmbracoBuilderExtensions.cs:116-126` (drop two `.Append<>` lines)
- Test: chat usage/audit tests under `tests/Umbraco.AI.Tests.Unit/` (migrate class-coupled ones)

**Interfaces:**
- Consumes: `IAIOperationTracker`, `AIOperationDescriptor`, `AITrackedOperationResult<ChatResponse>`, `AIAuditMetadata.ExtractFromRuntimeContext`, `AIAuditResponse`.
- Produces: `AITrackingChatMiddleware` now injects `IAIOperationTracker` + `IAIRuntimeContextAccessor`.

- [ ] **Step 1: Rewrite `AITrackingChatClient`** to do the full non-streaming + streaming tracking via the tracker. Chat: `RecordUsageWhenEmpty = false`, audit response = `{ Data = messages, Usage = response.Usage }`, metadata from runtime context.
```csharp
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat.Middleware;

internal sealed class AITrackingChatClient : AIBoundChatClientBase
{
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingChatClient(IChatClient innerClient, IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
        : base(innerClient)
    {
        _tracker = tracker;
        _contextAccessor = contextAccessor;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var descriptor = BuildDescriptor(messages);

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var response = await base.GetResponseAsync(messages, options, token);
                return new AITrackedOperationResult<ChatResponse>
                {
                    Result = response,
                    Usage = response.Usage,
                    AuditResponse = new AIAuditResponse { Data = response.Messages, Usage = response.Usage },
                };
            },
            cancellationToken);

        return tracked.Result;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var descriptor = BuildDescriptor(messages);

        var scope = await _tracker.BeginAsync(descriptor, cancellationToken);
        var updates = new List<ChatResponseUpdate>();
        Exception? captured = null;

        // yield cannot sit inside try/catch, so drive the enumerator manually (matches prior behavior).
        await using var enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                ChatResponseUpdate current;
                try
                {
                    if (!await enumerator.MoveNextAsync()) break;
                    current = enumerator.Current;
                }
                catch (Exception ex) { captured = ex; break; }

                updates.Add(current);
                yield return current;
            }

            if (captured is not null)
            {
                await scope.FailAsync(captured);
                throw captured;
            }

            var aggregated = updates.ToChatResponse();
            await scope.CompleteAsync(
                aggregated.Usage,
                new AIAuditResponse { Data = aggregated.Messages, Usage = aggregated.Usage });
        }
        finally
        {
            scope.Dispose();
        }
    }

    private AIOperationDescriptor BuildDescriptor(IReadOnlyList<ChatMessage> messages) => new()
    {
        Capability = AICapability.Chat,
        PromptData = messages,
        Metadata = AIAuditMetadata.ExtractFromRuntimeContext(_contextAccessor.Context),
        RecordUsageWhenEmpty = false,
    };
}
```
> Behavior parity: non-streaming records usage from `response.Usage` (skipped when null, `RecordUsageWhenEmpty=false`), audit completes with messages+usage. Streaming aggregates via `ToChatResponse()` exactly as the old `AITrackingChatClient`, then completes; on mid-stream exception it fails the audit (`CancellationToken.None` inside the scope) and rethrows — matching the old auditing client's `WrapStreamWithErrorCapture`.

- [ ] **Step 2: Update `AITrackingChatMiddleware`** to inject and pass through:
```csharp
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat.Middleware;

public sealed class AITrackingChatMiddleware : IAIChatMiddleware
{
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingChatMiddleware(IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
    {
        _tracker = tracker;
        _contextAccessor = contextAccessor;
    }

    public IChatClient Apply(IChatClient client) => new AITrackingChatClient(client, _tracker, _contextAccessor);
}
```

- [ ] **Step 3: Remove the two dropped middleware from the chat pipeline.** In `UmbracoBuilderExtensions.cs`, delete lines 124-125 (`.Append<AIUsageRecordingChatMiddleware>()` and `.Append<AIAuditingChatMiddleware>()`). The chat pipeline becomes: `... AIGuardrailChatMiddleware → AITrackingChatMiddleware → AIContextInjectingChatMiddleware`.

- [ ] **Step 4: Delete the four dead files.**
```bash
git rm Umbraco.AI/src/Umbraco.AI.Core/Analytics/Usage/Middleware/AIUsageRecordingChatClient.cs \
       Umbraco.AI/src/Umbraco.AI.Core/Analytics/Usage/Middleware/AIUsageRecordingChatMiddleware.cs \
       Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingChatClient.cs \
       Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingChatMiddleware.cs
```

- [ ] **Step 5: Build + migrate/run chat tests.**

Run: `dotnet test Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Umbraco.AI.Tests.Unit.csproj --filter "FullyQualifiedName~Chat"`
Expected: PASS. Tests referencing `AIUsageRecordingChatClient`/`AIAuditingChatClient` by type must be retargeted to `AITrackingChatClient` (behavior assertions on queued usage/audit are unchanged — same mocks, same expectations).

- [ ] **Step 6: Commit.**
```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(core): Collapse chat usage/audit wrappers into one tracker-backed client

AITrackingChatClient now records usage + audit via IAIOperationTracker,
replacing AIUsageRecordingChatClient and AIAuditingChatClient. Streaming
aggregation and error-capture semantics preserved.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Collapse Embedding into one tracker-backed client

Mirrors Task 3. Embedding differences: metadata comes from `options.AdditionalProperties` (not runtime context); audit response = `{ Data = embeddings, Usage = usage }`; `RecordUsageWhenEmpty = false`; no streaming.

**Files:**
- Modify: `Chat/Middleware/AITrackingEmbeddingGenerator.cs` (becomes single tracker-backed generator)
- Modify: its middleware `AITrackingEmbeddingMiddleware` (inject tracker)
- Delete: `Analytics/Usage/Middleware/AIUsageRecordingEmbeddingGenerator.cs` + its middleware
- Delete: `AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs` + its middleware
- Modify: `Configuration/UmbracoBuilderExtensions.cs:128-132` (drop two `.Append<>` lines)
- Test: embedding tests (migrate class-coupled ones)

**Interfaces:**
- Consumes: `IAIOperationTracker`, `AIOperationDescriptor`, `AITrackedOperationResult<GeneratedEmbeddings<Embedding<float>>>`.
- Produces: `AITrackingEmbeddingMiddleware` injects `IAIOperationTracker`.

- [ ] **Step 1: Rewrite `AITrackingEmbeddingGenerator<string, Embedding<float>>`** — single `GenerateAsync` via `tracker.TrackAsync`:
```csharp
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var valueList = values.ToList();
        var descriptor = new AIOperationDescriptor
        {
            Capability = AICapability.Embedding,
            PromptData = valueList,
            Metadata = ExtractMetadataFromOptions(options),
            RecordUsageWhenEmpty = false,
        };

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var result = await base.GenerateAsync(valueList, options, token);
                return new AITrackedOperationResult<GeneratedEmbeddings<Embedding<float>>>
                {
                    Result = result,
                    Usage = result.Usage,
                    AuditResponse = new AIAuditResponse { Data = result, Usage = result.Usage },
                };
            },
            cancellationToken);

        return tracked.Result;
    }
```
> `ExtractMetadataFromOptions` reproduces the current embedding LogKeys extraction from `options.AdditionalProperties` (see `AIAuditingEmbeddingGenerator.cs:49-56`). `result.Usage` is the M.E.AI `GeneratedEmbeddings.Usage`; the old code read `LastUsageDetails` which was set from the same source — verify equivalence when implementing (the tracking generator captured `response.Usage`).
> **Verify at implementation:** the old audit response `Data` was `LastEmbeddings` (the embeddings collection). Confirm `result` (the `GeneratedEmbeddings`) serializes identically, or set `Data = result` to the same shape the old `LastEmbeddings` held.

- [ ] **Step 2:** Inject `IAIOperationTracker` into `AITrackingEmbeddingMiddleware.Apply` (same shape as Task 3 Step 2).
- [ ] **Step 3:** Drop lines 131-132 (`AIUsageRecordingEmbeddingMiddleware`, `AIAuditingEmbeddingMiddleware`) from the embedding pipeline.
- [ ] **Step 4:** `git rm` the four dead embedding files.
- [ ] **Step 5:** Build + run embedding tests: `dotnet test ... --filter "FullyQualifiedName~Embedding"` → PASS (migrate class-coupled tests).
- [ ] **Step 6:** Commit: `refactor(core): Collapse embedding usage/audit wrappers into one tracker-backed generator` (+ trailer).

---

## Task 5: Collapse SpeechToText into one tracker-backed client

Mirrors Task 3. STT differences: no token usage (`Usage = null`, `RecordUsageWhenEmpty = true`); audit response = `{ Data = text }` (no usage); prompt data = `BuildPromptData(options)`; metadata from runtime context; has streaming.

**Files:**
- Modify: `SpeechToText/AITrackingSpeechToTextClient.cs` (single tracker-backed client)
- Modify: its middleware (inject tracker)
- Delete: `Analytics/Usage/Middleware/AIUsageRecordingSpeechToTextClient.cs` + middleware
- Delete: `AuditLog/Middleware/AIAuditingSpeechToTextClient.cs` + middleware
- Modify: `Configuration/UmbracoBuilderExtensions.cs:134-138` (drop two `.Append<>` lines)
- Test: STT tests (migrate class-coupled ones)

**Interfaces:**
- Consumes: `IAIOperationTracker`, `AIOperationDescriptor`, `AITrackedOperationResult<SpeechToTextResponse>`, `AIAuditMetadata`.
- Produces: STT tracking middleware injects `IAIOperationTracker` + `IAIRuntimeContextAccessor`.

- [ ] **Step 1: Rewrite `AITrackingSpeechToTextClient`.** Non-streaming via `TrackAsync`; streaming via `BeginAsync` + manual enumeration concatenating `update.Text` (reproduce the old `LastTranscriptionText` aggregation), then `CompleteAsync(usage: null, new AIAuditResponse { Data = text })`. Keep `BuildPromptData(options)` (move it here from the deleted auditing client). `#pragma warning disable MEAI001`.
```csharp
    // non-streaming
    var descriptor = new AIOperationDescriptor
    {
        Capability = AICapability.SpeechToText,
        PromptData = BuildPromptData(options),
        Metadata = AIAuditMetadata.ExtractFromRuntimeContext(_contextAccessor.Context),
        RecordUsageWhenEmpty = true,
    };
    var tracked = await _tracker.TrackAsync(descriptor, async token =>
    {
        var response = await base.GetTextAsync(audioSpeechStream, options, token);
        return new AITrackedOperationResult<SpeechToTextResponse>
        {
            Result = response,
            Usage = null,
            AuditResponse = new AIAuditResponse { Data = response.Text },
        };
    }, cancellationToken);
    return tracked.Result;
```
Streaming: same `BeginAsync` + manual-enumerator pattern as Task 3 Step 1, but accumulate `update.Text` via `string.Concat`, and on completion `await scope.CompleteAsync(null, new AIAuditResponse { Data = concatenatedText })`.

- [ ] **Step 2:** Inject tracker + context accessor into the STT tracking middleware.
- [ ] **Step 3:** Drop lines 137-138 from the STT pipeline.
- [ ] **Step 4:** `git rm` the four dead STT files.
- [ ] **Step 5:** Build + run STT tests: `dotnet test ... --filter "FullyQualifiedName~SpeechToText"` → PASS.
- [ ] **Step 6:** Commit: `refactor(core): Collapse speech-to-text usage/audit wrappers into one tracker-backed client` (+ trailer).

---

## Task 6: Final verification + dead-code sweep

**Files:** none new; verification only.

- [ ] **Step 1: Confirm no lingering references** to deleted types:
```bash
grep -rn "AIImageGenerationTracker\|AIUsageRecording\(Chat\|Embedding\|SpeechToText\)\|AIAuditing\(Chat\|Embedding\|SpeechToText\)\|LastUsageDetails\|LastResponseMessages\|LastEmbeddings\|LastTranscriptionText" Umbraco.AI/src Umbraco.AI/tests
```
Expected: no matches in `src`; any in `tests` must be migrated.

- [ ] **Step 2: Full solution build.**
Run: `dotnet build Umbraco.AI/Umbraco.AI.slnx`
Expected: PASS, no warnings about unused usings in edited files.

- [ ] **Step 3: Full test run.**
Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx`
Expected: PASS (all green).

- [ ] **Step 4: Behavioral spot-check via the demo site (optional but recommended).** Start the demo site, run a chat + a speech-to-text + an image generation, and confirm audit-log entries and usage records still appear with the same fields (profile/model/provider/feature, TraceId, duration, tokens where applicable). Use `/demo-site-management` + `/demo-site-automation`.

- [ ] **Step 5: Verify the acceptance criteria** in the spec §10 are all checked.

- [ ] **Step 6: Finish the branch.** Invoke `superpowers:finishing-a-development-branch` (PR vs. local merge). Then resolve the two flagged questions with the user before merge:
  - Accept the §7 image Activity-enrichment delta (or gate it off)?
  - Backport to `v17/dev` (per CLAUDE.md multi-version sync policy)?

---

## Self-review

- **Spec coverage:** §5.1 tracker → Task 1; §5.2 collapse → Tasks 3-5; §5.3 image → Task 2; §5.4 DI/ordering → Tasks 1-5 (per-capability); §4 faithfulness table → Global Constraints + per-task descriptors; §7 delta → Global Constraints + Task 2/6; §8 testing → Task 1 unit tests + per-task migration + Task 6. All covered.
- **Placeholder scan:** two intentional "verify at implementation" notes remain in Task 4 (embedding `Usage`/`Data` equivalence) — these are genuine correctness checks the implementer must confirm against the old `LastUsageDetails`/`LastEmbeddings` values, not vague hand-waving; kept deliberately. `BuildPromptData` bodies are marked "unchanged" with the exact source location. No "TBD"/"handle edge cases" placeholders.
- **Type consistency:** `AIOperationDescriptor`, `AITrackedOperationResult<T>`, `AIOperationScope.CompleteAsync(UsageDetails?, AIAuditResponse?)`, `IAIOperationTracker.TrackAsync/BeginAsync`, `AIAuditMetadata.ExtractFromRuntimeContext` — names/signatures consistent across Tasks 1-5. `AIAuditResponse { Data, Usage }` and `AIAuditPrompt { Data, Capability }` match the existing types read from source.
