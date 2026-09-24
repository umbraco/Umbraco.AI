# Plan

Task checklist for `umb-build-loop`. v18 work happens on `v18/feature/decision-capability`
(existing worktree `.claude/worktrees/v18-decision-capability`). v17 is ported after v18 is
fully done and wired (T24). Docs go to Umbraco.Docs (T26).

Read `umb-build-loop-gotchas` and `add-ai-capability` before starting.

## Setup

- [x] **T0** — story: DR-11 (AC4). Bring the spike branch up to date by **merging**
  `origin/v18/dev` into `v18/feature/decision-capability` (not rebasing; see DECISION-LOG).
  Move the spike's `docs/plans/decision-capability/` to `docs/archive/decision-capability/`
  with a `> **Status:**` line on each file. Add this plan folder
  (`docs/plans/decision-capability-release/`) to the branch.
  Acceptance: `dotnet build Umbraco.AI/Umbraco.AI.slnx` and its tests pass on the merged
  branch.
  depends-on: none.

- [x] **T1** — story: DR-11 (AC1-AC3). Delete `Umbraco.AI.Tests.Common/Decision/Spike/` and
  `Decision/JevSpikeProviderTests.cs`. Switch the former-T12 real-provider gating tests in
  `AIProfileServiceTests`/`AIConnectionServiceTests` to `FakeDecisionCapability`, keeping
  both the disabled and enabled (positive-control) cases.
  Acceptance: Core unit + integration suites green. `grep -ri "spike\|JevSpike"` over
  `Umbraco.AI/src` and `Umbraco.AI/tests` finds nothing.
  depends-on: T0.

## Core

- [x] **T2** — story: DR-1 (AC1-AC4, AC8-AC11, AC14). Replace the flat question/response with
  per-kind types per `ARCHITECTURE.md` "Core types": `AIDecisionQuestion`,
  `AIDecisionQuestion<TResponse>`, `AIBinary/Choice/ScoreDecisionQuestion`,
  `AIDecisionOption`, `AIDecisionResponse` + the three response types. Delete
  `AIDecisionKind` and the `For*` factories. Update `ValidatingDecisionClient` to the
  per-type rules and keep it outermost. Update every existing Decision test and fake to the
  new types. `IAIDecisionClient` stays non-generic.
  depends-on: T1. parallel-group: A

- [x] **T3** — story: DR-1 (AC6, AC7, AC12), DR-3. Add `AISettings.DefaultDecisionProfileId`,
  `AIOptions.DefaultDecisionProfileAlias`, and the Decision arm in all three capability
  switches in `AIProfileService` (lines ~85/105/124), mirroring ImageGeneration.
  depends-on: T1. parallel-group: A

- [x] **T4** — story: DR-7 (AC3). Add empty sealed `AIDecisionProfileSettings` next to
  `AIImageGenerationProfileSettings` and its case in `AIProfileSettingsSerializer`.
  depends-on: T1. parallel-group: A

- [ ] **T5** — story: DR-1 (AC5, AC13). Make `IAIDecisionService.AskAsync` generic on the
  question's response type (`Guid`, `string` alias, builder, and no-profile/default
  overloads). Throw `AIProviderException` when the client returns the wrong response type.
  depends-on: T2, T3.

## Provider

- [ ] **T6** — story: DR-2 (AC1, AC16). Scaffold `Umbraco.AI.TypeSafe` with the
  `add-provider` skill (every registration point it lists), plus
  `tests/Umbraco.AI.TypeSafe.Tests.Unit` added to the root `.slnx`. Provider `typesafe` /
  "TypeSafe AI", settings `ApiKey` (sensitive, required) and `Endpoint`, a
  `TypeSafeDecisionCapability` with static model list `jev-latest`, `IHttpClientFactory`,
  and a per-file `UMBRACOAI_DECISION` pragma. The client can be a stub that throws
  `NotImplementedException` until T7.
  depends-on: T2. parallel-group: B

- [ ] **T7** — story: DR-2 (AC2-AC15). Implement `TypeSafeDecisionClient`: the wire mapping
  in `SPEC.md` "Provider", response mapping (probabilities re-keyed by label, usage),
  bounded 429/529 retry honoring `Retry-After`, and 401/422 mapped to exception types that
  `AIErrorClassifyingDecisionClient` classifies (confirm which first). Tests fake only the
  `HttpMessageHandler`.
  depends-on: T6.

