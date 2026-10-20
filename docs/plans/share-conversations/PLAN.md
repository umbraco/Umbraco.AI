# Plan

## Scope of this checklist

Sliced for the `v18` line. Per `BRIEF.md`, this ships on `v17` too, at the same time — built via
the normal Backport Workflow once the `v18` line is done, not as a duplicate task list (see SC-19).
Copilot Workspace is live on both `v18/dev` and `v17/dev`, so nothing here is blocked on that
anymore.

Story references (`US1`–`US4`, `ACn`) are to `STORIES.md`. Confirmed with the user: owner-side
first (`US1`+`US2`), then recipient-side (`US3`+`US4`).

---

- [x] **SC-01** — Domain model + repository interface (Core)
  story: US1, US2, US3, US4 (foundation)
  depends-on: none
  `AIConversationShare` domain model (`ConversationId`, `SharedWithUserKey`, `SharedByUserKey`,
  `ScopeMode`, `CutoffMessageId`, `DateShared`, `DateRevoked`) and `IAIConversationShareRepository`
  in `Umbraco.AI.Agent.Conversations.Core`, per `ARCHITECTURE.md`'s data model section.

- [x] **SC-02** — EF Core entity + repository implementation (Persistence)
  story: US1, US2, US3, US4 (foundation)
  depends-on: SC-01
  `AIConversationShareEntity`, `EFCoreAIConversationShareRepository`, `DbSet` + configuration on
  `UmbracoAIConversationsDbContext` — mirrors `AIConversationResourceEntity` /
  `EFCoreAIConversationRepository`'s existing shape.

- [x] **SC-03** — SqlServer + SQLite migrations
  story: US1, US2, US3, US4 (foundation)
  depends-on: SC-02
  `UmbracoAIConversations_Shares` migration on both providers (root `CLAUDE.md`'s Database
  Migrations section has the `dotnet ef migrations add` commands; use context
  `UmbracoAIConversationsDbContext`). One task, not two — both providers are generated from the
  same entity change and reviewed together.

