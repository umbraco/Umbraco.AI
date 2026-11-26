# Architecture: Agent memory

Memory lives in the Agent package as a MAF `AIContextProvider`. Before each run it loads the
two layers from one table, puts the memories into the stable head of the prompt, puts any
changes made since the chat started into the volatile tail, and hands the agent `remember` and
`forget` tools. The *space* comes from a pluggable resolver: Copilot Workspace plugs in projects
and shared chats, and anything unclaimed falls back to "you + this agent".

Lasting knowledge is the user's *personal context*: AI Contexts and resources they pick in
their profile, owned by core and loaded by a new step in core's context resolver chain, but
only when the agent or prompt running allows it. See "Personal context".

![Memory flow for one run](./diagrams/memory-run-flow.svg)

> [!IMPORTANT]
> **8 guesses need your OK.**
>
> 1. **No `recall` tool in v1.** Every active memory in both layers goes into the prompt, capped
>    per space (see SPEC's limits). Lasting knowledge moves to personal resources, which
>    already load on demand, so `recall` only comes if memories alone outgrow the caps.\
>    *If not:* add `recall` now and inject only the newest few; costs a tool round trip before
>    the agent knows anything.
> 2. **`forget` is a tool too**, so "forget that" works in chat, not only in the lists.\
>    *If not:* `remember` only; forgetting happens in the UI.
> 3. **The sidebar has no frozen snapshot.** Its runs keep no session state, so the memory block
>    is rebuilt each run. It still only changes when memory changes, so the cost is one cache
>    miss after a save.\
>    *If not:* store the sidebar snapshot server-side, keyed by thread id, with an expiry.
> 4. **The Decision check can only make a verdict stricter.** It can hold or refuse a save the
>    rules passed, never pass one the rules held or refused. If it errors or has no answer, the
>    rules' verdict stands (see "Suspect check").\
>    *If not:* the Decision check can also pass a save the rules held, trusting the model over
>    the rules.
> 5. **Held memories expire after 7 days** if nobody keeps them, and replaced memories are
>    purged 7 days after being replaced. Both are cleared when the list is next read. No
>    background job.\
>    *If not:* they wait forever, or a scheduled job clears them.
> 6. **A chat that has ever been shared counts as shared**, read from the existing share rows
>    (revoked rows are kept with `DateRevoked`). No new column on the conversation.\
>    *If not:* a `MemorySpaceLockedAt` column on the conversation, set on first share.
> 7. **Personal context has its own two core tables**, shaped like a Workspace project's
>    context ids and resources, not a hidden AI Context owned by the user.\
>    *If not:* a user-owned `AIContext` with an owner column. It reuses versioning, but every
>    context list, picker, Deploy export and permission check must then filter it out.
> 8. **The caller opts in with a runtime-context flag.** The personal context resolver does
>    nothing unless the run sets `Umbraco.AI.IncludePersonalContext`, so core features that don't
>    know about it (tests, Search, Automate) never pick it up.\
>    *If not:* it loads for every run with a user, and each feature opts out instead.

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
  function-invoking middleware and stream out as normal tool calls. They aren't system tools
  (`IAISystemTool`, like `get_context_resource`): those are offered on every agent and prompt
  run, so memory tools would show with memory off and leak into prompts. Their arguments are
  typed records with `[Description]` on each field, created through the same function factory
  as the system tools, so the model sees the same style. The names stay `remember` and
  `forget`, which read more naturally to a model than `save_memory`.
- **One table with a space type and key**, not a table per layer. A new layer (the company
  layer later) is a new space type, not a migration.
- **Space types are strings owned by whoever defines them.** The Agent package defines `user`
  and `user-agent`; Workspace defines `project` and `conversation`. The Agent package never
  knows what a project is.
- **`IAIMemorySpaceResolver` is the hook**, an Umbraco ordered collection
  (`AIMemorySpaceResolverCollectionBuilder : OrderedCollectionBuilderBase`, reached with
  `builder.AIMemorySpaceResolvers()`, like `AIAgentSelectors()`). Each resolver gets the run's
  context and either claims the space or passes. The Agent package appends the built-in
  `user-agent` resolver, which always claims; Workspace adds its own with
  `InsertBefore<UserAgentMemorySpaceResolver, WorkspaceMemorySpaceResolver>()`, so the fallback
  stays last. It also answers "can this user read or
  change this space", so access checks stay with the package that owns the space.
- **Short ids in the prompt, just long enough to be unique.** Each memory shows the model an
  id that starts as the first 8 characters of its Guid. Ids that would clash within one run's
  memories are lengthened until they differ. The provider maps them back only among the
  memories it injected or saved this run, and an id that matches more than one is refused,
  never guessed. The helper is `AIShortIdMap` (internal), a shared design for any tool that
  lists entities to a model (project decision `short-ids-for-models`).
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
- **A personal switch** in the "Your AI" box ("Let agents remember things about me") turns
  "about you" off for that user only, stored with the CMS's `IUserDataService`. It is stored
  only once the user flips it, so an untouched switch follows the site policy (`Allowed` on,
  `OptIn` off). Under `NotAllowed` the switch is hidden.
- **No user, no memory.** Runs without a backoffice user (Automate) get no block and no tools.
- **Notifications for each lifecycle moment**: `AIMemorySavingNotification` (cancelable),
  `AIMemorySavedNotification`, `AIMemoryDeletingNotification` (cancelable),
  `AIMemoryDeletedNotification`, matching the agent and project notifications.
- **Memory is user data, not config.** It is never deployed between environments.
- **Personal context is core's, not the Agent package's**, because prompts use it too. Memory
  doesn't depend on it. See "Personal context".

## Settings extension

The AI Settings screen today is one workspace view with fixed boxes, saving a fixed list of
keys. Settings are already stored as key-value rows (`AISettingsEntity`: `Key`, `Value`), so
add-on settings need no new table.

- **Server:** add-ons register a settings provider that lists its definitions (key, type,
  default) in a new Umbraco lazy collection (`AISettingsProviderCollectionBuilder :
  LazyCollectionBuilderBase`, reached with `builder.AISettingsProviders()`, like
  `AIGuardrailEvaluators()`). Order doesn't matter, so it isn't an ordered collection. `IAISettingsService` gains
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

Checks are pluggable: `IAIMemorySaveCheck` is an Umbraco ordered collection
(`AIMemorySaveCheckCollectionBuilder : OrderedCollectionBuilderBase`, reached with
`builder.AIMemorySaveChecks()`), so a package can `Append`, `InsertBefore` or `Remove` a check. Each check gets the new text, the
target space and the run's messages (from M.E.AI's `FunctionInvokingChatClient.CurrentContext`),
and returns saved, held or not saved with a reason, or no opinion. The strictest answer wins
(not saved, then held, then saved), and a not saved stops the rest from running.

