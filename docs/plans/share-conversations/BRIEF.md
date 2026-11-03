# Brief

## Problem

Copilot Workspace conversations are persisted per-user today — `AIConversationService` enforces
`conversation.UserKey == actingUser.Key` on every read, so nobody but the owner can ever open a
conversation. Editors and admins who use the AI copilot want to **share a chat with a colleague so
they can discuss it together** — e.g. "look what the AI suggested for this page" or "here's how I
debugged this." There's no way to do that today; the only option is to screenshot or retype the
conversation.

**Who's it for:** backoffice users sharing with other backoffice users, inside the same CMS
instance. Not site visitors, not people outside the CMS (no public/anonymous link).

**Why now:** Copilot Workspace has shipped and is live on `v18/dev` (and `v17/dev`) — this is the
first feature built on top of it.

**v1 scope:** the owner shares a conversation with one or more named colleagues, choosing whether
it's frozen at that point (Snapshot) or keeps including new messages (Live); it shows up in the
recipient's own "shared with me" list; the recipient gets an email the first time they're shared
with (reusing Umbraco core's existing email-sending pipeline — not a new notification system); the
recipient can open and read it, but never send messages into it or edit its attached context. The
owner can see, from their own side, that a conversation is currently shared and who it's shared
with, and can revoke that access.

**Owner-side visibility is a real requirement, not just the Share action itself.** It's not enough
that the owner *can* open a "Share" dialog and see the current recipient list there — a
conversation that's actively shared needs to be visibly distinguishable from one that isn't,
somewhere the owner naturally looks (their own conversation list), not only inside a dialog they
have to think to open.

**Constraints:**
- Builds on `Umbraco.AI.Agent.Copilot.Workspace` (`Conversations.Core` / `.Persistence` /
  `.Persistence.{SqlServer,Sqlite}` / `.Web` + the `Copilot.Workspace.Web.StaticAssets` frontend),
  which now exists on `v18/dev`. Re-verify the conversation model against the current `v18/dev`
  code before designing (it was last checked against the pre-merge RC branches).
- Ships for both `v18` and `v17` at the same time, matching this repo's normal
  keep-active-versions-in-sync policy. Build via the usual Backport Workflow (branch off each
  `vN/dev`) rather than a single cross-version branch.
- Existing entity-action pattern in the frontend (Pin/Archive on a conversation,
  `conversation/entity-actions/conversation.actions.ts`) is the natural place a "Share" action
  would live, so a new UI action doesn't need an unfamiliar shape.

**Riskiest unknowns:**
- **Attachment exposure.** A conversation can carry `ContextIds` and `Resources` (attached
  pages/files, conversation-scoped). The recipient sees the exact same attachment list the owner
  sees — no per-viewer filtering. This was considered and deliberately rejected: message text
  isn't filtered either, so a reply that references an attachment would already describe it
  regardless, making filtering just the attachment list a half-measure that creates confusing gaps
  without real protection. Instead, the owner sees a plain warning before sharing: the recipient
  will see everything referenced in the conversation, including things they might not otherwise
  have permission to open. This is a disclosed, informed choice, not a silent permission grant.
- Section-grant permission model: confirm during design whether "the recipient can open the
  Copilot Workspace section at all" is a sufficient precondition for accepting a share, or whether
  a stricter check is needed.

**Smallest version worth shipping (this is v1):** the owner picks one or more named colleagues
(not a blanket link) and shares a conversation, choosing Snapshot or Live; the recipient is
emailed the first time they're shared with and it appears in their "shared with me" list; the
recipient opens it read-only; the owner can see it's shared and with whom from their own side, and
can revoke it. Nothing in this feature is deferred to a later phase at this point.

## Non-goals

- **Live real-time *viewing*** (the recipient watching new messages stream in while the page is
  open) **is still out of scope even though Live *scope* is in.** Live scope means a share keeps
  including new messages on the next load/refresh — it does not mean the open page updates itself
  without a reload.
- **Forking/copying.** A shared conversation is strictly read-only for the recipient — no "save a
  copy and keep chatting" action. Deferred, not ruled out.
- **Multi-writer / collaborative editing.** Only the owner can ever send messages into their own
  conversation. Two people typing into the same conversation is out of scope entirely.
- **Public/anonymous access.** No unauthenticated link, no sharing outside the CMS login boundary.
- **Commenting or discussion threads attached to the shared conversation itself.** The "discuss it
  together" use case is expected to happen elsewhere (Slack, Teams, in person) — the feature only
  needs to make the conversation visible to the other person, not host a discussion about it.
- Replacing or changing anything about how conversations behave for their owner — sharing is
  purely additive.

## Kill criteria

If a real privacy or compliance concern surfaces once this is designed in more detail (e.g. the
disclosure-warning approach to attachment exposure is found to be insufficient, or shared content
is found to routinely include personal data that shouldn't cross even an internal share), stop and
re-scope rather than ship something that quietly over-shares.
