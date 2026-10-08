# Decision log

- 08-10-2026: Recorders are internal, not Umbraco notifications. Notifications are a public
  contract, and the test-usage recorder needs to run in the caller's own flow to see the
  ambient collector. Public hook points can come later if a need shows up.
- 08-10-2026: Built after the Copilot release, since every AI call goes through this code.
  Copilot shipped stable (18.1.1 / 17.1.1) on 08-10-2026, so the hold is lifted.
- 08-10-2026: The audit parent bug (#529) and the dashboard gap (#530) were fixed on their own
  first, both lines (#532/#533, #534/#535). #531 (usage lost on cancel) is left for T1, where the
  analytics recorder's end write moves to `CancellationToken.None`.
- 08-10-2026: Test coverage of today's behaviour is already strong, so "pin behaviour first" is
  the first commit of T1 rather than its own PR. Gaps listed in `PLAN.md`.
- 08-10-2026: Five design calls need confirming before T1 starts. See "Decisions to confirm" in
  `ARCHITECTURE.md`.
- 08-10-2026: All five confirmed by the maintainer: (1) recorder failures are isolated, including
  the audit factory's missing-profile throw; (2) every end write uses `CancellationToken.None`;
  (3) fixed order audit, trace, analytics, test usage; (4) plain internal ordered DI list, not a
  collection builder; (5) `Blocked` becomes a real outcome, counted as a failure on the dashboard.
- 08-10-2026 (T1, #537): `AIUsageRecordingService` is left with one job, queueing a save. Kept as
  is rather than merged into the analytics recorder (a recorder must not touch a repository) or
  made generic (only one example so far). Revisited in T5, once audit has moved and there is a
  second example to compare.
- 08-10-2026: Added T5, an explicit final review of thin types, stale names, stranded members and
  docs, so these calls aren't made piecemeal in each refactor PR.
- 08-10-2026 (T2): `IAIAuditLogService` is public, so its unused synchronous write methods are
  marked `[Obsolete]` (removal in v20) instead of removed, and the parent fallback inside
  `QueueStartAuditLogAsync` stays for external callers. The tracker itself no longer reads
  `AIAuditScope`.
- 08-10-2026 (T2): The trace recorder can't see the audit entry, so the audit recorder tags the
  Activity with its own entry ID (the link belongs to the entry's owner) and the trace recorder
  reads the user from the back-office user, as the audit factory does. Side effect: the user tag
  is now set when auditing is off too. The profile ID tag is no longer set when the ID is empty.
- 08-10-2026 (T2): `AIOperationOutcome` carried an `AIAuditResponse`, which leaked audit into the
  neutral outcome and made every tracking client build an audit type. Replaced with
  `object? ResponseData` on both the outcome and `AITrackedOperationResult`; the audit recorder
  builds `AIAuditResponse` from it plus the outcome's usage. This also removes usage being passed
  twice per call (#528). `AIAuditResponse` stays: the public audit service uses it.
- 08-10-2026 (T3): `AIUsageRecordResult` is public with a required `Succeeded`, so `Blocked` is an
  added optional flag rather than a change to `Succeeded`. Usage records store "Blocked"; hourly
  aggregation and live statistics count it in `FailureCount`, so dashboard totals are unchanged.
  The audit log still decides `Blocked` from the exception in the public
  `QueueRecordAuditLogFailureAsync`; it agrees with the tracker because both check
  `AIGuardrailBlockedException`.
- 08-10-2026 (T3): The usage record status became an enum (`AIUsageRecordStatus`), stored by name in
  the existing string column, so no migration. This changes the type of the public
  `AIUsageRecord.Status`; accepted by the maintainer because `AIUsageRecord` is only produced and
  consumed through internal interfaces. Package validation is off for every product, so there is
  no compatibility suppression file to update.
- 08-10-2026 (T4): "Error category worked out twice" dropped. The error-classifying clients set a
  provider category; `AIAuditLogService.CategorizeError` only maps that category to the audit
  enum, and falls back to reading the message text for exceptions no classifier saw (tool errors,
  cancellations). Removing the fallback would leave those as Unknown, for no gain.
- 08-10-2026 (T4): `RecordUsageWhenEmpty` removed, not re-documented: every client set it to true,
  so its "skip" branch never ran. Calls are always recorded, with or without token counts.
- 08-10-2026 (T4): The captured identity (`AIUsageContext`) gained `ProfileVersion` and
  `FeatureVersion`, so the audit and trace recorders build from it and the runtime context is read
  once per call. `AIOperationStart.RuntimeContext` is gone. The name `AIUsageContext` now
  undersells it; left for T5's naming review.
- 08-10-2026 (T5): No shared "queue a repository save" helper. `AIAuditLogService`'s queue methods
  do real work before queueing (status, snapshots, redaction, error category), so
  `AIUsageRecordingService` is the only thin one; one example doesn't justify a helper.
- 08-10-2026 (T5): `AIUsageRecordingService` keeps its name. It is internal and "recording" still
  fits (it records usage by queueing the save); a rename is churn without a reader benefit.
- 08-10-2026 (T5): `AIUsageContext` keeps its name, as it is public; its doc comment now says it is
  the call's identity, shared by every recorder.
- 08-10-2026 (T5): The declared log values (LogKeys) moved from the tracking clients into the
  tracker's one read of the runtime context (`AIOperationStart.LogValues`). The clients no longer
  know about the audit log, and image audit entries now get the log values too.
- 08-10-2026 (T5): The unused optional `modelId` parameters on the public
  `AIUsageContext`/`AIAuditContext.ExtractFromRuntimeContext` methods are left: removing an
  optional parameter is a binary break and they cost nothing. Revisit if T6's validation flags
  them for another reason.
- 08-10-2026 (T5): The recorder contracts have nothing unused: `EnterScope` is used by audit,
  `ResponseData` by audit, `Blocked` by audit and analytics.
- 08-10-2026 (T6): Package validation is on for every product through a shared
  `PackageValidation.props`, baselined on `X.0.0` with X read from the product's `version.json`.
  Every product has an `X.0.0` on NuGet, so no per-product baseline was needed.
- 08-10-2026 (T7): Package validation only turns on once a product's version is past `X.0.0`. The
  SDK downloads the baseline at restore, so the T6 version broke restore for a new product and for
  every product after a major cutover, until `X.0.0` shipped. The post-release bump to `X.0.1` turns
  it on.
- 08-10-2026 (T6): Every break since 18.0.0 was accepted as baseline suppressions rather than
  restored, as none are meant for outside callers:
  - The chat, embedding and speech-to-text tracking middleware became internal (this refactor).
  - `AIUsageRecord.Status` became `AIUsageRecordStatus` (T3).
  - `IAIUmbracoMediaResolver.GetMediaType` and the `approvalPolicy` parameter on
    `IAIAgentFactory.CreateAgentAsync` only break custom implementations of those interfaces.
    The agent factory change already shipped in Agent 18.1.0, and nothing outside Agent calls it.
  - Changed constructors on the five content tools, `AIUsageTelemetryProvider`,
    `ProviderMapDefinition` and `CapabilitiesConnectionController` are only called by DI.
- 08-10-2026: Added T7. The refactor was built by hand rather than through `umb-build-loop`, so no
  task had its reviewer gate. T7 runs the playbook's reviewer agent over every task's diff at the
  end, compares the work with how the build loop would have shaped it, and writes the missing
  `BUILD-LOG.md`.
- 08-10-2026: Added T6, re-enabling package validation at the end, baselined on each branch's
  major base release (18.0.0 / 17.0.0), with suppressions for the breaks accepted along the way.
- 08-10-2026 (T3): A non-streamed call that returns a failure (response ending on `ErrorContent`)
  is signalled through an optional `AITrackedOperationResult.Failure`, so the caller still gets the
  response while recorders see a failed call. Same rule the streaming path already applied.
- 08-10-2026 (T2): `IAIOperationRecording.EnterScope` returns `IDisposable?` rather than
  `AIAuditScope?`, and `AIOperationScope.EnterScope` opens every recording's scope.
