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
- **Email**: sends via Umbraco core's `IEmailSender` (`Umbraco.Cms.Core.Mail`) when a share is first
  created — see decision 5. Not a new mechanism; the same generic pipeline core already uses for
  forgot-password and content-change alert emails.

## Data model & persistence

### `AIConversationShareEntity` (new child table, mirrors `AIConversationResourceEntity`)

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `ConversationId` | `Guid` | FK → `AIConversationEntity.Id`, cascade delete |
| `SharedWithUserKey` | `Guid` | The recipient. Unique together with `ConversationId`, filtered to active rows only (`WHERE DateRevoked IS NULL`) — see "Why a filtered unique index" below |
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

**Why a filtered unique index, not a plain one, and what re-sharing after a revoke does**: a plain
unique index on `(ConversationId, SharedWithUserKey)` would combine badly with soft-revoke —
once a recipient's row is revoked, it still occupies that unique slot forever, so the owner could
never share with that same person on that conversation again. That's not the intent; revoke-then-
regrant is a normal workflow (undo a mistake, or re-add someone later). The index is filtered to
`WHERE DateRevoked IS NULL`: uniqueness holds only among *active* shares for a given recipient, so
multiple revoked rows for the same `(ConversationId, SharedWithUserKey)` pair can coexist — one per
share/revoke cycle — which is exactly the audit trail the "why soft-revoke" rationale above wants.

The repository's save/upsert operation looks up the recipient's *active* row only (`DateRevoked IS
NULL`). If one exists, it updates `ScopeMode`/`CutoffMessageId` on it (not a new share — no email,
per decision 5). If none exists — whether this recipient has never been shared with, or was shared
with and then revoked — it inserts a brand-new row with a fresh `DateShared`. Sharing again after a
revoke is therefore a new grant, not a resurrection of the old one: it gets its own row (preserving
the revoked row's history untouched) and **does** fire the share-notification email, the same as
any other first-time share for that recipient. This is a deliberate, not incidental, consequence of
treating "no active row" as the one condition that means "create."

## Connected systems

- **Deploy / uSync**: not applicable. `AIConversation`/`AIMessage` themselves aren't Deploy- or
  uSync-tracked entities — they're runtime per-user data, not content that moves between
  environments. A share row is the same kind of thing, one level further removed.
- **AI operation audit trail** (`IAIOperationTracker`): not applicable. That system tracks AI
  provider calls/capability usage (chat, embeddings, etc.) — sharing is ordinary CRUD on a new
  entity, not an AI operation.
- **Notification events**: `AIConversationSaving`/`SavedNotification` already exist for
  create/update/delete on `AIConversation` itself. Sharing doesn't modify the conversation entity,
  so it doesn't need to publish through those — a share is its own entity with its own lifecycle.
  It does get one new, non-cancelable notification of its own: `AIConversationShareCreatedNotification`
  (carrying the new `AIConversationShare` and its `AIConversation`), published only when a genuinely
  new share row is created — never on a scope-change update to an already-active recipient. This
  exists purely as the seam decision 5's email hangs off: the sharing service itself knows nothing
  about email, a separate notification handler reacts to this to send one. Same shape as every other
  "already happened" notification in this codebase (e.g. `AIAgentSelectedNotification`) — takes an
  `EventMessages` parameter like its siblings, even though nothing currently cancels it.

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

### 5. Email on first share, via core's existing `IEmailSender` — not on every re-share

**Corrected from an earlier pass:** this decision originally said the sharing service "publishes
`SendEmailNotification`" directly. Checked against the real Umbraco CMS source rather than assumed:
`SendEmailNotification` is an internal interception point `EmailSender` (the concrete
`IEmailSender`) publishes itself, when asked, so *other* handlers can intercept or override a send —
it isn't something a feature author constructs and publishes to send mail. The actual, correct way
to send an email is to inject `IEmailSender` and call `SendAsync(EmailMessage, emailType, ...)`;
this is the exact method core's own password-reset and content-notification emails go through.

**Decision:** a new `AIConversationShareCreatedNotificationHandler`
(`INotificationAsyncHandler<AIConversationShareCreatedNotification>`, reacting to the notification
`AIConversationShareService` publishes on a genuinely new share — see the Connected systems section)
resolves the recipient's and owner's `IUser` records via `IUserService.GetAsync(key)`,
builds an `EmailMessage`, and calls `IEmailSender.SendAsync(message, emailType)`. Same underlying
core pipeline already used for forgot-password and content-change alert emails — not new
infrastructure, just the first consumer of it in this product. No new toast/bell mechanism —
checked the codebase and Umbraco core for one; none exists to hook into, and building one would be a
materially bigger lift than reusing the email pipeline for what's a small, internal-collaboration
signal.

**Decision:** the email fires only when a share row is *created*, not when
`ShareConversationAsync`'s idempotent upsert (decision — see "Recipients are named" AC4 in
`STORIES.md`) updates an existing recipient's scope. Re-sharing to someone who already has access
(e.g. switching them from Snapshot to Live) is not a new event worth emailing about — avoids
spamming a recipient every time the owner adjusts scope.

