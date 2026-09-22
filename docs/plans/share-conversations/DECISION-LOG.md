# Decision Log

- **2026-09-22** — Read-only shared link over live multi-user co-editing. Building on top of the
  Copilot Workspace conversation model (single-owner `UserKey`, no ACL), full collaborative editing
  (two people writing into one conversation) would need new concurrency/locking work; a read-only
  link reuses the existing owner-only write path unchanged. Killed: live shared chat editing.
- **2026-09-22** — Internal-only link (logged-in backoffice users), not a public/anonymous link.
  Smaller privacy surface, reuses backoffice auth as-is. Killed: unauthenticated external link.
- **2026-09-22** — Shared attachments must be redacted per the *viewer's* own permissions, not
  shown verbatim as the owner saw them. Chosen over the simpler "show everything" option despite
  the extra work, given the conversation may carry pages/files the recipient isn't cleared to see.
- **2026-09-22** — v1 scope is link **+** a "shared with me" list, not link-only. Killed:
  link-only MVP (deferring the list to a later version).
- **2026-09-22** — Sharing is revocable at any time by the owner; access is cut off immediately
  once turned off. This is also the privacy kill-switch for the "kill criteria" scenario in
  BRIEF.md.
- **2026-09-22** — v1 is strictly read-only for the recipient; no "save a copy and keep chatting"
  fork action. Deferred, not ruled out.
- **2026-09-22** — Manual refresh is sufficient for the recipient; no live/streaming updates
  required while they have the shared conversation open. Killed: real-time viewer sync.
- **2026-09-22** — Owner chooses, at share time, whether the share is frozen at the current message
  or continues to include future messages. Mechanism for enforcing the cutoff is an open TODO for
  `umb-design`, not decided here.
- **2026-09-22** — Wait for the Copilot RC to finalize and merge into `v18/dev`/`v17/dev` before
  starting build work, rather than branching off the still-moving RC release line now. Avoids
  building on a branch that could still change and extra RC-merge conflicts.
- **2026-09-22** — Ships for v18 and v17 at the same time, per the repo's keep-active-versions-in-sync
  policy. Killed: v18-only first with a deferred v17 port decision.

## umb-design

- **2026-09-22** — Recipients are named (owner picks specific colleagues), not a single
  "anyone with the link" toggle. Confirmed with the user; makes `SharedWithUserKey` and the
  "shared with me" list well-defined. Killed: blanket org-wide link.
- **2026-09-22** — Shared reads go through a brand-new `GetSharedConversationAsync` path that never
  calls `GetOwnedOrThrowAsync` or any write method, instead of widening the existing
  `UserKey == actingUser` equality check to also accept an active share. Killed: widening the
  single ownership check — too easy for a future write path to inherit the "or shared" branch by
  accident.
- **2026-09-22** — Attachment redaction (`CanViewAsync`) is a new default-implemented
  (default: allow) member on `IAIContextResourceType`, not a bespoke Conversations-only check.
  Additive, no existing resource type needs to change. Killed: a Conversations-local permission
  check that would only apply to conversation attachments, not resource types generally.
- **2026-09-22** — Shares live in a new child table (`AIConversationShareEntity`), one row per
  (conversation, recipient), mirroring how `Resources` already got its own child table instead of
  a JSON column like `ContextIds`. Killed: a JSON list on `AIConversationEntity` — can't answer
  "every conversation shared with user X" without a full scan, and can't revoke one recipient
  without rewriting the whole blob.
- **2026-09-22** — Snapshot scope is a stored cutoff reference on the share row, not a copied
  snapshot of the message set. Killed: physically copying messages at share time — duplicates
  storage and attachments, and gives Live mode nowhere to live.
- **2026-09-22 (correction)** — The cutoff reference is `CutoffMessageId` (`AIMessage.Id`),
  resolved to that message's existing `Sequence` at read time, not a raw `CutoffMessageCount` int.
  User pointed out messages already have a stable id; `AIMessage` also already has an
  authoritative, server-assigned `Sequence` built for exactly this kind of ordering. Reuses both
  instead of recomputing a count. Also defined the fallback if the anchor message is later deleted
  (regenerate/truncate): show everything that still exists, never error.
- **2026-09-22** — Revocation is soft (`DateRevoked`), not a row delete — preserves an audit trail
  of who had access and when.
- **2026-09-22** — Confirmed the existing backoffice section `[Authorize]` policy on
  `ConversationsManagementControllerBase` stays as a precondition unchanged; the share check is
  additional, not a replacement. Resolves the open question flagged in `BRIEF.md`.
- **2026-09-22 (correction)** — The read-only chat view isn't new work: archived conversations
  already have one (`UaiConversationWorkspaceContext.isReadonly$`, wired to `<uai-chat readonly>`).
  User caught that the first pass of `SPEC.md` described this as a new `readonly` input to build.
  Corrected to extend the existing mechanism (`isReadonly$` also true for `isShared`, a second
  `#reload()` path via `requestSharedView`) instead. Net effect: less new frontend work than
  originally scoped, not a different design.
