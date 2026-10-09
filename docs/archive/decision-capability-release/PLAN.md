# Plan

> **Status:** Archived 09-10-2026. Shipped: merged to `v18/dev` (#419) and `v17/dev` (#428) on 09-10-2026, together with the follow-on `decision-evaluators` plan (#430, #431). Only T26 (the public Umbraco.Docs PR, branch `ai/decision-docs`) was still open at archive time.

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

- [x] **T5** — story: DR-1 (AC5, AC13). Make `IAIDecisionService.AskAsync` generic on the
  question's response type (`Guid`, `string` alias, builder, and no-profile/default
  overloads). Throw `AIProviderException` when the client returns the wrong response type.
  depends-on: T2, T3.

## Provider

- [x] **T6** — story: DR-2 (AC1, AC16). Scaffold `Umbraco.AI.TypeSafe` with the
  `add-provider` skill (every registration point it lists), plus
  `tests/Umbraco.AI.TypeSafe.Tests.Unit` added to the root `.slnx`. Provider `typesafe` /
  "TypeSafe AI", settings `ApiKey` (sensitive, required) and `Endpoint`, a
  `TypeSafeDecisionCapability` with static model list `jev-latest`, `IHttpClientFactory`,
  and a per-file `UMBRACOAI_DECISION` pragma. The client can be a stub that throws
  `NotImplementedException` until T7.
  depends-on: T2. parallel-group: B

- [x] **T7** — story: DR-2 (AC2-AC15). Implement `TypeSafeDecisionClient`: the wire mapping
  in `SPEC.md` "Provider", response mapping (probabilities re-keyed by label, usage),
  bounded 429/529 retry honoring `Retry-After`, and 401/422 mapped to exception types that
  `AIErrorClassifyingDecisionClient` classifies (confirm which first). Tests fake only the
  `HttpMessageHandler`.
  depends-on: T6.

## Management API

- [x] **T8** — story: DR-6 (AC1, AC2, AC5, AC6). Add `GET capabilities/enabled`, excluding
  `Moderation`/`Media` and gated capabilities that are off. Make `AllProviderController`
  drop providers with zero enabled capabilities.
  depends-on: T1. parallel-group: A

- [x] **T9** — story: DR-3 (AC1). Add `defaultDecisionProfileId` to
  `SettingsResponseModel`, `UpdateSettingsRequestModel`, `SettingsMapDefinition`. No
  capability check, matching the other default slots. Remove the `-DefaultDecisionProfileId`
  `Umbraco.Code.MapAll` exclusions and the `TODO(T9)` comment T3 left in
  `SettingsMapDefinition.cs`.
  depends-on: T3. parallel-group: B

- [x] **T10** — story: DR-7 (AC4). Add `DecisionProfileSettingsModel` (`$type: "decision"`)
  to `ProfileSettingsModels.cs` and both directions in `ProfileMapDefinition`.
  depends-on: T4. parallel-group: B

- [x] **T11** — story: DR-4 (AC1-AC11). Add `Constants.ManagementApi.Feature.Decision`,
  `DecisionControllerBase`, `AskDecisionController` (`POST decision/ask`), and the
  polymorphic request/response models, per `SPEC.md`. Flag check first (404), then
  validation (400), then profile resolution. Match Chat's completion-endpoint auth policy
  (confirm which first).
  depends-on: T5.

- [x] **T12** — **wire: TypeSafe + Decision API into the demo site.** story: DR-2 (AC17),
  DR-4 (AC12), DR-3 (AC1). On the demo site with the flag on and a real TypeSafe key: create
  a connection and a Decision profile, set it as default via `PUT settings`, then ask all
  three kinds via `IAIDecisionService` and via a real authenticated `POST decision/ask`.
  With the flag off, confirm `POST decision/ask` returns 404 and TypeSafe is missing from
  `GET providers`. Use the demo-site recipe in `umb-build-loop-gotchas`; force-refresh any
  persisted connection/profile each run.
  depends-on: T7, T8, T9, T10, T11.

## Frontend

- [x] **T13** — story: DR-5, DR-6, DR-7 (prerequisite). Regenerate the OpenAPI client
  (`npm run generate-client` against the running demo site) once, after every backend API
  change has landed. Commit the generated `src/api/` changes only.
  depends-on: T12.