**Why the recipient's own email address, not a generic system address**: the recipient is an
existing backoffice user; their email is already on their user record (the same address core's own
notification emails already go to) — no new address collection needed.

**TODO for `feature-build`**: exact email copy (subject, body, what it links to) — same
product/legal-adjacent review flag as the disclosure-warning copy in decision 2, not just
engineering wording, per the org's GDPR-conscious posture.

### 6. Owner-side "this is shared" indicator uses Umbraco's real entity-sign (flag) system

`BRIEF.md` requires the owner to see, from their own conversation list, that a conversation is
currently shared — not only inside the Share dialog.

**Superseded approach, kept for context:** an earlier pass of this decision swapped the row's
single icon (pin → share → chat, picking one). Dropped once the user pointed out Umbraco already
has a dedicated mechanism for exactly this — small badge icons overlaid on a tree item's main
icon — and asked for that instead. Checked it directly in `@umbraco-cms/backoffice`'s own source
rather than assuming:

- **`UmbEntityFlag`** (`core/entity-flag`): `{ alias: string }` — a plain tag an entity carries,
  independent of anything UI-specific.
- **`entitySign`** (`core/entity-sign`): the extension type that turns a flag alias into a visible
  badge. A manifest like Umbraco core's own `Umb.EntitySign.Document.HasScheduledPublish` declares
  `forEntityTypes`, `forEntityFlags`, and (for the built-in `icon` kind) `meta.iconName` +
  `meta.iconColorAlias` + a localized `meta.label`.
- **`<umb-entity-sign-bundle .entityType=... .entityFlags=...>`** wraps the row's existing main
  icon (slotted as its child) and overlays every matching registered sign as a small corner badge
  (~8px, circular cut-out background) on top of it — multiple signs can stack. Hovering shows a
  popover with each sign's label. This is the exact mechanism Umbraco's own document tree uses for
  "has pending changes," "protected," "scheduled," etc.

**Decision:** register a real `entitySign` manifest for conversations —
`forEntityTypes: [UAI_CONVERSATION_ENTITY_TYPE]`, `forEntityFlags: ['Uai.Conversation.IsShared']`,
`meta: { iconName: 'icon-share', iconColorAlias: 'positive', label: '#uaiCopilotWorkspace_isSharedSignLabel' }`
— and wrap `uai-copilot-workspace-conversation-tree-item`'s existing icon-rendering in
`<umb-entity-sign-bundle>`, passing `entityFlags: hasActiveShares ? [{ alias: 'Uai.Conversation.IsShared' }] : []`.
This is strictly additive to the row's existing icon logic (pin vs. chat) — the main icon
rendering doesn't change at all, the sign just overlays on top of whichever one is showing. Pinned
and shared now render *simultaneously*, which the earlier icon-swap design couldn't do. Hovering
the badge gives a quick "Shared" label without opening anything, for free. Rejected alternative
(the superseded icon-swap): couldn't show both states at once and wasn't the established
convention for this exact kind of thing — this repo's own "mirror an existing pattern, don't
hand-roll" rule, applied one level more carefully than the first pass did.

