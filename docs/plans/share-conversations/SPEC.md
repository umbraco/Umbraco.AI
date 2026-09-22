# Spec

## Management API surface

All routes are rooted at the existing `conversations` segment
(`ConversationsManagementApiConstants.Conversations.RouteSegment`), versioned `1.0`, behind the
existing backoffice section `[Authorize]` policy. Owner-only endpoints return the existing
not-found-vs-forbidden-indistinguishable 404 (`ConversationNotFound()`) so a non-owner can't probe
which conversation ids exist, exactly like `DeleteConversationController` does today.

### `POST /conversations/{id}/shares` — create or update shares (owner-only)

Request:
```json
{
  "recipientUserKeys": ["<guid>", "..."],
  "scopeMode": "snapshot" | "live"
}
```

Behavior:
- 404 if `{id}` isn't owned by the acting user (indistinguishable from not-found).
- For each `recipientUserKey`: creates a new share row, or updates the existing one (recipient
  already shared with) rather than erroring — idempotent.
- `scopeMode: "snapshot"` sets `CutoffMessageId` to the id of the conversation's current last
  message, resolved server-side — never accepted from the client, the same "never trust
  caller-supplied identity of what's being frozen" posture as `UserKey` elsewhere in this service.
  `scopeMode: "live"` leaves it `null`.
- For each recipient a **new** share row is created for (not an update to an existing one): sends
  them an email via `SendEmailNotification` (see `ARCHITECTURE.md` decision 5), naming the owner
  and linking to the shared conversation. Updating an existing recipient's scope does not send a
  second email.
- 200 with the resulting list of active shares for this conversation (same shape as the `GET`
  below).

### `GET /conversations/{id}/shares` — list current recipients (owner-only)

- 404 if not owned by the acting user.
- 200 with active (non-revoked) shares: `[{ userKey, userName, scopeMode, dateShared }]` — used by
  the Share modal's "currently shared with" list.

### `DELETE /conversations/{id}/shares/{userKey}` — revoke one recipient (owner-only)

- 404 if the conversation isn't owned by the acting user, **or** if there is no active share for
  that `userKey` (same indistinguishability principle).
- Sets `DateRevoked`; the row is kept, not deleted (see `ARCHITECTURE.md`'s "Why soft-revoke"
  rationale in the data model section).
- 204 No Content. The recipient loses access on their next request — no other side effect.

### `GET /conversations/shared-with-me` — the recipient's list (paged)

Parallel to the existing `GET /conversations` (`AllConversationsController`), but sourced from
active shares where the acting user is the recipient, not from owned conversations.

- Query params mirror the existing list endpoint where they still make sense: `skip`, `take`,
  `search`. `projectId`/`includeArchived` do not apply (shared conversations aren't grouped into
  the recipient's own projects, and archiving is an owner-only concept) and are omitted.
- 200 `PagedViewModel<SharedConversationSummaryResponseModel>` — conversation id, title, owner's
  display name, `scopeMode`, `dateShared`. No message content in the list view.

### `GET /conversations/{id}/shared-view` — the recipient's read (single conversation + messages)

- 404 if there is no active (non-revoked) share for `{id}` addressed to the acting user.
- 200 `SharedConversationResponseModel`:
  - Conversation metadata (title, agent, dates) — same shape as the owner's response model, minus
    owner-only fields (`isPinned`, `isArchived`, `projectId`).
  - `messages`: for a `Snapshot` share, every message whose `Sequence` is `<=` the `CutoffMessageId`
    message's `Sequence` (resolved at read time); for a `Live` share, all messages. Both in the
    existing ascending order. If the `CutoffMessageId` message no longer exists (e.g. the owner
    used regenerate and truncated past it), falls back to every message that still exists — never
    an error.
  - `resources` / `contextIds`: unfiltered — the same attachment list the owner sees. See
    `ARCHITECTURE.md` decision 2 for why; the corresponding disclosure happens at share time, in
    the Share modal (below), not by filtering the read.
- No write operations exist on this route or any other route for a non-owner. There is no
  "shared" variant of send-message, rename, delete, truncate, or attach-resource.

## Frontend components

All in `Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets`, following existing patterns in the
`conversation/` feature folder.

### `UaiConversationShareAction` (new entity action)

- Same shape as `UaiConversationRenameAction`: resolves the conversation, opens a modal, applies
  the result through the repository.
- Opens `UAI_SHARE_CONVERSATION_MODAL` (new modal token) with: a recipient picker (reusing
  whichever existing backoffice user-picker component the codebase already has — identify during
  `umb-build-loop`), a Snapshot/Live radio choice, a list of current recipients with a revoke
  button per row (backed by `GET`/`DELETE .../shares`), and a **disclosure notice** — shown
  whenever the conversation has any `contextIds`/`resources` attached, stating plainly that the
  recipient will see everything referenced in the conversation, including pages or files they
  might not otherwise have permission to open (see `ARCHITECTURE.md` decision 2; exact copy is a
  TODO for `umb-build-loop`). Not a confirmation the owner has to actively dismiss beyond the
  normal submit — a visible warning, not a blocking gate, consistent with this being the owner's
  judgment call to make.
- Must observably: let the owner add one or more recipients and pick a scope in one submit (`POST
  .../shares`), revoke an existing recipient without reopening the modal from scratch, and surface
  errors through the same toast/notification pattern other conversation actions already use.
- Appears in the same entity-action menu as Pin/Archive/Rename/Move/Delete — owner-only, so it
  never appears on a conversation the acting user doesn't own (matches how those existing actions
  are already scoped).

