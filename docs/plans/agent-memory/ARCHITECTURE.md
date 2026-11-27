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

<details>
<summary>8 guesses, agreed 26-11-2026</summary>

1. **No `recall` tool in v1.** Every active memory in both layers goes into the prompt, capped
   per space (see SPEC's limits). Lasting knowledge moves to personal resources, which
   already load on demand, so `recall` only comes if memories alone outgrow the caps.\
   *If not:* add `recall` now and inject only the newest few; costs a tool round trip before
   the agent knows anything.
2. **`forget` is a tool too**, so "forget that" works in chat, not only in the lists.\
   *If not:* `remember` only; forgetting happens in the UI.
3. **The sidebar has no frozen snapshot.** Its runs keep no session state, so the memory block
   is rebuilt each run. It still only changes when memory changes, so the cost is one cache
   miss after a save.\
   *If not:* store the sidebar snapshot server-side, keyed by thread id, with an expiry.
4. **The Decision check can only make a verdict stricter.** It can hold or refuse a save the
   rules passed, never pass one the rules held or refused. If it errors or has no answer, the
   rules' verdict stands (see "Suspect check").\
   *If not:* the Decision check can also pass a save the rules held, trusting the model over
   the rules.
5. **Held memories expire after 7 days** if nobody keeps them, and replaced memories are
   purged 7 days after being replaced. Both are cleared when the list is next read. No
   background job.\
   *If not:* they wait forever, or a scheduled job clears them.
6. **A chat that has ever been shared counts as shared**, read from the existing share rows
   (revoked rows are kept with `DateRevoked`). No new column on the conversation.\
   *If not:* a `MemorySpaceLockedAt` column on the conversation, set on first share.
7. **Personal context has its own two core tables**, shaped like a Workspace project's
   context ids and resources, not a hidden AI Context owned by the user.\
   *If not:* a user-owned `AIContext` with an owner column. It reuses versioning, but every
   context list, picker, Deploy export and permission check must then filter it out.
8. **The caller opts in with a runtime-context flag.** The personal context resolver does
   nothing unless the run sets `Umbraco.AI.IncludePersonalContext`, so core features that don't
   know about it (tests, Search, Automate) never pick it up.\
   *If not:* it loads for every run with a user, and each feature opts out instead.

</details>

## Decisions

- **The Agent package owns memory**, not Copilot Workspace or a new package. The Copilot
  sidebar gets memory too, and Workspace only adds its spaces through a hook.
- **A MAF `AIContextProvider`** (`AIMemoryContextProvider`), attached in `AIAgentFactory`
  beside the existing `ChatHistoryProvider`. It does nothing unless the agent has memory on,
  there's a signed-in backoffice user, and the run isn't unattended (see "Unattended runs").
- **The memory block goes in through the runtime context, not `AIContext.Instructions`.** The
  provider appends a text resource to `Umbraco.AI.AdditionalResources`, so the existing
  context injector puts it in the stable head after agent instructions and AI Contexts.
  `AIContext.Instructions` was rejected: MAF merges it into the agent instructions, and the
  system-message client's duplicate check breaks when that text changes (spike). Messages were
  rejected too: they're saved into history and sit mid-list.
- **The block is frozen per chat, with a fingerprint.** On Workspace the provider keeps the
  injected list in the MAF session state, which is saved with the conversation, and reuses it
  on every turn, so the cached prefix stays byte-identical. Beside the list it stores what the
  list was built for: space type and key, agent id, effective mode, the site switch, the
  personal policy and the user's personal switch. When any of them differs on a later turn,
  the provider rebuilds the list (one cache miss) instead of carrying memories into a space or
  mode that must not see them (SPEC R16a). On every turn it also drops ids that no longer exist
  or aren't active from the frozen list, so deleted text leaves the prompt and the stored
  session state at once.
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
  stays last. Workspace's composer gains `[ComposeAfter(typeof(UmbracoAIAgentComposer))]`, since
  `InsertBefore` throws when the Agent composer's `Append` hasn't run yet.
- **Each resolver also answers access** for the space types it owns: "can this user read or
  change this space". `UserAgentMemorySpaceResolver` owns `user` and `user-agent` and allows
  only the user whose key is the space key. `WorkspaceMemorySpaceResolver` owns `project` and
  `conversation`: it first checks the user has the Copilot Workspace section, then the
  project's owner, or for a chat the owner or an active Live share (Snapshot and revoked shares
  read nothing). The API asks the resolver whose `SpaceTypes` contains the requested type; no
  such resolver means 404.
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
- **Unattended runs get no memory and no personal context.** Automate runs actions as a
  workspace service account, so "is there a user" can't tell them apart from a person. A new
  `Constants.ContextKeys.UnattendedRun` (bool, core) marks the run instead. Automate's
  `RunAgentAction` sets it, and so do `AgentTestFeature` and `PromptTestFeature`, so a test run
  never picks up the runner's own memories or personal context. The memory provider and
  `PersonalContextResolver` both return nothing when it's set. A run with no user is treated the
  same. `ContextIdsOverride` wasn't reused as the marker: inline agents set it too
  (`AIAgentService`), and they may opt in to personal context.
