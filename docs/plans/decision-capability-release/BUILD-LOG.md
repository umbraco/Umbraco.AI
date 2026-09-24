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

- **T8** — `b7db71d3` — New `Capability` feature folder with `GET capabilities/enabled` (implemented
  capabilities ∩ enabled). `AllProviderController` drops providers with zero enabled
  capabilities. `ByIdProviderController` deliberately unchanged. Took 2 review rounds:
  1. FAIL: the public 2-arg `AllProviderController` constructor was removed. Restored as
     `[Obsolete]`, chaining via `StaticServiceProvider`, with `[ActivatorUtilitiesConstructor]`
     on the new one (the c0834532 pattern). Written up as a gotcha memory. Also added a guard test,
     `ImplementedCapabilitiesSurfaceTests`, which goes red if a new `IAI*Capability` interface
     isn't in the implemented set.
  2. PASS. Reviewer reran 1237/1237 unit + 32/32 integration. T12 must prove
     `GET providers` activates live (two constructors).

- **T9** — `f7998705` — `defaultDecisionProfileId` on the settings response/request models and
  `SettingsMapDefinition`. T3's temporary MapAll exclusions and `TODO(T9)` are removed. The
  reviewer confirmed `PUT settings` is full-replace, so T15 must always send the field (added to
  T15's plan line). Reviewer reran 1239/1239 unit + 32/32 integration. PASS on first review.

- **T10** — `c0141f1e` — `DecisionProfileSettingsModel` (`$type: "decision"`) and both
  `ProfileMapDefinition` arms. Create and update share `MapSettingsFromRequest`. The OpenAPI
  schema picks up the derived type generically. Reviewer reran 1243/1243 unit + 32/32
  integration. PASS on first review.

- **T11** — `f68c2ac3` — `POST decision/ask` (`AskDecisionController`, polymorphic
  request/response models, `Constants.Feature.Decision`). Auth is `BackOfficeAccess` via the
  shared base, same as Chat. `DecisionCapabilityGateFilter` (a resource filter) 404s before model
  binding, so flag-off + bad body still gives 404. Took 2 review rounds:
  1. FAIL on 4 points: (a) missing `$type` might 500; (b) the controller's validation had
     drifted from Core (null option entry → NRE → 500); (c) the `"not found"` substring mapped a
     missing default profile to 404 instead of 400; (d) the gate filter had no test.
     Fixes: (a) no source change needed. The reviewer confirmed in the CMS source that
     `NamedSystemTextJsonInputFormatter` catches `NotSupportedException`. It's now pinned by a
     TestHost test through the real named-options wiring, which added the test-only
     `Microsoft.AspNetCore.TestHost` package. (b) One shared internal `DecisionQuestionValidator` in
     Core, used by both the client and the controller. (c) Substring mapping removed; all service
     `InvalidOperationException`s → 400. (d) Filter tests added.
  2. PASS. Reviewer reran 1296/1296 unit + 32/32 integration.

- **T12** (wire) — `1571d533` (fix found by the wire check) — Live on the demo site against the
  real Jev API (`jev-1.13.0`), with the builder's run re-run by the orchestrator. The key came from
  user-secrets via a `$Umbraco:AI:Secrets:TypeSafeApiKey` reference and was never logged.
  Flag ON: connection, profile and default force-saved. Via `IAIDecisionService`: binary
  (true/0.99), choice (oceania/1.0), score (2 → positive/1.0), plus binary by alias, all with
  token usage. Real authenticated HTTP (API user + `client_credentials` token):
  `POST decision/ask` ×3 → 200; `GET settings` has `defaultDecisionProfileId`;
  `capabilities/enabled` includes Decision; `providers` includes typesafe; missing `$type` and
  1-option choice → 400. Flag OFF: ask → 404; typesafe and Decision gone. Both boots had no DI
  errors, so `AllProviderController`'s two-constructor activation works live.
  **Bug found:** responses had no `$type` because `Ok(derived)` erased the declared base type.
  Fixed via `DeclaredType`, with a TestServer regression test through the real output formatter
  (red/green). Reviewed PASS, re-verified live. Written up as a gotcha memory.
  Scratch: `demos/v18/Umbraco.AI.DemoSite/TEMP_DecisionReleaseVerification.cs` (gitignored), log
  prefix `[T12]`. Tip for later wire tasks: `IBackOfficeUserClientCredentialsManager.SaveAsync`
  prefixes the client id with `umbraco-back-office-`, so request the token with the prefixed id.

- **T13** — `e5c88b4a` — Regenerated the core OpenAPI client from the running demo site
  (`generate-client:core`). Additions only: `DecisionService.ask`,
  `CapabilitiesService.getEnabledCapabilities`, the `$type` unions for Decision
  question/response, the `decision` arm on `ProfileSettingsModel`, and `defaultDecisionProfileId`.
  No unrelated drift, and the generator version is unchanged. `npm run build:core` passes
  (api-extractor included). PASS on first review.

- **T14** — `317ecf26` — `src/decision/`: public question/result types, `UaiDecisionController.ask`
  with typed overloads, a repository, and a data source mapping `kind`↔`$type`. Exported via
  root `exports.ts` and present in the api-extractor rollup, with no generated `*Model` leak.
  Took 2 review rounds:
  1. FAIL: no overload accepted the `UaiDecisionQuestion` union (TS2769 for runtime-built
     questions), and an unknown response `$type` resolved as `{ data: undefined }`. Fixed with a
     4th union overload and an exhaustive `never` default returning an error. Added specs that
     profileIdOrAlias and signal are forwarded.
  2. PASS. Follow-up: the union spec was being narrowed to the binary overload, so it now uses a
     union-typed helper. Red/green proved `build:core` fails TS2769 without the overload (tsconfig
     type-checks test files). Vitest 34/34.

- **T15** — `b19952b8` — Internal `UaiEnabledCapabilitiesRepository` (module-scoped shared promise,
  cleared on error so it can retry), `defaultDecisionProfileId` through settings
  types/repository/context, and a Default Decision Profile picker. The ImageGeneration and
  Decision pickers render only once they're known to be enabled, so there's no flash. The full
  `#model` is always PUT, so hidden values survive. All six pickers are now localized (text
  unchanged). Took 2 review rounds:
  1. FAIL: the shared-promise/retry cache was untested (the editor spec mocked the whole
     repository). Added 3 repository specs. Isolation uses a cache-busting dynamic import, since
     `vi.resetModules()` re-ran `customElements.define`.
  2. PASS. Vitest 45/45, run twice. The reviewer confirmed that removing `??=` or the reset
     fails a spec. Suggestion kept for later: a `resetEnabledCapabilitiesCacheForTests()` seam
     would be more conventional than the `?t=` import trick.

- **T16** — `a56a09ed` — Added the `uaiCapabilities.decision` label, `UaiDecisionProfileSettings` +
  `isDecisionSettings`, both type-mapper directions, `case "decision"` in the workspace view,
  and a new `uai-decision-profile-settings` element with a localized "no settings" message
  (`uaiProfile_noSettingsAvailable`, generic and reusable). The reviewer confirmed a new Decision
  profile's first save works: it sends null settings, which the backend maps to empty. Vitest
  47/47. PASS on first review.
  Follow-ups for the final pre-PR cleanup (not blocking): (1) the new element's doc comment
  claims it dispatches `uai-profile-settings-change`, but it never does; reword it. (2) There's a
  pre-existing gap: `createDefaultSettings`/`getCapabilityIcon` only cover chat and embedding
  (ImageGeneration and SpeechToText are missing too). Both fall back safely.

- **T17** (wire) — no code change — Live backoffice check in a real browser (Playwright) on the
  demo site. The orchestrator checked the screenshots. Flag ON: a TypeSafe connection created
  through the UI with a `$`-config API key reference; the create-profile dialog offers Decision;
  a new Decision profile saves and reopens showing the "no settings" view; the Default Decision
  Profile picker lists only Decision profiles and the value persists after reload.
  `UaiDecisionController` loaded via the `@umbraco-ai/core` import map and was called from page
  context for binary (true/0.98), choice (oceania/1.0) and score (2 → positive/1.0), each with
  the right `kind`. Flag OFF: the Decision and ImageGeneration pickers are hidden and the other
  four render. After a save, the DB still holds `DefaultDecisionProfileId` (checked in the DB
  rather than by a restart, because the T12 hosted service would overwrite it). TypeSafe is
  absent from the provider list. No console errors from product code. One 404 came from a
  leftover spike-era "Jev Spike" connection in the gitignored demo DB, not from the product.

- **T18** — `9e7c1c50` — `AISettingsArtifact.DefaultDecisionProfileUdi` plus export (Match
  dependency) and import blocks in `UmbracoAISettingsServiceConnector`, a line-for-line copy of
  the ImageGeneration fix (5a3fa194). The profile connector needed no change. Deploy tests 26/26
  (was 19). The Deploy test project is in the root slnx. PASS on first review.
  **Release-time item:** raise the root `Directory.Packages.props` `Umbraco.AI.Core` floor from
  `[18.3.4` to the Core version that ships Decision (next minor, 18.4.0). Precedent only raises
  it in release prep. The ranged pack recompile is the safety net: a solo Deploy pack against
  18.3.4 fails to compile.

- **T19** (wire) — no code change — Real host with the demo site referencing
  `Umbraco.AI.Deploy` and `Umbraco.Deploy.OnPrem` 18.1.1 (gitignored demo edits; OnPrem is the
  package whose composer registers `IServiceConnectorFactory`, and it has an unlicensed grace
  period). Connectors were resolved via `IServiceConnectorFactory`. The settings export has
  `DefaultDecisionProfileUdi` and the profile as a dependency. After clearing the setting,
  `ProcessAsync` import restored it. The Decision profile import gives non-null
  `AIDecisionProfileSettings`. Re-run by the orchestrator: all PASS.
  Notes: (1) `install-demo-site.sh` has no option to reference Deploy from the demo csproj, a
  tooling gap outside this feature. (2) The demo SQLite DB throws "database table is locked"
  under background-job load when an EF scope stays open for tens of seconds. Deploy's
  directory-wide export hit this, as did unrelated jobs (`AIUsageHourlyAggregationJob`, Automate
  outbox cleanup). That's an environment limit, not a Decision defect, and was worked around by
  calling `GetArtifactAsync`/`ProcessAsync` directly.

- **T20** — `60ddc7d8` — Three actions (`umbracoAI.askYesNoDecision`, `askChoiceDecision`,
  `askScoreDecision`, group "AI"). Compose-time `Exclude<T>()` when the flag is off (read from
  `builder.Config`, same key Core binds) plus a run-time guard (→ Validation). Error mapping
  mirrors `TranscribeAudioAction`. Took 2 review rounds:
  1. FAIL: the actions duplicated Core's 2..255 / 2..10 count checks (the same drift risk T11
     fixed), and `ParseOptions` was untested. The checks were removed, so Core's
     `ValidatingDecisionClient` (outermost) is the only enforcer and its `ArgumentException`
     reaches the action unchanged. 7 `ParseOptions` tests added through the real `ExecuteAsync`.
  2. PASS. Automate tests 60/60 (was 38).

