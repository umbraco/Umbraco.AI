# Build Log

- **T0** - `82decdbb8` - worktree `v18/feature/extraction-capability` cut from `v18/dev`, plan folder committed. `dotnet build Umbraco.AI/Umbraco.AI.slnx`: 0 errors.
- **T1** - `15b10a079` - capability 9, flag, empty profile settings, diagnostic id. 20 flag specs
  on (failed first with CS0117). Reviewer PASS first round; unit 1987/1987, integration 32/32.
  Smoke: no entry point yet, live check in T9.
- **T2** - core types, validator, validating client, answer checker. 51 specs on, then more after
  review. Review round 1 FAIL (service-level specs for AC9, AC33-35 deleted instead of left for
  T3); restored, plus list/duration checker guards and extra validation cases. Round 2 PASS. Unit
  2051/2051, integration 32/32. Smoke: no entry point yet, live check in T9.
- **T3** - factory, middleware, notifications, scoped clients, service, builder, defaults, DI, plus
  the `EnabledCapabilities` entry pulled from T6. 11 spec files on (167 tests); fail-first shown by
  mutation (40 failures). Reviewer PASS first round; two doc/helper suggestions applied. Unit
  2144/2144, integration 34/34; Deploy and Automate build. Smoke: DI resolution tests; live in T9.
- **T6** - `defaultExtractionProfileId` on the settings API, MapAll exclusions removed, slot
  validation. 4 specs on (failed first with CS0117/CS1061). Reviewer PASS first round; unit
  2151/2151, integration 34/34. Note: the current Settings screen still omits the field on save
  (same as before T6); T12 must send it. Smoke: live in T9.
- **T7** - `ExtractionProfileSettingsModel` (`$type: "extraction"`) and both mapper arms. 9 specs on
  (failed first with CS0246), run through the real mapper and serializer. Reviewer PASS first
  round; unit 2160/2160, integration 34/34. Smoke: live in T9.
- **T8** - `POST extraction/extract`: base controller, controller, polymorphic models, shared
  `UsageModel`. 55 pinned specs on (fail-first not shown; specs pinned names ahead). Review round 1
  FAIL (unit/item-type parsing accepted "3" and "seconds,minutes"; untested mapping); fixed with
  name-only parsing and new tests, red first. Round 2 PASS. Unit 2235/2235, integration 34/34.
  Smoke: real request in T9.
- **T4** - `Umbraco.AI.Cloudflare` scaffold copied from TypeSafe: provider, settings, capability
  (static `@cf/cloudflare/clairic`), client stub, every add-provider registration point. 14 specs on
  (failed first with CS0246). Reviewer PASS first round; root slnx runs the 14 tests. Security
  follow-ups carried into T5, Core floor into T21. Smoke: live in T9.
- **T5** - `CloudflareExtractionClient`, `CloudflareApi`, cached model-search connection check.
  Specs red against the stub (60 failed), then green. Review round 1 FAIL: token cut before masking,
  2xx `success:false` classed as a network error, adapter duplicated Core's missing-answer check, no
  culture guard. Fixed with red-first tests; round 2 PASS. Cloudflare 112/112 (passes under
  da_DK too). Smoke: live Clairic call in T9; wire-shape guesses listed in DECISION-LOG.
- **T15** - `DefaultExtractionProfileUdi` on the Deploy settings artifact, export with dependency,
  import through the shared resolver. 9 specs on (failed first with CS1061/CS0117). Reviewer PASS
  first round; Deploy 44/44. Core floor left for release prep (T21). Smoke: real host in T16.
- **T17** - `ExtractValuesAction`, settings, field, constant, compose-time exclusion. EX-10 specs
  on; spec's settings theory split so Core-owned rules go through Core (memory gotcha). Review
  round 1 FAIL (action-owned rules and time/datetime formats untested; unit parsing accepted "3");
  fixed red-first; round 2 PASS. Automate 274/274. Core floor deferred to T21. Smoke: T18.
