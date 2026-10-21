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
