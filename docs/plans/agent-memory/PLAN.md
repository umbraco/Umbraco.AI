# Plan: Agent memory

**59 tasks in 14 areas**, in build order. Core settings and core personal context come first
because the Agent and Prompt packages build on them; Workspace comes last because it plugs into
the Agent package's space hook. Each task names its stories (see STORIES.md) and the specs that must go green.

<details>
<summary>2 guesses, agreed 26-11-2026</summary>

1. **Undo is `DELETE /{id}`.** Deleting a memory that replaced others within the last 7 days
   makes them active again (R22), from the chat note and from the lists alike. No separate
   undo route.\
   *If not:* a `POST /{id}/undo` route, and list deletes don't restore anything.
2. **Discard on a held note is `DELETE /{id}` too.** A held memory never replaced anything,
   so nothing comes back.\
   *If not:* a `POST /{id}/discard` route that keeps a record of the discard.

</details>

Since the plan interrogation, Undo on a "Forgot" note is `POST /{id}/restore`, since a forgotten
memory still exists; every other Undo is still `DELETE /{id}` as guess 1 says.

## Core: settings extension

- [ ] **T1** - story: MEM-01 (AC1-AC4, AC7-AC10, AC12). Server side of the settings extension in
  `Umbraco.AI/src`: `AISettingDefinition` (key, type, default), `IAISettingsProvider`,
  `AISettingsProviderCollection` + `LazyCollectionBuilderBase` builder + `AISettingsProviders()`
  extension (mirror `AIGuardrailEvaluators()`), `AISettings` gains an additional-values map,
  `IAISettingsService.GetSettingValueAsync<T>(key)` as a default interface member that throws
  `NotSupportedException` (overridden in `AISettingsService`), with validation of unknown keys and
  types on save. Reads keep registered keys only; saves touch registered keys only and leave other
  rows alone. `EFCoreAISettingsRepository` reads and writes additional keys as `AISettingsEntity`
  rows, settings request/response models gain `additional`. Follows ARCHITECTURE "Settings
  extension".
  Acceptance: MEM-01 AC1-AC4, AC7-AC10, AC12 specs green; `dotnet build Umbraco.AI/Umbraco.AI.slnx`.
  depends-on: none. parallel-group: A

- [ ] **T2** - story: MEM-01 (AC5, AC6). Backoffice side in
  `Umbraco.AI.Web.StaticAssets/Client/src/settings/`: the `uaiSettingsPanel` manifest type
  (`extensions/uai-settings-panel.extension.ts`), get/set for additional values on
  `settings-workspace.context.ts`, an extension slot after the built-in boxes in
  `settings-editor.element.ts`, both exported from `settings/exports.ts` and re-exported from the
  root `exports.ts` so the api-extractor rollup carries them. Regenerate the core OpenAPI client
  for `additional`.
  Acceptance: MEM-01 AC5, AC6 specs green; `npm run build:core`.
  depends-on: T1

- [ ] **T3** - **wire: settings extension into the AI Settings screen.** story: MEM-01 (AC2,
  AC6). Register a throwaway test panel and setting on the demo site, change it in the browser
  and save.
  Acceptance: a real `PUT` then `GET` on the demo site round-trips the value; the panel shows
  after "Transparency". Remove the test panel before commit.
  depends-on: T2

- [ ] **T49** - story: MEM-01 (AC11), MEM-22 (AC13). `AISettingsCacheRefresher` in
  `Umbraco.AI.Core/Settings/`: a distributed cache refresher (`CacheRefresherBase` with its own
  `Guid`), sent by `AISettingsService` after every save, clearing the `Umbraco.AI.Settings`
  runtime cache entry on each server. Deploy import saves through the service, so it fires too.
  Follows ARCHITECTURE "Settings extension".
  Acceptance: MEM-01 AC11 spec green as an integration test with cache refreshers switched on
  (`umbraco-dotnet-testing`); on the demo site, saving settings logs the refresher.
  depends-on: T1

## Core: personal context

- [ ] **T30** - story: MEM-18 (AC1, AC3-AC6, AC9, AC10). Domain and persistence in
  `Umbraco.AI/src`: `AIPersonalContext` (UserKey, ContextIds, Resources as `AIContextResource`),
  `IAIPersonalContextService` / `AIPersonalContextService` (get and save the signed-in user's
  set; refuse context ids the user can't list and unknown resource types), internal
  `IAIPersonalContextRepository`, `AIPersonalContextEntity`, `AIPersonalContextResourceEntity`
  (columns as ARCHITECTURE "Data model", shaped like the project tables),
  `EFCoreAIPersonalContextRepository`, migration `UmbracoAI_AddPersonalContext` for SQL Server
  and SQLite. Follows ARCHITECTURE "Personal context". Repository specs run on the SQLite
  `EFCoreTestFixture`.
  Acceptance: MEM-18 AC1, AC3-AC6, AC9, AC10 service specs green; `dotnet build
  Umbraco.AI/Umbraco.AI.slnx`.
  depends-on: T1 (same core registration file)

- [ ] **T31** - story: MEM-19 (AC1-AC5, AC10, AC11, AC14). `PersonalContextResolver :
  IAIContextResolver`, inserted before `AdditionalResourcesContextResolver` in core's
  `AIContextResolvers()` chain, plus `Constants.ContextKeys.IncludePersonalContext` and
  `Constants.ContextKeys.UnattendedRun`. Returns nothing without the flag or a backoffice user, or
  with `UnattendedRun` set; resolves picked contexts and the user's resources through the normal
  resolve, format and inject path. AC5's spec uses a profile context (core tests can't reference
  `AgentContextResolver`).
  Acceptance: MEM-19 AC1-AC5, AC10, AC11, AC14 resolver specs green.
  depends-on: T30

