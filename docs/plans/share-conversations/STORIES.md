# Stories

## Definition of Ready / Definition of Done

**Ready:** role/capability/value stated and non-hollow; Given/When/Then acceptance criteria cover
the happy path; out-of-scope explicit; passes INVEST.

**Done:** all acceptance criteria pass as executable specs (via `bdd-specs`); sad-path criteria
covered too; ships identically on `v18` and `v17` (backported per the repo's normal Backport
Workflow — not a separate acceptance criterion per story, just a release-process step that applies
uniformly once a story is done on one line).

## Epic: Share Conversations

Four vertical slices, each mapping to one real UI surface + its backing endpoint(s), per
`SPEC.md`. Snapshot-vs-Live scope and the redaction-free/disclosure-at-share-time design
(`ARCHITECTURE.md` decisions) are acceptance criteria within the relevant stories, not stories of
their own — they're properties of sharing and reading a share, not separate capabilities.

Sharing has no entity action and no separate manage-view modal (`ARCHITECTURE.md` decision 7) — the
context panel's "Shared with" section and its picker modal are the one surface that both adds and
removes recipients, so US1 and US2 below both describe interactions with that same surface rather
than two different screens.

---

### US1 — Share a conversation with a colleague

As a Copilot Workspace user who owns a conversation,
I want to pick one colleague at a time and choose whether they see it frozen at this point or as
it keeps going,
so that we can discuss it together.

**AC1 — Share with one recipient, frozen at the current point**
Given I own a conversation with existing messages and open its "Shared with" section
When I choose "Choose", pick one colleague in the picker modal, and choose "Snapshot", then submit
Then a share is created for that colleague with `CutoffMessageId` set to the conversation's
current last message, and the picker modal closes back to the "Shared with" section showing the
new recipient

**AC2 — Adding a second recipient is a separate, repeatable action**
Given a conversation is already shared with one colleague
When I choose "Choose" again and pick a different colleague
Then a second share is created without affecting the first, and both now appear in the "Shared
with" section

**AC3 — Share as "Live"**
Given I own a conversation and open the picker modal
When I pick a colleague and choose "Live" from the mode dropdown
Then the created share has no cutoff (`CutoffMessageId` is null), so the recipient will see new
messages as they're added

**AC4 — Re-sharing to an existing recipient updates their share instead of duplicating**
Given a conversation is already shared with a colleague as "Snapshot"
When I pick that same colleague again in the picker modal and choose "Live"
Then that recipient's existing share is updated to "Live", not duplicated, and still appears as
one row in the "Shared with" section

**AC5 — Disclosure notice shown when the conversation has attachments**
Given a conversation has at least one attached resource or context
When I open the share-picker modal
Then I see a notice that the recipient will see everything referenced in the conversation,
including things they might not otherwise have permission to open

**AC6 — No disclosure notice when there's nothing to disclose**
Given a conversation has no attached resources or contexts
When I open the share-picker modal
Then no disclosure notice is shown

**AC7 — Recipient is emailed when newly shared with**
Given I share a conversation with a colleague who didn't already have access to it
When the share is created
Then they receive an email naming me and linking to the conversation

**AC8 — Re-sharing to an existing recipient does not send a second email**
Given a conversation is already shared with a colleague
When I share the same conversation with them again, changing only the mode
Then they do not receive another email

Sad path

**AC9 — Cannot share a conversation you don't own**
Given a conversation exists that I do not own
When I attempt to create a share for it
Then the request is rejected as not found (indistinguishable from a non-existent conversation)

**AC10 — Submit is disabled until a person is picked**
Given I've opened the share-picker modal and not yet picked anyone
When I look at the footer
Then "Share" is disabled

**AC11 — A Snapshot share that can't be anchored to a message fails, never silently becomes Live**
Given a conversation has no messages yet (or one is deleted from under the request by a concurrent
regenerate)
When I try to share it as "Snapshot"
Then the share request fails with an error — it never succeeds with a cutoff that behaves like
"Live"

---

### US2 — See and manage who a conversation is shared with

As the owner of a conversation,
I want to see, without opening anything extra, whether it's currently shared and with whom, and
remove a recipient directly,
so that I always know who can see it and can fix a mistake or end the sharing once we're done.