**Data shape, to avoid an N+1 query:** the owner's paged conversation list
(`AIConversationService.GetConversationsPagedAsync` → `IAIConversationRepository.GetPagedAsync`)
needs a `HasActiveShares: bool` on each row, computed in the same query
(`Shares.Any(s => s.DateRevoked == null)` in the EF Core projection), not a follow-up query per
conversation. `AIConversation`/`ConversationResponseModel` both gain this field, read-only,
server-computed — never set by a caller, following the same posture as `UserKey`.

**Re-verified against the real `v18/dev` code, not the pre-merge RC branches** (per this repo's
`feedback_design_verify_against_real_code` memory): `AIConversation`, `AIConversationEntity`,
`AIMessage`, `AIConversationResourceEntity`, and `AIConversationService` (including
`GetOwnedOrThrowAsync`'s not-found/not-owned indistinguishability) all match what decisions 1–5
were designed against, byte-for-byte on the relevant members. One unrelated addition since RC
finalization: `AIMessage.AgentId` and `AIConversationService.GetLastAssistantAgentIdAsync`
(agent-auto-selection tracking) — doesn't affect anything in this feature.

**Mockups:** static HTML + screenshot produced for all three screens this feature adds/changes —
the Share modal, the read-only chat/context-panel view, and the sidebar (shared indicator +
"shared with me" group) — see `SPEC.md`'s Frontend components section for each, and
`docs/mockups/starters/copilot-workspace.html` for the reusable Copilot Workspace section starter
they're built from (also created this pass, alongside a `copilot-sidebar.html` starter for the
contextual Copilot, on request, for future features to reuse).

### 7. Sharing lives entirely in the context panel's "Shared with" section — no entity action

**Superseded approach, kept for context:** two earlier passes both routed through
`UaiConversationShareAction` in the entity-action menu — first as one combined modal, then split
into a manage view plus a separate picker modal. Dropped at the user's request for something
simpler: sharing should work exactly like the context panel's existing Contexts/Resources
sections — a visible list with an inline add control — rather than needing its own menu item and
modal(s) at all.

**What the existing sections actually do, checked against the real source, not assumed:**
`conversation-context-panel.element.ts` doesn't render its rows itself — both Contexts and
Resources delegate to the shared `<uai-resource-list compact>` component
(`Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/context/components/resource-list/resource-list.element.ts`).
Each row is a `uui-ref-node` with an inline `uui-action-bar` trash-icon button
(`#renderRemoveAction`) that removes the item directly — no separate confirm step, no second
modal. The "+ Add" control is a `uui-button look="placeholder"` that opens a picker modal
(`UAI_CONTEXT_RESOURCE_TYPE_PICKER_MODAL`) directly; picking an item updates local state and fires
a plain `UmbChangeEvent` that the parent panel listens for and persists.

**Decision:** give "Shared with" the identical shape, not a parallel one:
- The section renders first, above Contexts and Resources — who can see a conversation is more
  load-bearing than what's attached to it — as a `uui-ref-node` per active recipient (name + scope
  mode in the detail slot, e.g. "Live"/"Snapshot"), sourced from
  `GET .../shares`, with the same inline `uui-action-bar` trash-icon remove button. Unlike
  Resources (which re-submits its whole local array on change), each row's remove button calls
  `DELETE .../shares/{userKey}` directly — shares already have independent per-row persistence
  identity and an immediate-revoke endpoint, so there's no "whole list" to resubmit.
- A "Choose" `uui-button look="placeholder"` — same control, same label convention as the
  Contexts/Resources add button — opens `UAI_SHARE_PICKER_MODAL` directly. No entity-action menu
  item, no intermediate manage view: this section *is* the management surface.
- Empty state: "Not shared with anyone yet." in place of the list, same "Choose" button.
- `UaiConversationShareAction` is dropped entirely — Rename/Move/Pin/Archive/Delete stay as
  entity actions (they have no natural visible-list home the way sharing now does), but sharing no
  longer needs one.