Two checks ship, in this order:

1. **`AIMemoryRulesCheck`**, the fixed rules in the table below. Free, and always runs.
2. **`DecisionMemorySaveCheck`**, which asks the site's default Decision profile one pick-one
   question about the text: safe, an order to the agent, copied from content, or a secret. It
   works like auto agent selection (`DecisionAgentSelector`): no opinion when the Decision
   feature is off, no default Decision profile is set, or the call fails (logged as a warning).
   A TypeSafe connection with Jev is one way to fill that profile. No new setting.

The rules check:

| Check | Result |
|---|---|
| Looks like a secret: key or token shapes, "password", card or national ID numbers | Not saved |
| Echoes a tool result (long shared phrases with one) but not the person's messages | Held |
| Reads like an order to the agent ("always add", "ignore") or has a link, email or code | Held if this run read tool output; otherwise saved |
| Going to a shared space and this run read tool output | Held |
| None of the above | Saved |

A held memory is stored with status `Held` and is never injected. It turns active only when a
person clicks Keep.

## Personal context

A user's own AI Contexts and resources, picked in the "Your AI" box of the user profile. It
lives in core because both agents and prompts read it.

- **Loaded by a new context resolver**, `PersonalContextResolver`, inserted into core's
  `AIContextResolvers()` chain before `AdditionalResourcesContextResolver`. It resolves the
  picked contexts and the user's own resources through the same resolve, format and inject
  path as every other source, so `Always` resources go in the system prompt and `OnDemand`
  ones become the existing lookup tool. A context already resolved from the agent or prompt is
  loaded once (the chain already drops duplicate ids).
- **Gated by the caller.** The resolver reads `Constants.ContextKeys.IncludePersonalContext` (bool)
  from the runtime context and the signed-in backoffice user. Without both it returns nothing
  (guess 8).
- **Agents opt out, prompts opt in.** `AIStandardAgentConfig` gains `IncludePersonalContext`
  (default true, and a missing value reads as true), carried by the config JSON like
  `MemoryMode`. `AIPrompt` gains a `IncludePersonalContext` column (default false), wired the way
  `IncludeEntityContext` is: entity, factory, migrations, version history adapter, API models,
  editor and the Prompt Deploy artifact. Each product's run sets the runtime flag from it.