- **Forget is soft.** `forget` sets status `Forgotten`, like a replaced row: never injected,
  listed or counted, deleted 7 days later by the same purge. `POST /{id}/restore` brings it
  back if the layer has room. So an injected "forget everything" can be undone from the notes.
- **Caps count active rows only.** Held, forgotten and replaced rows never fill a layer. Keep,
  restore and merge-undo each check the cap before a row turns active again.
- **Move uses the same rules as `remember`.** `AIMemoryService.MoveMemoryAsync` checks the target
  layer the way a save does (no lesson into "about you", no "about you" when switched off,
  space full), refuses held rows, and clears `ReplacedById` links on the rows the moved memory
  replaced, so Undo can't restore them into the wrong layer.
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
- **Public interfaces don't break.** `GetSettingValueAsync<T>` and
  `IAIConversationShareService.ConversationHasEverBeenSharedAsync` ship as default interface
  members that throw `NotSupportedException`, so a third-party implementation still compiles;
  the built-in services override them.
- **Leftover rows are harmless.** Reading settings keeps only registered keys, so a row left by
  an uninstalled add-on never reaches the client or Deploy. Saving touches only registered
  keys and leaves other rows alone, instead of rejecting the whole save.
- **Load-balanced sites see changes at once.** `AISettingsService` caches settings in the
  runtime cache. A new distributed cache refresher, `AISettingsCacheRefresher` (core), clears
  that cache on every server when settings save, including a Deploy import. So the kill
  switches and the personal policy reach every server from its next run.
- **Backoffice, panels:** a new manifest type, `uaiSettingsPanel`, adds a box to the existing
  Settings view, ordered by weight after the built-in boxes. The panel element reads and writes
  its keys through the settings workspace context, so the one Save button saves everything.
- **Backoffice, tabs:** a package can already add a whole tab with a `workspaceView` manifest
  on the settings workspace. It gets the same context, so its values save with the same button.
- **Deploy:** core's AI Settings artifact gains an `Additional` map carrying every registered
  add-on value, so an add-on's settings move with Deploy without a connector of its own. On
  import only keys the target has registered, with a value of the right type, are saved; the
  rest are skipped, so a target without the add-on still imports. A missing map keeps the
  target's values, the way a missing `DisclosureNoticeMode` does today. What gets carried: SPEC
  R36-R37.

Memory uses a panel: one switch doesn't earn a tab.

## Suspect check

Checks are pluggable: `IAIMemorySaveCheck` is an Umbraco ordered collection
(`AIMemorySaveCheckCollectionBuilder : OrderedCollectionBuilderBase`, reached with
`builder.AIMemorySaveChecks()`), so a package can `Append`, `InsertBefore` or `Remove` a check.
Each check gets the new text, the target space and this turn's outside content, and returns
saved, held or not saved with a reason, or no opinion.

**Outside content** is built once per save by the provider, from M.E.AI's
`FunctionInvokingChatClient.CurrentContext.Messages`, and covers only this turn: the messages
after the last user message. That list carries the whole history (Workspace loads it, the
sidebar resends it), so without the cut one old tool result would mark every later save. It
holds tool results, the open page or element (system message parts from the serialized entity
and element context keys), the run's additional resources, and attachments in this turn's
user message. Tests build it through a real `FunctionInvokingChatClient`, since
`CurrentContext`'s setter is protected. The strictest answer wins
(not saved, then held, then saved), and a not saved stops the rest from running.

Two checks ship, in this order:

1. **`AIMemoryRulesCheck`**, the fixed rules in the table below. Free, and always runs.
2. **`DecisionMemorySaveCheck`**, which asks the site's default Decision profile one pick-one
   question about the text: safe, an order to the agent, copied from content, a secret, or a
   personal detail about someone. A personal detail is held (`personal-in-shared`) only when
   it's going to a shared space, meaning the `space` layer in anything but "you + this agent";
   in "about you" or a private chat it's no opinion. It
   works like auto agent selection (`DecisionAgentSelector`): no opinion when the Decision
   feature is off, no default Decision profile is set, or the call fails (logged as a warning).
   A TypeSafe connection with Jev is one way to fill that profile. No new setting. Without a
   Decision profile, the memory rules text still tells the agent to ask before saving something
   personal to a shared space, but nothing enforces it.