### `conversation.repository.ts` additions

- `share(conversation, recipientUserKeys, scopeMode)` → `POST .../shares`
- `getShares(conversation)` → `GET .../shares`
- `revokeShare(conversation, userKey)` → `DELETE .../shares/{userKey}`
- `requestSharedWithMe(skip, take, search?)` → `GET /conversations/shared-with-me`
- `requestSharedView(id)` → `GET /conversations/{id}/shared-view`

### Sidebar: "Shared with me" grouping

- A new group in the existing conversation sidebar list, visually parallel to the existing
  Pinned/Recent groupings, sourced from `requestSharedWithMe()`.
- Entries show title + owner's display name (so the recipient knows whose conversation it is).
- Only a "View" affordance — no Pin/Archive/Rename/Move/Delete entity actions render for shared
  entries; those remain exclusively on the owner's own conversations.
- **Not** a separate section or tab — reuses the one existing sidebar list surface, since Copilot
  Workspace doesn't have a second navigational area today and this feature doesn't need one.

### Chat detail view and context panel: read-only mode

A read-only mode already exists — it's what archived conversations use today — and this feature
extends it rather than building a parallel mechanism, across both surfaces that need to respect it.

**Today:** `UaiConversationWorkspaceContext` (the single store both the chat and the context panel
observe) exposes `isReadonly$ = c?.isArchived ?? false`. `conversation-chat-view.element.ts`
observes it and passes it straight through to the shared `<uai-chat readonly
readonly-notice="...">`, which hides the composer and shows a localized notice when `readonly` is
true. The context panel, however, does **not** consume `isReadonly$` — its resource and context
pickers check `conversation.isArchived` directly, in four separate places, to decide whether "add a
resource/context" is enabled.

**Extension:**
- `UaiConversationDetailModel` gains an `isShared: boolean` field (alongside the existing
  `isPinned`/`isArchived`), following the same flat-boolean shape, plus a single derived
  `readonly: boolean` computed once at mapping time (`isArchived || isShared`).
- `isReadonly$` becomes `c?.readonly ?? false` (was `c?.isArchived ?? false`) — a shared
  conversation is read-only for exactly the same reason an archived one is: no writes are ever
  valid against it.
- The context panel's four `conversation.isArchived` checks switch to `conversation.readonly`
  instead — one field every consumer reads, rather than each site individually growing an
  `|| conversation.isShared`. Without this, a shared conversation's context panel would render
  working-looking "add a resource/context" controls that silently fail, since there's no write
  endpoint for a shared view. It shows the same (unfiltered — see `ARCHITECTURE.md` decision 2)
  resource list the owner sees; it just can't be edited.
- `UaiConversationWorkspaceContext.#reload()` needs a second path: when the route target is a
  shared conversation, it calls the new `requestSharedView(id)` instead of the owner's
  `requestById(id)`, and maps the `SharedConversationResponseModel` into
  `UaiConversationDetailModel` via a new `toSharedConversationDetailModel()` (parallel to the
  existing `toConversationDetailModel()`) that sets `isShared: true` and leaves
  `isPinned`/`isArchived` at their defaults, since those concepts don't apply to a share.
- The `readonly-notice` text varies by *why* it's read-only (archived vs. shared-by-\<owner name\>)
  — `conversation-chat-view.element.ts` already passes this as a plain attribute, so this is a
  matter of picking the right localized string (a new `uaiCopilotWorkspace_sharedReadOnlyNotice`,
  likely interpolating the owner's display name) based on `isArchived` vs `isShared`, not a new
  plumbing mechanism.
- Message rendering itself (text, tool calls, attachments) is entirely unchanged — reusing the
  existing renderer is the main reason to extend the existing components rather than building
  parallel ones.
