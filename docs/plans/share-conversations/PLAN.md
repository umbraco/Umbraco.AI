# Plan

## Blocked until the Copilot RC merges

None of these tasks can start yet. Copilot Workspace (the product this feature builds on) is not
on `v18/dev` or `v17/dev` — it's still confined to the Copilot RC release branches, deliberately
held out of dev until that release goes final (`BRIEF.md`). This checklist is ready to execute the
moment that merge lands on `v18/dev`; re-check `[[project_copilot_rc_2026_08]]` (memory) or the RC
branches directly before starting `umb-build-loop`.

## Scope of this checklist

Sliced for the `v18` line. Per `BRIEF.md`, this ships on `v17` too, at the same time — built via
the normal Backport Workflow once the `v18` line is done, not as a duplicate task list (see SC-16).

Story references (`US1`–`US4`, `ACn`) are to `STORIES.md`. Confirmed with the user: owner-side
first (`US1`+`US2`), then recipient-side (`US3`+`US4`).

---

- [ ] **SC-01** — Domain model + repository interface (Core)
  story: US1, US2, US3, US4 (foundation)
  depends-on: none
  `AIConversationShare` domain model (`ConversationId`, `SharedWithUserKey`, `SharedByUserKey`,
  `ScopeMode`, `CutoffMessageId`, `DateShared`, `DateRevoked`) and `IAIConversationShareRepository`
  in `Umbraco.AI.Agent.Conversations.Core`, per `ARCHITECTURE.md`'s data model section.

- [ ] **SC-02** — EF Core entity + repository implementation (Persistence)
  story: US1, US2, US3, US4 (foundation)
  depends-on: SC-01
  `AIConversationShareEntity`, `EFCoreAIConversationShareRepository`, `DbSet` + configuration on
  `UmbracoAIConversationsDbContext` — mirrors `AIConversationResourceEntity` /
  `EFCoreAIConversationRepository`'s existing shape.

- [ ] **SC-03** — SqlServer + SQLite migrations
  story: US1, US2, US3, US4 (foundation)
  depends-on: SC-02
  `UmbracoAIConversations_Shares` migration on both providers (root `CLAUDE.md`'s Database
  Migrations section has the `dotnet ef migrations add` commands; use context
  `UmbracoAIConversationsDbContext`). One task, not two — both providers are generated from the
  same entity change and reviewed together.

- [ ] **SC-04** — Owner-side sharing service methods
  story: US1 (AC1–4, AC8), US2 (AC1–2, AC4–5)
  depends-on: SC-03
  parallel-group: backend-service
  Share (upsert one row per recipient, idempotent, resolves `CutoffMessageId` server-side per
  `ScopeMode`), list shares, revoke — all owner-checked (`ARCHITECTURE.md` decision 1: this is
  strictly additive to the existing owner-only write path, not a widened check).

- [ ] **SC-05** — Share-notification email
  story: US1 (AC6–7)
  depends-on: SC-04
  Publishes `SendEmailNotification` (`Umbraco.Cms.Core.Notifications`) when `ShareConversationAsync`
  creates a **new** share row — not when it updates an existing recipient's scope. Per
  `ARCHITECTURE.md` decision 5. Separate task from SC-04 despite calling into it, since it's a
  distinct concern (notification, not CRUD) touching different files (a new
  `NotificationEmailModel` + template, not the share/list/revoke logic itself).

- [ ] **SC-06** — Recipient-side shared-read service methods
  story: US3 (AC1–2 backend half), US4 (AC1–2, AC5–6)
  depends-on: SC-03
  parallel-group: backend-service
  `GetSharedConversationAsync` (resolves the active share, applies the `Sequence`-based cutoff for
  `Snapshot`, falls back to "everything that still exists" if the cutoff message is gone) and
  `GetSharedConversationsPagedAsync`. A separate code path from SC-04/SC-05 and from
  `AIConversationService`'s existing owner methods — never calls `GetOwnedOrThrowAsync` or any
  write method, per `ARCHITECTURE.md` decision 1. Independent files from SC-04/SC-05, safe to run
  in parallel with them.

- [ ] **SC-07** — Owner-side share controllers
  story: US1 (AC1–4, AC6–8), US2 (AC1–2, AC4–5)
  depends-on: SC-05
  parallel-group: backend-api
  `POST/GET conversations/{id}/shares`, `DELETE conversations/{id}/shares/{userKey}` — one
  controller per action, per the existing pattern, inheriting `ConversationControllerBase`.
  Depends on SC-05 (not just SC-04) so the email trigger is already in place before the endpoint
  that fires it is wired up.

