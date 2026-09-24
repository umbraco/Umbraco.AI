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
