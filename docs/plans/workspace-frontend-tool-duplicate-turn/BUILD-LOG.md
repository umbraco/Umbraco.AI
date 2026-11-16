# Build log

- **16-11-2026, T0** `32d166011`: 17 pending specs (S1 x8, S2 x6 unit/xUnit; S3 x3 integration/NUnit). Workspace slnx: unit 165 pass/17 skip, integration 2 pass/3 skip. Review PASS after 1 round (story id headers). No smoke: specs only, no runtime change.
- **16-11-2026, T1** `833ea0dd8`: `RewriteMessagesAsync` on the internal repository. S3 AC1-AC3 + cross-conversation + duplicate-id specs green on SQLite (integration 7 pass; unit 165 pass/17 skip). Review PASS after 1 round (AC3 spec forced the failure on the first statement, so proved nothing; now fails the update after the delete, red-checked). Smoke: integration tests on `UmbracoIntegrationTest` with real migrations are the gate, since nothing calls it until T3/T4; the live smoke is T5.