- **Stored per user** in two core tables (see "Data model"), behind `IAIPersonalContextService`.
  The whole set is read and saved as one document, the way a project saves its contexts and
  resources.
- **Saved straight away.** The profile panel has no Save button, so each add, edit or remove
  saves at once.
- **The "Your AI" profile app is core's**, with the context picker and the resource list in
  compact mode (`uai-context-picker`, `uai-resource-list`), and an extension slot for a new
  `uaiUserProfileProperty` manifest type. The Agent package adds the personal memory switch
  there, so it sits in the same box; the memory table stays the Agent package's own profile
  app, below it.
- **Moving memories into a resource is manual in v1.** The user adds a resource and deletes
  the memories. A "Turn into resource" bulk action can come later (DECISION-LOG).
- **Clean-up.** A user delete removes their personal context; a context delete removes that
  context from every user's list.
- **A site-wide kill switch.** Core's AI Settings gains a built-in `PersonalContextEnabled`
  setting (default true) in a "Personal Context" box. When it's off the resolver returns nothing
  and the "Your AI" box hides the Contexts and Resources rows; stored data is kept. It's a core
  field, not an add-on setting, because core owns personal context.
- **Not deployed.** Like memory, it's user data.

## What changes

**Server: core** (`Umbraco.AI/src/`)

```text
Umbraco.AI.Core/Settings/
  AISettingDefinition.cs                key, type, default
  IAISettingsProvider.cs                lists one package's definitions
  AISettingsProviderCollection.cs       + LazyCollectionBuilderBase builder, AISettingsProviders()
  AISettings.cs                         + Additional values map, + PersonalContextEnabled
  AISettingsService.cs                  + GetSettingValueAsync<T>, validates additional keys
Umbraco.AI.Persistence/Settings/
  EFCoreAISettingsRepository.cs         reads/writes additional keys as rows
Umbraco.AI.Web/Api/Management/Settings/
  Models/                               + additional map on request/response
Umbraco.AI.Core/PersonalContext/
  AIPersonalContext.cs                  UserKey, ContextIds, Resources (AIContextResource)
  IAIPersonalContextService.cs          get / save the signed-in user's set
  AIPersonalContextService.cs           validates context ids and resource types
  IAIPersonalContextRepository.cs       internal
  PersonalContextResolver.cs            IAIContextResolver, gated by IncludePersonalContext
  UserDeletedPersonalContextHandler.cs  deletes the user's set
  ContextDeletedPersonalContextHandler.cs  drops a deleted context from every user's list
Umbraco.AI.Core/Constants.cs            + ContextKeys.IncludePersonalContext
Umbraco.AI.Core/Configuration/          + PersonalContextResolver in the resolver chain
Umbraco.AI.Persistence/PersonalContext/
  AIPersonalContextEntity.cs, AIPersonalContextResourceEntity.cs, EFCoreAIPersonalContextRepository.cs
Umbraco.AI.Persistence.SqlServer/Migrations/  UmbracoAI_AddPersonalContext
Umbraco.AI.Persistence.Sqlite/Migrations/     UmbracoAI_AddPersonalContext
Umbraco.AI.Web/Api/Management/PersonalContext/
  Controllers/, Models/, Mapping/       GET and PUT /personal-context
```

**Backoffice: core** (`Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/settings/`)

```text
extensions/uai-settings-panel.extension.ts  the uaiSettingsPanel manifest type
workspace/settings-root/
  settings-workspace.context.ts       + get/set for additional values
  settings-editor.element.ts          + "Personal Context" box, + extension slot for panels after
                                      the built-in boxes
exports.ts                            exports the manifest type and context token
```

**Backoffice: core, personal context** (`.../Client/src/personal-context/`)