- [x] **T14** — story: DR-5 (AC1-AC7). Add `src/decision/`: `UaiDecisionController`
  (`@public`, typed overloads), repository, server data source, `types.ts`, exports wired
  into root `src/exports.ts`. Map `kind` ↔ `$type`. Don't leak generated types.
  depends-on: T13. parallel-group: C

- [x] **T15** — story: DR-3 (AC2, AC3), DR-6 (AC3, AC4). Add the enabled-capabilities
  repository (fetched once, shared), the "Default Decision Profile" picker, and render the
  ImageGeneration and Decision pickers only when enabled. Hidden values are preserved on
  save. `PUT settings` is full-replace (every field, nulls included; confirmed in T9), so the
  save payload must always carry every default id, even for a hidden picker. Leaving one out
  nulls it. Settings types, repository and workspace context get `defaultDecisionProfileId`.
  depends-on: T13. parallel-group: C

- [x] **T16** — story: DR-7 (AC1, AC2). Add `uaiCapabilities_decision` to `lang/en.ts`,
  `UaiDecisionProfileSettings` + `isDecisionSettings`, both type-mapper directions, the
  `case "decision"` in `profile-details-workspace-view.element.ts`, and
  `uai-decision-profile-settings` showing a "no settings" message.
  depends-on: T13. parallel-group: C

- [x] **T17** — **wire: frontend into the running backoffice.** story: DR-3 (AC2, AC3),
  DR-5, DR-6 (AC3), DR-7 (AC1, AC2). In a browser on the demo site: create a TypeSafe
  connection and Decision profile through the UI, open the profile, and set the default in
  Settings. Call `UaiDecisionController.ask` for each kind from backoffice code or the
  console. Restart with the flag off and confirm both experimental pickers are hidden, a
  stored value survives a save, and TypeSafe is gone from the provider list.
  `npm run build:core` must pass, including the api-extractor rollup.
  depends-on: T14, T15, T16.

## Deploy

- [x] **T18** — story: DR-8 (AC1-AC5). Add `DefaultDecisionProfileUdi` to
  `AISettingsArtifact`, and export (+ dependency) and import blocks in
  `UmbracoAISettingsServiceConnector`, mirroring ImageGeneration's fix (#227). Add a
  Decision profile round-trip test for `UmbracoAIProfileServiceConnector`. Raise
  `Umbraco.AI.Core`'s floor if needed.
  depends-on: T3, T4. parallel-group: B

- [x] **T19** — **wire: Deploy connector in a real host.** story: DR-8. Resolve the settings
  and profile connectors from a running demo site (with `Umbraco.AI.Deploy` referenced).
  Export a real settings artifact containing `DefaultDecisionProfileUdi` and import it back.
  depends-on: T18, T12.

## Consumers

- [x] **T20** — story: DR-9 (AC1-AC9). In `Umbraco.AI.Automate`: add `AskYesNoDecisionAction`,
  `AskChoiceDecisionAction`, `AskScoreDecisionAction` with settings/output classes per
  `SPEC.md`, modeled on `TranscribeAudioAction`. Compose-time exclusion when the flag is off
  (confirm `ActionCollectionBuilder` supports `Exclude<T>()`, else document the fallback in
  DECISION-LOG). Run-time flag guard. Pick the field editor for options/levels (confirm what
  exists). Raise the `Umbraco.AI.Core` floor.
  depends-on: T5, T3. parallel-group: D

- [x] **T21** — **wire: Automate actions on the demo site.** story: DR-9 (AC6, AC10). Build
  a real automation, "Ask yes/no" → If, run it against the real TypeSafe profile, and
  confirm the branch taken. Restart with the flag off and confirm the actions are absent.
  depends-on: T20, T12.

- [x] **T22** — story: DR-10 (AC1-AC8). In `Umbraco.AI.Agent`'s `SelectAgentForPromptAsync`:
  try Decision first per `ARCHITECTURE.md` decision 7, falling back to the unchanged chat
  path. Per-file pragma. Raise the `Umbraco.AI.Core` floor.
  depends-on: T5, T3. parallel-group: D

