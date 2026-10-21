[Plan folder](docs/plans/share-conversations/)

## Why the change

Lets a Copilot Workspace conversation owner share a read-only copy of a conversation with named colleagues, either frozen at today's messages or kept live, so a colleague can see it without needing to be looped into the chat itself.

## What it looks like

**Where to find it:** Copilot section › any conversation › context panel's new "Shared with" section (owner) and the sidebar's new "Shared with me" group (recipient); the owner's own conversation rows also gain a small "Shared" badge.

**Who can see it:** Anyone with access to the Copilot Workspace section — same access as the rest of Copilot Workspace, no new permission.

![Sidebar shared badge](https://github.com/umbraco/Umbraco.AI/blob/507b352f27b0df45be23e77b531f6f023b6cd786/docs/plans/share-conversations/screenshots/sidebar-shared-badge.png?raw=true)

![Shared with context panel section](https://github.com/umbraco/Umbraco.AI/blob/507b352f27b0df45be23e77b531f6f023b6cd786/docs/plans/share-conversations/screenshots/context-panel-shared-with.png?raw=true)

## Special things to note

- A whole-feature review (the last task, after all 18 individual tasks had already passed their own review) caught one real, user-facing bug and it's already fixed in this PR: a Snapshot share ("frozen at today's messages") could silently turn into an unbounded Live share if the owner later regenerated a reply, because the regenerate's message-sequence renumbering could fool the old cutoff-resolution fallback into showing the recipient everything written afterward. Fixed by bounding the fallback to the share's own `DateShared` timestamp instead of sequence numbers. See `DECISION-LOG.md`'s whole-feature-review entry for the full trace.
- **Needs a decision:** whether "sharing a conversation that has no messages yet" should be a supported case at all. Today the service just rejects the share outright (there's no message to anchor a Snapshot cutoff to), which is the safe default, but nobody has decided if that's the permanent answer or a gap to close later (`ARCHITECTURE.md` decision 8).
- The "Shared with me" sidebar badge's dynamic "Shared by `<owner>`" hover text required a second, custom `entitySign` extension (a real Umbraco core mechanism, not a workaround) because the existing owner-side badge's label is static per-manifest and can't vary per row. Both badges use the same real extension system and the same visual styling; see `DECISION-LOG.md`'s SC-17 entries for the investigation.
- No migration-ID compatibility concerns for this PR specifically — it's new on `v18/dev` only. (The `v17/dev` backport, a separate PR, deliberately reuses this PR's exact migration IDs — see that PR's own notes.)

## Change outline

The new share belongs on its own table, keyed to the conversation it shares and the recipient it's shared with:

```sql
CREATE TABLE umbracoAIConversationsShare (
  Id UNIQUEIDENTIFIER PRIMARY KEY,
  ConversationId UNIQUEIDENTIFIER NOT NULL REFERENCES umbracoAIConversationsConversation(Id) ON DELETE CASCADE,
  SharedWithUserKey UNIQUEIDENTIFIER NOT NULL,
  SharedByUserKey UNIQUEIDENTIFIER NOT NULL,
  ScopeMode INT NOT NULL,              -- 0 = Snapshot, 1 = Live
  CutoffMessageId UNIQUEIDENTIFIER NULL,
  DateShared DATETIME2 NOT NULL,
  DateRevoked DATETIME2 NULL
)
-- unique on (ConversationId, SharedWithUserKey) WHERE DateRevoked IS NULL:
-- revoking and re-sharing the same person inserts a fresh row rather than reviving the old one.
```

Five new Management API endpoints, all under the existing `conversations` route:

| Method | Route | Who can call it | What it does |
|---|---|---|---|
| `POST` | `/conversations/{id}/shares` | Conversation owner | Shares with one or more colleagues (frontend only ever sends one) |
| `GET` | `/conversations/{id}/shares` | Conversation owner | Lists active shares on a conversation |
| `DELETE` | `/conversations/{id}/shares/{userKey}` | Conversation owner | Revokes one recipient's access |
| `GET` | `/conversations/{id}/shared-view` | The share's recipient | Reads the conversation read-only, bounded by scope mode |
| `GET` | `/conversations/shared-with-me` | Any backoffice user | Lists conversations shared with the caller |

New backoffice registrations:

| Type | Name | Where it appears |
|---|---|---|
| Context panel section | Shared with | Conversation workspace, context panel, above Contexts |
| Sidebar group | Shared with me | Copilot Workspace sidebar, between Recent and Archived |
| Modal | Share picker | Opened from the "Shared with" section's "Choose" button |
| Entity sign | Shared (static) | Owner's own conversation rows, when `hasActiveShares` is true |
| Entity sign | Shared (dynamic, "Shared by `<owner>`") | Recipient's "Shared with me" rows |

Opening a conversation doesn't need a second route for "it's shared with me, not owned by me" — the store tries the owner lookup first and falls back on a 404 (the backend's own 404 is already indistinguishable between "doesn't exist" and "not yours"):

```diff
 ConversationWorkspaceElement.conversationId = routeId
   store.setConversationId(id)
     #reload(id)
       requestById(id)
+      if not found:
+        setSharedConversationId(id)   // requestSharedView(id) instead
```

The chat history load mirrors the same fallback independently, since it's a separate fetch the store doesn't make on the chat's behalf.