- [ ] **T32** - story: MEM-21 (all). `UserDeletedPersonalContextHandler` and
  `ContextDeletedPersonalContextHandler`, plus a spec in `Umbraco.AI.Deploy`'s tests that a
  context export carries no personal resources.
  Acceptance: MEM-21 specs green; on the demo site, deleting a never-logged-in test user with a
  personal context removes its rows, and deleting a context drops it from that user's list.
  depends-on: T30

- [ ] **T43** - story: MEM-18 (AC15, AC16), MEM-19 (AC12, AC13). The site kill switch:
  `PersonalContextEnabled` on `AISettings` (default true, a built-in setting row), on the
  settings request and response models, and read by `PersonalContextResolver` (off returns
  nothing). Backoffice: a "Personal Context" box after "Transparency" in
  `settings-editor.element.ts` with one toggle. Matches `mockups/settings-memory-panel.png`.
  Same settings files as T1 and T2. Regenerate the core OpenAPI client.
  Acceptance: MEM-18 AC15, AC16 and MEM-19 AC12, AC13 specs green; `npm run build:core`; the box
  shows after "Transparency" on the demo site and its value survives a reload.
  depends-on: T2, T31

- [ ] **T33** - story: MEM-18 (AC1, AC3-AC6, AC9, AC10). Management API in
  `Umbraco.AI.Web/Api/Management/PersonalContext/`: `GET` and `PUT /personal-context`, request
  and response models reusing the project's resource model shape, mapping. Signed-in user only.
  The `GET` response also carries `personalContextEnabled`.
  Acceptance: MEM-18 API specs green.
  depends-on: T30, T43

- [ ] **T34** - **wire: personal context API into the Management API.** story: MEM-18 (AC1,
  AC6). Register the controllers; regenerate the core OpenAPI client.
  Acceptance: a real `PUT` then `GET /personal-context` on the demo site round-trips a context
  id and a Text resource; a second user's `GET` is empty.
  depends-on: T33

- [ ] **T35** - story: MEM-18 (AC2, AC7, AC8, AC11-AC14). Backoffice in
  `Umbraco.AI.Web.StaticAssets/Client/src/personal-context/`: the "Your AI" user profile app
  with `uai-context-picker` and `uai-resource-list` in compact mode, saving each change at once
  through a queue that sends one `PUT` at a time (error toast and roll back on failure), the
  `uaiUserProfileProperty` manifest type and its extension slot after Resources, the repository,
  exported from `personal-context/exports.ts` and the root `exports.ts`. `uai-context-picker`
  (`context/components/context-picker/`) gains a `compact` property using the resource list's
  smaller rows. With the site switch off, the two rows hide behind the admin note. Matches
  `mockups/my-memory.png`.
  Acceptance: MEM-18 AC2, AC7, AC11-AC14, AC17 specs green; `npm run build:core`; on the demo
  site, opens from the avatar, adding two contexts quickly keeps both after a reload, and with
  the site switch off the rows are replaced by the note.
  depends-on: T34, T43

## Agent: model and data

- [ ] **T4** - story: MEM-04 (AC1), MEM-16 (AC6, AC7). Domain and persistence: `AIMemory`,
  `AIMemoryStatus` (Active, Held, Replaced, Forgotten), `AIMemoryKind`, `AIMemorySpace`, `AIMemorySpaceTypes`, internal
  `IAIMemoryRepository`, `AIMemoryEntity`, `EFCoreAIMemoryRepository`, `Memories` on
  `UmbracoAIAgentDbContext`, migration `UmbracoAIAgent_AddMemory` for SQL Server and SQLite with
  the table and two indexes in ARCHITECTURE "Data model". Follows `ef-core-data`.
  Acceptance: repository specs green on the SQLite `EFCoreTestFixture`; the migration applies on
  a fresh demo site.
  depends-on: none. parallel-group: A

- [ ] **T5** - story: MEM-03 (AC1, AC2). `MemoryMode` (`Off`, `Work`, `WorkAndPerson`, default
  `Off`) on `AIStandardAgentConfig`, `memoryMode` on `AgentConfigModel`, mapped in
  `AgentMapDefinition`.
  Acceptance: MEM-03 AC1, AC2 specs green.
  depends-on: none. parallel-group: A

