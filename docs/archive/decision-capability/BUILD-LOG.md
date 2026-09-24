# Build Log

> **Status:** Archived 24-09-2026. Completed spike (all 12 tasks done, never merged). Superseded by the full feature plan in `docs/plans/decision-capability-release/`, which builds on this spike's Core code.

- **T1** — `8e83dfc` — `AICapability.Decision = 8` added, `Moderation = 3` and the
  `TextToSpeech`/`SpeechToSpeech` placeholder comment left untouched. Verified: builder ran
  `dotnet build` on `Umbraco.AI.Core.csproj` (0 errors); reviewer independently re-ran the
  build and confirmed every existing `switch` over `AICapability` has a safe default branch,
  so the new value can't break anything already shipped. PASS on first review pass.

- **T2** — `6af897a7` — `Decision/` core types added (Kind, Question, Options, Response +
  factories, IAIDecisionClient, Diagnostics). First review pass found 2 Important issues:
  a custom `AIDecisionUsage` type would have forced an unplanned converter in T7 and lost
  usage fields the tracking pipeline already reads, and the response factories couldn't set
  `ModelId`/`Usage`. Both fixed (switched `Usage` to M.E.AI `UsageDetails`, added optional
  `modelId`/`usage` params to the factories) and re-reviewed. PASS on second review pass.

- **T3** — `2f0389eb` — `AIExperimentalOptions.Decision` (default off) + the
  `AIExperimentalFeatures.IsCapabilityEnabled` switch case added, mirroring `ImageGeneration`
  exactly. Verified for real: reviewer built a scratch xUnit project (assembly named
  `Umbraco.AI.Tests.Unit` for `InternalsVisibleTo`) referencing the freshly built
  `Umbraco.AI.Core.dll`, compiled only `AIExperimentalFeaturesTests.cs`, ran it — 7/7 passed,
  including both new Decision cases. PASS on first review pass.

- **T4** — `3078ecab` — `ValidatingDecisionClient` added (sad-path enforcement: empty/whitespace
  `Prompt` and `Choice` with <2 `Choices` both throw `ArgumentException` before reaching any
  inner client). Verified: reviewer independently built a scratch project and got 5/5 real
  passes on `ValidatingDecisionClientTests.cs`. PASS with carry-forward items (not defects):
  the class isn't wired into any factory yet (that's T7), and — important — T7 must place it
  *outside* `AIErrorClassifyingDecisionClient`/tracking, not innermost, or caller errors get
  miscategorized as provider failures. PLAN.md's T7/T8 entries were updated with explicit
  acceptance criteria for this. Also applied a small suggested fix (`ArgumentNullException.
  ThrowIfNull(question)`) before committing.

- **T5+T6** — `be8bac70` — `IAIDecisionCapability` + 3-arity `AIDecisionCapabilityBase*`, plus
  `DeclaredSettingsDecisionClient`/`CapabilitySettingsDecisionClient` (built together —
  inseparable). Also landed: `391d2722` (stranded T3 spec file) and `6ba224f4` (stranded T2
  spec file), both caught by review as commit-hygiene gaps.

  Took 3 review rounds:
  1. FAIL — broke an existing test (`CapabilitySettingsSurfaceTests`, a guard that fires
     whenever a new capability interface appears unregistered) + `AIDecisionOptions` as a
     sealed class gave `ApplyCapabilitySettings` nothing it could actually change + no
     round-trip test proved Decision's settings reached a request. Fixed: registered the new
     `ClientNoun`; converted `AIDecisionOptions`/`AIDecisionQuestion` to records + `with {}`;
     added a round-trip test.
  2. FAIL — the record-based fix only solved the *copy* problem, not the *mutate* problem: an
     immutable record + a `void` hook still can't produce a changed value. Fixed by switching
     `ApplyCapabilitySettings` to return the new `AIDecisionOptions` (`Func<...>` instead of
     `Action<...>`).
  3. **User redirected mid-fix**: since `IAIDecisionClient` stands in for what M.E.AI would
     provide, its options type should follow M.E.AI's own convention
     (`ChatOptions`/`SpeechToTextOptions` — mutable classes with `Clone()`), not become a
     record. Reverted the `Func<...>` approach; `AIDecisionOptions` is a mutable class with
     `Clone()` (mirroring `ChatOptions.Clone()` exactly); `AIDecisionQuestion` reverted to its
     original T2 shape (it was never part of the clone-and-mutate lifecycle, converting it was
     unnecessary); `ApplyCapabilitySettings` is `void` again, byte-for-byte matching the
     SpeechToText sibling's signature/docs; `CapabilitySettingsDecisionClient.Apply()` clones,
     mutates the clone via the hook, returns the clone — line-for-line the same shape as
     `CapabilitySettingsChatClient.Apply()`. PASS on the third review pass, independently
     verified (1156/1156 full suite).

