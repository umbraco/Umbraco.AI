# Plan: Agent memory

**42 tasks in 9 areas**, in build order. Core settings and core personal context come first
because the Agent and Prompt packages build on them; Workspace comes last because it plugs into
the Agent package's space hook. Each task names its stories (see STORIES.md) and the specs that must go green.

> [!IMPORTANT]
> **2 guesses need your OK.**
>
> 1. **Undo is `DELETE /{id}`.** Deleting a memory that replaced others within the last 7 days
>    makes them active again (R22), from the chat note and from the lists alike. No separate
>    undo route.\
>    *If not:* a `POST /{id}/undo` route, and list deletes don't restore anything.
> 2. **Discard on a held note is `DELETE /{id}` too.** A held memory never replaced anything,
>    so nothing comes back.\
>    *If not:* a `POST /{id}/discard` route that keeps a record of the discard.

## Core: settings extension

- [ ] **T1** - story: MEM-01 (AC1-AC4, AC7, AC8). Server side of the settings extension in
  `Umbraco.AI/src`: `AISettingDefinition` (key, type, default), `IAISettingsProvider`,
  `AISettingsProviderCollection` + `LazyCollectionBuilderBase` builder + `AISettingsProviders()`
  extension (mirror `AIGuardrailEvaluators()`), `AISettings` gains an additional-values map,
  `IAISettingsService.GetSettingValueAsync<T>(key)` with validation of unknown keys and types,
  `EFCoreAISettingsRepository` reads and writes additional keys as `AISettingsEntity` rows,
  settings request/response models gain `additional`. Follows ARCHITECTURE "Settings extension".
  Acceptance: MEM-01 AC1-AC4, AC7, AC8 specs green; `dotnet build Umbraco.AI/Umbraco.AI.slnx`.
  depends-on: none. parallel-group: A

- [ ] **T2** - story: MEM-01 (AC5, AC6). Backoffice side in
  `Umbraco.AI.Web.StaticAssets/Client/src/settings/`: the `uaiSettingsPanel` manifest type
  (`extensions/uai-settings-panel.extension.ts`), get/set for additional values on
  `settings-workspace.context.ts`, an extension slot after the built-in boxes in
  `settings-editor.element.ts`, both exported from `exports.ts`. Regenerate the core OpenAPI
  client for `additional`.
  Acceptance: MEM-01 AC5, AC6 specs green; `npm run build:core`.
  depends-on: T1

- [ ] **T3** - **wire: settings extension into the AI Settings screen.** story: MEM-01 (AC2,
  AC6). Register a throwaway test panel and setting on the demo site, change it in the browser
  and save.
  Acceptance: a real `PUT` then `GET` on the demo site round-trips the value; the panel shows
  after "Transparency". Remove the test panel before commit.
  depends-on: T2

## Core: personal context