- [ ] **SC-08** — Recipient-side share controllers
  story: US3 (AC1–2 backend half), US4 (AC1–2, AC5–6)
  depends-on: SC-06
  parallel-group: backend-api
  `GET conversations/shared-with-me`, `GET conversations/{id}/shared-view`. Independent files
  from SC-07 (different controller classes, no shared route-constants edit expected since both
  root at the existing `conversations` segment) — safe to run in parallel with it.

- [ ] **SC-09** — Wire: backend endpoints resolve for real
  story: US1, US2, US3, US4 (backend acceptance)
  depends-on: SC-07, SC-08
  Not unit-test-green alone. Against the running demo site: create a share (and confirm a real
  email is sent, not just that `SendEmailNotification` was published), list it, read it back via
  `shared-view`, see it in `shared-with-me`, re-share the same recipient and confirm no second
  email, revoke it, confirm the next `shared-view` request 404s. This is the backend half of every
  story's acceptance.

- [ ] **SC-10** — OpenAPI client regen + `conversation.repository.ts` additions
  story: US1, US2, US3, US4 (frontend data-access foundation)
  depends-on: SC-09
  Requires the running demo site (root `CLAUDE.md`'s `npm run generate-client`). Adds `share`,
  `getShares`, `revokeShare`, `requestSharedWithMe`, `requestSharedView` per `SPEC.md`.

- [ ] **SC-11** — Detail model + workspace-context + context-panel readonly plumbing
  story: US4 (AC3–4)
  depends-on: SC-10
  parallel-group: frontend-features
  `UaiConversationDetailModel.isShared`/`readonly`, `toSharedConversationDetailModel()`,
  `isReadonly$` switched to `c?.readonly`, the context panel's four `conversation.isArchived`
  checks switched to `conversation.readonly`, the `readonly-notice` text variant. Per `SPEC.md`'s
  "Chat detail view and context panel" section — this is the one place both surfaces' read-only
  behavior is defined, so it's one task rather than two.

- [ ] **SC-12** — Share modal
  story: US1 (AC1–6), US2 (AC1–3)
  depends-on: SC-10
  parallel-group: frontend-features
  `UAI_SHARE_CONVERSATION_MODAL`: recipient picker, Snapshot/Live choice, current-recipients list
  with revoke, disclosure notice (copy is a TODO from `ARCHITECTURE.md` — flag for review before
  this task is called done, not just implemented with placeholder text). Independent files from
  SC-11 and SC-14 — safe to run in parallel with both.

- [ ] **SC-13** — Wire: Share entity action into the conversation menu
  story: US1, US2 (frontend acceptance)
  depends-on: SC-12
  `UaiConversationShareAction` registered in `manifests.ts`, owner-only per existing entity-action
  scoping, opens SC-12's modal. Acceptance: verified in the running backoffice — the action
  actually appears in a conversation's ⋯ menu and completes a real share end-to-end, not just a
  unit test on the action class.

- [ ] **SC-14** — "Shared with me" sidebar grouping
  story: US3 (AC1–3)
  depends-on: SC-10
  parallel-group: frontend-features
  New grouping parallel to Pinned/Recent, sourced from `requestSharedWithMe()`, no entity actions
  rendered on its entries. Independent files from SC-11 and SC-12 — safe to run in parallel with
  both.

- [ ] **SC-15** — Wire: recipient reads a shared conversation end-to-end
  story: US3, US4 (frontend acceptance)
  depends-on: SC-11, SC-14
  Opening an entry from the "Shared with me" sidebar grouping loads via
  `requestSharedView`/`toSharedConversationDetailModel()` and renders the chat + context panel
  read-only, with the shared-by-owner notice. Acceptance: verified in the running backoffice as a
  real click-through, not just unit tests on the store/model.

- [ ] **SC-16** — Backport to v17
  story: US1, US2, US3, US4 (all)
  depends-on: SC-01 through SC-15
  Branch off `v17/dev` once available (per the repo's Backport Workflow), replay SC-01–SC-15,
  verify the same wire checks (SC-09, SC-13, SC-15) against the `v17` demo site. Not a new design
  pass — `ARCHITECTURE.md`/`SPEC.md` apply unchanged; only the target branch differs.
