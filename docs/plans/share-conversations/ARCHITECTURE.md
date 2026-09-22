# Architecture

## Extension points

No new *kind* of Umbraco extension point. This reuses the three extension points already
established by Copilot Workspace conversations, plus one existing Umbraco **core** extension
point for the share-notification email:

- **Management API**: new per-action controllers under `Umbraco.AI.Agent.Conversations.Web`,
  following the existing one-controller-per-action pattern (`AllConversationsController`,
  `CreateConversationController`, etc.), inheriting `ConversationControllerBase`. No new auth
  mechanism — these inherit the same host-applied backoffice section `[Authorize]` policy every
  other conversation endpoint already has.
- **EF Core persistence**: a new child table, `Umbraco.AI.Agent.Conversations.Persistence`,
  SqlServer + Sqlite migrations, following the existing `ConversationResources` pattern.
- **Frontend**: new entity action + modal + sidebar grouping in
  `Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets`, following the existing
  `UaiConversationRenameAction` (action-opens-modal-then-calls-repository) shape.
- **Email**: publishes Umbraco core's `SendEmailNotification` (`Umbraco.Cms.Core.Notifications`)
  when a share is first created — see decision 5. Not a new mechanism; the same generic pipeline
  core already uses for forgot-password and content-change alert emails.

## Data model & persistence

### `AIConversationShareEntity` (new child table, mirrors `AIConversationResourceEntity`)

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `ConversationId` | `Guid` | FK → `AIConversationEntity.Id`, cascade delete |
| `SharedWithUserKey` | `Guid` | The recipient. Unique together with `ConversationId` (re-sharing to the same person updates their existing row instead of duplicating) |
| `SharedByUserKey` | `Guid` | Owner at the moment of sharing — explicit rather than inferred from the conversation, so the audit trail survives even if ownership rules ever change later |
| `ScopeMode` | `int` (enum: `Snapshot` = 0, `Live` = 1) | Owner's choice at share time |
| `CutoffMessageId` | `Guid?` | Set only when `ScopeMode == Snapshot` — the id of the last message visible at share time (references `AIMessage.Id`). `null` for `Live` (no ceiling) |
| `DateShared` | `DateTime` | |
| `DateRevoked` | `DateTime?` | Soft-revoke, not a hard delete — see rationale below |

SQL Server + SQLite migrations, `UmbracoAIConversations_` prefix, following the existing three
migrations in this project (`_Initial`, `_ConversationContext`, `_SessionState`).

**Why a child table, not a JSON column on `AIConversationEntity`** (the way `ContextIds` is
stored): shares need an independent lifecycle per recipient — revoke one person without touching
others, an audit timestamp per recipient, and a query that says "every conversation shared with
user X" across the whole table. A JSON blob per conversation would need a full-table scan and
parse to answer that last one; an indexed `SharedWithUserKey` column answers it directly. This is
the same reasoning that already put `Resources` in its own child table instead of a JSON column
like `ContextIds`.

**Why a cutoff message id, not a copied snapshot of messages**: an alternative design would
physically copy the message set into a separate "shared conversation" snapshot at share time.
Rejected — it duplicates storage and every attached file/resource reference those messages carry,
and gives `Live` mode (keep including new messages) nowhere natural to live. A single nullable
cutoff column serves both modes: `Snapshot` reads every message whose `Sequence` is `<=` the
cutoff message's `Sequence`, `Live` reads all of them.

**Why a message id, not a raw count**: `AIMessage` already has a stable `Id` (documented as
"stable across loads/resumes for correlation") *and* a server-assigned, authoritative `Sequence`
int purpose-built for ordering. `CutoffMessageId` references that `Id`, resolved to the message's
`Sequence` at read time (`WHERE Sequence <= (SELECT Sequence FROM Messages WHERE Id =
@CutoffMessageId)`) — reusing the existing, already-authoritative ordering field rather than
storing a count that would duplicate what `Sequence` already means. It's also more precise about
intent: the row says "shared through this specific message," not "shared through whatever the Nth
position happened to be" — a distinction that matters if a message earlier in the conversation is
ever added, removed, or renumbered for a reason unrelated to sharing.

**Graceful degradation if the cutoff message no longer exists**: `TruncateAfterLastUserMessageAsync`
(the regenerate feature) can delete messages after a point in the conversation, which could delete
the very message a `Snapshot` share was anchored to. If the read-time lookup for `CutoffMessageId`
finds nothing, the shared-view read falls back to returning every message that still exists —
never an error, and never a reason to reveal more than what remains (there is nothing beyond the
current tail to withhold or expose either way).

**Why soft-revoke (`DateRevoked`), not delete**: preserves who had access to what and when, which
matters if a privacy review ever needs to reconstruct exposure — a hard delete erases that history
at the exact moment it might matter most.

## Key decisions

### 1. Shared reads are a separate code path from owned reads — never a widened equality check

`AIConversationService.GetConversationAsync` today does exactly one check:
`conversation.UserKey == actingUser`. The naive extension is
`UserKey == actingUser || HasActiveShare(...)`. **Rejected.** `GetOwnedOrThrowAsync` — the helper
every *write* path (update, delete, truncate) calls — currently gets its ownership guarantee for
free by calling the same `GetConversationAsync`. Widen that one check and every present and future
write path inherits the "or shared" branch unless someone remembers to re-check. That's exactly
the shape of bug that turns into a real incident.