The rules check:

| Check | Result |
|---|---|
| Looks like a secret: key or token shapes, "password", card or national ID numbers | Not saved |
| Echoes outside content (long shared phrases with it) but not the person's typed text | Held |
| Reads like an order to the agent ("always add", "ignore") or has a link, email or code | Held if this turn has outside content; otherwise saved |
| Going to a shared space and this turn has outside content | Held |
| None of the above | Saved |

A held memory is stored with status `Held` and is never injected. It turns active only when a
person clicks Keep.

## Personal context

A user's own AI Contexts and resources, picked in the "Your AI" box of the user profile. It
lives in core because both agents and prompts read it.

- **Loaded by a new context resolver**, `PersonalContextResolver`, inserted into core's
  `AIContextResolvers()` chain before `AdditionalResourcesContextResolver`. The Agent and Prompt
  packages switch their own resolvers from `Append` to
  `InsertBefore<PersonalContextResolver, ...>`, so the chain reads Profile, Content,
  KnowledgeSet, Agent or Prompt, Personal, AdditionalResources: the agent's own contexts stay
  ahead of personal context and the memory block, and a memory change never shifts them. Sites
  upgrading see one cache miss when the order changes. It resolves the
  picked contexts and the user's own resources through the same resolve, format and inject
  path as every other source, so `Always` resources go in the system prompt and `OnDemand`
  ones become the existing lookup tool. A context already resolved from the agent or prompt is
  loaded once (the chain already drops duplicate ids).
- **Gated by the caller.** The resolver reads `Constants.ContextKeys.IncludePersonalContext` (bool)
  from the runtime context and the signed-in backoffice user. Without both, or with
  `UnattendedRun` set, it returns nothing (guess 8). `AIInlineAgentBuilder` builds its config
  with `IncludePersonalContext = false` unless the builder turns it on, so code-built agents
  don't change behaviour on upgrade.
- **Agents opt out, prompts opt in.** `AIStandardAgentConfig` gains `IncludePersonalContext`
  (default true, and a missing value reads as true), carried by the config JSON like
  `MemoryMode`. `AIPrompt` gains a `IncludePersonalContext` column (default false), wired the way
  `IncludeEntityContext` is: entity, factory, migrations, version history adapter, API models,
  editor and the Prompt Deploy artifact. Each product's run sets the runtime flag from it.
- **Stored per user** in two core tables (see "Data model"), behind `IAIPersonalContextService`.
  The whole set is read and saved as one document, the way a project saves its contexts and
  resources.
- **Saved straight away, one at a time.** The profile panel has no Save button, so each add,
  edit or remove saves at once. The panel queues saves and sends the next `PUT` only after the
  last one returns, so two quick changes can't overwrite each other.
- **The "Your AI" profile app is core's**, with the context picker and the resource list in
  compact mode, and an extension slot for a new
  `uaiUserProfileProperty` manifest type. The Agent package adds the personal memory switch
  there, so it sits in the same box; the memory table stays the Agent package's own profile
  app, below it. `uai-resource-list` has a compact mode already; `uai-context-picker` doesn't,
  so it gains a `compact` property that uses the same smaller rows.
- **Moving memories into a resource is manual in v1.** The user adds a resource and deletes
  the memories. A "Turn into resource" bulk action can come later (DECISION-LOG).
- **Clean-up.** A user delete, or an admin erase (see "Erasing a user's AI data"), removes
  their personal context; a context delete removes that context from every user's list.
- **A site-wide kill switch.** Core's AI Settings gains a built-in `PersonalContextEnabled`
  setting (default true) in a "Personal Context" box. When it's off the resolver returns nothing
  and the "Your AI" box hides the Contexts and Resources rows; stored data is kept. It's a core
  field, not an add-on setting, because core owns personal context. It deploys with AI
  Settings, like the disclosure notice mode.
- **Not deployed.** Like memory, it's user data.

## Erasing a user's AI data

Umbraco refuses to delete a user who has ever logged in, so the user-delete handlers almost
never run. GDPR erasure needs its own action.

- **Core owns the action and the endpoint**, because personal context is core's and core has
  no idea what other packages store. `DELETE /users/{userKey}/ai-data` uses the CMS's own
  checks: `AuthorizationPolicies.SectionAccessUsers` on the controller, then
  `UserPermissionResource.WithKeys(userKey)` against `UserPermissionByResource`, as
  `DisableUserController` does.