- **T9** - live wire against real Cloudflare on the v18 demo site (flag on). Connection with
  `$Umbraco:AI:Variables:CloudflareAccountID` + `$Umbraco:AI:Secrets:CloudflareApiKey`: save OK,
  connection test passed (model search), models list shows Clairic. Extraction profile created and
  set as default via `PUT settings`. First live extract got 400 `[5006] enum [object Object] not in
  text,number,date at '/damaged/items'`: list `items` must be a plain string. Also a not-found answer
  carried Clairic's guessed span. Both fixed red-first (Cloudflare 126/126; review round 1 FAIL on a
  two-assertion test, split). Re-run: 200, all 7 types correct (Danish "701,10 kr" → 701.1, list
  "mug","teapot" with spans, not-found has no span), ~0.2s, 736 input tokens. Alias profile → 200,
  unknown profile → 404, duplicate id → 400 "Duplicate field id 'orderId'.". Audit log: Extraction
  Succeeded with 711 tokens, and the rejected call logged as Failed with a readable message.
  Real shapes: bare response, `model` "clairic", datetime "…T09:00:00.000Z". The flag-off check
  (404, capability hidden) is folded into the flag-off restart in T18.
- **T10** - `npm run generate-client:core` against the running site. Adds `ExtractionService.extract`,
  the 7+7 `$type` field and answer models, `defaultExtractionProfileId` and the `extraction` profile
  settings type. No unrelated changes. Generated code only, no review.
- **T11** - `src/extraction/`: public `UaiExtractionController`, repository, server data source,
  types, exports via root `exports.ts`. 31 specs on (failed first on missing modules). Review round 1
  FAIL (no compile-time `never` check on answer `$type`); added, plus literal unions for duration
  unit and list item type (values confirmed by the live Cloudflare error). Vitest 156/156;
  `build:core` passes; public rollup has the controller and no generated models. Smoke: T14.
- **T12** - capability label + `icon-filter`, Extraction profile settings type/guard/mappers and
  "no settings" element, "Default Extraction Profile" picker (only when enabled), settings
  repository sends the field both ways. 16 specs on (red first). Label matched to sibling casing.
  Reviewer PASS first round; vitest 172/172, `build:core` passes. Smoke: T14.
- **T13** - `Uai.PropertyEditorUi.ExtractionFieldList` + `Uai.Modal.ExtractionFieldConfigEditor`,
  stacked medium modal per the mockups, stored value matches `ExtractValuesField`. 62 specs on (not
  run red first; first run red only on a test-env stub). Review round 1 FAIL (untranslated item
  type inside a phrase; duration/list saved shape and decimals range untested; alias rule change
  not logged); fixed. Round 2 PASS. Vitest 240/240, `build:core` passes. Smoke: T14.
- **T14** - **wire, browser, v18 demo site (flag on).** First found the branch's frontends had never
  been built and the site predated the core bundle, so AI was missing from the backoffice; fixed by
  `npm run build` + restart (local only, nothing to commit). Then, live:
  - AI › Profiles: "Cloudflare Clairic" row shows the Extraction tag
    ([screenshot](./screenshots/profiles-list.png)).
  - AI › Profiles › Cloudflare Clairic: Cloudflare connection, Clairic model, "This capability has no
    additional settings" ([screenshot](./screenshots/extraction-profile.png)).
  - AI › Settings: "Default Extraction Profile" row with the filter icon shows Cloudflare Clairic
    ([screenshot](./screenshots/settings-extraction-default.png)). Save keeps the stored id.
  - Console: `new UaiExtractionController(host).extract(...)` returned typed answers live; Danish
    "1.250,00 kr" → 1250, date and list correct, 356 tokens.
  - Automation › Extraction demo › Support ticket triage › Extract Values step (double-click node):
    settings, field list, field modal, seven-type picker and duplicate-alias error all match the
    mockups ([settings](./screenshots/extract-values-settings.png),
    [field](./screenshots/extraction-field-config.png),
    [picker](./screenshots/extraction-field-picker.png),
    [error](./screenshots/extraction-field-config-error.png)). Differences: help text under Fields and
    Content comes from the action (T17) rather than the mockup wording.
  - Bug found and fixed: the Alias help showed "e.g. ." because `umb-property-layout` renders
    `description` through UFM, which ate `${…}`. Example moved into the description slot as `<code>`;
    review round 1 FAIL (prettier, fake UUI token), fixed. Vitest 241/241.
  - Flag-off checks (picker hidden, value kept) are folded into the flag-off restart in T18.