## Management API

- [ ] **T8** — story: DR-6 (AC1, AC2, AC5, AC6). Add `GET capabilities/enabled`, excluding
  `Moderation`/`Media` and gated capabilities that are off. Make `AllProviderController`
  drop providers with zero enabled capabilities.
  depends-on: T1. parallel-group: A

- [ ] **T9** — story: DR-3 (AC1). Add `defaultDecisionProfileId` to
  `SettingsResponseModel`, `UpdateSettingsRequestModel`, `SettingsMapDefinition`. No
  capability check, matching the other default slots. Remove the `-DefaultDecisionProfileId`
  `Umbraco.Code.MapAll` exclusions and the `TODO(T9)` comment T3 left in
  `SettingsMapDefinition.cs`.
  depends-on: T3. parallel-group: B

- [ ] **T10** — story: DR-7 (AC4). Add `DecisionProfileSettingsModel` (`$type: "decision"`)
  to `ProfileSettingsModels.cs` and both directions in `ProfileMapDefinition`.
  depends-on: T4. parallel-group: B

- [ ] **T11** — story: DR-4 (AC1-AC11). Add `Constants.ManagementApi.Feature.Decision`,
  `DecisionControllerBase`, `AskDecisionController` (`POST decision/ask`), and the
  polymorphic request/response models, per `SPEC.md`. Flag check first (404), then
  validation (400), then profile resolution. Match Chat's completion-endpoint auth policy
  (confirm which first).
  depends-on: T5.

- [ ] **T12** — **wire: TypeSafe + Decision API into the demo site.** story: DR-2 (AC17),
  DR-4 (AC12), DR-3 (AC1). On the demo site with the flag on and a real TypeSafe key: create
  a connection and a Decision profile, set it as default via `PUT settings`, then ask all
  three kinds via `IAIDecisionService` and via a real authenticated `POST decision/ask`.
  With the flag off, confirm `POST decision/ask` returns 404 and TypeSafe is missing from
  `GET providers`. Use the demo-site recipe in `umb-build-loop-gotchas`; force-refresh any
  persisted connection/profile each run.
  depends-on: T7, T8, T9, T10, T11.

## Frontend

- [ ] **T13** — story: DR-5, DR-6, DR-7 (prerequisite). Regenerate the OpenAPI client
  (`npm run generate-client` against the running demo site) once, after every backend API
  change has landed. Commit the generated `src/api/` changes only.
  depends-on: T12.

- [ ] **T14** — story: DR-5 (AC1-AC7). Add `src/decision/`: `UaiDecisionController`
  (`@public`, typed overloads), repository, server data source, `types.ts`, exports wired
  into root `src/exports.ts`. Map `kind` ↔ `$type`. Don't leak generated types.
  depends-on: T13. parallel-group: C

- [ ] **T15** — story: DR-3 (AC2, AC3), DR-6 (AC3, AC4). Add the enabled-capabilities
  repository (fetched once, shared), the "Default Decision Profile" picker, and render the
  ImageGeneration and Decision pickers only when enabled. Hidden values are preserved on
  save. Settings types, repository and workspace context get `defaultDecisionProfileId`.
  depends-on: T13. parallel-group: C

- [ ] **T16** — story: DR-7 (AC1, AC2). Add `uaiCapabilities_decision` to `lang/en.ts`,
  `UaiDecisionProfileSettings` + `isDecisionSettings`, both type-mapper directions, the
  `case "decision"` in `profile-details-workspace-view.element.ts`, and
  `uai-decision-profile-settings` showing a "no settings" message.
  depends-on: T13. parallel-group: C

- [ ] **T17** — **wire: frontend into the running backoffice.** story: DR-3 (AC2, AC3),
  DR-5, DR-6 (AC3), DR-7 (AC1, AC2). In a browser on the demo site: create a TypeSafe
  connection and Decision profile through the UI, open the profile, and set the default in
  Settings. Call `UaiDecisionController.ask` for each kind from backoffice code or the
  console. Restart with the flag off and confirm both experimental pickers are hidden, a
  stored value survives a save, and TypeSafe is gone from the provider list.
  `npm run build:core` must pass, including the api-extractor rollup.
  depends-on: T14, T15, T16.