**Decision:** add a distinct method, `GetSharedConversationAsync(id, cancellationToken)`, on a
path that never touches `GetOwnedOrThrowAsync` or any write method. It resolves via a new
`IAIConversationShareRepository.GetActiveShareAsync(conversationId, viewerUserKey)` and returns a
**separate read-only view model**, not the owner's `AIConversation`. Owner access and shared access
are two parallel, independently auditable paths that happen to render through the same frontend
chat component in read-only mode — not one path with a permission branch threaded through it.

### 2. No attachment redaction — sharing is a disclosed, full permission-adjacent grant

A shared conversation shows the recipient the exact same attachment list (`resources`/
`contextIds`) the owner sees. Nothing is filtered per-viewer. This is consistent with how message
content already works: sharing a conversation discloses what shaped it, in full — attachments are
not a special case.

**Rationale:** message content is never redacted (`BRIEF.md`, non-goals). If a resource shaped the
assistant's reply — the entire point of attaching it — that reply's visible text can already
describe, quote, or paraphrase it. Filtering the attachment list would only ever cover the picker's
identity metadata, not the channel that actually carries the risk, while actively producing a
worse experience: a reply that visibly leans on something the recipient can't see any trace of
anywhere in the UI reads as broken, not protected.

**What protects the recipient instead: disclosure at share time.** The Share modal (see `SPEC.md`)
shows the owner a warning before the first share is confirmed — sharing this conversation shows the
recipient everything referenced in it, including pages or files they might not otherwise have
permission to open. This puts the judgment call where it belongs, with the person who knows what's
in the conversation, rather than implying a technical guarantee the system can't keep.

This satisfies `BRIEF.md`'s kill criteria rather than triggering it: the criteria call for stopping
if a privacy risk is found and left unaddressed. This risk was found and is addressed, by
disclosure rather than filtering. A human sanity check on the disclosure copy before shipping is
still worthwhile, per the org's general GDPR-conscious posture.

### 3. Section-grant is a precondition, not a substitute, for the share check

`ConversationsManagementControllerBase` already gates every conversation endpoint behind the
host's backoffice section `[Authorize]` policy (applied by the hosting product's own
`IApplicationModelConvention`, per that class's doc comment). This answers the open question from
`BRIEF.md`: **both** checks apply, in order — a recipient must already be able to open the Copilot
Workspace section at all (existing, unchanged), *and* have an active share row for that specific
conversation (new). Sharing never loosens or replaces the section gate.

### 4. Recipients are named, not a single "anyone with the link" toggle

Confirmed with the user: the owner picks specific colleagues (a recipient picker), matching the
"discuss it together" use case. This is what makes the `SharedWithUserKey` column and the
"shared with me" list well-defined — a blanket link would have made "who is this shown to"
ill-defined and the "shared with me" list would have had to become the *owner's* "what I've
shared" list instead.

### 5. Email on first share, via core's existing `SendEmailNotification` — not on every re-share

**Decision:** when the owner-side sharing service (see decision 1, `GetSharedConversationAsync`'s
counterpart on the write side) creates a **new** share row for a recipient, it publishes
`SendEmailNotification` (`Umbraco.Cms.Core.Notifications`) with a share-specific
`NotificationEmailModel` — the same core pipeline already used for forgot-password and
content-change alert emails (`Umbraco.Core.Events.UserNotificationsHandler` /
`Umbraco.Core.Notifications.SendEmailNotification`), not new infrastructure. No new toast/bell
mechanism — checked the codebase and Umbraco core for one; none exists to hook into, and building
one would be a materially bigger lift than reusing the email pipeline for what's a small,
internal-collaboration signal.

**Decision:** the email fires only when a share row is *created*, not when
`ShareConversationAsync`'s idempotent upsert (decision — see "Recipients are named" AC4 in
`STORIES.md`) updates an existing recipient's scope. Re-sharing to someone who already has access
(e.g. switching them from Snapshot to Live) is not a new event worth emailing about — avoids
spamming a recipient every time the owner adjusts scope.

**Why the recipient's own email address, not a generic system address**: the recipient is an
existing backoffice user; their email is already on their user record (the same address core's own
notification emails already go to) — no new address collection needed.

**TODO for `umb-build-loop`**: exact email copy (subject, body, what it links to) — same
product/legal-adjacent review flag as the disclosure-warning copy in decision 2, not just
engineering wording, per the org's GDPR-conscious posture.

## TODOs carried into `umb-plan` / `umb-build-loop`

- Exact copy for the share-time disclosure warning (see decision 2) — needs product/legal-adjacent
  review given the org's GDPR-conscious posture, not just engineering wording.
- Exact copy for the share-notification email (see decision 5) — same review flag.
- Which existing backoffice user-picker component to reuse for the recipient picker in the Share
  modal — not identified this session.
- No v17-specific CMS API incompatibility is expected (nothing here uses a v18-only API), but this
  hasn't been explicitly checked against the v17 line yet.
