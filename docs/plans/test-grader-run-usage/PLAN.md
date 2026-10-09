# Plan

Build on `v18/feature/test-grader-run-usage-516`, then backport to `v17/dev` (T7).
Paths are relative to `Umbraco.AI/`. Unit tests go in `tests/Umbraco.AI.Tests.Unit/`.

- [x] **T1** — Usage model types. Add `CallCount`, `UnreportedCallCount`, `Models` (non-null list)
  to `AITestTokenUsage`; add sealed `AITestModelTokenUsage` (`src/Umbraco.AI.Core/Tests/`).
  Tests: JSON round-trip with `Constants.DefaultJsonSerializerOptions`, and old JSON without the
  new fields loads with an empty `Models` and `CallCount` 0.
  story: S2, S3 (AC3.1, AC3.4) · depends-on: — · parallel-group: A

- [x] **T2** — Internal usage collector. Add internal `AIUsageCollector` (thread-safe; groups by
  capability, provider, model, profile; counts unreported calls) and `AIUsageCollectionScope`
  (`AsyncLocal`, restores parent on dispose) in `src/Umbraco.AI.Core/Observability/`. Produces
  an internal snapshot, not a Tests type, so Observability stays independent of Tests.
  Tests (`Observability/AIUsageCollectorTests.cs`): grouping, sums, unreported, nesting (AC1.11),
  concurrent isolation (AC1.9).
  story: S1, S2 · depends-on: — · parallel-group: A

- [x] **T3** — wire: collector into the operation tracker. `AIOperationScope.CompleteAsync` and
  `FailAsync` report usage plus provider/model/profile (read from the runtime context, same
  keys as `AIUsageContext.ExtractFromRuntimeContext`) to the current collector, independent of
  the analytics enabled flag. No-op when no scope is open.
  Tests (`AIOperationTrackerTests.cs`): a tracked call inside a scope is collected, including with
  analytics disabled (AC1.4); no scope means no effect.
  story: S1 · depends-on: T2 · parallel-group: —

- [x] **T4** — wire: collector into `AITestRunner.ExecuteSingleRunAsync`. Open a scope around
  `testFeature.ExecuteAsync`, dispose it before grading, map the snapshot to
  `AITestOutcome.TokenUsage` (null when zero calls). Error path unchanged.
  Tests (`Tests/AITestRunnerTests.cs`, fake feature that drives tracked calls / a fake tracker
  scope): AC1.1–1.3, AC1.5–1.8, AC1.10, AC2.1–2.6. Grader signatures untouched (AC3.5).
  story: S1, S2, S3 · depends-on: T1, T3 · parallel-group: —

- [x] **T5** — wire: Management API. Add `CallCount`, `UnreportedCallCount`, `Models` to
  `TestTokenUsageResponseModel`, add `TestModelTokenUsageResponseModel`, map in
  `TestMapDefinition`. Regenerate the TypeScript client (`npm run generate-client`, needs the
  demo site running) and build `npm run build:core`.
  Tests: mapping test (AC3.2).
  story: S3 · depends-on: T1 · parallel-group: B (with T2/T3)

- [x] **T6** — wire: live check on the demo site. Run a prompt or agent test from the backoffice
  with a real chat profile; confirm the run detail view and the API response show populated
  `tokenUsage` with a `models` entry (AC3.3). Record the result in BUILD-LOG.md.
  story: S3 · depends-on: T4, T5 · parallel-group: —

- [x] **T8** — Analytics uses the call identity captured at `BeginAsync` (one read point for
  analytics and test collection; removes `AIOperationIdentity`). Specs: a nested call that rewrites
  the runtime context mid-call does not change the model analytics records, on success and failure.
  story: S2 · depends-on: T3 · parallel-group: —

- [x] **T9** — Split the usage breakdown by feature (type, ID, alias) and rename `Models` to
  `Breakdown` (core, API, TS client). `AIUsageContext` gains `FeatureAlias`. Specs: different
  features give separate entries; feature identity captured at begin; runner and mapping carry it.
  story: S2 · depends-on: T8 · parallel-group: —

- [x] **T10** — Breakdown entries and totals gain `DurationMs` and `FailedCallCount` from the
  tracker (core, API, TS client). Prompt transcript `usage` marked deprecated (removal v20).
  story: S1, S2 · depends-on: T9 · parallel-group: —

- [x] **T11** — Whole-feature review fixes: correct API/XML docs (grader calls excluded; TokenUsage
  describes counts, failures, duration, breakdown), stale tracker comment, Guid.Empty comment, named
  arguments for the collector entry, leftover per-model test names. Names kept.
  story: all · depends-on: T10 · parallel-group: —

- [x] **T12** — New `AITestOutcome.Usage` (`AITestUsage`, `AITestUsageEntry`); `TokenUsage` and
  `AITestTokenUsage` reverted to their dev shape, always null, `[Obsolete]` (v20). Same in the API
  (`usage` new, `tokenUsage` obsolete). Stored in the existing `OutcomeTokenUsageJson` column.
  story: all · depends-on: T11 · parallel-group: —

- [x] **T7** — Backport to `v17/dev` via the `backport` skill (separate worktree, draft PR #526).
  Confirm the v17 tracker/runner code matches before porting.
  story: all · depends-on: T6 and the v18 PR · parallel-group: —
