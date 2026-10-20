# Build Log

- **SC-09** — Recipient-side share Management API controllers. Commit `cd77a0ec`. Two review
  rounds: (1) FAIL — a builder deleted two pending DI-resolution test placeholders on a false
  premise (claimed SC-08 set a delete-them precedent; SC-08 actually left its own skipped and
  retargeted). Restored both and aligned all three placeholders (including the already-committed
  SC-08 one) on identical wording pointing at SC-10's live verification. Also resolved a new
  finding into an explicit decision (ARCHITECTURE.md decision 9, STORIES.md US4 AC7): a Snapshot
  share's `dateModified`/`lastMessageAt` were about to leak that the owner kept chatting after the
  cutoff — even with no message content exposed, the metadata alone said "something happened after
  this." Fixed by freezing both fields at the cutoff message's own timestamp for Snapshot shares
  only. This required a disclosed, reviewed widening of SC-07's already-committed
  `IAIConversationShareReadService.GetSharedConversationAsync` return tuple (adding the share's
  `ScopeMode` — data already in scope, no new query) since the Web layer had no other way to tell
  Snapshot from Live; reviewer confirmed this was the right call given the interface is pre-release
  (only exists on this feature branch, no external consumer to break). (2) PASS. `dotnet
  build`/`dotnet test`: 164 unit + 3 skipped + 2 integration passing throughout.
- **SC-08** — Owner-side share Management API controllers. Commit `848707f9`. Two review rounds:
  (1) FAIL — `Enum.TryParse` alone accepts a numeric string (`"7"`) for a 2-member enum with no
  corresponding value, letting a nonsense scope mode silently flow through and behave like Live
  (no cutoff ever gets set for an undefined value); fixed by requiring `Enum.IsDefined` too. The
  builder's own addition beyond the dispatch — a distinct 400 for an unparseable scope mode, plus
  catching the cutoff-unresolvable exception before the general ownership/not-found catch (C#
  matches the first applicable `catch`, so order matters: the more specific exception type must
  come first or it's unreachable) — both held up under review. (2) PASS, plus 2 added test cases
  (a GET happy path, the "Unknown user" display-name fallback) that were real coverage gaps, not
  required fixes. `dotnet build`/`dotnet test`: 157 unit + 3 skipped + 2 integration passing
  throughout.
- **SC-07** — Recipient-side shared-read service methods. Commit `9f2044c0`. Two review rounds:
  (1) FAIL — 1 Important finding: a cross-service test (revoke via SC-04's service, then read via
  this one) used a mock with no actual shared state, so it would have passed even if revoke were a
  complete no-op — rewritten with a stateful mock and proven to genuinely fail when the revoke
  callback is disabled. Plus two cheap fixes: a misleadingly-named test (asserted a `null` return but
  was named `_ThrowsNotFound`) and lingering plan-doc citations in two files that a follow-up round
  caught the interface file had too, after the first fix only touched the implementation. (2) PASS.
  Follows the existing `ConversationChatHistoryProvider` precedent for a specialized in-assembly
  reader bypassing the owner-gated service — confirmed this is a real precedent, not invented.
  `dotnet build`/`dotnet test`: 147 unit + 3 skipped + 2 integration passing throughout.
- **SC-06** — Share-notification email. Commit `feab5c0a`. Pre-build research corrected a real
  inaccuracy in ARCHITECTURE.md decision 5 (the design doc said the sharing service "publishes
  `SendEmailNotification`" directly — checked against the real `Umbraco.Cms` source and found that's
  backwards; the actual mechanism is `IEmailSender.SendAsync`). Two review rounds on the build
  itself: (1) FAIL — 1 Critical (the email's "view conversation" link used an invented route and the
  wrong constant — the section *alias* instead of its *pathname* — so it pointed nowhere real) + 3
  Important (a happy-path test loose enough to pass even with the broken link; the handler could
  still throw outside its one `try/catch`, which — because `AIConversationShareService` awaits this
  notification inline per recipient — would have failed the owner's whole share request and silently
  dropped any remaining recipients; an unnecessary/incorrectly-justified assembly-visibility change
  that a simpler, already-proven form would have covered). All fixed, verified against the real
  frontend route files and a working sibling precedent rather than re-guessed. (2) PASS. Also
  resolved as an explicit, documented trade-off (not a defect): the email sends inline rather than
  queued to a background worker, accepting SMTP latency on the share request for what's scoped as a
  low-volume, internal-collaboration signal. `dotnet build`/`dotnet test`: 137 unit + 2 skipped + 2
  integration passing throughout.
- **SC-05** — `hasActiveShares` on the owner's paged conversation list. Commit `9b9c0460`. Two
  review rounds: (1) FAIL — the new tests each set up only one conversation, so they'd still pass if
  the production query dropped its `ConversationId` correlation entirely (every row would report the
  same global flag); fixed by adding a two-conversations-same-owner test proving the flag differs
  per row in the same page. Also added, same round: a doc-comment disclosure that the field is only
  accurate from the paged list endpoint (always `false` from `GetById`/create/update, which don't
  compute it), and moved the new SQLite test scaffolding into a shared `Fixtures/` folder for later
  Conversations-repository tests to reuse instead of re-copying it. (2) PASS. First EF-Core-backed
  (not pure-mock) test harness for this product's repository layer — justified since a mocked
  repository can't meaningfully prove a correlated-subquery shape is actually correlated. `dotnet
  build`/`dotnet test`: 130 unit + 2 skipped + 2 integration passing throughout (one known
  pre-existing flaky integration test, unrelated, noted but not chased).
- **SC-04** — Owner-side sharing service methods. Commit `3cb6d512`. Three review rounds: (1) FAIL
  — 3 Important findings: a new domain notification (`AIConversationShareCreatedNotification`)
  didn't match this codebase's real notification shape (missing `EventMessages`, verified against
  `AIAgentSelectedNotification`'s real precedent rather than the builder's unverified claim there
  was none); a stale plan-task-id code comment; and the serious one — a Snapshot share whose cutoff
  message couldn't be resolved (empty conversation, or a race with regenerate) silently fell back to
  behaving like a Live share with no ceiling, the opposite of what the owner asked for. Resolved by
  the orchestrator as a new, explicit design decision (ARCHITECTURE.md decision 8, STORIES.md AC11):
  fail the share request outright rather than silently widening it. Also settled during this round:
  no unit tests existed for the new service, against this codebase's own convention of every service
  having them — added real tests (moving the two staged spec stubs into the real test project) rather
  than deferring. (2) PASS on the code, with two non-blocking suggestions (a loose exception-type
  assertion that wouldn't have caught the exact bug just fixed if it regressed; a wrong doc-comment
  cross-reference). (3) Fixed both anyway since they were cheap and the first one meaningfully
  strengthens the regression test; final PASS. `dotnet build`/`dotnet test`: 125 unit (up from 111)
  + 2 integration passing throughout, 2 unit tests honestly left skipped pointing at SC-07/SC-08 work
  they depend on.
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
