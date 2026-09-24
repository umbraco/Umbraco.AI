# Build Log

- **T0** — `b88ff340` (merge) + `bd3a0e2a` — Merged `origin/v18/dev` into
  `v18/feature/decision-capability` (clean, 14 files, no conflicts). Spike plan folder moved to
  `docs/archive/decision-capability/` with a Status line on each file; this plan folder added.
  Verified: `dotnet test Umbraco.AI/Umbraco.AI.slnx` → 1188/1188 unit + 32/32 integration.
  Setup task done directly by the orchestrator (git + docs only, no feature code).
  Note: `wdp.port` reads 44355 in both the main checkout and this worktree, so don't run both
  demo sites at the same time.

- **T1** — `d6dd07dc` — Deleted `Tests.Common/Decision/Spike/` (6 files) and
  `JevSpikeProviderTests.cs`, plus the 5 `*_ForRealJevSpikeProvider` tests. Reviewer
  independently reran: 1168/1168 unit (−20, exactly the deleted tests) + 32/32 integration.
  All six on/off gating proofs remain on `FakeDecisionCapability` with a real
  `AIExperimentalFeatures`. PASS on first review. Carry-forward: "Jev" still named in Core
  doc comments (`DeclaredSettingsDecisionClient.cs`, `DeclaredSettingsEnforcementTests.cs:216`)
  and a `"jev-test"` model id. Core should be vendor-neutral, so folded into T2.

- **T2** — `b5009ab3` — Per-kind question/response types replace the flat shape;
  `AIDecisionKind` and the `For*` factories deleted; `ValidatingDecisionClient` rules per
  type; "Jev" removed from Core. Took 3 review rounds:
  1. FAIL: null `Options`/`Levels` coverage was lost in the moved spec. Also fixed: a null
     entry in `Options` threw NRE; boundary and p=0.5 tests; split a two-assertion test;
     tracking/OTel branches for choice/score/unknown subtypes now tested.
  2. FAIL: the new OTel test's `ActivityListener` caught any span on the shared
     `"Umbraco.AI"` source, so parallel chat/embedding tests could make it flake. Fixed by
     filtering on `gen_ai.decision`.
  3. PASS. Reviewer reran 1195/1195 unit + 32/32 integration twice.

- **T3** — `613dc364` — `AISettings.DefaultDecisionProfileId`, `AIOptions.DefaultDecisionProfileAlias`,
  and the Decision arm in all three `AIProfileService` switches. Persisted reflectively, no
  migration. The `Umbraco.Code.MapAll` analyzer forced a temporary `-DefaultDecisionProfileId`
  exclusion in `SettingsMapDefinition.cs` (`TODO(T9)`). T9's plan line now says to remove it.
  Reviewer reran 1198/1198 unit + 32/32 integration, and `Umbraco.AI.Deploy.slnx` builds.
  PASS on first review.
  Process note: the builder used `git stash` for a baseline warning check and left an applied
  entry (`T3-baseline-check-*`) on the shared stash stack. The user was asked to drop it. Later
  builders are told not to stash.

- **T4** — `35095a45` — Empty sealed `AIDecisionProfileSettings` + deserialize case in
  `AIProfileSettingsSerializer` (serialize goes by runtime type). Not `[Experimental]`,
  matching the unmarked `AIImageGenerationProfileSettings`; only `Core/Decision/` types carry
  it. Reviewer confirmed `{}` round-trips non-null through the serializer, `AIProfileFactory`
  and the Deploy profile connector. Reran 1200/1200 unit + 32/32 integration. PASS on first
  review.

- **T5** — `7795b345` — `IAIDecisionService.AskAsync<TResponse>` with none/Guid/alias/builder
  overloads. Staged `DecisionPipelineHarness` + `AskTypedDecisionTests` moved in;
  `AIDecisionServiceRealPipelineTests` refactored onto the harness. Took 3 review rounds:
  1. FAIL: the response-type mismatch check ran in the service, *outside* tracking, so audit
     recorded success while the caller got `AIProviderException`. Fixed: an internal
     `ExpectedResponseType` on the question hierarchy. `AIErrorClassifyingDecisionClient`
     (inside tracking) now throws, and the service only narrows. New factory-level specs prove
     the tracker records a failure. Written up as a gotcha memory.
  2. FAIL: 3 new CS1735 doc warnings and a stale remarks paragraph in `IAIDecisionService.cs`.
     Fixed, and the mismatch exception deduped into `AIDecisionExceptionFactory`.
  3. PASS. Reviewer reran 1216/1216 unit + 32/32 integration, with zero CS1735.
  Note: closing direct subclassing of the non-generic `AIDecisionQuestion` base (via an
  internal abstract member) was accepted. Only `AIDecisionQuestion<TResponse>` is the
  extension point.

- **T6** — `d0229d01` — `Umbraco.AI.TypeSafe` scaffolded via the `add-provider` skill: provider
  `typesafe`, settings (sensitive required `ApiKey`, `Endpoint`), Decision-only capability
  (`jev-latest`), stub client with the injectable-delay internal constructor. Registered in the
  root slnx (src + tests), install scripts (sh/ps1), `azure-pipelines.yml` `level1Products`,
  version `18.0.0`, changelog scope `typesafe`. Added to the root README/CLAUDE.md provider
  lists. Reviewer compared it with FireworksAI point by point: 5/5 TypeSafe tests, root slnx
  builds, `publicReleaseRefSpec` matches siblings. PASS on first review. Carry-forward: the
  stub `NotImplementedException` (`TypeSafeDecisionClient.cs:58`) must be gone after T7.

- **T7** — `2e76bc9b` — Real `TypeSafeDecisionClient`: the wire mapping was confirmed against
  docs.typesafe.ai/api with no differences from SPEC. Usage is mapped. Non-2xx surfaces as
  `HttpRequestException{StatusCode}` (the base classifier maps 401→Authentication,
  422→InvalidRequest, 429→RateLimited, 529→Transient). 429/529 retried twice, `Retry-After`
  honored and capped at 30s, with an injectable delay and clock. Took 4 review rounds:
  1. FAIL: the retry cap, HTTP-date and default-backoff rules had no tests. Also fixed: response
     not disposed if the error-body read threw; score labels now come from `question.Levels`
     (authoritative) instead of the response `legend`; an unknown choice key now throws.
  2. FAIL: the score clamp and dropped out-of-range probability keys had no tests.
  3. (Builder-flagged) The lower-clamp bound was only really tested by a -0.6 case, so that
     case was added.
  4. PASS. 58/58 TypeSafe tests. Each guard has red/green proof.
