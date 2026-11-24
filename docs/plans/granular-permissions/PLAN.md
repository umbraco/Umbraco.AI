# Plan: Granular permissions

**48 tasks in 11 areas.** Built on `v18/dev` first. The v17 and v19 ports come after, through
`change-backport` (see the last area). Story ids are in [STORIES.md](./STORIES.md).

> [!IMPORTANT]
> **1 guess needs your OK.**
>
> 1. Rollback of an entity type with no known right (one added by a package we don't know) is
>    refused with 403 (T6). Each versionable adapter names the right its rollback needs
>    (`RollbackPermission`); one that names none can't be rolled back.\
>    *If not:* an adapter that names no right only needs the AI section, as today. That leaves
>    rollback open for any add-on that hasn't declared a right.

How to read it: areas run top to bottom. Tasks with the same `parallel-group` letter share no
file or migration and can be built at once. Every `wire:` task is proven through the running
demo site (`git wdp-port` gives its port), never by unit tests alone.

The first parallel group is **S**: T1 (core permission service), T14 (agent column) and T28
(prompt column). They touch three different products.

Pending specs are staged in [`specs/`](./specs/README.md) at their final repo paths. Each task
moves its specs into the real test project and makes them pass. Agent and Prompt frontends have
no test runner today, so their backoffice criteria are proven by the `wire:` smoke tests. Core's
frontend criteria use Vitest.

## Core: permission service (Umbraco.AI)

- [x] **T1** - story: GP-1 (AC3), GP-2 (AC1, AC4). Add `Umbraco.AI.Core/Security/AIPermissions.cs`
  with public string constants for all 11 verbs in SPEC.md "Rights" (`Uai.Connection.Manage` …
  `Uai.Prompt.Manage`). Add internal `IAIPermissionService` and `AIPermissionService`:
  `HasPermission(verb)` reads `IReadOnlyUserGroup.Permissions` of the current backoffice user's
  groups (no extra database query, SPEC Targets); `CanUse(allowedUserGroupIds, userGroupKeys)`
  is true when the list is null or empty (R9) or shares a key (R10); a helper returns the current
  user's group keys and whether they're in the admin group. Register in the core Composer. Add
  Add internal `AIPermissionDeniedException` beside it. `InternalsVisibleTo` for `Umbraco.AI.Agent.Core`, `Umbraco.AI.Prompt.Core`,
  `Umbraco.AI.Agent.Copilot.Workspace.Core` and `Umbraco.AI.Automate` where not already there.
  Acceptance: `AIPermissionService` specs in `Umbraco.AI.Tests.Unit` green (any group holds the
  verb → true; none → false; no admin bypass, R1; empty list → usable; deleted key matches no one,
  R14).
  depends-on: none. parallel-group: S

- [x] **T2** - story: GP-2 (AC4, AC6). Add `AIPermissionRequirement(verb)` and
  `AIPermissionAuthorizationHandler` (calls `IAIPermissionService.HasPermission`) in
  `Umbraco.AI.Web/Authorization/`. Add one public policy name per right to
  `AIAuthorizationPolicies.cs`. Each policy holds the existing `AISectionRequirement` plus the
  new requirement. Register them in `Configuration/UmbracoBuilderExtensions.cs`. Agent and
  Prompt policies are registered here too, since the verbs live in core.
  Acceptance: handler specs green (right + section → pass; right without section → fail; section
  without right → fail).
  depends-on: T1

## Core: Management API (Umbraco.AI)

- [x] **T3** - story: GP-2 (AC1, AC2, AC4, AC5, AC6). Swap `SectionAccessAI` for the area policy
  on the create, update and delete controllers of Connection, Profile, Context and Guardrail, on
  `POST connections/{idOrAlias}/test`, and on `PUT settings`. Read controllers keep their current
  check (#509).
  Acceptance: `specs/…/Api/Management/AreaWriteAuthorizationTests.cs` green: 201 with the right, 403 without,
  the entity unchanged after a 403, `GET profiles` 200 without any AI right.
  depends-on: T2. parallel-group: A

- [x] **T4** - story: GP-2 (AC2). Test and TestRun controllers: create, update, delete, run, run
  batch, run by tags and test run delete → Manage tests policy.
  Acceptance: `AreaWriteAuthorizationTests` theory green for the test controllers: 403 without `Uai.Test.Manage`, success with it.
  depends-on: T2. parallel-group: A

- [x] **T5** - story: GP-4 (AC1-AC5), GP-2 (AC7). Audit log list and detail → View logs policy.
  Audit log delete and cleanup → Manage settings policy (spec guess 1). Usage summary, time
  series and breakdown → View analytics policy.
  Acceptance: `MonitoringAuthorizationTests` and the audit log row of `AreaWriteAuthorizationTests` green.
  depends-on: T2. parallel-group: A

- [x] **T6** - story: GP-3 (AC1-AC4). In `Versioning/Controllers/EntityVersionHistoryController.cs`,
  rollback checks the manage right of the area that owns `entityType`, through
  `IAuthorizationService` and the T2 requirements. Each `IAIVersionableEntityAdapter` declares
  that right as `RollbackPermission` (default null); the core adapters, `agent` and `prompt`
  set it. An adapter that declares none gets 403 (PLAN guess 1). An
  `AIPermissionDeniedException` thrown by an adapter's rollback (an add-on's save rule, T46) also
  returns 403. Version list and detail stay as they are.
  Acceptance: `RollbackAuthorizationTests` green: profile rollback 200 with Manage profiles, 403 without, version
  unchanged after a 403, agent rollback 403 with only Manage profiles.
  depends-on: T2. parallel-group: A

- [x] **T7** - **wire: area policies into the Management API.** story: GP-2, GP-3, GP-4. On the
  demo site, make a "Context managers" group with the AI section and only Manage contexts, and a
  user in it.
  Acceptance: as that user, real requests: `POST contexts` 201, `POST connections` 403,
  `GET profiles` 200, audit log list 403, profile rollback 403. As admin (after T9), all succeed.
  depends-on: T3, T4, T5, T6, T9

## Core: upgrade (Umbraco.AI)

- [x] **T8** - story: GP-6 (AC1, AC2, AC5, AC6, AC7). Add `Migrations/GrantAIPermissionsToSectionGroups.cs`
  and add it to `UmbracoAIMigrationPlan.cs` after `MigrateAISectionAlias` and
  `AddAISectionToAdminGroup`. Raw SQL like `AddAISectionToAdminGroup`: for each group with the AI
  section, and the admin group, insert each of the 9 core verbs into the CMS's fallback permission
  table, skipping a group that already holds any of them. Clears the cached user groups and
  users (`AppCaches`) instead of `RebuildCache`. Never deletes.
  Acceptance: `Umbraco.AI.Tests.Integration` migration specs green on SQLite: section groups and
  admin get 9 verbs; a turned-off verb stays off on re-run; no duplicate rows; a group given the
  section afterwards has none.
  depends-on: T1. parallel-group: B

- [x] **T9** - **wire: core grant step into the demo site upgrade.** story: GP-6 (AC1, AC2). Start
  the demo site on its existing database.
  Acceptance: the migration runs on start; the Administrators group shows all 9 core rights ticked
  on the user group screen; a second start adds nothing (row count unchanged).
  depends-on: T8

## Core: backoffice (Umbraco.AI)

- [x] **T10** - story: GP-1 (AC1, AC2, AC4). Add `Client/src/permissions/constants.ts` (verb
  constants, `UAI_PERMISSION_ENTITY_TYPE`) and `permissions/manifests.ts` with one
  `entityUserPermission` per core right, grouped as Configuration and Monitoring. Register through
  the core `manifests.ts`. Labels, descriptions and group headings in `lang/*`. Export the
  constants and entity type through `exports.ts` so Agent and Prompt can reuse them (see
  `.claude/memory/frontend-entry-points.md`).
  Acceptance: Vitest spec: 9 manifests, each with its verb and group; `npm run build:core` passes.
  depends-on: T1. parallel-group: B

- [x] **T11** - story: GP-5 (AC1, AC3-AC6). Add `Umb.Condition.UserPermission.Fallback` with the
  area's verb to Connection, Profile, Context, Knowledge set, Guardrail and Settings: menu item,
  collection action, bulk action, create and delete entity actions, the workspace and its save
  action, and the collection.
  Acceptance: Vitest specs that each area's manifests carry the condition; `npm run build:core`.
  depends-on: T10

- [x] **T12** - story: GP-4 (AC6), GP-5. Same condition on Audit log (View logs), Analytics (View
  analytics) and Tests (Manage tests) manifests.
  Acceptance: Vitest specs; `npm run build:core`.
  depends-on: T10

- [x] **T47** - story: GP-18 (AC3, AC4). `core/components/user-group-settings-list/user-group-settings-list.element.ts`
  gains an optional `editableUserGroupIds` property. When set, rows for other groups render in the
  list's existing read-only look (no edit, no remove), and the Add picker lists only those groups.
  When unset, it behaves as today. Its only other user is the agent tool rules field.
  Acceptance: `npm run build:core`; Vitest spec of the row-editability helper.
  depends-on: none. parallel-group: B

- [x] **T13** - **wire: AI rights and hidden areas into the backoffice.** story: GP-1, GP-4 (AC6),
  GP-5. Smoke test on the demo site.
  Acceptance: the user group screen shows the AI box with Configuration and Monitoring rights;
  ticking Manage contexts saves and reopens ticked; the "Context managers" user (T7) sees Contexts
  with create and save, sees no Connections, Audit log or Analytics menu items, gets no editor at a
  connection's direct URL, and can still pick a profile (contexts have no profile field, so this is
  checked in the agent editor's profile picker).
  depends-on: T7, T11, T12

## Agent: server (Umbraco.AI.Agent)

- [x] **T14** - story: GP-7 (AC4, AC5). Add `AllowedUserGroupIds` (`IReadOnlyList<Guid>`, empty =
  everyone) to `Agents/AIAgent.cs`. Persistence: entity column (JSON, keys lowercase "D" format),
  factory, and migrations `UmbracoAIAgent_AddAllowedUserGroupIds` for SQL Server and SQLite, the
  way `SurfaceIds` is done. `AIAgentVersionableEntityAdapter` includes the list in the snapshot
  so rollback restores it.
  Acceptance: `Umbraco.AI.Agent.Tests.Integration` specs: save and reload keep the list; existing
  rows read as empty; rollback restores an empty list.
  depends-on: none. parallel-group: S

- [x] **T15** - story: GP-6 (AC3). Add `Migrations/UmbracoAIAgentMigrationPlan.cs` (new package
  migration plan) and `GrantAIAgentPermissions.cs`: `Uai.Agent.Manage` to every AI section group
  and the admin group, same rules as T8.
  Acceptance: integration spec: groups with the section get the verb once; re-run adds nothing.
  depends-on: T1, T8

- [x] **T16** - story: GP-9 (AC1, AC3, AC5, AC7), GP-10 (AC1-AC3), GP-7 (AC7). In
  `AIAgentService`: list methods filter in the repository query, one `LIKE '%"<key>"%'` per user
  group ORed, plus empty or null (SPEC Targets, no per-row load). Get-by-id and alias return null
  for an agent the user can't use, unless the caller asked for managed access and has Manage
  agents. Every run and stream method refuses an agent the user can't use with a typed
  "agent not available" failure. Scope still applies (R13). Users come from
  `IAIPermissionService`; an explicit `AIAgentExecutionOptions.UserGroupIds` wins when set.
  Acceptance: `Umbraco.AI.Agent.Tests.Unit` and `.Integration` specs green for each criterion
  listed, including a SQLite query spec that the filter is one query.
  depends-on: T1, T14

- [x] **T17** - story: GP-8 (AC1, AC2, AC4, AC6, AC7). Group-list save rule in
  `AIAgentService.SaveAgentAsync`, copied from the CMS `UserGroupService` rule (18.3.2): admins
  change freely; a non-admin may change the list only on an agent they can use, may add or remove
  only groups they belong to, and may not leave it empty when it wasn't (spec guess 5). A refused
  save throws `AIPermissionDeniedException` (core, internal) and saves nothing.
  Acceptance: unit specs for each criterion.
  depends-on: T16

- [x] **T46** - story: GP-18 (AC1, AC2, AC5-AC8), GP-8 (AC8). Extend the save rule from T17 in
  `AIAgentService.SaveAgentAsync` to `AIStandardAgentConfig.UserGroupPermissions`: compare the
  saved and incoming dictionaries; a non-admin may add, change or remove only entries keyed by
  groups they belong to (SPEC R22). Same refusal as T17: both throw the core's internal `AIPermissionDeniedException`, and nothing
  is saved. Rollback
  (`AIAgentVersionableEntityAdapter.RollbackAsync` calls `SaveAgentAsync`) is covered by both
  rules (R23): add the rollback specs, no adapter change expected.
  Acceptance: `AgentToolRuleGroupRuleTests` and the rollback rows of `AgentUserGroupListRuleTests`
  green.
  depends-on: T17

- [ ] **T18** - story: GP-9 (AC6). `Agents/Selection/AIAgentSelectionService.cs` drops agents the
  user can't use before selection runs.
  Acceptance: unit spec: the best-matching agent limited to another group isn't picked.
  depends-on: T16. parallel-group: C

- [ ] **T19** - story: GP-10 (AC4). Make sure agent test runs call the agent service with the
  runner's groups (architecture guess 5). Add the spec; change the test-run path only if it
  passes a different user.
  Acceptance: spec: a test run by a user outside the agent's groups is refused.
  depends-on: T16. parallel-group: C

- [ ] **T20** - story: GP-14 (AC3, AC4). Add public `Agents/IAIAgentUserGroupResolver.cs` and its
  collection builder: a resolver claims an `onBehalfOf` kind (the text before `:`) and returns
  group keys, or says the caller may not act for it, or that it has no user. Register the empty
  collection in the Agent Composer.
  Acceptance: unit specs: unknown kind → not handled; resolver refusals map to the right result.
  depends-on: T16. parallel-group: C

- [ ] **T21** - story: GP-2 (agents), GP-7 (AC4), GP-8 (AC4, AC8), GP-9 (AC2-AC5), GP-10 (AC2),
  GP-14 (AC3, AC4), GP-18 (AC5, AC6, AC8). In `Umbraco.AI.Agent.Web/Api/Management/Agent/`: `AllAgentController` takes
  `includeRestricted` and `onBehalfOf`; `ByIdOrAliasAgentController` returns 404 when not usable
  and takes `onBehalfOf`; Create, Update and Delete use the Manage agents policy; models and
  `AgentMapDefinition` carry `allowedUserGroupIds`; run, stream, stream-agui, suggest-starters and
  its availability return 403 `ProblemDetails` "Agent not available"; a refused group-list or
  tool-rule change returns 403, from both save and rollback; `onBehalfOf` errors return 403 or 400
  as in SPEC.md.
  Acceptance: `Umbraco.AI.Agent.Tests.Integration` endpoint specs for each status.
  depends-on: T2, T20, T46

- [ ] **T22** - **wire: agent limits into the Management API.** story: GP-7, GP-8, GP-9, GP-10.
  On the demo site, limit one agent to "Editors".
  Acceptance: real requests: a user outside "Editors" doesn't see it in `GET agents`, gets 404 on
  its detail and 403 on `run`; a member sees and runs it; a manager outside "Editors" sees it with
  `includeRestricted=true`; a non-admin adding "Translators" gets 403; a non-admin changing the
  "Translators" tool rules gets 403; the Agent migration grants Manage agents on start.
  depends-on: T15, T18, T19, T21

## Agent: backoffice (Umbraco.AI.Agent)

- [ ] **T23** - story: GP-7 (AC4), GP-14. Regenerate the Agent OpenAPI client
  (`npm run generate-client` against the running demo site) for `allowedUserGroupIds`,
  `includeRestricted` and `onBehalfOf`.
  Acceptance: `npm run build:agent` passes with the new types.
  depends-on: T22

- [ ] **T24** - story: GP-1 (AC1, AC4), GP-5, GP-9 (AC2). Add `agent/permissions/manifests.ts`
  (Manage agents, `UAI_PERMISSION_ENTITY_TYPE`, group "Add-ons"). Add the fallback condition to the
  agent menu, collection, entity actions and workspace manifests. The AI section's agent collection
  asks with `includeRestricted=true`.
  Acceptance: `npm run build:agent`.
  depends-on: T10, T23. parallel-group: D

- [ ] **T25** - story: GP-7 (AC1-AC3, AC6), GP-8 (AC3, AC5). Access box in
  `agent-availability-workspace-view.element.ts`: `umb-user-group-input`; deleted keys show
  "Deleted user group" with a `color="danger"` Deleted tag; read-only when the user can't use the
  agent and isn't an admin; the picker lists only the user's own groups unless admin. Match
  `mockups/agent-access.html`. Localized labels.
  Acceptance: `npm run build:agent`.
  depends-on: T23. parallel-group: D

- [ ] **T48** - story: GP-18 (AC3, AC4). `agent-governance-workspace-view.element.ts` passes the
  current user's group keys as `editableUserGroupIds` to `user-group-tool-permissions.element.ts`,
  which forwards them to the list. Admins pass nothing, so every row stays editable.
  Acceptance: `npm run build:agent`.
  depends-on: T23, T47. parallel-group: D

- [ ] **T26** - story: GP-14 (AC1, AC2). `property-editor-ui-agent-picker.element.ts` reads
  `workspaceId` from its config; `agent-picker.element.ts` passes
  `onBehalfOf=automate-workspace:<workspaceId>` when it's set, and asks as today when not.
  Acceptance: `npm run build:agent`.
  depends-on: T23. parallel-group: D

- [ ] **T27** - **wire: agent Access box and rights into the backoffice.** story: GP-1, GP-5,
  GP-7, GP-8, GP-18. Smoke test on the demo site.
  Acceptance: Manage agents shows under Add-ons on the user group screen; an agent's Availability
  view shows the Access box, opens the CMS picker, saves and reopens "Editors"; a deleted group
  shows the danger tag; a non-admin outside the agent's groups sees the field read-only; a user
  without Manage agents has no Agents menu item; Copilot for a user outside "Editors" doesn't list
  the limited agent; on Governance, a non-admin in "Editors" sees the "Translators" tool rules row
  read-only and Add lists only their groups, while an admin can edit every row.
  depends-on: T24, T25, T48

## Prompt (Umbraco.AI.Prompt)

- [ ] **T28** - story: GP-12 (AC3). Same as T14 for `Prompts/AIPrompt.cs`: column, factory,
  `UmbracoAIPrompt_AddAllowedUserGroupIds` for SQL Server and SQLite,
  `AIPromptVersionableEntityAdapter` snapshot.
  Acceptance: Prompt specs: save and reload keep the list; rollback restores it.
  depends-on: none. parallel-group: S

- [ ] **T29** - story: GP-6 (AC4). New Prompt migration plan plus `GrantAIPromptPermissions`
  (`Uai.Prompt.Manage`), same rules as T8.
  Acceptance: spec: section groups get the verb once.
  depends-on: T1, T8

- [ ] **T30** - story: GP-13 (AC2, AC3, AC5), GP-12 (AC4, AC5). `AIPromptService`: list filter in
  the query, null detail when not usable (unless managed access with Manage prompts), execute
  refuses with a typed failure, and the group-list save rule from T17.
  Acceptance: `Umbraco.AI.Prompt.Tests.Unit` specs for each criterion.
  depends-on: T1, T28

- [ ] **T31** - story: GP-2 (prompts), GP-13 (AC2-AC5), GP-12 (AC4, AC5). Prompt controllers:
  `includeRestricted`, 404 detail, Manage prompts policy on writes, `allowedUserGroupIds` on
  models and mapping, `execute` 403 `ProblemDetails` "Prompt not available", 403 on a refused
  group-list change. No `onBehalfOf`.
  Acceptance: endpoint specs (Prompt has no integration project: add the specs to
  `Umbraco.AI.Prompt.Tests.Unit` against the controllers, and prove the HTTP behaviour in T32).
  depends-on: T2, T30

- [ ] **T32** - **wire: prompt limits into the Management API.** story: GP-12, GP-13. On the demo
  site, limit one prompt to "Editors".
  Acceptance: real requests: outside "Editors", `GET prompts` hides it, detail 404, `execute` 403;
  a member executes it; a manager sees it with `includeRestricted=true`; the Prompt migration
  grants Manage prompts on start.
  depends-on: T29, T31

- [ ] **T33** - story: GP-1, GP-5, GP-12 (AC1, AC2, AC6). Regenerate the Prompt client; add
  `prompt/permissions/manifests.ts` (Manage prompts, "Add-ons"); add the condition to prompt menu,
  collection, entity actions and workspace; AI section collection uses `includeRestricted=true`;
  Access box in `prompt-availability-workspace-view.element.ts` mirroring T25. Match
  `mockups/prompt-access.html`.
  Acceptance: `npm run build:prompt`.
  depends-on: T10, T25, T32

- [ ] **T34** - **wire: prompt Access box and limits into the backoffice.** story: GP-12, GP-13.
  Smoke test on the demo site.
  Acceptance: the Access box saves and reopens "Editors"; for a user outside "Editors" the prompt
  property action and toolbar don't offer the prompt; for a member they do.
  depends-on: T33

## Copilot Workspace (Umbraco.AI.Agent.Copilot.Workspace)

- [ ] **T35** - story: GP-11 (AC1, AC2). `StreamConversationAGUIController` maps the agent
  service's "agent not available" failure to 403 `ProblemDetails` "Agent not available".
  Conversation read endpoints don't check the agent limit.
  Acceptance: `Umbraco.AI.Agent.Copilot.Workspace.Tests.Integration` specs: history 200, new
  message 403.
  depends-on: T16

- [ ] **T36** - **wire: Workspace refusal into the running site.** story: GP-11. On the demo site,
  start a Workspace chat with an agent, then limit the agent to a group the user isn't in.
  Acceptance: the chat reopens with its messages; sending a message shows the refusal; the stream
  request returns 403.
  depends-on: T22, T35

## Automate (Umbraco.AI.Automate)

- [ ] **T37** - story: GP-14 (AC1-AC4). Add `Security/AutomateWorkspaceAgentUserGroupResolver.cs`
  (kind `automate-workspace`): checks the caller can access the workspace, then gets its service
  account through `IWorkspaceServiceAccountResolver` and returns that user's group keys. Register
  it in the Automate Composer. Raise the `Umbraco.Automate.Core` floor to `[18.5.0, 18.999.999)`
  (architecture guess 3).
  Acceptance: `Umbraco.AI.Automate.Tests.Unit` specs: service account groups returned; no access
  → refusal; no service account → "no user".
  depends-on: T20

- [ ] **T38** - story: GP-15 (AC1, AC2), GP-16 (AC1-AC3). `Actions/RunAgentAction.cs`: before
  running, check the service account can use the agent; on failure, fail with category
  `Authentication` and the reason text from R18; no service account and a limited agent → fail
  without running (R19). Implement `IPublishValidatableStepType`: publishing fails naming the step
  and agent when the service account can't use it.
  Acceptance: unit specs for each criterion.
  depends-on: T16, T37

- [ ] **T39** - **wire: Automate checks into the demo site.** story: GP-14, GP-15, GP-16. On the
  demo site (it has Umbraco.Automate), a workspace whose service account is in "Automation".
  Acceptance: the "Run agent" picker lists an agent limited to "Automation" and hides one limited
  to "Editors" while the builder is in "Editors"; publishing with the "Editors" agent fails naming
  the step; limiting a published step's agent away and running gives the R18 reason in the run log.
  depends-on: T26, T38

## Deploy (Umbraco.AI.Agent.Deploy, Umbraco.AI.Prompt.Deploy)

- [ ] **T40** - story: GP-17 (AC1, AC2, AC4, AC5). `Artifacts/AIAgentArtifact.cs` gains
  `AllowedUserGroups` (aliases). `UmbracoAIAgentServiceConnector` maps keys to aliases on export
  and aliases to the target's keys on import, through the same code path as tool permission
  groups. A missing alias fails the import with a message naming it, before anything is saved.
  Acceptance: `Umbraco.AI.Agent.Deploy.Tests.Unit` specs for each criterion.
  depends-on: T14. parallel-group: E

- [ ] **T41** - story: GP-17 (AC3, AC4, AC5). Same for `AIPromptArtifact` and
  `UmbracoAIPromptServiceConnector`.
  Acceptance: `Umbraco.AI.Prompt.Deploy.Tests.Unit` specs.
  depends-on: T28. parallel-group: E

- [ ] **T42** - **wire: groups through a real Deploy export and import.** story: GP-17. The demo
  site has no Umbraco.Deploy: add it locally for this check only, don't commit it.
  Acceptance: exporting an agent and a prompt limited to a custom group writes the alias into
  their artifacts; importing on a site with that group maps to its key; importing where the group
  is missing fails naming the alias.
  depends-on: T40, T41

## Shipping

- [ ] **T43** - story: GP-1 to GP-17 (shipping). Raise cross-product floors in
  `Directory.Packages.props` to the versions that ship this feature: Agent, Prompt and Copilot
  Workspace on `Umbraco.AI.Core`/`Web` (they use the internal permission service); Automate,
  Copilot Workspace and Agent.Deploy on `Umbraco.AI.Agent.Core`; Prompt.Deploy on the Prompt
  packages.
  Acceptance: each touched product builds with `UseProjectReferences=false` against a local pack.
  depends-on: T22, T32, T36, T39, T42

- [ ] **T44** - story: GP-1 to GP-17 (shipping). User docs in `umbraco/UmbracoDocs`, `18/ai-in-umbraco/`:
  the AI rights, the agent and prompt Access field, and Automate's service account rule. Through
  `umbraco-docs-pr`, as a separate PR.
  Acceptance: docs PR open, Vale clean, linked from the code PR.
  depends-on: T27, T34, T39

- [ ] **T45** - story: GP-1 to GP-17 (shipping). After the v18 PR is approved, port to `v17/dev`
  and the v19 line through `change-backport`, one worktree and PR per line. Reuse the same
  migration IDs on every line. Check the `Umbraco.Automate.Core` floor that matches 18.5.0 on
  v17 and v19 (architecture guess 3 left it unchecked).
  Acceptance: draft PRs open on both lines, CI green.
  depends-on: T43
