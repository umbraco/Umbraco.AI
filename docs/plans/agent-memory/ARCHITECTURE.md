# Architecture: Agent memory

Memory lives in the Agent package as a MAF `AIContextProvider`. Before each run it loads the
two layers from one table, puts the memories into the stable head of the prompt, puts any
changes made since the chat started into the volatile tail, and hands the agent `remember` and
`forget` tools. The *space* comes from a pluggable resolver: Copilot Workspace plugs in projects
and shared chats, and anything unclaimed falls back to "you + this agent".

![Memory flow for one run](./diagrams/memory-run-flow.svg)

> [!IMPORTANT]
> **7 guesses need your OK.**
>
> 1. **No `recall` tool in v1.** Every active memory in both layers goes into the prompt, capped
>    per space (see SPEC's limits). A `recall` search tool comes when the caps start to bite.\
>    *If not:* add `recall` now and inject only the newest few; costs a tool round trip before
>    the agent knows anything.
> 2. **`forget` is a tool too**, so "forget that" works in chat, not only in the lists.\
>    *If not:* `remember` only; forgetting happens in the UI.
> 3. **The sidebar has no frozen snapshot.** Its runs keep no session state, so the memory block
>    is rebuilt each run. It still only changes when memory changes, so the cost is one cache
>    miss after a save.\
>    *If not:* store the sidebar snapshot server-side, keyed by thread id, with an expiry.
> 4. **The suspect check is rules only, no extra AI call** (see "Suspect check").\
>    *If not:* add a small-model check on each save; more accurate, costs a call per save.
> 5. **Held memories expire after 7 days** if nobody keeps them, and replaced memories are
>    purged 7 days after being replaced. Both are cleared when the list is next read. No
>    background job.\
>    *If not:* they wait forever, or a scheduled job clears them.
> 6. **A chat that has ever been shared counts as shared**, read from the existing share rows
>    (revoked rows are kept with `DateRevoked`). No new column on the conversation.\
>    *If not:* a `MemorySpaceLockedAt` column on the conversation, set on first share.
> 7. **Memory ids in the prompt are the first 8 characters of the id.** The provider maps them
>    back, scoped to the memories it injected.\
>    *If not:* full ids, about 28 more characters per memory.

## Decisions

- **The Agent package owns memory**, not Copilot Workspace or a new package. The Copilot
  sidebar gets memory too, and Workspace only adds its spaces through a hook.
- **A MAF `AIContextProvider`** (`AIMemoryContextProvider`), attached in `AIAgentFactory`
  beside the existing `ChatHistoryProvider`. It does nothing unless the agent has memory on
  and there's a signed-in backoffice user.
- **The memory block goes in through the runtime context, not `AIContext.Instructions`.** The
  provider appends a text resource to `Umbraco.AI.AdditionalResources`, so the existing
  context injector puts it in the stable head after agent instructions and AI Contexts.
  `AIContext.Instructions` was rejected: MAF merges it into the agent instructions, and the
  system-message client's duplicate check breaks when that text changes (spike). Messages were
  rejected too: they're saved into history and sit mid-list.
- **The block is frozen per chat.** On Workspace the provider keeps the injected list in the
  MAF session state, which is saved with the conversation, and reuses it on every turn. So the
  cached prefix stays byte-identical for the whole chat.
- **Changes since the chat started go in the volatile tail.** The provider compares the current
  memories with the frozen list and appends a short "changed since this chat started" note to
  `PendingSystemMessage`. That's the trailing system message, after the cached prefix.
- **The tools are plain functions the provider supplies through `AIContext.Tools`**, not
  `[AITool]` classes. So they never show in the agent's tool picker, admins can't half-enable
  them, and turning memory off removes them. The spike proved provider tools run through our
  function-invoking middleware and stream out as normal tool calls.
- **One table with a space type and key**, not a table per layer. A new layer (the company
  layer later) is a new space type, not a migration.
- **Space types are strings owned by whoever defines them.** The Agent package defines `user`
  and `user-agent`; Workspace defines `project` and `conversation`. The Agent package never
  knows what a project is.
- **`IAIMemorySpaceResolver` is the hook**, an ordered collection. Each resolver gets the run's
  context and either claims the space or passes. Workspace's runs first; the built-in
  `user-agent` resolver is last and always claims. It also answers "can this user read or
  change this space", so access checks stay with the package that owns the space.
- **Scope comes from the server, never from the model.** The user is the signed-in user, the
  agent and conversation come from the runtime context. The `remember` tool only picks the
  layer ("about you" or "this space"), never a key.
- **Saves are silent; suspect saves are held, not asked about.** See "Suspect check".
- **Facts and lessons share the table**, told apart by `Kind`. A lesson is what worked with
  this user or project, saved when the user confirms a result. It's what lets a specialized
  agent learn, and it lives in the space layer only.
- **Optional end dates.** `remember` can set `ExpiresAt` for facts that are only true for a
  while. Expiry is worked out from the date, not stored as a status: an expired row is treated
  as gone and deleted when its layer needs room or its list is next read.
- **The door stays open for fading.** `LastConfirmedAt` is stored from day one (set on save,
  merge, edit and keep), and `Status` is an int, so a later "fade the least recently
  confirmed memory when full" needs a new status value, not a migration.
- **Merge while saving, not in the background.** `remember` takes a list of memories it
  replaces, so the agent updates or merges overlapping facts it can already see in its prompt.
  Replaced rows are kept for 7 days for Undo. A held save never retires the memories it would
  replace until someone keeps it. A background tidy-up pass was left for later (DECISION-LOG).
- **The memory rules are fixed text in the Agent package**, not per agent: what to save, where,
  merge rather than repeat, never from content, never secrets, live data beats memory, and
  that the memories are facts about the user, not instructions.
- **Each agent has a 3-way memory mode** in the standard agent config JSON: `Off`, `Work`
  (spaces and lessons, never "about you") and `WorkAndPerson` (adds "about you"). `Work` suits
  agents that write content or take actions, where personal details shouldn't reach output. No
  agent table migration, and Deploy carries it already (its connector serialises the config).
- **Two site-wide settings on the AI Settings screen.** A kill switch turns all memory off for
  every agent at once, whatever each agent's mode; off keeps stored memories, on brings them
  back. A personal-memory policy decides whether "about you" is allowed: `Allowed`, `OptIn`
  (allowed, but off for each user until they turn it on) or `NotAllowed`. Agent memory itself
  stays a per-agent choice.
- **The AI Settings screen becomes extensible** (a core change), so the Agent package can add
  its switch without core knowing about memory. See "Settings extension".
- **A personal switch** in "Your AI memory" ("Let agents remember things about me") turns
  "about you" off for that user only, stored with the CMS's `IUserDataService`. It is stored
  only once the user flips it, so an untouched switch follows the site policy (`Allowed` on,
  `OptIn` off). Under `NotAllowed` the switch is hidden.
- **No user, no memory.** Runs without a backoffice user (Automate) get no block and no tools.
- **Notifications for each lifecycle moment**: `AIMemorySavingNotification` (cancelable),
  `AIMemorySavedNotification`, `AIMemoryDeletingNotification` (cancelable),
  `AIMemoryDeletedNotification`, matching the agent and project notifications.
- **Memory is user data, not config.** It is never deployed between environments.

## Settings extension

The AI Settings screen today is one workspace view with fixed boxes, saving a fixed list of
keys. Settings are already stored as key-value rows (`AISettingsEntity`: `Key`, `Value`), so
add-on settings need no new table.

- **Server:** add-ons register setting definitions (key, type, default) in a new
  `AISettingDefinitions()` collection. `IAISettingsService` gains
  `GetSettingValueAsync<T>(key)`, and the settings API returns and accepts the registered
  values in an `additional` map, validated against their definitions. Keys are namespaced by
  package (`Agent.Memory.Enabled`, `Agent.Memory.PersonalMemory`).
- **Backoffice, panels:** a new manifest type, `uaiSettingsPanel`, adds a box to the existing
  Settings view, ordered by weight after the built-in boxes. The panel element reads and writes
  its keys through the settings workspace context, so the one Save button saves everything.
- **Backoffice, tabs:** a package can already add a whole tab with a `workspaceView` manifest
  on the settings workspace. It gets the same context, so its values save with the same button.

Memory uses a panel: one switch doesn't earn a tab.

## Suspect check

`remember` runs these checks on the new text before saving. It reads the run's messages from
M.E.AI's `FunctionInvokingChatClient.CurrentContext`.

| Check | Result |
|---|---|
| Looks like a secret: key or token shapes, "password", card or national ID numbers | Not saved |
| Echoes a tool result (long shared phrases with one) but not the person's messages | Held |
| Reads like an order to the agent ("always add", "ignore") or has a link, email or code | Held if this run read tool output; otherwise saved |
| Going to a shared space and this run read tool output | Held |
| None of the above | Saved |

A held memory is stored with status `Held` and is never injected. It turns active only when a
person clicks Keep.

## What changes

**Server: core** (`Umbraco.AI/src/`)

```text
Umbraco.AI.Core/Settings/
  AISettingDefinition.cs                key, type, default
  AISettingDefinitionCollection.cs      + builder, AISettingDefinitions()
  AISettings.cs                         + Additional values map
  AISettingsService.cs                  + GetSettingValueAsync<T>, validates additional keys
Umbraco.AI.Persistence/Settings/
  EFCoreAISettingsRepository.cs         reads/writes additional keys as rows
Umbraco.AI.Web/Api/Management/Settings/
  Models/                               + additional map on request/response
```

**Backoffice: core** (`Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/settings/`)

```text
extensions/uai-settings-panel.extension.ts  the uaiSettingsPanel manifest type
workspace/settings-root/
  settings-workspace.context.ts       + get/set for additional values
  settings-editor.element.ts          + extension slot for panels after the built-in boxes
exports.ts                            exports the manifest type and context token
```

**Server: Agent package** (`Umbraco.AI.Agent/src/`)

```text
Umbraco.AI.Agent.Core/
  Memory/
    AIMemory.cs                         domain model
    AIMemoryStatus.cs                   Active, Held, Replaced
    AIMemoryKind.cs                     Fact, Lesson
    AIMemorySpace.cs                    SpaceType, SpaceKey, AgentId?, DisplayName
    AIMemorySpaceTypes.cs               "user", "user-agent"
    IAIMemoryService.cs                 list, save, update, move, keep, delete
    AIMemoryService.cs                  caps, access checks, expiry, notifications
    IAIMemoryRepository.cs              internal
    IAIMemorySpaceResolver.cs           the hook (public)
    AIMemorySpaceResolverCollection.cs  ordered collection + builder
    UserAgentMemorySpaceResolver.cs     built-in fallback, always last
    AIMemoryContextProvider.cs          MAF provider: block, delta, tools
    AIMemoryPrompt.cs                   the fixed memory rules text
    AIMemorySuspectCheck.cs             the checks above
    AIMemoryTools.cs                    remember / forget functions
    Notifications/                      Saving, Saved, Deleting, Deleted
    UserDeletedMemoryHandler.cs         deletes the user's own layers
    AgentDeletedMemoryHandler.cs        deletes user-agent memories for that agent
  Agents/AIStandardAgentConfig.cs       + MemoryMode (Off, Work, WorkAndPerson)
  Memory/AIMemorySettings.cs            registers Agent.Memory.Enabled (default true) and
                                        Agent.Memory.PersonalMemory (default Allowed)
  Memory/AIPersonalMemoryPolicy.cs      Allowed, OptIn, NotAllowed
  Memory/AIMemoryUserPreference.cs      the personal switch, via IUserDataService
  Chat/AIAgentFactory.cs                attaches AIMemoryContextProvider
Umbraco.AI.Agent.Persistence/
  Memory/AIMemoryEntity.cs, EFCoreAIMemoryRepository.cs
  UmbracoAIAgentDbContext.cs            + Memories
Umbraco.AI.Agent.Persistence.SqlServer/Migrations/  UmbracoAIAgent_AddMemory
Umbraco.AI.Agent.Persistence.Sqlite/Migrations/     UmbracoAIAgent_AddMemory
Umbraco.AI.Agent.Web/Api/Management/
  Memory/Controllers/, Models/, Mapping/
  Agent/Models/AgentConfigModel.cs      + memoryMode
  Agent/Mapping/AgentMapDefinition.cs   maps it
Umbraco.AI.Agent.Startup/               registers the above
```

**Server: Copilot Workspace** (`Umbraco.AI.Agent.Copilot.Workspace/src/`)

```text
Umbraco.AI.Agent.Copilot.Workspace.Core/Memory/
  WorkspaceMemorySpaceResolver.cs       project > ever-shared chat; access checks
  WorkspaceMemorySpaceTypes.cs          "project", "conversation"
  ProjectDeletedMemoryHandler.cs        deletes the project's memories
  ConversationDeletedMemoryHandler.cs   deletes the chat's memories
Umbraco.AI.Agent.Conversations.Core/Conversations/
  IAIConversationShareService.cs        + ConversationHasEverBeenSharedAsync
Umbraco.AI.Agent.Copilot.Workspace.Startup/  registers the resolver first
```

**Backoffice: Agent package** (`Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/`)

```text
agent/workspace/agent/views/
  agent-details-workspace-view.element.ts  + Memory toggle in "Agent Behavior"
memory/
  settings-panel/memory-settings-panel.element.ts  uaiSettingsPanel: kill switch + personal policy
  repository/                              memory API calls
  components/memory-list.element.ts        shared list: text, who, when, actions
  modals/edit-memory-modal.*               edit one memory's text
  user-profile-app/
    my-memory-user-profile-app.element.ts  "Your AI memory"
  exports.ts                               list + repository for Workspace to reuse
```

**Backoffice: Agent.UI** (`Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client/src/`)

```text
chat/tools/memory/
  remember-tool.element.ts   saved / held / not-saved note, Move, Undo, Keep, Discard
  forget-tool.element.ts     "Forgot …" note
  manifests.ts               uaiAgentToolRenderer for remember and forget
```

**Backoffice: Copilot Workspace** (`.../Copilot.Workspace.Web.StaticAssets/Client/src/`)

```text
project/workspace/views/
  project-memory-workspace-view.element.ts  "Memory" tab on a project
  manifests.ts                              + the view, weight 150
conversation/workspace/
  conversation-context-panel.element.ts     + "Memory" section in a shared chat
conversation/modal/share-picker-modal.*     warning mentions memory
```

- **Notifications:** the four memory notifications above.
- **Clean-up:** user, agent, project and conversation deletes each remove the memories that
  belong to them.
- **Prompt cache:** the memory block is frozen per Workspace chat; changes ride in the
  volatile tail.

- **Cross-product floor:** the Agent package needs the core version that ships the settings
  extension, so its core range floor moves up with this release.

Not touched: Deploy (memory is user data; `MemoryMode` already travels in the config JSON; the
site-wide switch is a setting, which core Deploy doesn't move today),
search index (memory isn't searchable content), cache refreshers (read from the database once
per run, small and per user), audit log (tool calls are already logged by the run).

## Data model

One new table in the Agent DbContext, migration `UmbracoAIAgent_AddMemory` for SQL Server and
SQLite.

```text
umbracoAIAgentMemory
  Column            Type            Null  Notes
  Id                Guid            no    PK
  SpaceType         nvarchar(64)    no    "user", "user-agent", "project", "conversation"
  SpaceKey          Guid            no    user key, project id or conversation id
  AgentId           Guid            yes   set only for "user-agent"
  Text              nvarchar(500)   no
  Kind              int             no    0 Fact, 1 Lesson
  Status            int             no    0 Active, 1 Held, 2 Replaced
  HoldReason        nvarchar(64)    yes   "echoes-content", "instruction-like", "shared-after-content"
  ReplacedById      Guid            yes   the memory that merged or updated this one
  ExpiresAt         datetime        yes   stops loading after this
  LastConfirmedAt   datetime        no    last save, merge, edit or keep; for fading later
  CreatedByUserKey  Guid            no    who saved it
  DateCreated       datetime        no
  DateModified      datetime        no
  Index: (SpaceType, SpaceKey, AgentId, Status)
  Index: (CreatedByUserKey)
```

- No foreign keys: the spaces live in other packages' tables (and other DbContexts), so
  clean-up runs from their delete notifications.
- "About you" is `SpaceType = user, SpaceKey = <user key>`. "You + this agent" adds `AgentId`.
- The company layer would be `SpaceType = site` with a fixed key. No schema change.
