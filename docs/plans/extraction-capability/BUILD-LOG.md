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
