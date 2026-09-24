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
