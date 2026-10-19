# Architecture

## Today

```text
Tracking client (chat / embedding / STT / image / decision)
└── AIOperationTracker.BeginAsync
      ├── audit:     extract AIAuditContext, create entry (parent = AIAuditScope.Current),
      │              set TraceId, QueueStartAuditLogAsync
      ├── tracing:   AIActivityEnricher.EnrichCurrentActivity
      └── identity:  extract AIUsageContext
    → AIOperationScope
      ├── EnterAuditScope()                  # caller wraps the work (added in #532)
      └── CompleteAsync / FailAsync
            ├── stopwatch
            ├── tracker.ReportUsage(AIUsageObservation)
            │     ├── CollectUsage   → AIUsageCollectionScope.Current?.RecordCall
            │     └── RecordUsageAsync → analytics on/off check, record factory, queue
            └── audit: QueueCompleteAuditLogAsync / QueueRecordAuditLogFailureAsync
```

The tracker owns audit, tracing and analytics details. `AIOperationScope` reaches back into
`tracker.AuditLogService`.

## Target

```text
Tracking client                         (unchanged)
└── AIOperationTracker.BeginAsync
      ├── stopwatch starts
      ├── AIOperationStart: descriptor + identity captured once
      └── for each recorder: recording = await recorder.BeginAsync(start)
    → AIOperationScope
      ├── EnterScope()      → each recording's EnterScope(), combined
      └── CompleteAsync / FailAsync
            ├── stopwatch stops
            ├── AIOperationOutcome: status, usage, duration, error, response data (built once)
            └── for each recording: await recording.EndAsync(outcome)

Recorders (internal, registered in order):
  AIAuditOperationRecorder       entry, parent link, audit scope, start/end status
  AITraceOperationRecorder       Activity tags (today's AIActivityEnricher call)
  AIAnalyticsOperationRecorder   on/off check, RecordUsageWhenEmpty, record factory, queue
  AITestUsageOperationRecorder   AIUsageCollectionScope.Current?.RecordCall
```

## Contracts (all internal)

```csharp
internal interface IAIOperationRecorder
{
    /// Called when a tracked call starts. Returns the per-call recording, or null to skip the call.
    ValueTask<IAIOperationRecording?> BeginAsync(AIOperationStart start, CancellationToken cancellationToken);
}

internal interface IAIOperationRecording
{
    /// Opens anything that must be ambient while the work runs (only audit uses this today).
    IDisposable? EnterScope() => null;

    /// Called once when the call ends. Never throws into the AI call.
    ValueTask EndAsync(AIOperationOutcome outcome);
}

internal sealed record AIOperationStart(
    AIOperationDescriptor Descriptor,
    AIUsageContext? Identity,
    AIRuntimeContext? RuntimeContext);   // for audit's own extraction until step 5 unifies them

internal sealed record AIOperationOutcome(
    AIOperationStatus Status,            // Succeeded | Failed (Blocked added in step 5)
    UsageDetails? Usage,
    long DurationMs,
    Exception? Exception,
    AIAuditResponse? Response);
```

`AIUsageObservation` (#517) folds into `AIOperationStart` + `AIOperationOutcome`.

## Decisions

These change behaviour slightly, or pick between two reasonable options. Each one is listed
again in `PLAN.md` at the step that makes it. All five were confirmed on 08-10-2026 (see
`DECISION-LOG.md`).

1. **Recorder failures are isolated.** A recorder that throws in `BeginAsync` or `EndAsync` is
   logged and skipped; the AI call carries on. Today one case doesn't do this:
   `AIAuditLogFactory.Create` throws when the profile keys are missing, and that exception
   escapes `BeginAsync` into the AI call. Isolating it means a missing profile no longer fails
   the call, it only loses the audit entry (with an error logged).
2. **End writes use `CancellationToken.None`.** Analytics currently queues with the call's own
   token, which loses the record when the call was cancelled (#531). All recordings end with
   `None`, as audit already does. This fixes #531.
3. **Order is fixed and documented**: audit, trace, analytics, test usage. Audit first so its
   scope and entry exist before anything else; today usage is reported before the audit end
   status is queued, and that order flips. Nothing depends on it (separate tables, separate
   queue items), but it is a visible change in the queue.
4. **Registration is a plain ordered list in DI**, not a collection builder. Collection builders
   are Umbraco's public extension pattern, and this is deliberately not an extension point yet.
5. **Blocked is a real outcome** (step 5). Guardrail blocks are `Blocked` in audit but `Failed`
   in analytics today. Analytics records still store a string status, so `Blocked` would appear
   there as a third value. The dashboard counts only `Succeeded`/`Failed`, so it would need to
   count `Blocked` as a failure to keep today's totals.