- **Each package erases its own data on a notification.** `IAIUserDataEraseService.EraseUserDataAsync`
  publishes `AIUserDataErasingNotification` (not cancelable) with the user key. Core's
  personal-context handler and the Agent package's memory handler each delete their rows. The
  user-delete handlers call the same service methods, so both routes delete the same things.
  A package that adds user data later adds a handler, nothing else.
- **Backoffice:** an entity action on the `user` entity type (`UmbEntityActionBase`), opening
  `umbConfirmModal` with the danger colour. The dialog lists what will go from a new
  `uaiUserDataEraseItem` manifest type: core registers "personal contexts and resources", the
  Agent package registers ""About you" and agent memories". So the wording matches what's
  installed, with no list to keep in step.
- **Shared spaces keep their memories**, still credited to the user, since others rely on them.

## Versions

Ships on v17, v18 and v19 through the normal port workflow. The three migrations keep the same
ids on every line (`UmbracoAI_AddPersonalContext`, `UmbracoAIAgent_AddMemory`,
`UmbracoAIPrompt_AddIncludePersonalContext`), so a site moving up a line never runs one twice.

## What changes

**Server: core** (`Umbraco.AI/src/`)

```text
Umbraco.AI.Core/Settings/
  AISettingDefinition.cs                key, type, default
  IAISettingsProvider.cs                lists one package's definitions
  AISettingsProviderCollection.cs       + LazyCollectionBuilderBase builder, AISettingsProviders()
  AISettings.cs                         + Additional values map, + PersonalContextEnabled
  AISettingsService.cs                  + GetSettingValueAsync<T>, validates additional keys,
                                        reads registered keys only
  IAISettingsService.cs                 + GetSettingValueAsync<T> as a default interface member
  AISettingsCacheRefresher.cs           distributed refresher, fired on save and Deploy import
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
  UserDataErasingPersonalContextHandler.cs  same, on an admin erase
  ContextDeletedPersonalContextHandler.cs  drops a deleted context from every user's list
Umbraco.AI.Core/UserData/
  IAIUserDataEraseService.cs, AIUserDataEraseService.cs  publishes the notification below
  AIUserDataErasingNotification.cs      user key; each package deletes its own rows
Umbraco.AI.Core/Constants.cs            + ContextKeys.IncludePersonalContext, + ContextKeys.UnattendedRun
Umbraco.AI.Core/InlineAgents/           AIInlineAgentBuilder: IncludePersonalContext defaults to false
Umbraco.AI.Core/Configuration/          + PersonalContextResolver in the resolver chain
Umbraco.AI.Persistence/PersonalContext/
  AIPersonalContextEntity.cs, AIPersonalContextResourceEntity.cs, EFCoreAIPersonalContextRepository.cs
Umbraco.AI.Persistence.SqlServer/Migrations/  UmbracoAI_AddPersonalContext
Umbraco.AI.Persistence.Sqlite/Migrations/     UmbracoAI_AddPersonalContext
Umbraco.AI.Web/Api/Management/PersonalContext/
  Controllers/, Models/, Mapping/       GET and PUT /personal-context
Umbraco.AI.Web/Api/Management/UserData/
  EraseUserAIDataController.cs          DELETE /users/{userKey}/ai-data, Users section + user permission
Umbraco.AI.Deploy/
  Artifacts/AISettingsArtifact.cs       + PersonalContextEnabled, + Additional map
  Connectors/ServiceConnectors/UmbracoAISettingsServiceConnector.cs
                                        exports both; import skips unregistered or mistyped keys
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
../context/components/context-picker/context-picker.element.ts  + compact property
user-data/
  entity-actions/erase-user-ai-data.action.ts         user entity action, danger confirm
  extensions/uai-user-data-erase-item.extension.ts    the uaiUserDataEraseItem manifest type
exports.ts                                            repository and manifest types
```

**Server: Agent package** (`Umbraco.AI.Agent/src/`)

