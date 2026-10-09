Closes #516 | [Plan folder](https://github.com/umbraco/Umbraco.AI/tree/v18/feature/test-grader-run-usage-516/docs/plans/test-grader-run-usage)

## Why the change

Test graders never saw how many tokens a run used or which model produced it, so cost, token-budget and CO2e graders were impossible; this adds `AITestOutcome.Usage` with the run's summed usage plus a breakdown by model and feature.

## Special things to note

- **Behaviour change in usage analytics (bug fix):** analytics used to read provider, model, profile and feature from the shared runtime context when a call *finished*. Nested calls rewrite that context partway through: the LLM guardrail judge (`AIGuardrailChatMiddleware` → `IAIChatService`) and the semantic search tool's embedding call. So the outer chat call's tokens could be logged against the judge or embedding model. `BeginAsync` now captures the usage context once, as the audit log already did, and both analytics and test collection use that single capture. Dashboard numbers may shift between models compared with before. Not live-checked on the demo site; covered by `AIOperationTrackerAnalyticsIdentityTests`.
- **Breakdown is split by feature as well as model.** Each entry carries `FeatureType`, `FeatureId` and `FeatureAlias`, so a grader can sum just the prompt's own call, just the guardrail judge calls (`inline-chat` / `guardrail-llm-evaluator`), or everything. To support this, the public `AIUsageContext` gains `FeatureAlias` (additive only).
- **Deprecation:** prompt tests still write their single call's usage into `transcript.FinalOutput.Usage`. It is kept so custom graders that read the transcript JSON keep working, and is documented as deprecated, to be removed in v20 (`PromptTestFeature.cs:193`). It is raw JSON, so `[Obsolete]` can't reach it; this needs a release-notes line. `outcome.Usage` covers every tracked call the feature made, and its `Breakdown` holds the prompt's own entry.
- **Duration and failures come from the tracker too.** Each entry and the totals carry `DurationMs` and `FailedCallCount`, using the same stopwatch value analytics already records (no second timer). `DurationMs` sums overlapping calls, so it is AI time, not wall-clock, and for streaming it includes the caller's time between chunks (as analytics always has). The features' own `timing` and `error` transcript JSON stays, since it covers the whole feature run and the error text.
- **New `Usage`, old `TokenUsage` obsoleted:** `AITestOutcome.TokenUsage` and `AITestTokenUsage` were public but never populated (the runner always set null, on v18 and v17). They keep their exact original shape, stay null, and are `[Obsolete("Always null. Use Usage instead. Will be removed in v20")]`. Same in the API: new `outcome.usage`, while `outcome.tokenUsage` stays null and obsolete. No proxy is needed because nothing ever had data in the old property. The repo's OpenAPI setup doesn't emit `deprecated`, so TS users get no hint; a release-notes line covers it.
- **Migration:** the test run column `OutcomeTokenUsageJson` (always null until now) is renamed to `OutcomeUsageJson` with a `RenameColumn` migration: SQL Server `20261008102116_UmbracoAI_RenameTestRunOutcomeUsageColumn`, SQLite `20261008102119_UmbracoAI_RenameTestRunOutcomeUsageColumn`. The v17 backport (#526) reuses these exact IDs. Integration tests build the schema with `EnsureCreated`, so the migration was checked with `dotnet ef migrations script` and on the demo site.
- The nested-call root cause (a nested call overwrites the parent's shared runtime context) is pre-existing and raised as #524. This PR only works around it for tracking, by capturing identity at `BeginAsync`.
- Grader-made calls (for example an LLM-judge grader) are deliberately **not** counted. The collection scope closes before grading (`AITestRunner.cs:270`). The live check confirmed this: the judge's 439/102 tokens were left out of the agent run's totals.
- A call that reports no usage, or a `UsageDetails` with all three counts null, adds to `CallCount` and `UnreportedCallCount` and not to the totals, so "unknown" is never shown as zero (`AIUsageCollector.cs:41`).
- All tracked capabilities are counted, not only chat. Embedding, image and speech calls get their own `Breakdown` entry tagged with `Capability`. The model ID comes from the profile (same source as the analytics dashboard), not from `ChatResponse.ModelId`.
- Public API changes are additive only (plus `[Obsolete]` on the never-populated `TokenUsage`). `IAITestGrader`, `AITestGraderBase` and the built-in graders are untouched, and graders read the data from the `outcome` they already receive. The collector types are `internal`.
- Errored runs are unchanged (status `Error`, no outcome, no usage).
- A stream that its consumer abandons partway is not collected. This matches analytics today. Test features always read streams to the end.
- Live-verified on the demo site with openai/gpt-4o (SQLite), on the final shape. The rename migration applied on an existing database and all 7 existing rows kept their JSON. A prompt test reported 1 call, 473/35 tokens, 3185 ms, one `Breakdown` entry with feature `prompt` / `seo-description`. An agent test reported 1 call, 5515/81, 1889 ms, feature `agent` / `content-assistant`. Both match the audit log exactly; the LLM-judge grader call was left out. `tokenUsage` was null throughout, and the run detail view shows the `usage` JSON. SQL Server was checked through the generated migration script only. Earlier, a failing prompt (OpenAI 400) reported 1 call, 1 unreported.
- v17 backport: #526 (draft). Same code and migration IDs; obsolete messages say v19 there.

## Change outline

The contract graders see grows, with additions only:

```diff
 AITestOutcome
 ├── TokenUsage : AITestTokenUsage?            // unchanged shape, always null, now [Obsolete]
+└── Usage : AITestUsage?                      // null when no tracked call
+    ├── InputTokens / OutputTokens / TotalTokens
+    ├── CallCount
+    ├── UnreportedCallCount
+    ├── FailedCallCount
+    ├── DurationMs                         // summed AI call time, not wall-clock
+    └── Breakdown : List<AITestUsageEntry>
+        ├── Capability, ProviderId?, ModelId?, ProfileId?, ProfileAlias?
+        ├── FeatureType?, FeatureId?, FeatureAlias?
+        ├── InputTokens / OutputTokens / TotalTokens
+        └── CallCount / UnreportedCallCount / FailedCallCount / DurationMs
```

The runner opens an ambient collection scope (`AsyncLocal`) around feature execution only:

```diff
 AITestRunner.ExecuteSingleRunAsync
-    transcript = await testFeature.ExecuteAsync(...)
+    using (var usageScope = AIUsageCollectionScope.Begin())
+    {
+        transcript = await testFeature.ExecuteAsync(...)
+        usage = usageScope.Collector.GetSnapshot()
+    }                                            // closed before grading
     outcome = new AITestOutcome {
-        TokenUsage = null
+        Usage = MapUsage(usage)                  // null when no tracked calls
     }
     GradeOutcomeAsync(transcript, outcome, ...)
```

Every tracked AI call feeds the open scope. It needs no changes to Prompt, Agent or third-party test features. There is one usage path: each finished call is captured once, then every consumer applies its own rules:

```
ScopedProfileChatClient            (writes profile/provider/model to runtime context)
└── AITrackingChatClient
    └── AIOperationTracker.BeginAsync   + captures AIUsageContext once here
        └── AIOperationScope.CompleteAsync / FailAsync
            └── + tracker.ReportUsage(new AIUsageObservation(...))     (one capture per call)
                  ├── + CollectUsage   → AIUsageCollectionScope.Current?.RecordCall(...)
                  │                      (whenever a scope is open, independent of analytics)
                  └──   RecordUsageAsync → persisted only when analytics is enabled
-                         (was: called separately, and read the live runtime context at completion)
```

New internal types in `Umbraco.AI.Core/Observability/`:

```diff
+AIUsageCollectionScope.cs       // AsyncLocal ambient scope, restores parent on dispose
+AIUsageCollector.cs             // thread-safe, groups by capability/provider/model/profile/feature
+AIUsageCollectorSnapshot.cs
+AIUsageCollectorEntry.cs
+AIUsageObservation.cs           // one finished call's usage, handed to every consumer
```

Management API adds `outcome.usage` (`TestUsageResponseModel` with `callCount`, `unreportedCallCount`, `failedCallCount`, `durationMs`, `breakdown[]` of `TestUsageEntryResponseModel`); `outcome.tokenUsage` is obsolete and null. The TS client is regenerated, and the run detail view now prints `outcome.usage` as JSON (a proper table is #523).

Persistence:

```diff
 AITestRunEntity
-    OutcomeTokenUsageJson
+    OutcomeUsageJson                 // RenameColumn migration, SQL Server + SQLite
```

🤖 Generated with [Claude Code](https://claude.com/claude-code)