- **2026-09-22 (clarification)** — User asked directly: does attachment redaction hide the
  resource's *existence*, or just block access to its *details*? Checked the frontend: the resource
  picker only ever shows identity (name/type/description) as a chip; there's no "open a resource"
  action anywhere in chat. So there's no separate "details" surface to distinguish — redaction
  means the chip doesn't render at all for an unauthorized viewer, full stop, confirming the
  full-omission (not a placeholder) design already in `SPEC.md`, now with the reasoning made
  explicit in `ARCHITECTURE.md`.
- **2026-09-22 (finding, same investigation)** — The context panel does **not** consume the
  store's `isReadonly$` — its resource/context pickers check `conversation.isArchived` directly in
  four places. Extending only `isReadonly$` (as originally scoped) would have left a shared
  conversation's context panel showing working-looking, but silently-failing, "add a
  resource/context" controls. Fixed by introducing one derived `readonly` field on
  `UaiConversationDetailModel` (`isArchived || isShared`) that both the chat view and the context
  panel read, instead of patching `isArchived` checks individually — found by tracing the actual
  render code, not assumed.
- **2026-09-22 (reversal, supersedes the "hide what the viewer can't see" decision from
  `umb-explore` and the `CanViewAsync` decision above)** — Dropped attachment redaction entirely.
  User pushed back: message content is already unredacted, so a reply that leans on an attachment
  can already describe or quote it — hiding just the attachment chip protects a label, not the
  substance, while making such a reply look broken (references something invisible). Since the
  system can't meaningfully protect this content, replaced silent filtering with a disclosure
  warning shown to the *owner* at share time instead: sharing shows the recipient everything
  referenced, permissions or not. This is a real product/privacy call, not a pure engineering one —
  made with the user, not assumed. Removes the new `IAIContextResourceType.CanViewAsync` Core
  interface member and its two TODOs (which permission check to reuse; whether `AIContext` needs
  the same treatment) — both moot now that nothing is filtered.

## umb-plan

- **2026-09-22** — Owner-side stories (US1 share, US2 manage/revoke) sequenced before
  recipient-side (US3 find, US4 read) in `PLAN.md`. Confirmed with the user. The owner side has no
  dependency on the recipient side and is independently demonstrable (shares exist, can be listed
  and revoked) before anything renders for a recipient.
- **2026-09-22** — Backend service layer split into two parallel tracks (SC-04 owner-side,
  SC-05 recipient-side) rather than one combined sharing service. Mirrors `ARCHITECTURE.md`
  decision 1 directly: shared reads were already required to be a separate code path from owned
  reads/writes, so the task split follows the same seam rather than cutting across it.
- **2026-09-22** — Migration generation for SqlServer + SQLite (SC-03) is one task, not two,
  even though `umb-plan`'s general guidance favors small single-seam tasks. Both providers are
  mechanically generated from the same entity change in the same commit — splitting them wouldn't
  produce two independently reviewable units, just two files reviewed together anyway.
- **2026-09-22** — Frontend feature work (SC-10 readonly plumbing, SC-11 Share modal, SC-13
  sidebar grouping) is one parallel-group, not sequenced — confirmed they touch disjoint files
  (workspace context + context panel vs. a new modal vs. sidebar grouping code) once
  `conversation.repository.ts` (SC-09) exists as their shared dependency.
- **2026-09-22** — v17 is one backport task (SC-15) at the end, not a parallel task list next to
  the v18 checklist — matches `BRIEF.md`'s decision to build via the normal Backport Workflow
  (build on one line, port to the other) rather than developing both simultaneously.
- **2026-09-22** — `bdd-specs`-generated pending specs are staged under this plan folder's own
  `specs/` subfolder, not written directly into `Umbraco.AI.Agent.Copilot.Workspace`'s real test
  projects. That product doesn't exist on this branch at all yet (not even an empty folder) — there
  is nowhere real to put them until SC-01 onward creates the product structure. Each staged file's
  header names its real destination and which `PLAN.md` task moves it there. This is a deviation
  from `bdd-specs`'s default assumption (the target project already exists); the feature's
  blocked-on-external-merge status is the reason, not a change to the discipline itself.

## Post-plan addition

- **2026-09-22** — Added a share-notification email, discovered as a gap after `PLAN.md` was
  already written (the user asked whether any notification was expected — nothing in `BRIEF.md`
  through `PLAN.md` had addressed it). Checked the codebase and Umbraco core first: no
  in-app/toast notification system exists to hook into, but core already has a generic, reusable
  `SendEmailNotification` pipeline (same one behind forgot-password and content-change alerts).
  Confirmed with the user: email, not silence, and not a new in-app notification system (bigger
  lift, nothing to reuse). Updated `BRIEF.md` (new confirmed requirement), `ARCHITECTURE.md`
  (decision 5), `SPEC.md` (POST /shares behavior), `STORIES.md` (US1 AC6–7, old AC6 renumbered to
  AC8), and `PLAN.md` (new task SC-05, full renumber SC-05→SC-16 through the rest of the list —
  see each file for the current state, not this entry, per the "snapshot not changelog" rule
  memory `plan-doc-architecture-spec-are-snapshots.md` — applied here to `STORIES.md`/`PLAN.md`
  too, not just `ARCHITECTURE.md`/`SPEC.md`).
- **2026-09-22** — Email sends only when a share row is newly *created*, not on an idempotent
  re-share that only changes scope. Avoids emailing a recipient every time the owner adjusts
  Snapshot/Live on an existing share.