- **T16** - **wire, Deploy in a real host.** Demo site run locally with `Umbraco.AI.Deploy` plus
  `Umbraco.Deploy.OnPrem` 18.0.1 (local-only csproj changes). Saving AI settings wrote
  `umbraco/Deploy/Revision/umbraco-ai-settings__….uda` with
  `"DefaultExtractionProfileUdi": "umb://umbraco-ai-profile/c92ed564…"` and that profile as a
  dependency. Live import could not run: Deploy logged "Licensing check failed. Deploy extraction
  will not be enabled." on the unlicensed demo, so the import path is covered by the 9 T15 unit
  specs only. Needs a licensed Deploy site to check import end to end.
- **T18 + T19** - **wire, Automate on the v18 demo site (flag on).** Workspace "Extraction demo"
  with a new API user "Automate service" as its service account (local demo data). Automations
  created through Automate's Management API, published, and run with the trigger endpoint:
  - "Extract Values" is in Automate's action catalogue.
  - **Support ticket triage** (Webhook → Extract Values → If `${ steps.extract.orderId.found }`
    Equals `true` → Log): run Completed; outputs `orderId` "A-88412", `amount` 701.1, `deliveredOn`
    "2026-10-03", `damagedProducts` ["mug","teapot"], each with `found` and `confidence`; the true
    branch ran, the false branch didn't (EX-10 AC14).
  - **Lead intake** (Webhook → Extract Values → Create Content): created "Lead: Nordlys Bryggeri ApS
    – Mette Jensen (demo 2026-11-12)" from a lead email (EX-11 AC1).
  - **Refund triage** (Webhook → Extract Values → If amount found → Request Approval): amount not
    in the ticket came back `found: false`, `value: null`; run Suspended at "Ask a person" with
    prompt "Refund for A-90211 has no clear amount. Approve?" (EX-11 AC2, without Ask Pick-One:
    no Decision provider on this demo).
  - **Voice note to quote** (Transcribe Audio → Extract Values → Create Content): blocked before
    Extraction. OpenAI rejected the uploaded file in Transcribe Audio ("Audio file might be corrupted
    or unsupported") for both m4a and WAV uploaded as File media. Existing Transcribe Audio
    behaviour, not this feature; EX-11 AC3 not proven.
  - **Danish culture (EX-11 AC4), site run with `LANG/LC_ALL=da_DK.UTF-8`:** If
    `${ steps.extract.amount.confidence }` GreaterThan `0.8` took the **false** branch for a 0.99999
    confidence. A Log step showed the bindings render as "0,9999942779854932" and "701,1". Automate
    stringifies numbers with the server culture, so numeric If checks against `0.8` fail on da-DK.
    This is the known risk in SPEC; it affects the Decision actions too. Automate issue drafted for
    the user to review before filing.
  - Step settings screens: see T14.
  - Flag-off check: next entry.
- **Flag-off restart (T9, T14, T18)** - demo site restarted with
  `Umbraco:AI:Experimental:Extraction = false`: `GET capabilities/enabled` lists Chat, Embedding,
  SpeechToText, Decision (no Extraction); Cloudflare missing from `GET providers`;
  `POST extraction/extract` → 404; Extract Values missing from Automate's action catalogue; AI ›
  Settings has no Extraction row, and pressing Save kept the stored `defaultExtractionProfileId`.
  The existing Support ticket triage automation failed its first step with Automate's own message
  "Step 'Extract ticket details' uses 'umbracoAI.extractValues', which is not available…", not
  "Extraction is disabled." (the compose-time exclusion removes the action, so the run-time check
  never runs). SPEC updated to match; see DECISION-LOG.
- **T20** - whole-feature review PASS (Decision parity complete or logged, no unlogged drift, no
  secrets, CI wiring for Cloudflare done; commitlint clean on all commits). Refactor round from its
  suggestions: shared internal enum-name parser and field-id check in Core (used by the API and
  Automate), localized "System Settings" headline in the Extraction element, prettier, and an
  "Adding a field type later" checklist in ARCHITECTURE. Round re-reviewed PASS. Full root suite:
  19 test projects green (Core unit 2243, integration 34, Automate 274, Cloudflare 126, Deploy 44);
  vitest 241. Open for a person: SPEC's 100-second HttpClient timeout assumption, and
  ARCHITECTURE's string-only list answers and separate field-list editor copies.
- **T21** - branch pushed; draft PR #655 into `v18/dev` with demo-site results, screenshots and
  the open decisions (Core floor, Deploy import, Automate culture issue, timeout assumption).