- [x] **SC-04** — Owner-side sharing service methods
  story: US1 (AC1, AC3, AC4, AC9), US2 (AC1, AC3, AC4, AC8, AC9)
  depends-on: SC-03
  parallel-group: backend-service
  Share (upsert one row per recipient, idempotent, resolves `CutoffMessageId` server-side per
  `ScopeMode`), list shares, revoke — all owner-checked (`ARCHITECTURE.md` decision 1: this is
  strictly additive to the existing owner-only write path, not a widened check). The service method
  still accepts a collection of recipient keys (`SPEC.md`'s `> ASSUMPTION`), even though the
  frontend only ever calls it with one — no special-casing for "exactly one" here.

- [x] **SC-05** — `hasActiveShares` on the owner's paged conversation list
  story: US2 (AC5, AC6 — backend half)
  depends-on: SC-03
  parallel-group: backend-service
  `AIConversationService.GetConversationsPagedAsync` → `IAIConversationRepository.GetPagedAsync`
  gains a `HasActiveShares: bool` per row, computed in the same EF Core projection
  (`Shares.Any(s => s.DateRevoked == null)`), not a follow-up query per conversation
  (`ARCHITECTURE.md` decision 6). `AIConversation`/`ConversationResponseModel` both gain the
  read-only field. Independent of SC-04 (reads, not writes) — safe to run in parallel.

- [ ] **SC-06** — Share-notification email
  story: US1 (AC7–8)
  depends-on: SC-04
  Publishes `SendEmailNotification` (`Umbraco.Cms.Core.Notifications`) when the owner-side sharing
  service creates a **new** share row — not when it updates an existing recipient's scope. Per
  `ARCHITECTURE.md` decision 5. Separate task from SC-04 despite calling into it, since it's a
  distinct concern (notification, not CRUD) touching different files (a new
  `NotificationEmailModel` + template, not the share/list/revoke logic itself). Subject/body copy
  is a placeholder pending the product/legal-adjacent review flagged in `ARCHITECTURE.md` — this
  task ships the mechanism, not the final wording.

- [ ] **SC-07** — Recipient-side shared-read service methods
  story: US3 (AC1, AC3 backend half), US4 (AC1, AC2, AC5, AC6)
  depends-on: SC-03
  parallel-group: backend-service
  `GetSharedConversationAsync` (resolves the active share, applies the `Sequence`-based cutoff for
  `Snapshot`, falls back to "everything that still exists" if the cutoff message is gone) and
  `GetSharedConversationsPagedAsync`. A separate code path from SC-04/SC-06 and from
  `AIConversationService`'s existing owner methods — never calls `GetOwnedOrThrowAsync` or any
  write method, per `ARCHITECTURE.md` decision 1. Independent files from SC-04/SC-05/SC-06, safe to
  run in parallel with them.

- [ ] **SC-08** — Owner-side share controllers
  story: US1 (AC1, AC3, AC4, AC7–9), US2 (AC1, AC3, AC4, AC8–9)
  depends-on: SC-06
  parallel-group: backend-api
  `POST/GET conversations/{id}/shares`, `DELETE conversations/{id}/shares/{userKey}` — one
  controller per action, per the existing pattern, inheriting `ConversationControllerBase`.
  Depends on SC-06 (not just SC-04) so the email trigger is already in place before the endpoint
  that fires it is wired up. The `GET /conversations` list endpoint's `hasActiveShares` field
  (SC-05) rides on the existing controller — no new controller for it.

- [ ] **SC-09** — Recipient-side share controllers
  story: US3 (AC1, AC3 backend half), US4 (AC1, AC2, AC5, AC6)
  depends-on: SC-07
  parallel-group: backend-api
  `GET conversations/shared-with-me`, `GET conversations/{id}/shared-view`. Independent files
  from SC-08 (different controller classes, no shared route-constants edit expected since both
  root at the existing `conversations` segment) — safe to run in parallel with it.

- [ ] **SC-10** — Wire: backend endpoints resolve for real
  story: US1, US2, US3, US4 (backend acceptance)
  depends-on: SC-08, SC-09
  Not unit-test-green alone. Against the running demo site: create a share (and confirm a real
  email is sent, not just that `SendEmailNotification` was published), confirm `hasActiveShares`
  flips to `true` on the owner's list, list the share, read it back via `shared-view`, see it in
  `shared-with-me`, re-share the same recipient and confirm no second email, revoke it, confirm
  the next `shared-view` request 404s. This is the backend half of every story's acceptance.

- [ ] **SC-11** — OpenAPI client regen + `conversation.repository.ts` additions
  story: US1, US2, US3, US4 (frontend data-access foundation)
  depends-on: SC-10
  Requires the running demo site (root `CLAUDE.md`'s `npm run generate-client`). Adds `share`
  (single `recipientUserKey`, not an array — `SPEC.md`'s repository section), `getShares`,
  `revokeShare`, `requestSharedWithMe`, `requestSharedView`.

- [ ] **SC-12** — Detail model + workspace-context + context-panel readonly plumbing
  story: US4 (AC3–4)
  depends-on: SC-11
  parallel-group: frontend-features
  `UaiConversationDetailModel.isShared`/`readonly`, `toSharedConversationDetailModel()`,
  `isReadonly$` switched to `c?.readonly`, the context panel's four `conversation.isArchived`
  checks switched to `conversation.readonly`, the `readonly-notice` text variant naming the owner.
  Per `SPEC.md`'s "Chat detail view and context panel" section — this is the one place both
  surfaces' read-only behavior is defined, so it's one task rather than two.

- [ ] **SC-13** — EntitySign manifest + owner's tree-item badge
  story: US2 (AC5–7)
  depends-on: SC-11
  parallel-group: frontend-features
  New `entitySign` manifest (`forEntityTypes: ['uai-conversation']`,
  `forEntityFlags: ['Uai.Conversation.IsShared']`, `kind: 'icon'`, `iconName: 'icon-share'`,
  `iconColorAlias: 'positive'`) plus wrapping `uai-copilot-workspace-conversation-tree-item`'s
  existing icon rendering in `<umb-entity-sign-bundle .entityFlags=${hasActiveShares ? [...] :
  []}>`, per `ARCHITECTURE.md` decision 6. Strictly additive to the row's existing pin/chat icon
  logic — no change to that logic itself. A new localization key for the hover-tooltip label is
  part of this task (flagged as not yet drafted in `ARCHITECTURE.md`'s TODOs). Independent files
  from SC-12, SC-14, SC-15, SC-17 — safe to run in parallel with all of them.

- [ ] **SC-14** — "Shared with" context panel section
  story: US2 (AC1–4)
  depends-on: SC-11
  parallel-group: frontend-features
  New section in `conversation-context-panel.element.ts`, rendered first (above Contexts), mirroring
  the existing Contexts/Resources sections' shape exactly (`ARCHITECTURE.md` decision 7): a
  `uui-ref-node` per active recipient with an inline `uui-action-bar` trash-icon remove button that
  calls `DELETE .../shares/{userKey}` directly (no confirm step, no whole-list resubmit — unlike
  Resources, each row has its own persistence identity), an empty state ("Not shared with anyone
  yet."), and a "Choose" `uui-button look="placeholder"` that opens SC-15's picker modal. Owner's own
  conversations only (`conversation.readonly === false`). Independent files from SC-12, SC-13,
  SC-15, SC-17 — safe to run in parallel with all of them, though SC-14 and SC-15 are wired together
  in SC-16.

- [ ] **SC-15** — Share-picker modal
  story: US1 (AC1, AC3–6, AC10)
  depends-on: SC-11
  parallel-group: frontend-features
  New `UAI_SHARE_PICKER_MODAL`: a one-item recipient-picker field (empty state opens the real
  `UMB_USER_PICKER_MODAL` constrained to a single pick — `ARCHITECTURE.md` decision 7's TODO to
  confirm that constraint is actually achievable is this task's first step, not an afterthought),
  a `uui-select` Snapshot/Live dropdown, and the disclosure banner (shown only when the conversation
  has attachments; copy is a placeholder pending the same review flagged for the email in SC-06).
  Submit is disabled until a person is picked. On submit, calls `share()` with the one picked
  recipient + mode. Independent files from SC-12, SC-13, SC-14, SC-17 — safe to run in parallel
  with all of them, though SC-14 and SC-15 are wired together in SC-16.

- [ ] **SC-16** — Wire: "Shared with" section + picker modal end-to-end
  story: US1, US2 (frontend acceptance)
  depends-on: SC-14, SC-15
  Acceptance: verified in the running backoffice, not unit tests alone. Opening a conversation's
  context panel, clicking "Choose", picking a real user and a mode, submitting, and seeing the new
  row appear in the "Shared with" section without a reload; then removing that row and confirming
  it disappears and the colleague's next request for the conversation 404s.

- [ ] **SC-17** — "Shared with me" sidebar grouping
  story: US3 (AC1–4)
  depends-on: SC-11
  parallel-group: frontend-features
  New grouping parallel to Pinned/Recent, sourced from `requestSharedWithMe()`, no entity actions
  rendered on its entries. Each entry gets the same icon + entity-sign badge visual treatment as
  SC-13's owner-side rows, but note the difference: these rows don't carry a `hasActiveShares`
  field from the backend (they come from `SharedConversationSummaryResponseModel`, not
  `ConversationResponseModel`) — every row in this group is definitionally shared-with-me, so its
  badge's `entityFlags` is set unconditionally, not computed from a boolean field. The hover
  tooltip reads "Shared by \<owner\>" (the summary's owner name), not the generic "Shared" SC-13's
  rows show. Independent files from SC-12, SC-13, SC-14, SC-15 — safe to run in parallel with all
  of them.

- [ ] **SC-18** — Wire: recipient reads a shared conversation end-to-end
  story: US3, US4 (frontend acceptance)
  depends-on: SC-12, SC-17
  Opening an entry from the "Shared with me" sidebar grouping loads via
  `requestSharedView`/`toSharedConversationDetailModel()` and renders the chat + context panel
  read-only, with the shared-by-owner notice, and no "Shared with" section (that's owner-only — see
  SC-14's scoping). Acceptance: verified in the running backoffice as a real click-through, not
  just unit tests on the store/model.

- [ ] **SC-19** — Backport to v17
  story: US1, US2, US3, US4 (all)
  depends-on: SC-01 through SC-18
  Branch off `v17/dev` (per the repo's Backport Workflow), replay SC-01–SC-18, verify the same wire
  checks (SC-10, SC-16, SC-18) against the `v17` demo site. Not a new design pass —
  `ARCHITECTURE.md`/`SPEC.md` apply unchanged; only the target branch differs.