- [x] **T23** — **wire: Copilot auto mode on the demo site.** story: DR-10 (AC9). With 2+
  agents and the real TypeSafe default profile, send a Copilot message in auto mode and
  confirm `agent_selected` arrives, with a Decision usage record and no classifier chat call.
  Repeat with the flag off and confirm the chat path is used.
  depends-on: T22, T12.

## v17 and docs

- [x] **T24** — story: DR-12 (AC1, AC2, AC4). Port everything to v17 using the `backport`
  skill: new `v17/feature/decision-capability` branch from `v17/dev`, TypeSafe
  `version.json` = `17.0.0`, all touched products build and test green, draft PR into
  `v17/dev` cross-linked with the v18 PR.
  depends-on: T17, T19, T21, T23.

- [x] **T25** — **wire: v17 demo site.** story: DR-12 (AC3). Real binary question through
  `POST decision/ask` on the v17 demo site.
  depends-on: T24.

- [ ] **T26** — story: DR-13 (AC1-AC4). Write the Umbraco.Docs pages in `SPEC.md` "Docs" for
  both `17/` and `18/` on branch `ai/decision-docs`, pushed as a draft PR.
  depends-on: T17, T21, T23.

## Follow-ups from decision review (25-09-2026)

- [x] **T27** — story: DR-9 (options UX). Add a reusable key/value list property editor UI to
  `@umbraco-ai/core` (`Uai.PropertyEditorUi.KeyValueList`): repeatable rows, each with a key
  and a value text input, plus add, remove and reorder, like the CMS `MultipleTextString`. The value
  is `Array<{ key: string; value: string }>`, and labels can be set in config. Switch
  `AskChoiceDecisionSettings.Options` to a list of key/value items using that editor, map it to
  `AIDecisionOption(Key, Description = Value)`, and delete the text-area `ParseOptions`. Not
  bindable (Umbraco.Automate can't bind complex list items yet, see the upstream issue). Both
  lines.
  depends-on: T20.
- [x] **T28** — **wire: key/value options in the real Automate UI.** In a browser on the demo
  site, add an "Ask pick-one" step, enter options through the new editor, save, reopen (values
  persist), and run the automation against real Jev. The choice output is one of the entered
  keys. Both lines.
  depends-on: T27.

## M.E.AI-direction rework (02-10-2026)

Per DECISION-LOG 02-10-2026 and the reworked ARCHITECTURE "Core types"/"Checks"/"Tracking and
telemetry", decision 3 and decision 6, and SPEC. Push only after T35: T29 changes the provider
contract, so TypeSafe, Web, Agent and Automate don't compile again until their own task lands
(see DECISION-LOG 02-10-2026, "Rework sequencing").

- [x] **T29** — story: DR-1 (AC1-AC4c, AC8-AC14), DR-14 (AC1-AC4, AC6-AC8). Core types and
  pipeline per ARCHITECTURE "Core types": `AIDecisionRequest` (`State`, `Questions`),
  `AIDecisionQuestion.Id` (Context removed), `AIDecisionQuestion<TAnswer>`,
  `AIDecisionScoreLevel`, `AIDecisionAnswer` + the three answer types (`TrueProbability`,
  `IsTrue(threshold)`, optional `Confidence`, index-keyed score probabilities),
  `AIDecisionResponse` (keyed `Answers`) and `AIDecisionResponse<TAnswer>`. Delete the three
  `*DecisionResponse` types. `IAIDecisionClient.GetResponseAsync(AIDecisionRequest, ...)`
  through every middleware/decorator (validating, scoped profile/inline, tracking, OTel, error
  classifier, capability-settings, declared-settings). `IAIDecisionService`: `AskAsync`
  (question, `state`) returning `AIDecisionResponse<TAnswer>`, and `GetDecisionResponseAsync`,
  each with default/Guid/alias/builder overloads. `DecisionQuestionValidator` batch rules
  (≥1 question, unique non-blank ids in a batch). Tracking snapshot and OTel tags per
  "Tracking and telemetry". Update `FakeDecisionClient`/harness and all Core Decision tests.
  Acceptance: `dotnet build`/`test Umbraco.AI/Umbraco.AI.slnx` green.
  depends-on: none.

- [x] **T30** — story: DR-15 (AC1-AC9), DR-14 (AC9, AC10). Provider-answer checks in
  `AIErrorClassifyingDecisionClient` per ARCHITECTURE "Checks": one answer per id and kind,
  probabilities in [0, 1], complete choice/score distributions, choice in keys, score in
  [0, N-1], sum within `max(0.02, 0.005 × count)`, confidence in [0, 1]. Failures are
  `AIProviderException` and recorded as failed.
  depends-on: T29. parallel-group: E

- [x] **T31** — story: DR-2 (AC2-AC10, AC9b), DR-14 (AC5). TypeSafe client takes an
  `AIDecisionRequest`: one HTTP call, questions keyed by id, `State` → `state` (first
  question's instructions when null), level descriptions as score criteria, index-keyed score
  probabilities, omitted zero entries filled for choice and score, usage once per call. Jev's
  question-count limit enforced if documented (else none). Update TypeSafe tests.
  Acceptance: `Umbraco.AI.TypeSafe.slnx` builds and tests green.
  depends-on: T29. parallel-group: E

- [x] **T32** — story: DR-4 (AC1-AC3, AC7, AC11b). `POST decision/ask`: top-level `state`,
  question without `context`, score levels as `{ description }`, flat per-kind responses per
  SPEC (`trueProbability`; `confidence` omitted when null; score probabilities by index; no
  `level`). Controller calls `GetDecisionResponseAsync` with one question and flattens.
  Regenerate the OpenAPI client (`npm run generate-client` against the demo site).
  depends-on: T29. parallel-group: E

- [x] **T33** — story: DR-5 (AC1-AC7). `UaiDecisionController.ask(question, { state?,
  profileIdOrAlias?, signal? })`, level objects, result types per SPEC (binary
  `trueProbability`; score probabilities `Record<number, number>`; optional `confidence`).
  Public rollup updated. Acceptance: `npm run build:core` and vitest green.
  depends-on: T32.

- [x] **T34** — story: DR-10 (AC1-AC8). Auto mode builds a one-question request with the
  user's message as `state` and reads `response.Answer.Choice`. Behavior otherwise unchanged.
  Acceptance: `Umbraco.AI.Agent.slnx` builds and tests green.
  depends-on: T29. parallel-group: E

- [x] **T35** — story: DR-9 (AC1-AC9, AC1b, AC3b, AC8). The three Automate actions: Context →
  request `State`; yes/no `Threshold` setting (double, 0..1, default 0.5, Validation outside
  range), `Answer = Probability >= Threshold`, `Confidence` output removed; pick-one/score
  `Confidence` outputs nullable; score `Level` = label of the nearest level from the question;
  score levels mapped to `AIDecisionScoreLevel`. Acceptance: `Umbraco.AI.Automate.slnx` builds
  and tests green. **Push the branch after this task.**
  depends-on: T29. parallel-group: E

- [x] **T36** — story: DR-16 (AC1-AC4, AC7). `Uai.PropertyEditorUi.DecisionQuestionList` in
  `@umbraco-ai/core` per SPEC: `uui-ref-node` list, item picker (Yes/no, Pick-one, Score) then
  a per-kind config modal (alias, instructions, kind fields; pick-one options via the
  key/value editor, score levels via `Umb.PropertyEditorUi.MultipleTextString`), edit and
  remove. Built like `uai-guardrail-rule-config-builder`. Flat value shape from ARCHITECTURE
  decision 6. Acceptance: `npm run build:core` and vitest green.
  depends-on: none. parallel-group: E

- [x] **T37** — story: DR-16 (AC5, AC6, AC8, AC9). "Ask questions" Automate action
  (`umbracoAI.askDecisions`, `DynamicOutputActionBase`): settings `ProfileId`, `Context`
  (bindable), `Questions` (the T36 editor, flat `AskDecisionsQuestion` list, 1..20); one
  `GetDecisionResponseAsync` call; output object per alias with its kind's fields; output schema
  from settings; validation (aliases, bounds) as `Validation`; excluded at compose time and
  guarded at run time when the flag is off, like the other three.
  depends-on: T35, T36.

- [x] **T38** — **wire: C#, HTTP and TS on the demo site against real Jev.** story: DR-1,
  DR-2 (AC17), DR-4 (AC12), DR-14 (AC1, AC2, AC5), DR-15 (AC10). A three-question batch through
  `IAIDecisionService` makes one Jev call and one usage record; each kind through
  `POST decision/ask` returns the new shapes; a 255-option choice and a 10-level score pass the
  answer checks; `UaiDecisionController.ask` from the backoffice returns typed data. Record
  whether Jev omits zero entries and how its probabilities round.
  depends-on: T30, T31, T32, T33.

- [x] **T39** — **wire: Automate on the demo site.** story: DR-9 (AC10), DR-16 (AC10). In a
  browser: an "Ask yes/no" step with a threshold feeds an If; an "Ask questions" step (yes/no +
  pick-one) is built through the picker and modals, saved, reopened (values persist), and feeds
  an If on `refund.answer` and a Switch on `category.choice`; the run records one Decision call
  for the batch step and both branches match the answers.
  depends-on: T35, T37, T38.

- [x] **T40** — **wire: Copilot auto mode.** story: DR-10 (AC9). Real Copilot message routes
  through Decision with the reworked request.
  depends-on: T34, T38.

- [x] **T41** — story: DR-13 (AC1-AC3). Update the Umbraco.Docs branch `ai/decision-docs`
  (17/ and 18/) to the reworked shapes per SPEC "Docs", plus the key/value options editor,
  bindable criteria, the yes/no threshold and the "Ask questions" action.
  depends-on: T38, T39.

- [x] **T42** — story: DR-12 (AC1, AC2, AC4). Port T29-T37 to `v17/feature/decision-capability`
  with the `backport` skill; all touched products build and test green; push to #428.
  depends-on: T39, T40.

- [x] **T43** — **wire: v17 demo site.** story: DR-12 (AC3). A batch request and an
  "Ask questions" step against real Jev on the v17 demo site.
  depends-on: T42.

## Pluggable agent selection (05-10-2026)

- [x] **T44** — story: DR-10 (AC1-AC8). Merge `origin/v18/dev` (pluggable agent selection, #463)
  into `v18/feature/decision-capability`. Resolve `AIAgentService.cs` by taking dev's version (the
  selection code moved to `IAIAgentSelectionService`) and move the Decision routing into a new
  `DecisionAgentSelector : IAIAgentSelector` per ARCHITECTURE decision 7, registered before
  `LLMAgentSelector`. Update `StickyAgentSelector`'s registration guidance to
  `Insert<StickyAgentSelector>()`. Re-target `DecisionAgentSelectionTests` at the selector (and
  one test through the real selection service proving chain order and the `decision` selector
  id). Acceptance: every touched product builds and tests green.
  depends-on: T43.
- [x] **T45** — **wire: Copilot auto mode through the selector.** story: DR-10 (AC9). Real
  `agents/auto/stream-agui` on the v18 demo site: flag on → Decision picks, selector id
  `decision` recorded; flag off → LLM selector picks.
  depends-on: T44.
- [x] **T46** — story: DR-12. Same merge + selector on `v17/feature/decision-capability` from
  `origin/v17/dev`; build/test green; push to #428. Then merge both capability branches into the
  stacked evaluator branches (#430, #431).
  depends-on: T45.

## Parallel groups

- **A** (after T1): T2, T3, T4, T8. Different files (Decision/, Settings+AIProfileService,
  AIProfileSettingsSerializer, Web capability/provider controllers).
- **B** (each after its own A prerequisite): T6, T9, T10, T18.
- **C** (after T13): T14, T15, T16. Different frontend folders.
- **D** (after T5 + T3): T20, T22. Different products.
- T26 can run alongside T24/T25.
- **E** (after T29): T30, T31, T32, T34, T35, plus T36 (frontend, no Core dependency).
  Different products/files; T30 is Core's error classifier only.

## First shippable slice

T0 → T1 → T2 → T3 → T5 → T6 → T7 → T11 → T12: a developer can install TypeSafe and ask all
three kinds from C# and over HTTP against real Jev. Everything after that adds reach (UI,
TS, Deploy, consumers, v17, docs).
