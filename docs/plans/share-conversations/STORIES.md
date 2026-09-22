# Stories

## Definition of Ready / Definition of Done (proposed — confirm during `umb-plan`'s interview)

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

---

### US1 — Share a conversation with named colleagues

As a Copilot Workspace user who owns a conversation,
I want to share it with one or more specific colleagues and choose whether they see it frozen at
this point or as it keeps going,
so that we can discuss it together.

**AC1 — Share with a single recipient, frozen at the current point**
Given I own a conversation with existing messages
When I choose "Share", pick one colleague, and choose "Snapshot"
Then a share is created for that colleague with `CutoffMessageId` set to the conversation's
current last message

**AC2 — Share with multiple recipients in one submit**
Given I own a conversation
When I choose "Share", pick two or more colleagues, and submit
Then a share is created for each recipient in one request

**AC3 — Share as "Live"**
Given I own a conversation
When I choose "Share", pick a colleague, and choose "Live"
Then the created share has no cutoff (`CutoffMessageId` is null), so the recipient will see new
messages as they're added

**AC4 — Re-sharing to an existing recipient updates their share instead of duplicating**
Given a conversation is already shared with a colleague as "Snapshot"
When I share the same conversation with the same colleague again, choosing "Live"
Then that recipient's existing share is updated to "Live", not duplicated

**AC5 — Disclosure notice shown when the conversation has attachments**
Given a conversation has at least one attached resource or context
When I open the Share dialog
Then I see a notice that the recipient will see everything referenced in the conversation,
including things they might not otherwise have permission to open

**AC6 — Recipient is emailed when newly shared with**
Given I share a conversation with a colleague who didn't already have access to it
When the share is created
Then they receive an email naming me and linking to the conversation

**AC7 — Re-sharing to an existing recipient does not send a second email**
Given a conversation is already shared with a colleague
When I share the same conversation with them again, changing only the scope
Then they do not receive another email

Sad path

**AC8 — Cannot share a conversation you don't own**
Given a conversation exists that I do not own
When I attempt to create a share for it
Then the request is rejected as not found (indistinguishable from a non-existent conversation)

---

### US2 — Manage who a conversation is shared with

As the owner of a shared conversation,
I want to see who currently has access and revoke it,
so that I can fix a mistake or end the sharing once we're done discussing it.

**AC1 — View current recipients**
Given a conversation is shared with two colleagues
When I open the Share dialog
Then I see both listed, each with their scope and the date they were shared

**AC2 — Revoke a recipient**
Given a conversation is shared with a colleague
When I revoke their access
Then that colleague can no longer open the conversation, and they no longer appear in the
current-recipients list

**AC3 — Revoking is immediate**
Given a colleague currently has access to a shared conversation
When the owner revokes their access
Then the colleague's next request for that conversation is rejected — no grace period

Sad path

**AC4 — Cannot manage shares on a conversation you don't own**
Given a conversation exists that I do not own
When I attempt to list or revoke its shares
Then the request is rejected as not found

**AC5 — Revoking a non-existent or already-revoked share**
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
Then I see it under a "Shared with me" grouping, showing its title and the owner's name

**AC2 — Revoked shares disappear from the list**
Given a conversation was shared with me and then revoked
When I open the sidebar
Then it no longer appears in "Shared with me"

**AC3 — No entity actions on a shared entry**
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

**AC3 — Chat is read-only**
Given I'm viewing a conversation shared with me
When I look at the chat view
Then there is no message composer, and a notice explains it's read-only because it was shared by
the owner

**AC4 — Attached context is visible but not editable**
Given a shared conversation has attached pages/files
When I view its context panel
Then I see the same attachments the owner sees, with no controls to add or remove them

**AC5 — Snapshot cutoff message later removed (regenerate) — falls back gracefully**
Given a Snapshot share's cutoff message was later deleted because the owner used regenerate
When I open the conversation
Then I see every message that still exists, and no error

Sad path

**AC6 — Cannot open a conversation not shared with me**
Given a conversation exists that has not been shared with me
When I request its shared view
Then the request is rejected as not found
