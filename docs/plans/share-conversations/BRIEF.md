# Brief

> **Status:** Not started — blocked on the Copilot Workspace product itself, which doesn't exist on
> `v18/dev` yet (only on RC branches).

## Problem

Copilot Workspace conversations are persisted per-user today — `AIConversationService` enforces
`conversation.UserKey == actingUser.Key` on every read, so nobody but the owner can ever open a
conversation. Editors and admins who use the AI copilot want to **share a chat with a colleague so
they can discuss it together** — e.g. "look what the AI suggested for this page" or "here's how I
debugged this." There's no way to do that today; the only option is to screenshot or retype the
conversation.

**Who's it for:** backoffice users sharing with other backoffice users, inside the same CMS
instance. Not site visitors, not people outside the CMS (no public/anonymous link).

**Why now:** parked as a follow-on idea while exploring the Copilot Workspace conversation model
that shipped in the (not-yet-merged) Copilot RC. No external customer commitment driving urgency —
this is a natural next step once persisted conversations exist at all.

**What success looks like:** a user can turn sharing on for a conversation, send a colleague to it
(link + it also shows up in a "shared with me" list for the recipient), the recipient can read it
but not send messages in it, and the owner can turn sharing off again at any time, immediately
cutting off access. No qualitative usage target set yet — this is a small internal collaboration
feature, not a metric-driven one.

**Decided: the recipient gets an email when first shared with.** Confirmed with the user —
without it, discovering a share depends on happening to check the sidebar, which undercuts the
"let's discuss this together" motivation. Reuses Umbraco core's existing generic email-sending
pipeline (the same one behind forgot-password and content-change alerts) rather than new
infrastructure. See `ARCHITECTURE.md`/`SPEC.md` for the mechanism.

**Constraints:**
- Builds on `Umbraco.AI.Agent.Copilot.Workspace` (`Conversations.Core` / `.Persistence` /
  `.Persistence.{SqlServer,Sqlite}` / `.Web` + the `Copilot.Workspace.Web.StaticAssets` frontend).
  That product does **not exist on `v18/dev` yet** — it's still confined to the Copilot RC release
  branches (`v18/release/2026.08.1` / `v17/release/2026.08.1`), deliberately held out of dev until
  that release goes final (see `[[project_copilot_rc_2026_08]]` memory). **Decided: build work
  waits.** No branch off the RC release line for actual implementation — `umb-build-loop` shouldn't
  start for this feature until the Copilot RC has merged into `v18/dev` (and `v17/dev`); re-check
  `[[project_copilot_rc_2026_08]]` (or the RC branches directly) at that point. **Design work
  (`umb-design`) proceeds now against the code as it exists on the RC branches** (e.g. the
  `v18-copilot-starter-prompts` worktree, which is branched from the RC line) — reading and
  reasoning about that code doesn't require a branch of its own, and the conversation model isn't
  expected to change shape before the RC finalizes. Re-verify against the merged `v18/dev` code
  once available, in case anything shifted during finalization.
- **Decided: ships for both `v18` and `v17` at the same time**, matching this repo's normal
  keep-active-versions-in-sync policy and the fact that Copilot Workspace itself is being built for
  both lines. Build via the usual Backport Workflow (branch off each `vN/dev` once available) rather
  than a single cross-version branch.
- Existing entity-action pattern in the frontend (Pin/Archive on a conversation,
  `conversation/entity-actions/conversation.actions.ts`) is the natural place a "Share" action
  would live, so a new UI action doesn't need an unfamiliar shape.

**Riskiest unknowns:**
- **Attachment leakage.** A conversation can carry `ContextIds` and `Resources` (attached
  pages/files, conversation-scoped). Decided requirement: a shared conversation must **redact any
  attachment the viewer isn't independently allowed to see** via their own permissions, rather than
  showing everything the owner could see. This is real work — each attachment needs a permission
  check against the *viewer*, not just "does the conversation exist" — and is the main reason this
  isn't a one-field change.
- **Snapshot vs. live scope.** The owner needs to choose, at share time, whether the recipient sees
  **only messages up to the moment of sharing** (a frozen point) or **also sees messages sent after
  that** (an evolving share that keeps including new turns). Manual refresh is fine either way — no
  requirement for live/streaming updates while the viewer has the page open. TODO for `umb-design`:
  work out how a "shared up to here" cutoff is represented and enforced server-side (a snapshot
  copy? a stored message-index cutoff on the share record?), since the underlying conversation is
  still mutable by the owner.
- Section-grant permission model: need to confirm during design whether "the recipient can open
  the Copilot Workspace section at all" is a sufficient precondition for accepting a share, or
  whether a stricter check is needed.

**Smallest version worth shipping:** link-based sharing **plus** a "shared with me" list — not
link-only. Confirmed with the user as the v1 bar, not a stretch goal.

## Non-goals

- **Live real-time viewing.** The recipient does not need to watch new messages stream in while
  they have the page open; a manual refresh showing the latest state (per the chosen snapshot/live
  scope above) is enough. No AG-UI streaming plumbing needed for the viewer side.
- **Forking/copying.** For v1, a shared conversation is strictly read-only for the recipient —
  no "save a copy and keep chatting" action. Explicitly deferred, not ruled out forever.
- **Multi-writer / collaborative editing.** Only the owner can ever send messages into their own
  conversation. Two people typing into the same conversation is out of scope entirely (this was
  ruled out earlier in favor of the read-only-link approach — see the killed
  full-collaboration alternative below).
- **Public/anonymous access.** No unauthenticated link, no sharing outside the CMS login boundary.
- **Commenting or discussion threads attached to the shared conversation itself.** The "discuss it
  together" use case is expected to happen elsewhere (Slack, Teams, in person) — the feature only
  needs to make the conversation visible to the other person, not host a discussion about it.
- Replacing or changing anything about how conversations behave for their owner — sharing is
  purely additive.

## Kill criteria

If a real privacy or compliance concern surfaces once this is designed in more detail (e.g. the
attachment-redaction check turns out to be infeasible to do reliably, or shared content is found to
routinely include personal data that shouldn't cross even an internal share), stop and re-scope
rather than ship something that quietly over-shares.