**The picker modal (`UAI_SHARE_PICKER_MODAL`), now a single-recipient form:** picking "the actual
user" is one person per invocation (repeatable, exactly like adding one resource at a time) —
- **Recipient**: a one-item picker field, same shape as a content-picker property editor — empty
  shows a "Choose a person" placeholder button; clicking it opens the real
  `UMB_USER_PICKER_MODAL` (`@umbraco-cms/backoffice/user`) to pick one user. No existing
  single-select wrapper exists anywhere in this codebase (checked: no `UMB_USER_PICKER_MODAL`,
  `user-picker`, or any owner/collaborator/assignee picker precedent in `Umbraco.AI`,
  `Umbraco.AI.Agent*`, or `Umbraco.AI.Agent.Copilot.Workspace`) — this is the first consumer, so
  `UMB_USER_PICKER_MODAL` is used directly with a max-of-one constraint rather than inventing a
  new lighter wrapper first. Once picked, the field shows the chosen person as one `uui-ref-node`
  row, re-pickable by clicking it again.
- **Mode**: a `uui-select` dropdown (Snapshot/Live), not a radio group — mirrors the codebase's own
  idiom for a small fixed-option enum, e.g. `agent-details-workspace-view.element.ts`'s output-format
  `uui-select` (`.options` as `{name, value, selected}[]`, `@change` reads `event.target.value`),
  rather than introducing a different control shape for the same kind of choice.
- The disclosure banner (unchanged from earlier passes — only shown when the conversation has
  attachments) and the submit (`POST .../shares` with the one picked recipient) are otherwise as
  before.

Rejected alternative (the prior two-modal design): added a menu item, a manage-view modal, and a
multi-recipient-with-chips picker, where this feature's only two building blocks — "a list you can
remove from" and "a picker you can add from" — already exist and are already used twice in this
exact panel.

### 8. A Snapshot share that can't resolve a cutoff message fails the share — it never silently becomes Live

Found during `feature-build` review (`SC-04`), not anticipated at design time: resolving the
`CutoffMessageId` for a `Snapshot` share means finding the conversation's current last message. Two
cases can come back with no message to anchor to — the conversation has zero messages yet, or a
regenerate truncates the tail in the narrow window between counting messages and fetching the last
one. The naive handling stores `CutoffMessageId = null` in both cases, but `null` is exactly what a
`Live` share's cutoff looks like (see the data model table). A share the owner explicitly chose as
"frozen at today's messages" would silently read as "no ceiling" instead — the opposite of what was
asked for, and a real exposure: everything the owner writes afterward becomes visible to a recipient
they only meant to show today's state to.

**Decision:** if a `Snapshot` share's cutoff can't be resolved to a concrete message id, the share
request fails outright (the service throws, the owner sees an error and can retry) rather than
falling back to `null`/Live. This is the same "never reveal more than intended" posture the data
model's graceful-degradation rule already applies at *read* time (a cutoff message deleted by
regenerate falls back to "everything that still exists," never to "everything, unbounded") — applied
here at *share* time instead, where the safe failure mode is "reject the share," not "silently widen
it." `feature-plan`/`feature-build` TODO, not resolved here: whether "Snapshot an empty conversation"
should be a supported case at all (it would need its own sentinel, since there's no message to
reference) is a real product question, not a bug to patch around — until decided, failing the share
is the only safe default.

## TODOs carried into `feature-plan` / `feature-build`

- Exact copy for the share-time disclosure warning (see decision 2) — needs product/legal-adjacent
  review given the org's GDPR-conscious posture, not just engineering wording.
- Exact copy for the share-notification email (see decision 5) — same review flag.
- Confirm `UMB_USER_PICKER_MODAL` actually supports (or can be configured/wrapped for) a
  max-selection-of-one constraint at `feature-build` time (decision 7) — identified this session as
  the real modal to reuse, but its single-select behavior wasn't verified beyond the type shape.
- No v17-specific CMS API incompatibility is expected (nothing here uses a v18-only API), but this
  hasn't been explicitly checked against the v17 line yet.
- The exact share-icon name and icon color alias used for the entitySign (decision 6, `icon-share`
  / `positive` in the design) need confirming against the real Umbraco icon/color sets during
  `feature-build`.
- Localization key and exact label text for the entitySign's hover tooltip (decision 6's
  `uaiCopilotWorkspace_isSharedSignLabel`) — not drafted this session.
