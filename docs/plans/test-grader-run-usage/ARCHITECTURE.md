# Architecture

## Extension points

None new. This hooks into two existing internal seams in `Umbraco.AI.Core`:

1. **`IAIOperationTracker` / `AIOperationScope`** (`Core/Observability/`). Every tracked AI call
   (chat, embedding, image generation, speech-to-text) ends in `AIOperationScope.CompleteAsync`
   or `FailAsync`, with the call's `UsageDetails` and the runtime context still in scope. That
   context already holds `ProviderId`, `ModelId`, `ProfileId` and `ProfileAlias`, set by
   `ScopedProfileChatClient` (and its embedding/image/STT siblings).
2. **`AITestRunner.ExecuteSingleRunAsync`** (`Core/Tests/`). Wraps the feature execution in a
   usage collection scope and copies the result onto `AITestOutcome.Usage` before grading.

Checked facts that make this work:

- The chat middleware order puts `AITrackingChatMiddleware` **outside**
  `AIFunctionInvokingChatMiddleware`. So one tracked call covers a whole tool-calling loop, and
  M.E.AI's `FunctionInvokingChatClient` already sums usage across its iterations into the
  response it returns. Nothing is double counted.
- The Agent test path (`AgentTestFeature` → `StreamAgentAGUIAsync` → `AIAgentFactory` →
  `IAIChatClientFactory.CreateClientAsync(profile)`) gets the full core middleware pipeline,
  tracking included. It runs in the same async flow as the runner, so an `AsyncLocal` scope
  opened by the runner reaches it.
- The Prompt test path also uses profile chat clients, so it is covered the same way.
- A tool that itself calls a model (a sub-agent or prompt tool) is tracked too, and correctly
  counts toward the run.

## Data model & persistence

Usage lives on a new `AITestOutcome.Usage` (`AITestUsage`), persisted as JSON in
`AITestRunEntity.OutcomeUsageJson`. That column was `OutcomeTokenUsageJson`, which only ever held
null because the old `TokenUsage` was never populated. A rename migration
(`UmbracoAI_RenameTestRunOutcomeUsageColumn`, SQL Server `20261008102116`, SQLite
`20261008102119`) renames it in place. The v17 backport must reuse these exact migration IDs.

`AITestOutcome.TokenUsage` and `AITestTokenUsage` keep their original shape, are never set, and
are `[Obsolete]` (removal in v20).

```
AITestOutcome
├── TokenUsage : AITestTokenUsage?          (existing, always null, obsolete)
└── Usage : AITestUsage?                     (new, null when no tracked call)
    ├── InputTokens / OutputTokens / TotalTokens   (summed over the run)
    ├── CallCount
    ├── UnreportedCallCount                        (calls that returned no usage)
    ├── FailedCallCount                            (calls that ended in FailAsync)
    ├── DurationMs                                 (summed AI call time, not wall-clock)
    └── Breakdown : List<AITestUsageEntry>
        ├── Capability, ProviderId, ModelId, ProfileId?, ProfileAlias?
        ├── FeatureType?, FeatureId?, FeatureAlias?
        ├── InputTokens / OutputTokens / TotalTokens
        └── CallCount / UnreportedCallCount / FailedCallCount / DurationMs
```

## Connected systems

- **Persistence:** one JSON column, renamed by migration (see above). Migration IDs must match on
  v17.
- **Management API:** new `outcome.usage` (`TestUsageResponseModel`, `TestUsageEntryResponseModel`).
  `outcome.tokenUsage` (`TestTokenUsageResponseModel`) keeps its shape, is never set and is
  obsolete. The generated TypeScript client is regenerated.
- **Backoffice UI:** the run detail view prints `outcome.usage` as raw JSON (one-line switch from
  `tokenUsage`). A proper table is follow-up #523.
- **Usage analytics:** shares one usage context with the collector, captured once in
  `BeginAsync` (as the audit log already did). This fixes analytics crediting tokens to a nested
  call's model. The collector runs independent of the analytics switch, so it works when analytics
  is off.
- **Audit log, OpenTelemetry:** unchanged.
- **Deploy:** not applicable. Test runs are not deployed.
- **Version history, notifications, search, cache refreshers:** not applicable. No new entity.

## Key decisions

1. **Collect at the operation tracker, not in each test feature.**
   Rationale: one place sees every tracked call from every feature, including future and
   third-party features, with provider/model/profile already resolved.
   Rejected: having `IAITestFeature` return usage. That needs a public interface change, makes
   every feature (including third-party ones) responsible for counting, and Agent would have to
   rebuild usage from AG-UI events, which don't carry it.

2. **Ambient `AsyncLocal` collection scope, internal to Core.**
   New internal types `AIUsageCollectionScope` (opened with `using`) and `AIUsageCollector`
   (thread-safe accumulator) in `Core/Observability/`. `AIOperationScope.CompleteAsync`/`FailAsync`
   report to the current collector, if any, before the fire-and-forget analytics record.
   Rationale: matches the existing `AIAuditScope` / runtime context scope pattern. Scopes nest
   by restoring the parent on dispose, and concurrent test executions in separate requests stay
   isolated.
   Rejected: a scoped DI service. Test runs can execute from background work without a request
   scope, and a DI scope doesn't follow the async flow into streamed agent calls reliably.
   > ASSUMPTION: Kept internal (per the "internal plumbing, public contracts" rule). Making it a
   > public extension point can come later if someone needs to collect usage outside tests.

3. **Usage lives on a new `AITestOutcome.Usage`, passed to graders through the existing `outcome`.**
   Rationale: `GradeAsync(transcript, outcome, config, ct)` already hands graders the outcome.
   Adding a property to a sealed class is non-breaking, so every existing grader keeps working
   with no signature change. The old `TokenUsage` was always null, so it is simply left null and
   obsoleted rather than repurposed under a name that only says tokens.
   Rejected: a new grader context type or `GradeAsync` overload. More public surface, and needs
   the obsolete-and-proxy dance, for no gain.

4. **Resolved profile comes from the breakdown, not a new run field.**
   `AITestRun.ProfileId` stays as-is (the variation override, null when none). The profile each
   call actually resolved to is on each `Breakdown` entry.
   Rejected: a new `ResolvedProfileId` on the run, which would need a column and a migration.

5. **Grader calls are excluded by scope timing.** The collection scope is disposed before
   `GradeOutcomeAsync` runs, so an LLM-judge grader's own calls never count.

6. **"Unknown" is not "zero".** `Usage` is `null` only when the run made no tracked call.
   A call that returned no `UsageDetails` increments `UnreportedCallCount` instead of adding
   zeros silently, so a budget grader can tell the total is incomplete.

7. **All capabilities, not only chat.** The tracker sees embedding, image and speech calls too,
   so they're included at no extra cost, each tagged with its `Capability`. `TotalTokens` sums
   everything that reported tokens.
   > ASSUMPTION: Model ID comes from the profile (runtime context), matching usage analytics,
   > not from `ChatResponse.ModelId`. Keeps one source of truth with the analytics dashboard.

8. **Errored runs record no usage.** If the feature throws, the run goes to `Error` with no
   outcome, as today. Graders never run on an errored run, so there is no reader for partial
   usage yet.
   Rejected: creating an outcome on the error path just to hold usage. It changes what the UI
   and API show for errored runs, for no current consumer.

> ASSUMPTION: No cached-input or reasoning token fields yet. Cheap to add later, since the
> object is JSON-persisted.