## Deploy

- [ ] **T18** — story: DR-8 (AC1-AC5). Add `DefaultDecisionProfileUdi` to
  `AISettingsArtifact`, and export (+ dependency) and import blocks in
  `UmbracoAISettingsServiceConnector`, mirroring ImageGeneration's fix (#227). Add a
  Decision profile round-trip test for `UmbracoAIProfileServiceConnector`. Raise
  `Umbraco.AI.Core`'s floor if needed.
  depends-on: T3, T4. parallel-group: B

- [ ] **T19** — **wire: Deploy connector in a real host.** story: DR-8. Resolve the settings
  and profile connectors from a running demo site (with `Umbraco.AI.Deploy` referenced).
  Export a real settings artifact containing `DefaultDecisionProfileUdi` and import it back.
  depends-on: T18, T12.

## Consumers

- [ ] **T20** — story: DR-9 (AC1-AC9). In `Umbraco.AI.Automate`: add `AskYesNoDecisionAction`,
  `AskChoiceDecisionAction`, `AskScoreDecisionAction` with settings/output classes per
  `SPEC.md`, modeled on `TranscribeAudioAction`. Compose-time exclusion when the flag is off
  (confirm `ActionCollectionBuilder` supports `Exclude<T>()`, else document the fallback in
  DECISION-LOG). Run-time flag guard. Pick the field editor for options/levels (confirm what
  exists). Raise the `Umbraco.AI.Core` floor.
  depends-on: T5, T3. parallel-group: D

- [ ] **T21** — **wire: Automate actions on the demo site.** story: DR-9 (AC6, AC10). Build
  a real automation, "Ask yes/no" → If, run it against the real TypeSafe profile, and
  confirm the branch taken. Restart with the flag off and confirm the actions are absent.
  depends-on: T20, T12.

- [ ] **T22** — story: DR-10 (AC1-AC8). In `Umbraco.AI.Agent`'s `SelectAgentForPromptAsync`:
  try Decision first per `ARCHITECTURE.md` decision 7, falling back to the unchanged chat
  path. Per-file pragma. Raise the `Umbraco.AI.Core` floor.
  depends-on: T5, T3. parallel-group: D

- [ ] **T23** — **wire: Copilot auto mode on the demo site.** story: DR-10 (AC9). With 2+
  agents and the real TypeSafe default profile, send a Copilot message in auto mode and
  confirm `agent_selected` arrives, with a Decision usage record and no classifier chat call.
  Repeat with the flag off and confirm the chat path is used.
  depends-on: T22, T12.

## v17 and docs

- [ ] **T24** — story: DR-12 (AC1, AC2, AC4). Port everything to v17 using the `backport`
  skill: new `v17/feature/decision-capability` branch from `v17/dev`, TypeSafe
  `version.json` = `17.0.0`, all touched products build and test green, draft PR into
  `v17/dev` cross-linked with the v18 PR.
  depends-on: T17, T19, T21, T23.

- [ ] **T25** — **wire: v17 demo site.** story: DR-12 (AC3). Real binary question through
  `POST decision/ask` on the v17 demo site.
  depends-on: T24.

- [ ] **T26** — story: DR-13 (AC1-AC4). Write the Umbraco.Docs pages in `SPEC.md` "Docs" for
  both `17/` and `18/` on branch `ai/decision-docs`, pushed as a draft PR.
  depends-on: T17, T21, T23.

## Parallel groups

- **A** (after T1): T2, T3, T4, T8. Different files (Decision/, Settings+AIProfileService,
  AIProfileSettingsSerializer, Web capability/provider controllers).
- **B** (each after its own A prerequisite): T6, T9, T10, T18.
- **C** (after T13): T14, T15, T16. Different frontend folders.
- **D** (after T5 + T3): T20, T22. Different products.
- T26 can run alongside T24/T25.

## First shippable slice

T0 → T1 → T2 → T3 → T5 → T6 → T7 → T11 → T12: a developer can install TypeSafe and ask all
three kinds from C# and over HTTP against real Jev. Everything after that adds reach (UI,
TS, Deploy, consumers, v17, docs).
