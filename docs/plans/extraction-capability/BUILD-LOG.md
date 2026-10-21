# Build Log

- **T0** - `82decdbb8` - worktree `v18/feature/extraction-capability` cut from `v18/dev`, plan folder committed. `dotnet build Umbraco.AI/Umbraco.AI.slnx`: 0 errors.
- **T1** - `15b10a079` - capability 9, flag, empty profile settings, diagnostic id. 20 flag specs
  on (failed first with CS0117). Reviewer PASS first round; unit 1987/1987, integration 32/32.
  Smoke: no entry point yet, live check in T9.
- **T2** - core types, validator, validating client, answer checker. 51 specs on, then more after
  review. Review round 1 FAIL (service-level specs for AC9, AC33-35 deleted instead of left for
  T3); restored, plus list/duration checker guards and extra validation cases. Round 2 PASS. Unit
  2051/2051, integration 32/32. Smoke: no entry point yet, live check in T9.