- [ ] **T30** - story: MEM-18 (AC1, AC3-AC6, AC9, AC10). Domain and persistence in
  `Umbraco.AI/src`: `AIPersonalContext` (UserKey, ContextIds, Resources as `AIContextResource`),
  `IAIPersonalContextService` / `AIPersonalContextService` (get and save the signed-in user's
  set; refuse context ids the user can't list and unknown resource types), internal
  `IAIPersonalContextRepository`, `AIPersonalContextEntity`, `AIPersonalContextResourceEntity`
  (columns as ARCHITECTURE "Data model", shaped like the project tables),
  `EFCoreAIPersonalContextRepository`, migration `UmbracoAI_AddPersonalContext` for SQL Server
  and SQLite. Follows ARCHITECTURE "Personal context".
  Acceptance: MEM-18 AC1, AC3-AC6, AC9, AC10 service specs green; `dotnet build
  Umbraco.AI/Umbraco.AI.slnx`.
  depends-on: none. parallel-group: A

- [ ] **T31** - story: MEM-19 (AC1-AC5, AC10, AC11). `PersonalContextResolver :
  IAIContextResolver`, inserted before `AdditionalResourcesContextResolver` in core's
  `AIContextResolvers()` chain, and `Constants.ContextKeys.IncludePersonalContext`. Returns nothing
  without the flag or a backoffice user; resolves picked contexts and the user's resources
  through the normal resolve, format and inject path.
  Acceptance: MEM-19 AC1-AC5, AC10, AC11 resolver specs green.
  depends-on: T30

- [ ] **T32** - story: MEM-21 (all). `UserDeletedPersonalContextHandler` and
  `ContextDeletedPersonalContextHandler`, plus a spec in `Umbraco.AI.Deploy`'s tests that a
  context export carries no personal resources.
  Acceptance: MEM-21 specs green.
  depends-on: T30

- [ ] **T33** - story: MEM-18 (AC1, AC3-AC6, AC9, AC10). Management API in
  `Umbraco.AI.Web/Api/Management/PersonalContext/`: `GET` and `PUT /personal-context`, request
  and response models reusing the project's resource model shape, mapping. Signed-in user only.
  Acceptance: MEM-18 API specs green.
  depends-on: T30

- [ ] **T34** - **wire: personal context API into the Management API.** story: MEM-18 (AC1,
  AC6). Register the controllers; regenerate the core OpenAPI client.
  Acceptance: a real `PUT` then `GET /personal-context` on the demo site round-trips a context
  id and a Text resource; a second user's `GET` is empty.
  depends-on: T33

- [ ] **T35** - story: MEM-18 (AC2, AC7, AC8, AC11-AC13). Backoffice in
  `Umbraco.AI.Web.StaticAssets/Client/src/personal-context/`: the "Your AI" user profile app
  with `uai-context-picker` and `uai-resource-list` in compact mode, saving each change at once
  (error toast and roll back on failure), the `uaiUserProfileProperty` manifest type and its
  extension slot after Resources, the repository, all exported from `exports.ts`. Matches
  `mockups/my-memory.png`.
  Acceptance: MEM-18 AC2, AC7, AC11-AC13 specs green; `npm run build:core`; opens from the
  avatar on the demo site.
  depends-on: T34

## Agent: model and data

- [ ] **T4** - story: MEM-04 (AC1), MEM-16 (AC6, AC7). Domain and persistence: `AIMemory`,
  `AIMemoryStatus`, `AIMemoryKind`, `AIMemorySpace`, `AIMemorySpaceTypes`, internal
  `IAIMemoryRepository`, `AIMemoryEntity`, `EFCoreAIMemoryRepository`, `Memories` on
  `UmbracoAIAgentDbContext`, migration `UmbracoAIAgent_AddMemory` for SQL Server and SQLite with
  the table and two indexes in ARCHITECTURE "Data model". Follows `ef-core-data`.
  Acceptance: repository specs green against SQLite and SQL Server; the migration applies on a
  fresh demo site.
  depends-on: none. parallel-group: A

- [ ] **T5** - story: MEM-03 (AC1, AC2). `MemoryMode` (`Off`, `Work`, `WorkAndPerson`, default
  `Off`) on `AIStandardAgentConfig`, `memoryMode` on `AgentConfigModel`, mapped in
  `AgentMapDefinition`.
  Acceptance: MEM-03 AC1, AC2 specs green.
  depends-on: none. parallel-group: A

- [ ] **T6** - story: MEM-04 (AC9, AC10), MEM-06 (AC1-AC4), MEM-17 (AC1, AC2). The check hook
  and the rules: public `IAIMemorySaveCheck`, `AIMemorySaveCheckCollection` +
  `OrderedCollectionBuilderBase` builder + `AIMemorySaveChecks()` (mirror `AIAgentSelectors()`) (strictest answer wins; not saved stops the rest), and
  `AIMemoryRulesCheck` first: secret shapes → not saved; echoes a tool result → held
  `echoes-content`; instruction-like or link/email/code after tool output → held
  `instruction-like`; shared space after tool output → held `shared-after-content`. Follows
  ARCHITECTURE "Suspect check".
  Acceptance: MEM-04 AC9, AC10, MEM-06 AC1-AC4 and MEM-17 AC1, AC2 specs green.
  depends-on: T4. parallel-group: B

- [ ] **T29** - story: MEM-17 (AC3-AC10). `DecisionMemorySaveCheck`, second in the collection:
  one `AIChoiceDecisionQuestion` (safe, an order to the agent, copied from content, a secret)
  through `IAIDecisionService` on the default Decision profile, mapped to held
  `instruction-like`, held `echoes-content` or not saved; no opinion when the Decision feature
  is off, no default profile is set, or the call throws (warning logged). Mirrors
  `DecisionAgentSelector`.
  Acceptance: MEM-17 AC3-AC10 specs green; on the demo site with a TypeSafe (Jev) default
  Decision profile, an order-like save the rules pass comes back held.
  depends-on: T6

- [ ] **T7** - story: MEM-09 (AC1, AC2). The space hook: public `IAIMemorySpaceResolver`
  (claim a run; can read; can write), `AIMemorySpaceResolverCollection` + `OrderedCollectionBuilderBase` builder +
  `AIMemorySpaceResolvers()` (mirror `AIAgentSelectors()`), built-in
  `UserAgentMemorySpaceResolver` always last.
  Acceptance: MEM-09 AC1, AC2 specs green.
  depends-on: T4. parallel-group: B

- [ ] **T8** - story: MEM-02 (AC3-AC6), MEM-12 (AC5, AC7-AC9), MEM-03 (AC3, AC5, AC6).
  Effective memory: `AIMemorySettings` registers `Agent.Memory.Enabled` (default true) and
  `Agent.Memory.PersonalMemory` (default `Allowed`) through an `IAISettingsProvider` added with `AISettingsProviders()` (T1); `AIPersonalMemoryPolicy`;
  `AIMemoryUserPreference` stores the personal switch with `IUserDataService` only once flipped.
  One method works out, for a run, which layers may be read and saved (R1-R1f).
  Acceptance: the listed specs green.
  depends-on: T1, T5. parallel-group: B

- [ ] **T9** - story: MEM-04 (AC12), MEM-05 (AC2-AC9), MEM-06 (AC5-AC9), MEM-09 (AC3,
  AC4), MEM-10 (all), MEM-16 (AC6, AC7). `IAIMemoryService`/`AIMemoryService`: list, save,
  update, move, keep, delete; caps (30 "about you", 50 per space); 300-character limit; expired,
  held-after-7-days and replaced-after-7-days rows cleared on read or when room is needed;
  `replaces` checks; delete restores replaced memories within 7 days (plan guess 1); access
  through the owning resolver; the four notifications.
  Acceptance: the listed specs green.
  depends-on: T6, T7, T8

## Agent: in the run

- [ ] **T10** - story: MEM-04 (AC5, AC6, AC9-AC12), MEM-05 (AC1, AC5-AC9), MEM-07 (all).
  `AIMemoryTools`: `remember` and `forget` as plain functions with the argument and result
  shapes in SPEC "Agent tools"; short ids through internal `AIShortIdMap` (first 8 characters,
  lengthened only where two would clash, ambiguous ids refused; ARCHITECTURE "Short ids") mapped
  back within the run's visible memories; reads run messages from `FunctionInvokingChatClient.CurrentContext` for the
  suspect check.
  Acceptance: the listed specs green.
  depends-on: T9, T29

- [ ] **T11** - story: MEM-03 (AC3-AC7), MEM-04 (AC1-AC4, AC7, AC8), MEM-08 (AC1-AC5, AC7),
  MEM-02 (AC3). `AIMemoryContextProvider` and `AIMemoryPrompt`: resolve the space, load both
  layers, append the block as a text resource to `Umbraco.AI.AdditionalResources`, freeze the
  injected list in the `AgentSession` state, append the "changed since this chat started" note
  to `PendingSystemMessage`, offer the tools through `AIContext.Tools`; nothing when memory is
  off for the run or there's no backoffice user.
  Acceptance: the listed specs green.
  depends-on: T10

- [ ] **T12** - **wire: memory provider into agent runs.** story: MEM-04 (AC1, AC2), MEM-08
  (AC6). Attach the provider in `AIAgentFactory`; register the service, repository, resolver
  collection, settings and tools in `Umbraco.AI.Agent.Startup`.
  Acceptance: on the demo site with Anthropic, an agent set to `WorkAndPerson` saves a fact in
  the Copilot sidebar and recalls it in a new chat; the run log shows the block after the AI
  Context.
  depends-on: T11

- [ ] **T13** - story: MEM-16 (AC1-AC3). `UserDeletedMemoryHandler` (own layers only) and
  `AgentDeletedMemoryHandler`, registered in Startup.
  Acceptance: MEM-16 AC1-AC3 specs green; deleting a test user on the demo site removes their
  rows.
  depends-on: T12

## Agent: Management API

- [ ] **T14** - story: MEM-12 (AC1, AC3, AC4), MEM-13 (AC1, AC2, AC6), MEM-15 (AC6, AC7), MEM-09
  (AC3, AC4). Memory controllers, models and mapping under
  `Umbraco.AI.Agent.Web/Api/Management/Memory/`: `GET /mine`, `PUT /mine/preferences`,
  `GET ?spaceType=&spaceKey=`, `PUT /{id}`, `POST /{id}/move`, `POST /{id}/keep`,
  `DELETE /{id}`, with the status codes in SPEC "Endpoints".
  Acceptance: the listed specs green.
  depends-on: T9

- [ ] **T15** - **wire: memory API into the Management API.** story: MEM-12 (AC1). Register the
  controllers, regenerate the Agent OpenAPI client (`npm run generate-client`).
  Acceptance: real `GET /mine` and `DELETE /{id}` calls on the demo site return the documented
  codes.
  depends-on: T12, T14

## Agent: screens

- [ ] **T16** - story: MEM-03 (AC1, AC2), MEM-02 (AC8). Memory dropdown (Off, Work only, Work
  and person) in "Agent Behavior" on `agent-details-workspace-view.element.ts`, with the
  site-off warning tag. Matches `mockups/agent-memory-setting.png`.
  Acceptance: specs green; `npm run build:agent`; the setting saves on the demo site.
  depends-on: T5, T15. parallel-group: C

- [ ] **T17** - story: MEM-02 (AC1, AC2, AC7). `memory-settings-panel.element.ts` as a
  `uaiSettingsPanel`: "Agent memory" toggle and "Personal memory" dropdown, disabled while the
  toggle is off. Matches `mockups/settings-memory-panel.png`.
  Acceptance: specs green; the box shows and saves on the demo site.
  depends-on: T3, T8, T15. parallel-group: C

- [ ] **T18** - story: MEM-13 (all). Memory repository, shared `memory-list.element.ts` (Memory,
  Kind, Status, Until, Saved by, Saved, actions; group header rows on light grey), centred
  `edit-memory-modal` (300-character counter, "Forget after" date), and `memory/exports.ts` for
  Workspace. Matches `mockups/edit-memory.png`.
  Acceptance: MEM-13 specs green; `npm run build:agent`.
  depends-on: T15. parallel-group: C

- [ ] **T19** - story: MEM-12 (all), MEM-18 (AC8). The personal switch as a
  `uaiUserProfileProperty` in core's "Your AI" box (hidden with a note under `NotAllowed`), and
  the memory table as the Agent package's own profile app below it, grouped by "About you" and
  each agent, "paused" and "memory is off for this agent" on group headers, empty state.
  Matches `mockups/my-memory.png` and `my-memory-paused.png`.
  Acceptance: MEM-12 and MEM-18 AC8 specs green; opens from the avatar on the demo site.
  depends-on: T18, T35

- [ ] **T20** - story: MEM-11 (all). `remember-tool.element.ts` and `forget-tool.element.ts`
  plus `uaiAgentToolRenderer` manifests in Agent.UI `chat/tools/memory/`: saved, learned,
  updated/merged, held, not saved, forgot; Move, Undo, Keep, Discard calling the memory API;
  errors in place; reopened notes show the memory's current state. Matches
  `mockups/chat-memory-notes.png` and `-moved.png`.
  Acceptance: MEM-11 specs green; `npm run build:agent-ui`; notes render in the sidebar and in
  Workspace on the demo site.
  depends-on: T15. parallel-group: C

## Personal context in agents and prompts

- [ ] **T36** - story: MEM-20 (AC1-AC3), MEM-19 (AC7, AC8). `IncludePersonalContext` on
  `AIStandardAgentConfig` (default true; a missing value reads true), `includePersonalContext` on
  the agent config API model and mapping, and the agent run setting
  `ContextKeys.IncludePersonalContext` from it. Same files as T5's `MemoryMode`.
  Acceptance: MEM-20 AC1-AC3 and MEM-19 AC7, AC8 specs green.
  depends-on: T5, T31

- [ ] **T37** - story: MEM-20 (AC4). "Include Personal Context" toggle after the Memory dropdown in
  "Agent Behavior", `agent-details-workspace-view.element.ts`. Matches
  `mockups/agent-memory-setting.png`.
  Acceptance: MEM-20 AC4 spec green; `npm run build:agent`.
  depends-on: T16, T36

- [ ] **T38** - **wire: personal context into agent runs.** story: MEM-19 (AC1, AC8). On the
  demo site, give a user an Always resource and chat in the Copilot sidebar.
  Acceptance: the logged system prompt holds the resource; after turning the agent's toggle
  off, the next run's doesn't.
  depends-on: T35, T37

- [ ] **T39** - story: MEM-20 (AC5-AC7, AC9), MEM-19 (AC6, AC9). `IncludePersonalContext` on
  `AIPrompt` (default false), wired like `IncludeEntityContext`: `AIPromptEntity`, factory,
  DbContext, migration `UmbracoAIPrompt_AddIncludePersonalContext` (SQL Server and SQLite, default
  0), `AIPromptVersionableEntityAdapter`, create, update and response models and mapping, and
  the prompt run setting `ContextKeys.IncludePersonalContext` from it.
  Acceptance: MEM-20 AC5-AC7, AC9 and MEM-19 AC6, AC9 specs green; `dotnet build
  Umbraco.AI.Prompt/Umbraco.AI.Prompt.slnx`.
  depends-on: T31. parallel-group: C

- [ ] **T40** - story: MEM-20 (AC8). "Include Personal Context" toggle after "Include Entity
  Context" in `prompt-details-workspace-view.element.ts`; regenerate the Prompt OpenAPI client.
  Matches `mockups/prompt-personal-context.png`.
  Acceptance: MEM-20 AC8 spec green; `npm run build:prompt`.
  depends-on: T39

- [ ] **T41** - story: MEM-20 (AC10, AC11). `AIPromptArtifact` and
  `UmbracoAIPromptServiceConnector` carry `IncludePersonalContext`, with connector tests alongside
  the `IncludeEntityContext` ones.
  Acceptance: MEM-20 AC10, AC11 specs green; `dotnet build
  Umbraco.AI.Prompt.Deploy/Umbraco.AI.Prompt.Deploy.slnx`.
  depends-on: T39

- [ ] **T42** - **wire: personal context into prompt runs.** story: MEM-19 (AC6, AC9). On the
  demo site, run a prompt with the toggle on, then off, for a user with an Always resource.
  Acceptance: the logged system prompt holds the resource only while the toggle is on.
  depends-on: T35, T40

## Copilot Workspace

- [ ] **T21** - story: MEM-15 (AC1, AC2). `IAIConversationShareService.ConversationHasEverBeenSharedAsync`,
  read from share rows including revoked ones.
  Acceptance: MEM-15 AC1, AC2 service specs green.
  depends-on: none. parallel-group: A

- [ ] **T22** - story: MEM-14 (AC1, AC2, AC8, AC9), MEM-15 (AC1-AC3, AC6, AC7).
  `WorkspaceMemorySpaceResolver` (project, then ever-shared chat; owner writes, recipients read
  shared chats, recipients outside a project never list it) and `WorkspaceMemorySpaceTypes`.
  Acceptance: the listed specs green.
  depends-on: T7, T21

- [ ] **T23** - **wire: Workspace spaces into memory.** story: MEM-14 (AC1), MEM-15 (AC1).
  Register the resolver in `Copilot.Workspace.Startup` with
  `AIMemorySpaceResolvers().InsertBefore<UserAgentMemorySpaceResolver, WorkspaceMemorySpaceResolver>()`; make sure the project and
  conversation ids reach the provider through the runtime context.
  Acceptance: on the demo site, a fact saved in a project chat loads in a second project chat
  and not in a private chat.
  depends-on: T12, T22

- [ ] **T24** - story: MEM-16 (AC4, AC5). `ProjectDeletedMemoryHandler` and
  `ConversationDeletedMemoryHandler`, registered in Startup.
  Acceptance: MEM-16 AC4, AC5 specs green.
  depends-on: T23

- [ ] **T25** - story: MEM-14 (AC3-AC7). Project "Memory" workspace view
  (`project-memory-workspace-view.element.ts`, weight 150) reusing the shared list, held rows
  first, tab badge counting held only, empty state. Matches `mockups/project-memory.png` and
  `-empty.png`.
  Acceptance: MEM-14 AC3-AC7 specs green; the tab shows on the demo site.
  depends-on: T18, T23. parallel-group: D

- [ ] **T26** - story: MEM-15 (AC4, AC5, AC8), MEM-14 (AC10). Memory section after "Shared
  with" in `conversation-context-panel.element.ts` with the info tooltip and read-only rows for
  recipients; the share picker warning mentions memory. Matches
  `mockups/conversation-memory.png` and `-recipient.png`.
  Acceptance: the listed specs green; both show on the demo site.
  depends-on: T18, T23. parallel-group: D

## Shipping

- [ ] **T27** - story: MEM-01 (all). Cross-product floors: the Agent and Prompt packages'
  `Umbraco.AI.Core` range floors move to the core version with the settings extension and
  personal context, Workspace's Agent floor moves to the version with the space hook, and
  Prompt.Deploy's Prompt floor moves to the version with `IncludePersonalContext`. Test with
  `UseProjectReferences=false`.
  Acceptance: each product's slnx builds with ranges.
  depends-on: T24, T25, T26, T38, T41, T42

- [ ] **T28** - story: MEM-04 (AC1). Full smoke test on the demo site with Anthropic: every
  screen, the five moments (first run, mistake, wait, finish, return), Workspace prompt cache
  hits across turns, and personal context in an agent chat and a prompt.
  Acceptance: smoke-tester PASS with screenshots.
  depends-on: T27