```text
Umbraco.AI.Agent.Core/
  Memory/
    AIMemory.cs                         domain model
    AIMemoryStatus.cs                   Active, Held, Replaced, Forgotten
    AIMemoryKind.cs                     Fact, Lesson
    AIMemorySpace.cs                    SpaceType, SpaceKey, AgentId?, DisplayName
    AIMemorySpaceTypes.cs               "user", "user-agent"
    IAIMemoryService.cs                 list, get, save, update, move, keep, forget, restore,
                                        delete, delete by space / user / agent
    AIMemoryService.cs                  caps on active rows, access checks, move rules, expiry
                                        and 7-day purge, notifications
    IAIMemoryRepository.cs              internal
    IAIMemorySpaceResolver.cs           the hook (public)
    AIMemorySpaceResolverCollection.cs  + OrderedCollectionBuilderBase builder, AIMemorySpaceResolvers()
    UserAgentMemorySpaceResolver.cs     built-in fallback, always last; access for user / user-agent
    AIMemoryContextProvider.cs          MAF provider: block + fingerprint, delta, tools,
                                        this turn's outside content
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
    UserDataErasingMemoryHandler.cs     same, on an admin erase
    AgentDeletedMemoryHandler.cs        deletes user-agent memories for that agent
  Agents/AIStandardAgentConfig.cs       + MemoryMode (Off, Work, WorkAndPerson), + IncludePersonalContext
  Chat/                                 run sets ContextKeys.IncludePersonalContext from the config
  Tests/AgentTestFeature.cs             sets ContextKeys.UnattendedRun
  Configuration/UmbracoBuilderExtensions.cs  AgentContextResolver: InsertBefore<PersonalContextResolver>
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
Umbraco.AI.Prompt.Core/Tests/PromptTestFeature.cs  sets ContextKeys.UnattendedRun
Umbraco.AI.Prompt.Core/Configuration/   PromptContextResolver: InsertBefore<PersonalContextResolver>
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
  WorkspaceMemorySpaceResolver.cs       project > ever-shared chat; access: Workspace section, then
                                        project owner, chat owner or active Live share
  WorkspaceMemorySpaceTypes.cs          "project", "conversation"
  ProjectDeletedMemoryHandler.cs        deletes the project's memories
  ConversationDeletedMemoryHandler.cs   deletes the chat's memories
Umbraco.AI.Agent.Conversations.Core/Conversations/
  IAIConversationShareService.cs        + ConversationHasEverBeenSharedAsync (default interface member)
Umbraco.AI.Agent.Copilot.Workspace.Startup/  InsertBefore<UserAgentMemorySpaceResolver, ...>,
                                             [ComposeAfter(typeof(UmbracoAIAgentComposer))]
```

**Server: Automate** (`Umbraco.AI.Automate/src/Umbraco.AI.Automate/`)

```text
Actions/RunAgentAction.cs               sets ContextKeys.UnattendedRun on the run
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
  user-data/manifests.ts                   uaiUserDataEraseItem: "About you" and agent memories
  exports.ts                               list + repository for Workspace to reuse
```

**Backoffice: Agent.UI** (`Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client/src/`)

```text
chat/tools/memory/
  remember-tool.element.ts   saved / held / not-saved note, Move, Undo, Keep, Discard
  forget-tool.element.ts     "Forgot …" note with Undo (restore)
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
  belong to them; an admin erase removes a user's own layers and personal context.
- **Prompt cache:** the memory block is frozen per Workspace chat and rebuilt only when its
  fingerprint changes; changes ride in the volatile tail.

- **Cross-product floor:** the Agent and Prompt packages need the core version that ships the
  settings extension and personal context, so their core range floors move up with this
  release. Prompt.Deploy needs the Prompt version with the new field, and Umbraco.AI.Deploy
  the core version with the new settings fields. Automate needs the core version with
  `UnattendedRun`, and Workspace the Agent version with the resolver hook.

Deploy: the prompt's `IncludePersonalContext` is added to the Prompt Deploy artifact and
connector. Core's AI Settings artifact carries `PersonalContextEnabled` and the add-on settings
map, which takes the site memory switch and personal memory policy with it (see "Settings
extension"). The agent's `MemoryMode` and `IncludePersonalContext` already travel in the agent
config JSON, so Agent.Deploy only gains specs. Not touched: memories and personal context,
which are user data and never deployed;
search index (memory isn't searchable content), a cache refresher for memories or personal
context (read from the database once per run, small and per user; settings do get one, see
"Settings extension"), audit log (tool calls are already logged by the run).

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
  Status            int             no    0 Active, 1 Held, 2 Replaced, 3 Forgotten
  HoldReason        nvarchar(64)    yes   "echoes-content", "instruction-like", "shared-after-content",
                                          "personal-in-shared"
  ReplacedById      Guid            yes   the memory that merged or updated this one
  ExpiresAt         datetime        yes   stops loading after this
  LastConfirmedAt   datetime        no    last save, merge, edit or keep; for fading later
  CreatedByUserKey  Guid            no    who saved it
  DateCreated       datetime        no
  DateModified      datetime        no
  Index: (SpaceType, SpaceKey, AgentId, Status)
  Index: (CreatedByUserKey)
```

- The 7 days for held, replaced and forgotten rows run from `DateModified`, set when the
  status changes.
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