- **T21** (wire) — no code change — Real automations built with `IAutomationService`, published,
  and triggered over real HTTP (`automations/{id}/trigger`), then polled via
  `IAutomationRunService`, against real Jev. Flag ON: all 3 aliases are in `ActionCollection` and
  `GET catalogue/actions`. Yes/no → `Answer=true` → the If (`${steps.askYesNo.answer} Equals true`)
  took the TRUE branch (confirmed by the step-run DB row). Pick-one gave oceania/1.0; score gave
  2/positive/1.0, both through the default profile. Flag OFF: the actions are absent from the
  collection and the catalogue.
  **Finding (Umbraco.Automate, not fixed here):** a previously published automation containing a
  now-excluded action doesn't fail. `WorkflowCompiler.CompileStep` logs a warning and drops the
  step. The If then read `""` and took the FALSE branch, the run finished `Completed`, and no
  provider call was made. The user chose to keep compose-time hiding and log this upstream (see
  DECISION-LOG). A second upstream finding: `StepRun.BranchOutcome` and the iteration fields are
  never persisted (`StepRunEntity` and `StepRunFactory` don't map them).

- **T22** — `8e2ebbfa` — `AIAgentService.SelectAgentForPromptAsync` tries Decision first via
  `TrySelectAgentViaDecisionAsync`. The gates run in order: 2..255 agents, the flag, then
  `HasDefaultProfileAsync(Decision)`. It asks one `AIChoiceDecisionQuestion` keyed by agent id,
  and an unknown key or any non-cancellation failure falls back to the unchanged chat
  classifier, with no user content in logs. `IAIDecisionService` and `IAIExperimentalFeatures` are
  required ctor params (the class is internal and DI-registered). Took 2 review rounds (both
  PASS; round 1's suggestions were applied): the gate moved from catching
  `InvalidOperationException` to `HasDefaultProfileAsync`; the whole attempt is wrapped so a DB
  error can't break auto mode for non-Decision sites; the deps became required instead of
  optional-null. Agent 236 unit + 3 integration. Follow-up (non-blocking): no spec proves
  `OperationCanceledException` propagates from the Decision attempt.
  All staged specs are now moved; `specs/README.md` says so.

- **T23** (wire) — no code change — Real authenticated `POST agents/auto/stream-agui` (surface
  `copilot`) with 2 force-refreshed agents (SEO helper, Translator), for the message "Please
  translate this paragraph into French". The SSE stream was read only up to `agent_selected`, so
  the chosen agent's own run never started and any chat usage is routing only. Flag ON: selected
  Translator; Decision requests +1, Chat +0 (no classifier call). Flag OFF: still Translator via
  the chat classifier; Decision +0, Chat +1. PASS. Builder-verified only; the orchestrator did
  not re-run it (a run takes 10-15 min here because of SQLite contention from Automate's
  background jobs).
