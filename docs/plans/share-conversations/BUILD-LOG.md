# Build Log

- **SC-03** — SqlServer + SQLite migrations. Commit `3d9a38c3`. One review round, PASS first try.
  Verified live on both providers (not just generated): applied via `dotnet ef database update`
  against a throwaway SQLite file and a throwaway SQL Server Docker container, inspected the real
  schema (`sys.indexes`/`.schema`) to confirm the filtered unique index, cascade delete, and
  uniqueness-violation behavior all work as designed; reviewer independently re-ran the same checks
  rather than trusting the builder's report, including spinning up its own SQL Server container.
  Orchestrator then smoke-tested through the actual application lifecycle: started the demo site for
  real, confirmed `[INF] Running Umbraco.AI.Agent.Conversations database migrations...` /
  `...completed successfully` in its startup log, and confirmed the `umbracoAIConversationsShare`
  table (with the filtered index) exists in the demo site's real SQLite database file — not just a
  standalone throwaway db.
- **SC-02** — EF Core entity + repository implementation (Persistence). Commits `0c0f6d12`
  (feature) + `54983aed` (follow-up: inlined two SC-01 doc comments that pointed at
  ARCHITECTURE.md by section, found during this task's review). Four review rounds: (1) FAIL —
  the `SaveAsync` insert path copied `Id`/`DateShared`/`DateRevoked` straight from the caller's
  domain object instead of stamping them server-side, which could silently insert an
  already-revoked "new" share, a wrong-dated one, or collide on a reused id from an old revoked
  row; also flagged new comments (plus two pre-existing SC-01 ones) pointing at `ARCHITECTURE.md`
  by name. (2) FAIL — one leftover `SPEC.md` comment pointer missed in the first fix. (3) PASS on
  the code; (4) final confirmation pass, PASS. The filtered unique index
  (`(ConversationId, SharedWithUserKey)` unique where `DateRevoked IS NULL`) — the first of its
  kind in this codebase — was verified via scratch migrations against both SQL Server and SQLite;
  the plain filter string worked identically on both, no provider branching needed. No runtime
  entry point exists yet (no migration applied, no controller) — `dotnet build`/`dotnet test`
  (111 unit + 2 integration, unrelated to this change) are the applicable gate until SC-03/SC-08
  land.
- **SC-01** — Domain model + repository interface (Core). Commit `eac90601`. Three review rounds:
  (1) FAIL — 3 Important findings, the key one a real gap in `ARCHITECTURE.md` itself (the
  recipient-uniqueness constraint didn't say whether it applied across revoked rows, which as
  written would have permanently blocked re-sharing with anyone ever revoked); resolved by filtering
  the uniqueness to active rows only and documenting it. (2) FAIL — one stale doc comment left after
  the first fix round. (3) PASS. `dotnet build` clean throughout; no runtime entry point exists yet
  for this foundation task (nothing calls these types until SC-02), so the build itself is the smoke
  gate here, per the reviewer's own conclusion.
