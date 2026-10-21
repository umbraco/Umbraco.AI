# Plan

Task checklist for `feature-build`. v18 work happens on `v18/feature/extraction-capability`, in a
worktree branched from `v18/dev`. v17 is ported after v18 is fully built and wired (T22). Docs go to
Umbraco.Docs (T24).

Read the repo's `add-ai-capability` and `add-provider` skills before starting. Every Core file
copies its Decision sibling in `Umbraco.AI/src/Umbraco.AI.Core/Decision/`; open that file and copy
from it, not from memory.

Pending specs are in `specs/` in this folder, at the path they belong in. Each task moves its
spec files into the test project, removes `Skip = "Pending"` / `it.skip`, and confirms they fail
before the code lands. Spec headers list names the spec writer guessed; rename freely. Final homes:

- Core: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Extraction/` and `.../Tests.Integration/Extraction/`
- Provider: `Umbraco.AI.Cloudflare/tests/Umbraco.AI.Cloudflare.Tests.Unit/`
- Automate: `Umbraco.AI.Automate/tests/Umbraco.AI.Automate.Tests.Unit/Extraction/`
- Deploy: `Umbraco.AI.Deploy`'s existing unit test project

## Setup

- [x] **T0** - story: none (setup). Create the worktree `v18/feature/extraction-capability` from
  `v18/dev` and commit this plan folder to it as the first commit.
  Acceptance: `dotnet build Umbraco.AI/Umbraco.AI.slnx` passes on the new branch.
  depends-on: none.

## Core

- [x] **T1** - story: EX-3 (AC1-AC3, AC5-AC7). Add `AICapability.Extraction = 9`,
  `AIExperimentalOptions.Extraction`, its arm in `AIExperimentalFeatures`, the
  `UMBRACOAI_EXTRACTION` experimental id, and the Extraction arm in `AIConnectionService` and
  `AIProfileService`'s capability checks. Add empty sealed `AIExtractionProfileSettings` and its
  case in `AIProfileSettingsSerializer`.
  Acceptance: EX-3 specs green; a positive-control test with the flag on.
  depends-on: T0. parallel-group: A

- [x] **T2** - story: EX-1 (AC1-AC14, AC22-AC29, AC33-AC35). Add the core types in
  `Umbraco.AI.Core/Extraction/` per `ARCHITECTURE.md` decision 1 (request, response, options,
  field and answer base classes, the seven field/answer pairs, `AIExtractionSpan`,
  `AIDurationUnit`, `AIExtractionListItemType`), `IAIExtractionClient`, `ExtractionFieldValidator`,
  `ValidatingExtractionClient` and `ExtractionAnswerChecker`. A fake client in
  `Umbraco.AI.Tests.Common` returns canned answers.
  Acceptance: validator and checker specs green with the fake client.
  depends-on: T1.

- [ ] **T3** - story: EX-1 (AC15-AC21, AC30-AC32, AC36, AC37), EX-3 (AC10), EX-4 (AC1 backend), EX-8. Add
  `AISettings.DefaultExtractionProfileId`, `AIOptions.DefaultExtractionProfileAlias`, the profile
  arms in `AIProfileService`, `IAIExtractionCapability` + base classes, `IAIExtractionClientFactory`
  with the wrap order in decision 3, the middleware collection, `AIErrorClassifyingExtractionClient`,
  `AIOpenTelemetryExtractionMiddleware`, `AITrackingExtractionMiddleware`, the two notifications,
  `ScopedProfileExtractionClient`, `IAIExtractionService`, `AIExtractionService`,
  `AIExtractionBuilder`, `Constants.FeatureTypes.InlineExtraction`, and the DI registration in the
  Core composer.
  Acceptance: service specs green, including the factory-level test that validation sits outside
  error classification and tracking (EX-1 AC31, AC32) and the nested-call context test (EX-8 AC8).
  depends-on: T2.

## Provider

- [ ] **T4** - story: EX-2 (AC1-AC6, AC28). Scaffold `Umbraco.AI.Cloudflare` with the
  `add-provider` skill and every registration point it lists (marketplace metadata, install
  scripts, CI, `changelog.config.json` scope, release manifest, root `.slnx`, `version.json` =
  `18.0.0`). Add `CloudflareProvider`, `CloudflareProviderSettings`,
  `CloudflareExtractionCapability` (static model `@cf/cloudflare/clairic`), and the test project in
  the root `.slnx`. The client may throw `NotImplementedException` until T5.
  Acceptance: `dotnet build Umbraco.AI.Cloudflare/Umbraco.AI.Cloudflare.slnx` and its tests pass;
  the test project shows up in a root-slnx `dotnet test` run.
  depends-on: T3. parallel-group: B

- [ ] **T5** - story: EX-2 (AC7-AC27). Implement `CloudflareExtractionClient`, copying
  `TypeSafeDecisionClient`: request and response mapping per `SPEC.md` "Cloudflare provider", both
  envelope shapes, 429 retry (3 attempts, 30s cap, `Retry-After`), Cloudflare error codes in the
  exception, and the cached model-search connection check. Tests fake only the
  `HttpMessageHandler`; delay and clock are injected.
  Acceptance: EX-2 specs green.
  depends-on: T4.

## Management API

- [ ] **T6** - story: EX-3 (AC8), EX-4 (AC1). Add Extraction to
  `EnabledCapabilitiesController.ImplementedCapabilities`, and `defaultExtractionProfileId` to the
  settings request/response models and `SettingsMapDefinition`.
  Also add an `IAIExtractionCapability` arm to `ImplementedCapabilitiesSurfaceTests.KindOf` and a
  `DefaultExtractionProfileId` row to `UpdateSettingsProfileCapabilityValidationTests.AllSlots`.
  Acceptance: specs green for both.
  depends-on: T3. parallel-group: B

- [ ] **T7** - story: EX-5 (AC3 backend). Add `ExtractionProfileSettingsModel`
  (`$type: "extraction"`) and both directions in `ProfileMapDefinition`.
  Acceptance: profile mapping spec green.
  depends-on: T1. parallel-group: B

- [ ] **T8** - story: EX-6 (AC1-AC12), EX-3 (AC9). Add `Constants.ManagementApi.Feature.Extraction`,
  `ExtractionControllerBase`, `ExtractController` (`POST extraction/extract`) and the polymorphic
  request/response models in `Umbraco.AI.Web/Api/Management/Extraction/`, copying
  `Api/Management/Decision/`. Answers dictionary uses `ExactKeyDictionaryJsonConverter`; return
  `OkObjectResult { DeclaredType = typeof(ExtractResponseModel) }`.
  Acceptance: controller specs green, including a serialization test that `$type` and exact keys
  survive.
  depends-on: T3.

- [ ] **T9** - **wire: Cloudflare + Extraction API into the demo site.** story: EX-1, EX-2 (AC7,
  AC22), EX-3 (AC4, AC9), EX-4 (AC1), EX-6, EX-8 (AC1, AC2). With the flag on and a real
  Cloudflare account: create a connection (test passes), create an Extraction profile, set it as
  default via `PUT settings`, then extract all seven field types via `IAIExtractionService` and via
  a real authenticated `POST extraction/extract`. Confirm one usage record and one audit entry per
  call. With the flag off, confirm 404 and that Extraction is absent from `GET capabilities/enabled`.
  Record real Clairic response shapes (envelope or bare) in `BUILD-LOG.md`, and fix the client if
  they differ from the docs.
  depends-on: T5, T6, T7, T8.

## Frontend (Core)

- [ ] **T10** - story: EX-4, EX-5, EX-7 (prerequisite). Regenerate the OpenAPI client
  (`npm run generate-client` against the running demo site) once, after T9. Commit only the
  generated `src/api/` changes.
  depends-on: T9.

- [ ] **T11** - story: EX-7 (AC1-AC5). Add `src/extraction/`: `UaiExtractionController`
  (`@public`), repository, server data source, `types.ts`, exported through the root
  `src/exports.ts`. Map field kind to `$type`. No generated types leak.
  Acceptance: `npm run build:core` passes, including the api-extractor rollup.
  depends-on: T10. parallel-group: C

- [ ] **T12** - story: EX-4 (AC2-AC6), EX-5 (AC1, AC2). Add `uaiCapabilities_extraction` +
  `icon-filter`, `UaiExtractionProfileSettings` + type guard, both mapper directions, the
  `case "extraction"` in `profile-details-workspace-view.element.ts`, the "no settings" element,
  and the "Default Extraction profile" picker on the Settings screen (shown only when enabled;
  hidden value preserved on save because `PUT settings` is full-replace).
  Acceptance: `npm run build:core` passes.
  depends-on: T10. parallel-group: C

- [ ] **T13** - story: EX-9 (AC1-AC18). Add `Uai.PropertyEditorUi.ExtractionFieldList` and
  `Uai.Modal.ExtractionFieldConfigEditor` in `src/property-editors/extraction-field-list/`,
  copying `decision-question-list/`. Match the two mockups in `mockups/` (stacked layout, medium
  modal). New strings go in `lang/en.ts`; Cancel, Save, Edit and Remove reuse the general keys.
  Acceptance: `npm run build:core` passes.
  depends-on: T10. parallel-group: C

- [ ] **T14** - **wire: frontend into the running backoffice.** story: EX-4 (AC2-AC6), EX-5,
  EX-7 (AC2), EX-9. In a browser on the demo site: create a Cloudflare connection and an
  Extraction profile through the UI, reopen the profile, set the default in Settings, and call
  `UaiExtractionController.extract` from the console. Restart with the flag off and confirm the
  picker is hidden and a stored value survives a save. Compare the field list editor and modal
  against the mockups.
  depends-on: T11, T12, T13.

## Deploy

- [ ] **T15** - story: EX-12 (AC1-AC5). Add `DefaultExtractionProfileUdi` to `AISettingsArtifact`,
  export (+ dependency) and import in `UmbracoAISettingsServiceConnector`, and an Extraction profile
  round-trip test for `UmbracoAIProfileServiceConnector`. Raise the `Umbraco.AI.Core` floor if
  needed.
  depends-on: T3. parallel-group: B

- [ ] **T16** - **wire: Deploy connectors in a real host.** story: EX-12. On the demo site with
  `Umbraco.AI.Deploy` referenced, export a settings artifact containing `DefaultExtractionProfileUdi`
  and import it back.
  depends-on: T15, T9.

## Automate

- [ ] **T17** - story: EX-10 (AC1-AC13, AC15-AC21). In `Umbraco.AI.Automate/src/.../Actions/`, add
  `ExtractValuesAction`, `ExtractValuesSettings` and `ExtractValuesField`, copying
  `AskDecisionsAction`. Outputs per `ARCHITECTURE.md` decision 8 (every alias always present).
  Compose-time exclusion and run-time flag check, as for the Decision actions. Raise the
  `Umbraco.AI.Core` floor.
  Acceptance: EX-10 specs green, including the output schema test for every field type.
  depends-on: T3. parallel-group: B

- [ ] **T18** - **wire: Extract Values on the demo site.** story: EX-10 (AC5, AC9, AC14, AC15),
  EX-9 (AC11). Build an automation in the backoffice: Webhook → Extract Values → If on
  `orderId.found`. Post a real body and confirm the branch taken and the outputs in the run detail.
  Restart with the flag off and confirm the action is gone and the existing step fails with
  "Extraction is disabled."
  depends-on: T17, T14.

- [ ] **T19** - **wire: idea recipes end to end.** story: EX-11 (AC1-AC4). Build the three recipes
  in EX-11 on the demo site with no code. Then switch the site to `da-DK` and run the confidence
  check (AC4). Record every result in `BUILD-LOG.md`. If AC4 shows the culture bug, draft the
  Automate issue (problem and expected behaviour only) for the user to review before filing.
  depends-on: T18.

## Review

- [ ] **T20** - story: all. Whole-branch review with the `reviewer` agent: Decision parity (every
  Decision file has its Extraction sibling or a note in `ARCHITECTURE.md` saying why not), no
  secrets, public API rules, localization keys.
  depends-on: T16, T19.

- [ ] **T21** - story: all. Open the v18 PR into `v18/dev` as a draft, with the demo-site results
  from T9, T14, T16, T18 and T19 in the description.
  depends-on: T20.

## v17 and docs

- [ ] **T22** - story: EX-13 (AC1-AC3). Port everything with the `change-backport` skill:
  `v17/feature/extraction-capability` from `v17/dev`, Cloudflare `version.json` = `17.0.0`,
  `AICapability.Extraction = 9`, the v17 frontend data-source split (EX-7 AC6). All touched products
  build and test green. Draft PR into `v17/dev`, cross-linked with the v18 PR.
  depends-on: T21.

- [ ] **T23** - **wire: v17 demo site.** story: EX-13 (AC4), EX-7 (AC6). Real
  `POST extraction/extract` and one Extract Values run on the v17 demo site.
  depends-on: T22.

- [ ] **T24** - story: EX-14 (AC1-AC3). Write the Umbraco.Docs pages for both `17/` and `18/`
  (capability, Cloudflare provider, Extract Values action with one recipe), marked experimental, on
  a branch pushed as a draft PR. Flag it for human review before it goes public.
  depends-on: T21.