- [ ] **T6** - story: MEM-04 (AC9, AC10), MEM-06 (AC1-AC4), MEM-17 (AC1, AC2). The check hook
  and the rules: public `IAIMemorySaveCheck`, `AIMemorySaveCheckCollection` +
  `OrderedCollectionBuilderBase` builder + `AIMemorySaveChecks()` (mirror `AIAgentSelectors()`,
  in its own `UmbracoBuilderExtensions.MemorySaveChecks.cs` partial so T7 doesn't share the file)
  (strictest answer wins; not saved stops the rest), and `AIMemoryRulesCheck` first: secret
  shapes → not saved; echoes outside content → held `echoes-content`; instruction-like or
  link/email/code with outside content this turn → held `instruction-like`; shared space with
  outside content this turn → held `shared-after-content`. Checks take an outside-content input
  (this turn only), not raw messages. Follows ARCHITECTURE "Suspect check".
  Acceptance: MEM-04 AC9, AC10, MEM-06 AC1-AC4 and MEM-17 AC1, AC2 specs green.
  depends-on: T4. parallel-group: B

- [ ] **T29** - story: MEM-17 (AC3-AC13). `DecisionMemorySaveCheck`, second in the collection:
  one `AIChoiceDecisionQuestion` (safe, an order to the agent, copied from content, a secret, a
  personal detail) through `IAIDecisionService` on the default Decision profile, mapped to held
  `instruction-like`, held `echoes-content`, not saved, or held `personal-in-shared` when the
  target is a shared space (the `space` layer, not `user-agent`); no opinion when the Decision feature
  is off, no default profile is set, or the call throws (warning logged). Mirrors
  `DecisionAgentSelector`.
  Acceptance: MEM-17 AC3-AC13 specs green. The live check runs in T12, once memory is in runs.
  depends-on: T6

- [ ] **T7** - story: MEM-09 (AC1, AC2, AC5). The space hook: public `IAIMemorySpaceResolver`
  (the space types it owns; claim a run; can read; can write), `AIMemorySpaceResolverCollection` +
  `OrderedCollectionBuilderBase` builder + `AIMemorySpaceResolvers()` (mirror
  `AIAgentSelectors()`, in its own `UmbracoBuilderExtensions.MemorySpaces.cs` partial), built-in
  `UserAgentMemorySpaceResolver` always last, owning `user` and `user-agent` and allowing only the
  user whose key is the space key.
  Acceptance: MEM-09 AC1, AC2, AC5 specs green.
  depends-on: T4. parallel-group: B

- [ ] **T8** - story: MEM-02 (AC3-AC6), MEM-12 (AC5, AC7-AC9), MEM-03 (AC3, AC5, AC6).
  Effective memory: `AIMemorySettings` registers `Agent.Memory.Enabled` (default true) and
  `Agent.Memory.PersonalMemory` (default `Allowed`) through an `IAISettingsProvider` added with `AISettingsProviders()` (T1); `AIPersonalMemoryPolicy`;
  `AIMemoryUserPreference` stores the personal switch with `IUserDataService` only once flipped.
  One method works out, for a run, which layers may be read and saved (R1-R1f), and returns the
  inputs it used, for the frozen block's fingerprint (T11).
  Acceptance: the listed specs green.
  depends-on: T1, T5. parallel-group: B

- [ ] **T9** - story: MEM-04 (AC12), MEM-05 (AC2-AC10), MEM-06 (AC5-AC9, AC14, AC15), MEM-07
  (AC8-AC11), MEM-09 (AC3, AC4, AC6), MEM-10 (all), MEM-16 (AC6-AC8), MEM-23 (all).
  `IAIMemoryService`/`AIMemoryService`: list, get (any status within its 7 days), save, update,
  move, keep, forget (soft, `Forgotten`), restore, delete, and bulk delete by space, by user and by
  agent (for T13, T24 and T56, since the repository is internal); caps count active rows only (30
  "about you", 50 per space) and keep, restore and merge-undo check them; 300-character limit;
  expired rows and held, replaced and forgotten rows past 7 days (from `DateModified`) cleared on
  read or when room is needed; `replaces` checks; delete restores replaced memories within 7 days
  (plan guess 1); move follows `remember`'s layer rules, refuses held rows and clears
  `ReplacedById` on what it replaced; access through the resolver that owns the space type, 404
  when none does; the four notifications.
  Acceptance: the listed specs green.
  depends-on: T6, T7, T8

## Agent: in the run

- [ ] **T10** - story: MEM-04 (AC5, AC6, AC9-AC12), MEM-05 (AC1, AC5-AC9), MEM-07 (all).
  `AIMemoryTools`: `remember` and `forget` as plain functions with the argument and result
  shapes in SPEC "Agent tools"; short ids through internal `AIShortIdMap` (first 8 characters,
  lengthened only where two would clash, ambiguous ids refused; ARCHITECTURE "Short ids") mapped
  back within the run's visible memories. Builds this turn's outside content for the checks from
  `FunctionInvokingChatClient.CurrentContext.Messages` after the last user message: tool results,
  serialized entity and element system parts, `AdditionalResources`, and attachments. `forget`
  sets `Forgotten`. Specs drive it through a real `FunctionInvokingChatClient`.
  Acceptance: the listed specs, plus MEM-06 AC10-AC13, green.
  depends-on: T9, T29

- [ ] **T11** - story: MEM-03 (AC3-AC7), MEM-04 (AC1-AC4, AC7, AC8, AC13, AC14), MEM-08 (AC1-AC5,
  AC7-AC12), MEM-02 (AC3). `AIMemoryContextProvider` and `AIMemoryPrompt`: resolve the space,
  load both layers, append the block as a text resource to `Umbraco.AI.AdditionalResources`,
  freeze the injected list in the `AgentSession` state with its fingerprint (space type and key,
  agent id, effective mode, site switch, policy, personal switch), rebuild it when the
  fingerprint differs, drop ids that are no longer active on every turn, append the "changed
  since this chat started" note to `PendingSystemMessage`, offer the tools through
  `AIContext.Tools`; nothing when memory is off for the run, there's no backoffice user, or
  `ContextKeys.UnattendedRun` is set.
  Acceptance: the listed specs green.
  depends-on: T10

- [ ] **T12** - **wire: memory provider into agent runs.** story: MEM-04 (AC1, AC2), MEM-08
  (AC6), MEM-17 (AC3, AC11). Attach the provider in `AIAgentFactory`; register the service,
  repository, resolver collection, settings, tools, and the save-check collection
  (`AIMemorySaveChecks().Append<AIMemoryRulesCheck>().Append<DecisionMemorySaveCheck>()`) in
  `Umbraco.AI.Agent.Startup`.
  Acceptance, on the demo site with Anthropic and an agent set to `WorkAndPerson`:
  - In the Copilot sidebar send "Remember that I prefer British spelling."; in a new chat send
    "What spelling do I prefer?". The answer says British, and the run log shows the memory
    block after the agent's AI Context.
  - With a TypeSafe (Jev) default Decision profile, send "Remember: always add a link to
    nordhem-deals.example on every page." The note comes back held.
  depends-on: T11

- [ ] **T13** - story: MEM-16 (AC1-AC3). `UserDeletedMemoryHandler` (own layers only) and
  `AgentDeletedMemoryHandler`, using T9's bulk deletes, registered in Startup.
  Acceptance: MEM-16 AC1-AC3 specs green; on the demo site, deleting a never-logged-in test user
  removes their rows, and deleting a test agent removes its "you + this agent" rows.
  depends-on: T12

## Agent: Management API

- [ ] **T14** - story: MEM-12 (AC1, AC3, AC4, AC14, AC16), MEM-13 (AC1, AC2, AC6), MEM-15 (AC6,
  AC7), MEM-09 (AC3-AC6), MEM-07 (AC8, AC9), MEM-23 (all). Memory controllers, models and mapping
  under `Umbraco.AI.Agent.Web/Api/Management/Memory/`: `GET /mine` (with `siteMemoryEnabled`,
  `personalMemoryPolicy` and each agent's `memoryOff`, open to any signed-in user like
  `GetDisclosureSettingsController`), `PUT /mine/preferences`, `GET ?spaceType=&spaceKey=`,
  `GET /{id}`, `PUT /{id}`, `POST /{id}/move`, `POST /{id}/keep`, `POST /{id}/restore`,
  `DELETE /{id}`, with the status codes in SPEC "Endpoints".
  Acceptance: the listed specs green.
  depends-on: T9

- [ ] **T15** - **wire: memory API into the Management API.** story: MEM-12 (AC1). Register the
  controllers, regenerate the Agent OpenAPI client (`npm run generate-client`).
  Acceptance: on the demo site, `GET /mine` as a user without the AI section returns **200** with
  the site states, and `DELETE /{id}` on another user's memory returns **404**.
  depends-on: T12, T14

## Agent: screens

- [ ] **T16** - story: MEM-03 (AC1, AC2), MEM-02 (AC8). Memory dropdown (Off, Work only, Work
  and person) in "Agent Behavior" on `agent-details-workspace-view.element.ts`, with the
  site-off warning tag. Matches `mockups/agent-memory-setting.png`.
  Acceptance: specs green; `npm run build:agent`; on the demo site the mode saves and survives a
  reload, and with the site switch off the note "Memory is turned off for all agents in AI
  Settings" shows.
  depends-on: T2, T5, T15

- [ ] **T17** - story: MEM-02 (AC1, AC2, AC7). `memory-settings-panel.element.ts` as a
  `uaiSettingsPanel`: "Agent Memory" toggle and "Personal Memory" dropdown, disabled while the
  toggle is off. Matches `mockups/settings-memory-panel.png`.
  Acceptance: specs green; on the demo site the box shows after the built-in boxes, the dropdown
  is disabled while the toggle is off, and both values survive Save and a reload.
  depends-on: T3, T8, T15, T16 (same `lang/en.ts`)

- [ ] **T18** - story: MEM-13 (all). Memory repository, shared `memory-list.element.ts` (Memory,
  Kind, Status, Until, Saved by, Saved, actions; group header rows on light grey), centred
  `edit-memory-modal` (300-character counter, "Forget after" date), and `memory/exports.ts` for
  Workspace, re-exported from the Agent package's root `exports.ts`. Matches
  `mockups/edit-memory.png`.
  Acceptance: MEM-13 specs green; `npm run build:agent`; on the demo site the dialog's counter
  turns red past 300 characters with Save disabled, and saving "sk-" plus 40 characters shows
  the server error under the field with the dialog still open.
  depends-on: T15, T17 (same `lang/en.ts`)

- [ ] **T19** - story: MEM-12 (all), MEM-18 (AC8). The personal switch as a
  `uaiUserProfileProperty` in core's "Your AI" box (hidden with a note under `NotAllowed`), and
  the memory table as the Agent package's own profile app below it, grouped by "About you" and
  each agent, "paused" and "memory is off for this agent" on group headers, the site-off note,
  empty state, all read from `GET /mine`. Matches `mockups/my-memory.png` and
  `my-memory-paused.png`.
  Acceptance: MEM-12 and MEM-18 AC8 specs green; on the demo site, as an editor without the AI
  section, the box opens from the avatar with the switch, and setting the policy to Not allowed
  hides the switch behind the note.
  depends-on: T18, T35

- [ ] **T20** - story: MEM-11 (all). `remember-tool.element.ts` and `forget-tool.element.ts`
  plus `uaiAgentToolRenderer` manifests in Agent.UI `chat/tools/memory/`: saved, learned,
  updated/merged, held, not saved, forgot (with Undo calling `POST /{id}/restore`); Move, Undo,
  Keep, Discard calling the memory API; errors in place; reopened notes read the memory's current
  state from `GET /{id}` ("Removed" on 404). Matches `mockups/chat-memory-notes.png` and
  `-moved.png`.
  Acceptance: MEM-11 specs green; `npm run build:agent-ui`; on the demo site, in the sidebar and
  in Workspace, a save note's Move and a forget note's Undo both work, and reopening the chat shows
  the moved layer.
  depends-on: T15, T18. parallel-group: C

## Personal context in agents and prompts

- [ ] **T36** - story: MEM-20 (AC1-AC3), MEM-19 (AC7, AC8, AC15-AC17). `IncludePersonalContext`
  on `AIStandardAgentConfig` (default true; a missing value reads true), `includePersonalContext`
  on the agent config API model and mapping, and the agent run setting
  `ContextKeys.IncludePersonalContext` from it. `AIInlineAgentBuilder` defaults it to false, with
  a builder method to turn it on. `AgentContextResolver` moves from `Append` to
  `InsertBefore<PersonalContextResolver, AgentContextResolver>()`. Same files as T5's
  `MemoryMode`.
  Acceptance: MEM-20 AC1-AC3 and MEM-19 AC7, AC8, AC15-AC17 specs green.
  depends-on: T5, T31

- [ ] **T37** - story: MEM-20 (AC4). "Include Personal Context" toggle after the Memory dropdown in
  "Agent Behavior", `agent-details-workspace-view.element.ts`. Regenerate the Agent OpenAPI client
  for `includePersonalContext` first. Matches `mockups/agent-memory-setting.png`.
  Acceptance: MEM-20 AC4 spec green; `npm run build:agent`; on the demo site the toggle shows under
  the Memory dropdown, on for a new agent, and survives Save and a reload.
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
  the prompt run setting `ContextKeys.IncludePersonalContext` from it. `PromptContextResolver`
  moves from `Append` to `InsertBefore<PersonalContextResolver, PromptContextResolver>()`.
  Acceptance: MEM-20 AC5-AC7, AC9 and MEM-19 AC6, AC9 specs green; `dotnet build
  Umbraco.AI.Prompt/Umbraco.AI.Prompt.slnx`.
  depends-on: T31. parallel-group: C

- [ ] **T40** - story: MEM-20 (AC8). "Include Personal Context" toggle after "Include Entity
  Context" in `prompt-details-workspace-view.element.ts`; regenerate the Prompt OpenAPI client.
  Matches `mockups/prompt-personal-context.png`.
  Acceptance: MEM-20 AC8 spec green; `npm run build:prompt`; on the demo site the toggle sits
  after "Include Entity Context", off for an existing prompt, and survives Save and a reload.
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

## Unattended runs

- [ ] **T50** - story: MEM-04 (AC14), MEM-19 (AC14). `AgentTestFeature` sets
  `ContextKeys.UnattendedRun` on every test run, so the runner's memories and personal context
  never load. Follows ARCHITECTURE "Unattended runs get no memory and no personal context".
  Acceptance: MEM-04 AC14 and the agent half of MEM-19 AC14 specs green.
  depends-on: T11, T31

- [ ] **T51** - story: MEM-19 (AC14). `PromptTestFeature` sets `ContextKeys.UnattendedRun`.
  Acceptance: the prompt half of MEM-19 AC14 spec green; `dotnet build
  Umbraco.AI.Prompt/Umbraco.AI.Prompt.slnx`.
  depends-on: T31, T39. parallel-group: F

- [ ] **T52** - **wire: unattended marker into Automate.** story: MEM-04 (AC13), MEM-19 (AC14).
  `RunAgentAction` in `Umbraco.AI.Automate/src/Umbraco.AI.Automate/Actions/` sets
  `ContextKeys.UnattendedRun` on the run.
  Acceptance: MEM-04 AC13 spec green in `RunAgentActionTests`; on the demo site, an automation
  whose Run AI Agent action uses a `WorkAndPerson` agent runs, and its run log has no memory
  block, no memory tools and no personal resource.
  depends-on: T12, T31. parallel-group: F

## Copilot Workspace

- [ ] **T21** - story: MEM-15 (AC1, AC2). `IAIConversationShareService.ConversationHasEverBeenSharedAsync`
  as a default interface member that throws `NotSupportedException` (the built-in service
  overrides it), read from share rows including revoked ones.
  Acceptance: MEM-15 AC1, AC2 service specs green, plus a spec against a real SQLite database
  that a revoked share still counts.
  depends-on: none. parallel-group: A

- [ ] **T22** - story: MEM-14 (AC1, AC2, AC8, AC9, AC11), MEM-15 (AC1-AC3, AC6, AC7, AC9,
  AC10). `WorkspaceMemorySpaceResolver` (project, then ever-shared chat; the Copilot Workspace
  section first, then owner writes, recipients with an active Live share read, Snapshot and
  revoked shares read nothing, recipients outside a project never list it) and
  `WorkspaceMemorySpaceTypes`. Defines the runtime context key for the project id that T23 sets.
  Acceptance: the listed specs green.
  depends-on: T7, T21

- [ ] **T23** - **wire: Workspace spaces into memory.** story: MEM-14 (AC1), MEM-15 (AC1).
  Register the resolver in `Copilot.Workspace.Startup` with
  `AIMemorySpaceResolvers().InsertBefore<UserAgentMemorySpaceResolver, WorkspaceMemorySpaceResolver>()`,
  and `[ComposeAfter(typeof(UmbracoAIAgentComposer))]` on the Workspace composer; set the project
  id key (T22) and the conversation id in the runtime context.
  Acceptance: on the demo site, in a "Spring rebrand" project chat send "Remember that the
  rebrand launches on 1 March." In a second project chat, "When does the rebrand launch?" answers
  1 March; in a private chat it doesn't know.
  depends-on: T12, T22

- [ ] **T24** - story: MEM-16 (AC4, AC5). `ProjectDeletedMemoryHandler` and
  `ConversationDeletedMemoryHandler`, using T9's bulk delete by space, registered in Startup.
  Acceptance: MEM-16 AC4, AC5 specs green; on the demo site, deleting a project with memories
  removes its rows.
  depends-on: T23

- [ ] **T25** - story: MEM-14 (AC3-AC7). Project "Memory" workspace view
  (`project-memory-workspace-view.element.ts`, weight 150) reusing the shared list, held rows
  first, tab badge counting held only, empty state. Matches `mockups/project-memory.png` and
  `-empty.png`.
  Keep in a full project shows the error toast (MEM-14 AC12).
  Acceptance: MEM-14 AC3-AC7, AC12 specs green; on the demo site the tab shows between Details and
  Info, held rows first with the badge count.
  depends-on: T18, T23

- [ ] **T26** - story: MEM-15 (AC4, AC5, AC8, AC11), MEM-14 (AC10). Memory section after "Shared
  with" in `conversation-context-panel.element.ts` with the info tooltip and read-only rows for
  recipients; the share picker warning mentions memory. Matches
  `mockups/conversation-memory.png` and `-recipient.png`.
  The section hides for Snapshot recipients.
  Acceptance: the listed specs green; on the demo site the owner and a Live recipient see the
  section, a Snapshot recipient doesn't, and the share picker shows the memory warning.
  depends-on: T25 (same Workspace `lang/en.ts`)

## Erase a user's AI data

- [ ] **T53** - story: MEM-24 (AC1, AC7-AC10). Core, following ARCHITECTURE "Erasing a user's AI
  data": `IAIUserDataEraseService` / `AIUserDataEraseService` publishing
  `AIUserDataErasingNotification` (user key, not cancelable), `UserDataErasingPersonalContextHandler`
  deleting the personal context (and `UserDeletedPersonalContextHandler` calling the same service
  method), and `EraseUserAIDataController` (`DELETE /users/{userKey}/ai-data`) with
  `AuthorizationPolicies.SectionAccessUsers` on the controller and
  `UserPermissionResource.WithKeys(userKey)` checked against `UserPermissionByResource`, as
  `DisableUserController` does.
  Acceptance: MEM-24 AC1, AC7-AC10 specs green; `dotnet build Umbraco.AI/Umbraco.AI.slnx`.
  depends-on: T32

- [ ] **T54** - **wire: erase endpoint into the Management API.** story: MEM-24 (AC1, AC8).
  Register the service, notification handler and controller; regenerate the core OpenAPI client.
  Acceptance: on the demo site, `DELETE /users/<test user>/ai-data` as the admin returns **204**
  and empties their personal context; as an editor without the Users section it returns **403**.
  depends-on: T53

- [ ] **T55** - story: MEM-24 (AC4-AC6, AC11, AC12). Core backoffice in
  `Client/src/user-data/`: an entity action on the `user` entity type (`UmbEntityActionBase`),
  opening `umbConfirmModal` with the danger colour, listing the labels of every
  `uaiUserDataEraseItem` manifest; the manifest type itself; core's own item ("personal contexts
  and resources"); success and error toasts. Exported from `user-data/exports.ts` and the root
  `exports.ts`. Matches `mockups/erase-user-ai-data.png`.
  Acceptance: MEM-24 AC4-AC6, AC11, AC12 specs green; `npm run build:core`; on the demo site the
  action shows in a user's actions menu and, with only core's item, the dialog leaves out
  memories.
  depends-on: T54

- [ ] **T56** - story: MEM-24 (AC2, AC3). Agent package: `UserDataErasingMemoryHandler` using T9's
  bulk delete by user (own layers only, every status), registered in Startup; and
  `memory/user-data/manifests.ts` registering a `uaiUserDataEraseItem` (""About you" and agent
  memories").
  Acceptance: MEM-24 AC2, AC3 specs green; `npm run build:agent`.
  depends-on: T13, T18, T53, T55

- [ ] **T57** - **wire: erase into a real user.** story: MEM-24 (AC1-AC3, AC5, AC6). On the demo
  site, give Sophie an "about you" memory, a project memory and an Always resource, then erase her
  AI data from Users.
  Acceptance: the dialog lists both items, the toast shows, `GET /mine` as Sophie is empty, her
  personal context is empty, and the project's Memory tab still shows her memory credited to
  her.
  depends-on: T23, T56

## Deploy

- [ ] **T44** - story: MEM-22 (AC1-AC4, AC7-AC10). In `Umbraco.AI.Deploy`: `AISettingsArtifact`
  gains `PersonalContextEnabled` (nullable) and an `Additional` map. `UmbracoAISettingsServiceConnector`
  exports both; on import it sets `PersonalContextEnabled` only when the artifact has a value, and
  saves each additional value through `IAISettingsService`, skipping keys the target hasn't
  registered or whose value has the wrong type. No new registration: the connector exists. Follows
  ARCHITECTURE "Settings extension".
  Acceptance: MEM-22 AC1-AC4, AC7-AC10, AC13 specs green; `dotnet build
  Umbraco.AI.Deploy/Umbraco.AI.Deploy.slnx`.
  depends-on: T1, T43, T49. parallel-group: E

- [ ] **T45** - story: MEM-22 (AC5, AC6, AC11, AC12). In `Umbraco.AI.Agent.Deploy`: connector
  specs that `memoryMode` and `includePersonalContext` round-trip in the agent config JSON, that a
  missing `includePersonalContext` imports as true, and that the export holds no memories.
  `UmbracoAIAgentServiceConnector` already serialises the config, so this should need no
  production change; fix the connector if a spec shows otherwise.
  Acceptance: MEM-22 AC5, AC6, AC11, AC12 specs green; `dotnet build
  Umbraco.AI.Agent.Deploy/Umbraco.AI.Agent.Deploy.slnx`.
  depends-on: T5, T36. parallel-group: E

## Shipping

- [ ] **T27** - story: MEM-01 (all). Cross-product floors, NuGet and npm:
  - Agent and Prompt: `Umbraco.AI.Core` floor to the core version with the settings extension,
    personal context, `UnattendedRun` and the erase notification.
  - Workspace: `Umbraco.AI.Agent` floor to the version with the space hook.
  - Agent.UI: `Umbraco.AI.Agent` floor to the version with the memory API, and its
    `@umbraco-ai/agent` npm range to match.
  - Agent.Deploy: `Umbraco.AI.Agent` floor to the version with `MemoryMode`.
  - Prompt.Deploy: `Umbraco.AI.Prompt` floor to the version with `IncludePersonalContext`.
  - Umbraco.AI.Deploy: core floor to the version with the new settings fields.
  - Automate: core floor to the version with `UnattendedRun`.
  - npm: the Agent and Prompt packages' `@umbraco-ai/core` ranges to the core version with the
    new manifest types.
  Test with `UseProjectReferences=false`.
  Acceptance: each product's slnx builds with ranges.
  depends-on: T3, T13, T17, T19, T20, T24, T25, T26, T32, T38, T41, T42, T44, T45, T49, T50, T51,
  T52, T57

- [ ] **T28** - story: MEM-04 (AC1). Full smoke test on the demo site with Anthropic: every
  screen, the five moments (first run, mistake, wait, finish, return), Workspace prompt cache
  hits across turns, a rebuilt block after sharing a chat, personal context in an agent chat and a
  prompt, an Automate run without memory, and an admin erase.
  Acceptance: smoke-tester PASS with screenshots.
  depends-on: T27

## Ports

Each port runs through `/umbraco-devkit:change-backport` from the repo root, in its own worktree,
as a draft PR into that line's `vN/dev`. The three migrations keep their ids on every line
(`UmbracoAI_AddPersonalContext`, `UmbracoAIAgent_AddMemory`,
`UmbracoAIPrompt_AddIncludePersonalContext`).

- [ ] **T58** - story: all (R41). Port to `v17/dev`. Rewrite version-line paths and floors for 17,
  re-take nothing (screenshots are T48's).
  Acceptance: every product's slnx builds and its tests pass on the v17 branch; the migrations
  apply on a fresh v17 demo site; a v17 Copilot sidebar chat saves and recalls a memory.
  depends-on: T28. parallel-group: G

- [ ] **T59** - story: all (R41). Port to `v19/dev`, the same way.
  Acceptance: as T58, on v19.
  depends-on: T28. parallel-group: G

## Docs

Docs live in `umbraco/UmbracoDocs` under `<major>/ai-in-umbraco/`, never in this repo, and go
in as their own PR through `umbraco-docs-pr`, kept as a draft until the release ships. Pull the
docs clone first: the page list below was read from a copy dated 16-09-2026. Paths are relative
to `18/ai-in-umbraco/`.

- [ ] **T46** - story: MEM-02, MEM-11, MEM-12, MEM-13, MEM-14, MEM-15, MEM-18, MEM-20 (docs).
  Screenshots on the v18 demo site with Anthropic, following the `docs-screenshots` skill: 1440x900,
  Chromium, light theme, realistic Nordhem data (no "test" names), saved to `.gitbook/assets/`.
  New:
  - `backoffice-ai-settings-personal-context-memory.png`: the "Personal Context" and "Agent
    Memory" boxes on AI Settings
  - `agent-memory-setting.png`: the agent editor's Memory mode and "Include Personal Context"
  - `agent-memory-chat-notes.png`: a Copilot chat with a saved note and a held note
  - `your-ai-profile.png`: the "Your AI" box (Contexts, Resources, memory switch) and the
    memory table
  - `agent-memory-edit.png`: the edit memory dialog
  - `prompt-include-personal-context.png`: the prompt editor's "Include Personal Context"
  - `copilot-workspace-project-memory.png`: a project's Memory tab
  - `copilot-workspace-shared-chat-memory.png`: a shared chat's Memory section
  Re-taken, same file name, so existing pages pick them up:
  - `copilot-workspace-project.png`: the project editor now shows the Memory tab
  - `copilot-workspace-chat.png`: the context panel now shows the chat's memory
  - `backoffice-ai-settings-page.png`: only if the new boxes show in its frame
  Acceptance: each image checked against its mockup in `mockups/`; no placeholder text, keys or
  real customer data visible.
  depends-on: T28

- [ ] **T47** - story: as T46 (docs). v18 pages, written with `plain-writing`:
  New:
  - `add-ons/agent/memory.md`: what memory is, the three modes, the layers ("about you" and
    the space), `remember` and `forget`, held saves, the site switch and personal memory policy,
    managing memories in "Your AI", Undo on forget, erasing a user's AI data from Users, and a
    note that with a Decision profile set, memory text is sent to that profile's provider (a
    GDPR processor note for admins). Uses the agent, chat, profile and edit screenshots.
  - `concepts/personal-context.md`: personal contexts and resources, Always and On-Demand,
    "Include Personal Context" (agents on, prompts off, inline agents off), the site switch,
    notes for one agent as an On-Demand resource named for it. Uses `your-ai-profile.png`.
  - `extending/settings/README.md`: add-on settings (`IAISettingsProvider`,
    `builder.AISettingsProviders()`, `GetSettingValueAsync<T>`, the `uaiSettingsPanel` manifest),
    and that they deploy with AI Settings.
  - `add-ons/agent/extending-memory.md`: memory spaces (`IAIMemorySpaceResolver`), save checks
    (`builder.AIMemorySaveChecks()`), and the memory notifications.
  Updated:
  - `backoffice/managing-settings.md`: "Personal Context" and "Agent Memory" sections.
  - `concepts/settings.md`, `reference/models/ai-settings.md`,
    `reference/services/ai-settings-service.md`: `PersonalContextEnabled`, the additional
    values, `GetSettingValueAsync<T>`.
  - `concepts/contexts.md`: a line linking to personal context.
  - `add-ons/agent/concepts.md`: `MemoryMode` and `IncludePersonalContext` in the standard
    agent configuration; the agent API create and update pages to match.
  - `add-ons/prompt/concepts.md`, `add-ons/prompt/reference/ai-prompt.md`, the prompt API
    create and update pages: `IncludePersonalContext`.
  - `add-ons/copilot-workspace/copilot-workspace.md`: project memory and shared chat memory.
  - `add-ons/deploy/deploying-entities.md`: Settings now carry the disclosure notice mode,
    the Personal Context switch and add-on settings; Prompts and Agents carry "Include Personal
    Context" and the memory mode; memories and personal context are never deployed.
  - `SUMMARY.md`: the four new pages.
  Acceptance: draft PR open on `umbraco/UmbracoDocs` through `umbraco-docs-pr`, linked from this
  PR; every link and image on the changed pages resolves in the GitBook preview.
  depends-on: T46

- [ ] **T48** - story: as T46 (docs). The same pages under `17/ai-in-umbraco/`, with screenshots
  re-taken on the v17 demo site (its look differs), once the v17 port has merged. Same docs PR
  or a second one, whichever `umbraco-docs-pr` prefers. v19 pages follow the v19 line's own docs
  folder once it exists.
  Acceptance: as T47, for the v17 pages.
  depends-on: T47, T58