**AC1 — "Shared with" section shows current recipients**
Given a conversation is shared with two colleagues
When I open the conversation and look at its context panel
Then I see a "Shared with" section, first above Contexts and Resources, listing both recipients
with their scope mode

**AC2 — Empty state before anyone has been added**
Given a conversation has never been shared
When I open its context panel
Then the "Shared with" section shows "Not shared with anyone yet." and a "Choose" button, instead
of a list

**AC3 — Remove a recipient inline**
Given a conversation is shared with a colleague
When I click the remove (trash) action on their row in the "Shared with" section
Then that colleague can no longer open the conversation, and their row disappears without opening
any further dialog

**AC4 — Revoking is immediate**
Given a colleague currently has access to a shared conversation
When the owner removes their row
Then the colleague's next request for that conversation is rejected — no grace period

**AC5 — Shared indicator on the conversation's own sidebar row**
Given a conversation has at least one active share
When I look at its row in the sidebar (Pinned or Recent)
Then I see a small badge overlaid on its icon, distinct from the plain chat icon

**AC6 — Badge and pin indicator show at the same time**
Given a conversation is both pinned and actively shared
When I look at its sidebar row
Then I see the pushpin icon with the badge overlaid on it — not one replacing the other

**AC7 — Badge is a quick hint, not a second management surface**
Given a conversation's sidebar row shows the shared badge
When I hover it
Then I see a short "Shared" label, and nothing opens — finding out "with whom" still means opening
the conversation and looking at its "Shared with" section

Sad path

**AC8 — Cannot manage shares on a conversation you don't own**
Given a conversation exists that I do not own
When I attempt to list or revoke its shares
Then the request is rejected as not found

**AC9 — Revoking a non-existent or already-revoked share**
Given a colleague's share has already been revoked, or never existed
When the owner attempts to revoke it again
Then the request is rejected as not found, not treated as a no-op success

---

### US3 — Find conversations shared with me

As a colleague someone has shared a conversation with,
I want to see it in my own conversation list,
so that I don't need to keep track of a link to find it again.

**AC1 — Shared conversation appears in "Shared with me"**
Given a colleague has shared a conversation with me
When I open the Copilot Workspace sidebar
Then I see it under a "Shared with me" grouping, as a single line with a plain icon and the same
badge treatment as any other shared row — no second line

**AC2 — The badge tells me who shared it, on hover**
Given a conversation is shared with me
When I hover the badge on its sidebar row
Then I see "Shared by \<owner\>", distinct from the plain "Shared" hover text the owner's own rows
show

**AC3 — Revoked shares disappear from the list**
Given a conversation was shared with me and then revoked
When I open the sidebar
Then it no longer appears in "Shared with me"

**AC4 — No entity actions on a shared entry**
Given a conversation is shared with me
When I look at it in the sidebar
Then I see no Pin/Archive/Rename/Move/Delete actions — only the ability to open it

---

### US4 — Read a conversation shared with me

As a colleague someone has shared a conversation with,
I want to open and read it exactly as they saw it,
so that I understand what happened without asking them to repeat it.

**AC1 — Read a Snapshot share**
Given a colleague shared a conversation with me as "Snapshot"
When I open it
Then I see every message up to the point they shared it, and no messages added afterward

**AC2 — Read a Live share**
Given a colleague shared a conversation with me as "Live"
When they send another message after sharing, and I reload the conversation
Then I see the new message too

**AC3 — Chat is read-only, with the owner named**
Given I'm viewing a conversation shared with me
When I look at the chat view
Then there is no message composer, and a notice explains it's read-only because it was shared by
the owner, naming them

**AC4 — Attached context is visible but not editable**
Given a shared conversation has attached pages/files
When I view its context panel
Then I see the same attachments the owner sees, with no controls to add or remove them, and no
"Shared with" section (that's owner-only)

**AC5 — Snapshot cutoff message later removed (regenerate) — falls back gracefully**
Given a Snapshot share's cutoff message was later deleted because the owner used regenerate
When I open the conversation
Then I see every message that still exists, and no error

Sad path

**AC6 — Cannot open a conversation not shared with me**
Given a conversation exists that has not been shared with me
When I request its shared view
Then the request is rejected as not found