- **T7** — `c1fce312` — Remaining `Decision/` plumbing: client factory, middleware collection,
  tracking client/middleware, OpenTelemetry middleware, error classifier, executing/executed
  notifications, scoped profile/inline clients — plus unplanned-but-necessary supporting
  pieces (`IAIConfiguredDecisionCapability`/`AIConfiguredDecisionCapability`,
  `Constants.FeatureTypes.InlineDecision`, a minimal `AIDecisionBuilder` that T8 should extend
  not replace). FAILed once: no committed test locked in the critical wrapping order (T4's
  carry-forward warning — `ValidatingDecisionClient` must be outermost, outside the error
  classifier/tracking, or caller errors get miscategorized as provider failures), plus the
  usual stranded-spec-file gap. Fixed: added `AIDecisionClientFactoryTests.cs` (4 tests
  through the *real* factory — invalid question → `ArgumentException` not
  `AIProviderException`; provider never invoked; tracker never invoked; genuine provider
  failure still becomes `AIProviderException`), fixed a stale round-trip test to go through
  the real factory like its siblings, committed `AITrackingDecisionClientTests.cs` alongside.
  PASS on second pass — reviewer confirmed the ordering-protection reasoning holds (traced
  both `AIErrorClassifyingDecisionClient`'s catch behavior and the tracker wiring) and
  reproduced 1161/1161 full suite + Integration 30/30.

- **T8** — `cbc26ab5` — `IAIDecisionService`/`AIDecisionService` (3 overloads: `Guid`, `string`
  alias, `Action<AIDecisionBuilder>`) + DI registration. Took 3 review rounds:
  1. FAIL — a new Core-only `IdOrAlias` type had no experimental gate, and T7's
     `ScopedInlineDecisionClient`/notifications/builder were built but left unconsumed,
     contradicting ARCHITECTURE.md §3. **User decided: wire the inline path in, don't delete
     it.** Mid-fix, further correction (from checking `IAIChatService.cs` directly): Chat/
     SpeechToText never use an `IdOrAlias`-shaped param at the Core layer — dropped that type
     entirely in favor of `Guid`/`string` overloads delegating into the builder path, matching
     precedent exactly.
  2. FAIL — real behavior bug: `ScopedInlineDecisionClient` decided feature-metadata
     stamping via `!scopeExisted` instead of `!builder.IsPassThrough` (Chat/SpeechToText's
     actual execute-path rule) — inverted attribution (pass-through calls wrongly stamped,
     normal calls from inside a parent scope wrongly didn't). Fixed with a real red→green
     demonstration (reverted the fix, watched the new test fail, restored it, watched it
     pass). Also added 3 more specs mirroring STT's coverage (pass-through, default-profile
     fallback, wrong-capability).
  3. FAIL — the fix's own documentation claimed the two rules "coincide except one case"
     when they actually diverge in two; the second case (normal call from inside an existing
     parent scope) had no test. Fixed: corrected the docs, added the missing test, plus 3
     small suggested cleanups (null/blank-alias guard, a misleading `ConfigureLegacy` name,
     inaccurate test doc comments).
  PASS on a 4th, dedicated confirmation pass — 1202/1202 full suite (unit + integration).

- **T9** — `3cb3ff66` — Proved (didn't assume) that `AIConnectionService`/`AIProfileService`'s
  generic `IsCapabilityEnabled(cap.Kind)` gating already covers `AICapability.Decision`
  correctly, with zero production changes. FAILed once: the 5 pre-written specs used a
  mocked `IAIExperimentalFeatures`, but PLAN.md's acceptance criteria explicitly required a
  real one. Fixed: built a real `AIExperimentalFeatures` (backed by a mocked
  `IOptionsMonitor<AIExperimentalOptions>`) for just those 5 tests via local-only helpers,
  left every other test's shared mock untouched, added a 6th test for the previously-uncovered
  "enabled" case on `GetConnectionsByCapabilityAsync`. PASS on confirmation — 1171/1171 unit +
  32/32 integration.

- **T10** — `62b382a5` — Disposable Jev spike provider (`JevSpikeProvider`/`JevSpikeDecisionCapability`/
  `JevSpikeDecisionClient`/`JevSpikeProviderSettings`/`JevAnswerDto`), deliberately placed in
  `Umbraco.AI.Tests.Common/Decision/Spike/` rather than any `src/` project — an
  `[AIProvider]`-attributed class there would auto-discover into every real Umbraco.AI
  install via `IDiscoverable`, which a throwaway spike must never do. Implements all three
  `AIDecisionKind`s (not just Binary — the DTO mapping cost nothing extra to generalize).
  FAILed once: the capability created its own `HttpClient` per call with nothing ever
  disposing it (socket-exhaustion pattern), and the error paths (bad JSON, unknown kind,
  missing API key) were implemented but had zero test coverage. Fixed: switched to
  `IHttpClientFactory`, mirroring `FireworksAIProvider`'s exact pattern (factory-owned,
  pooled client — the client wrapper's `Dispose()` is now a correct no-op); added 5 error-path
  tests including one that asserts the real outgoing request's bearer header and path. PASS
  on confirmation — 10/10 on the spike's own tests, 1178/1178 unit + 32/32 integration full
  suite. T11/T12 remain — both explicitly manual/flag-toggle verification against real Jev
  credentials, not more production code.

- **T11** — `9920a88d` — Real live round trip verified against Jev's actual API using a real
  key (user-supplied via demo-site secrets). T10's guessed wire format was wrong in several
  ways (endpoint path, batch request/response shape, and — the actual root cause of the
  remaining 400s — the type discriminator string `"binary"` doesn't exist in Jev's API, only
  `"noul"`). See DECISION-LOG.md's 2026-09-24 entry for full detail. Took 2 review rounds
  (round 2 caught a stale fallback path missed in round 1's fix) plus a process correction:
  the orchestrator briefly hand-edited these files directly while iterating against the live
  API — caught mid-flight and handed to a builder to redo through the normal cycle. PASS on
  confirmation — genuine live result `Kind=Binary BinaryAnswer=True Confidence=0.99`,
  1183/1183 unit + 32/32 integration.

- **T12** — `7997360b` — Proved the real `JevSpikeProvider` (not just T9's generic test-double)
  goes fully inert when `Umbraco:AI:Experimental:Decision` is off — hidden from capability
  listing, empty from connections-by-capability, profile creation rejected. FAILed once: the
  first pass of tests only covered the disabled side, so they'd have passed vacuously even if
  the provider never exposed `AICapability.Decision` at all. Fixed: added 2 positive-control
  tests (flag on → Decision IS listed / connection IS returned), consolidated the
  now-3x-duplicated provider-construction helper into one shared `JevSpikeProviderFactory`.
  PASS on confirmation — 1188/1188 unit + 32/32 integration, the final green run for the whole
  feature.

## Plan complete

All 12 tasks (T1-T12) are done, reviewed, and committed on `v18/feature/decision-capability`.
See `DECISION-LOG.md` for the full record of what was decided and corrected along the way —
most notably T5/T6's three-round detour to make `AIDecisionOptions` follow M.E.AI's own
mutable-options convention (per user redirect) rather than an idiomatic-but-inconsistent C#
record, and T11's discovery that Jev's real wire format differed from every guess T10 made
(wrong endpoint, wrong shape, and a type discriminator — `"noul"`, not `"binary"` — that
doesn't exist anywhere in `AIDecisionKind`'s own naming).