```text
user-profile-app/your-ai-user-profile-app.element.ts  "Your AI": contexts, resources, property slot
extensions/uai-user-profile-property.extension.ts     the uaiUserProfileProperty manifest type
repository/                                           personal context API calls
exports.ts                                            repository and manifest type
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
    AIMemorySpaceResolverCollection.cs  + OrderedCollectionBuilderBase builder, AIMemorySpaceResolvers()
    UserAgentMemorySpaceResolver.cs     built-in fallback, always last
    AIMemoryContextProvider.cs          MAF provider: block, delta, tools
    AIMemoryPrompt.cs                   the fixed memory rules text
    IAIMemorySaveCheck.cs               the check hook (public)
    AIMemorySaveCheckCollection.cs      + OrderedCollectionBuilderBase builder, AIMemorySaveChecks();
                                        strictest answer wins
    AIMemoryRulesCheck.cs               the fixed rules
    DecisionMemorySaveCheck.cs          asks the default Decision profile
    AIMemoryTools.cs                    remember / forget functions
  Tools/AIShortIdMap.cs                 internal: Guid <-> shortest unique id (min 8) for a run
    Notifications/                      Saving, Saved, Deleting, Deleted
    UserDeletedMemoryHandler.cs         deletes the user's own layers
    AgentDeletedMemoryHandler.cs        deletes user-agent memories for that agent
  Agents/AIStandardAgentConfig.cs       + MemoryMode (Off, Work, WorkAndPerson), + IncludePersonalContext
  Chat/                                 run sets ContextKeys.IncludePersonalContext from the config
  Memory/AIMemorySettings.cs            IAISettingsProvider: Agent.Memory.Enabled (default true) and
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
  Agent/Models/AgentConfigModel.cs      + memoryMode, + includePersonalContext
  Agent/Mapping/AgentMapDefinition.cs   maps it
Umbraco.AI.Agent.Startup/               registers the above
```

**Server: Prompt package** (`Umbraco.AI.Prompt/src/`), mirroring `IncludeEntityContext`

```text
Umbraco.AI.Prompt.Core/Prompts/
  AIPrompt.cs                           + IncludePersonalContext (default false)
  AIPromptVersionableEntityAdapter.cs   + the field in version history
  AIPromptService.cs                    run sets ContextKeys.IncludePersonalContext
Umbraco.AI.Prompt.Persistence/          entity, factory, DbContext
Umbraco.AI.Prompt.Persistence.SqlServer/Migrations/  UmbracoAIPrompt_AddIncludePersonalContext
Umbraco.AI.Prompt.Persistence.Sqlite/Migrations/     UmbracoAIPrompt_AddIncludePersonalContext
Umbraco.AI.Prompt.Web/Api/Management/Prompt/  create, update, response models and mapping
Umbraco.AI.Prompt.Web.StaticAssets/.../prompt-details-workspace-view.element.ts  + toggle
Umbraco.AI.Prompt.Deploy/  AIPromptArtifact + UmbracoAIPromptServiceConnector carry the field
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
Umbraco.AI.Agent.Copilot.Workspace.Startup/  InsertBefore<UserAgentMemorySpaceResolver, ...>
```

**Backoffice: Agent package** (`Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/`)

```text
agent/workspace/agent/views/
  agent-details-workspace-view.element.ts  + Memory dropdown and "Include Personal Context" toggle
                                           in "Agent Behavior"
memory/
  settings-panel/memory-settings-panel.element.ts  uaiSettingsPanel: kill switch + personal policy
  repository/                              memory API calls
  components/memory-list.element.ts        shared list: text, who, when, actions
  modals/edit-memory-modal.*               edit one memory's text
  user-profile-app/
    memory-switch.user-profile-property.ts uaiUserProfileProperty: the personal switch in "Your AI"
    my-memory-user-profile-app.element.ts  the memory table
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

- **Cross-product floor:** the Agent and Prompt packages need the core version that ships the
  settings extension and personal context, so their core range floors move up with this
  release. Prompt.Deploy needs the Prompt version with the new field.

Deploy: the prompt's `IncludePersonalContext` is added to the Prompt Deploy artifact and
connector. Not touched: the rest of Deploy (memory and personal context are user data;
`MemoryMode` and `IncludePersonalContext` already travel in the agent config JSON; the site-wide
switch is a setting, which core Deploy doesn't move today),
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

Two new tables in the core DbContext, migration `UmbracoAI_AddPersonalContext` for SQL Server
and SQLite. The resource table copies `umbracoAIContextResource`'s columns, keyed by user.

```text
umbracoAIPersonalContext
  Column            Type            Null  Notes
  UserKey           Guid            no    PK
  ContextIds        nvarchar(4000)  no    the user's order, stored like a project's ContextIds
  DateModified      datetime        no

umbracoAIPersonalContextResource
  Column            Type            Null  Notes
  Id                Guid            no    PK
  UserKey           Guid            no    FK to umbracoAIPersonalContext, cascade delete
  ResourceTypeId    nvarchar(255)   no    an [AIContextResourceType]
  Name              nvarchar(255)   no
  Description       nvarchar(1000)  yes
  SortOrder         int             no
  Settings          nvarchar(max)   no    the resource type's settings JSON
  InjectionMode     int             no    0 Always, 1 OnDemand
  Index: (UserKey, SortOrder)
```

The Prompt table gains `IncludePersonalContext bit not null default 0`.
