# Architecture: Granular permissions

Area rights are CMS user group permissions ("fallback permissions"), ticked on the CMS's own
user group screen and checked by one internal `AIPermissionService`. Who can use an agent or
prompt is a list of user group keys saved on the agent or prompt, checked by that same service
inside `IAIAgentService` and `IAIPromptService`.

![Access check flow](./diagrams/access-check-flow.svg)

> [!NOTE]
> **5 guesses, approved as built on 25-11-2026.**
>
> 1. The "who can use it" list is a JSON column, `AllowedUserGroupIds`, on the agent and
>    prompt tables, filtered with `LIKE` the way `SurfaceIds` already is.\
>    *If not:* a join table per product (`umbracoAIAgentUserGroup`, `umbracoAIPromptUserGroup`).
>    More migration work, but plain SQL joins.
> 2. The Automate step picker asks the agent list endpoint for the agents of a given
>    workspace's service account (`onBehalfOf=automate-workspace:<id>`). A new public
>    collection, `IAIAgentUserGroupResolver`, turns that into group keys. Umbraco.AI.Automate
>    adds the one resolver it needs.\
>    *If not:* Umbraco.AI.Automate ships its own list endpoint and its own picker element.
>    That means a client project in Umbraco.AI.Automate, which has none today.
> 3. Umbraco.AI.Automate raises its `Umbraco.Automate.Core` floor from 18.0.0 to 18.5.0, plus
>    the matching floors on v17 and v19 (not yet checked). 18.5.0 is the first version that
>    passes `workspaceId` to field editors and has the publish check hook.\
>    *If not:* the picker shows the editor's agents and nothing is checked at publish. The step
>    still fails at run time with a clear reason.
> 4. The way around the limit stays open: a user runs an agent that starts an automation, and
>    that automation runs an agent the user can't use, as the service account. The service
>    account is who acts in Automate by design. Admins control it through the workspace's
>    groups.\
>    *If not:* Automate has to carry the person who started the chain through every trigger
>    and check them as well. That is Automate-wide work, best split into its own feature.
> 5. Test runs check the agent limit against the groups of the person running the test, the
>    same as any other run.\
>    *If not:* test runs skip the limit, so anyone with "Manage tests" can run any agent
>    through a test.

## Decisions

- **CMS fallback permissions for area rights**, not our own table or screen. The CMS already
  shows a package's `entityUserPermission` manifests on the user group screen, saves verbs it
  doesn't know, and exposes them on the server (`IReadOnlyUserGroup.Permissions`) and the client
  (`Umb.Condition.UserPermission.Fallback`). Checked on v17.8.2, v18.3.2 and v19.0.0-rc1. The
  shapes are the same on all three.
- **One right per area and action, named `Uai.<Area>.<Action>`**, matching the package's `Uai.` aliases (the `Umb.` prefix is the CMS's). For example
  `Uai.Connection.Manage` and `Uai.AuditLog.View`. A later split into Create, Update and
  Delete adds new verbs and one upgrade step that grants them to groups that hold `Manage`. No
  existing verb changes meaning.
- **One internal `AIPermissionService`** in `Umbraco.AI.Core`, shared with Agent, Prompt and
  Copilot Workspace through `InternalsVisibleTo`. The verb names and policy names are public
  constants, since they are stored and used by other code. The service stays internal.
- **Management API checks are authorization policies, one per right.** Each policy holds the
  existing AI section requirement plus a new `AIPermissionRequirement(verb)`. A write controller
  swaps `SectionAccessAI` for its area's policy. Endpoints that are open today stay open (#509).
- **Agent and prompt limits are checked inside the services**, not in each endpoint. Run
  methods refuse, and list methods filter in the existing query. Every caller is covered:
  Copilot, Copilot Workspace, automatic selection, the agent run endpoints, prompt execute,
  Automate and test runs.
- **Changing an agent's or prompt's user group list follows the CMS's rule for assigning
  user groups** (`UserGroupService`, 18.3.2): a non-admin can only add or remove groups they
  belong to, and only on an item they can already use. The services check it on save, so the
  API can't skip it. The agent's existing tool rules per group follow the same rule (SPEC R22):
  a non-admin only changes the rows of their own groups. Rollback goes through
  `SaveAgentAsync`, so it is checked too. Rejected: leaving it to Manage agents alone, which let a manager give
  themselves any agent. Rejected: limiting the field to the Users section, which still let a
  non-admin user manager add their own group.
- **The limit is separate from Scope.** Scope is where an agent appears. The limit is who may
  use it. Both must pass.
- **Store user group keys, map them by alias in Deploy.** The CMS gives built-in groups the
  same key on every site, but custom groups get a random key per site. The Agent Deploy
  connector already maps tool permission group keys this way. It's the same code path.
- **No CMS granular (per-item) permissions for the limit.** The CMS table
  `umbracoUserGroup2GranularPermission` has a foreign key to `umbracoNode`, so it can't hold an
  agent's or prompt's key. Per-item rights later would need our own table either way.
- **Upgrade grants by raw SQL in package migrations,** as `AddAISectionToAdminGroup` does, with
  clearing the CMS's cached user groups and users (`AppCaches`) so the new rights show at once,
  without `RebuildCache`'s full content cache rebuild. Each product grants its own verbs. Each
  step skips a group that already holds any of its verbs, as the CMS's own
  `AddElementContainerPermissions` does, so a re-run neither duplicates nor turns rights back on.
- **A deleted user group stays in a saved limit and matches no one.** Removing it on delete
  would turn a limit of one group into "everyone".
- **The access field uses the CMS user group picker (`umb-user-group-input`)**, the same
  modal the agent's tool permissions already open.

## What changes

**Server: Umbraco.AI (core)**

```text
Umbraco.AI/src/
├── Umbraco.AI.Core/
│   ├── Security/
│   │   ├── AIPermissions.cs                  # public verb constants, e.g. Uai.Connection.Manage
│   │   ├── IAIPermissionService.cs           # internal: HasPermission, CanUse, current user's groups
│   │   ├── AIPermissionService.cs
│   │   └── AIPermissionDeniedException.cs    # internal: a service save refused by R12/R22; rollback maps it to 403
│   └── Migrations/
│       ├── UmbracoAIMigrationPlan.cs         # adds the grant step after MigrateAISectionAlias
│       └── GrantAIPermissionsToSectionGroups.cs  # core verbs to groups with the AI section + admin
└── Umbraco.AI.Web/
    ├── Authorization/
    │   ├── AIAuthorizationPolicies.cs        # new public policy names, one per right
    │   ├── AIPermissionRequirement.cs        # requirement holding one verb
    │   └── AIPermissionAuthorizationHandler.cs
    ├── Configuration/UmbracoBuilderExtensions.cs  # registers the new policies
    └── Api/Management/
        ├── Connection|Profile|Context|Guardrail|Test|TestRun|Settings/Controllers/*  # write endpoints → area policy
        ├── AuditLog/Controllers/*            # reads → View logs; delete, cleanup → see SPEC
        ├── Analytics/Usage/Controllers/*     # → View analytics
        └── Versioning/Controllers/EntityVersionHistoryController.cs  # rollback: right picked by entity type
```

**Server: Umbraco.AI.Agent and Umbraco.AI.Prompt**

```text
Umbraco.AI.Agent/src/
├── Umbraco.AI.Agent.Core/
│   ├── Agents/AIAgent.cs                     # AllowedUserGroupIds
│   ├── Agents/AIAgentService.cs              # filter lists, refuse runs, manager sees all, group-list save rule
│   ├── Agents/IAIAgentUserGroupResolver.cs   # public: onBehalfOf kind → group keys
│   ├── Agents/Selection/AIAgentSelectionService.cs  # drop agents the user can't use
│   ├── Versioning/AIAgentVersionableEntityAdapter.cs  # snapshot includes the new list
│   └── Migrations/
│       ├── UmbracoAIAgentMigrationPlan.cs    # new package migration plan
│       └── GrantAIAgentPermissions.cs        # Uai.Agent.Manage to AI section groups + admin
├── Umbraco.AI.Agent.Persistence/Agents/      # entity column, factory, repository filter
├── Umbraco.AI.Agent.Persistence.SqlServer/Migrations/  # UmbracoAIAgent_AddAllowedUserGroupIds
├── Umbraco.AI.Agent.Persistence.Sqlite/Migrations/     # UmbracoAIAgent_AddAllowedUserGroupIds
└── Umbraco.AI.Agent.Web/Api/Management/Agent/
    ├── Controllers/AllAgentController.cs     # includeRestricted, onBehalfOf
    ├── Controllers/ByIdOrAliasAgentController.cs  # 404 when not usable, onBehalfOf
    ├── Controllers/Create|Update|DeleteAgentController.cs  # → Manage agents policy
    └── Models/*, Mapping/AgentMapDefinition.cs     # allowedUserGroupIds

Umbraco.AI.Prompt/src/
├── Umbraco.AI.Prompt.Core/
│   ├── Prompts/AIPrompt.cs                   # AllowedUserGroupIds
│   ├── Prompts/AIPromptService.cs            # filter lists, refuse execute, manager sees all, group-list save rule
│   └── Migrations/                           # new plan + GrantAIPromptPermissions
├── Umbraco.AI.Prompt.Persistence/Prompts/    # entity column, factory, repository filter
├── Umbraco.AI.Prompt.Persistence.SqlServer|Sqlite/Migrations/  # UmbracoAIPrompt_AddAllowedUserGroupIds
└── Umbraco.AI.Prompt.Web/Api/Management/Prompt/  # same endpoint changes as agents
```

**Server: Copilot Workspace, Deploy and Automate**

```text
Umbraco.AI.Agent.Copilot.Workspace/src/…/StreamConversationAGUIController.cs  # refused run → 403, history still readable
Umbraco.AI.Agent.Deploy/src/…/UmbracoAIAgentServiceConnector.cs     # AllowedUserGroupIds: key ↔ alias
Umbraco.AI.Agent.Deploy/src/…/Artifacts/AIAgentArtifact.cs          # AllowedUserGroups (aliases)
Umbraco.AI.Prompt.Deploy/src/…/UmbracoAIPromptServiceConnector.cs   # same mapping for prompts
Umbraco.AI.Prompt.Deploy/src/…/Artifacts/AIPromptArtifact.cs
Umbraco.AI.Automate/src/Umbraco.AI.Automate/
├── Actions/RunAgentAction.cs                 # check before run; IPublishValidatableStepType
└── Security/AutomateWorkspaceAgentUserGroupResolver.cs  # "automate-workspace" resolver
```

**Backoffice**

```text
Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/
├── permissions/
│   ├── constants.ts                          # verb constants, UAI_PERMISSION_ENTITY_TYPE
│   └── manifests.ts                          # entityUserPermission per core right
├── <area>/menu/manifests.ts                  # + Umb.Condition.UserPermission.Fallback
├── <area>/collection/action|bulk-action/manifests.ts  # same condition
├── <area>/entity-actions/manifests.ts        # same condition on create and delete
├── <area>/workspace/<area>/manifests.ts      # same condition on the workspace and its save action
├── <area>/collection/manifests.ts            # same condition on the collection (list) view
├── core/components/user-group-settings-list/user-group-settings-list.element.ts  # rows read-only outside editableUserGroupIds; picker limited to them
└── lang/*                                    # labels, descriptions, group headings
Umbraco.AI.Agent/src/…/Client/src/
├── agent/permissions/manifests.ts            # Manage agents, same entity type, group "add-ons"
├── agent/workspace/agent/views/agent-availability-workspace-view.element.ts  # Access box
├── agent/workspace/agent/views/agent-governance-workspace-view.element.ts  # passes the user's editable groups
├── agent/components/user-group-tool-permissions/user-group-tool-permissions.element.ts  # forwards them
├── agent/components/agent-picker/agent-picker.element.ts  # passes onBehalfOf
├── property-editors/agent-picker/property-editor-ui-agent-picker.element.ts  # reads workspaceId config
└── agent/menu|collection|entity-actions|workspace manifests  # same condition
Umbraco.AI.Prompt/src/…/Client/src/
├── prompt/permissions/manifests.ts           # Manage prompts
├── prompt/workspace/prompt/views/prompt-availability-workspace-view.element.ts  # Access box
└── prompt/menu|collection|entity-actions|workspace manifests  # same condition
```

- **Cache:** the grant steps clear the cached user groups and users only (not `RebuildCache`).
  Lists aren't cached today, so nothing new to clear.
- **Version history:** the agent and prompt snapshots include `AllowedUserGroupIds`, so a
  rollback restores it.
- **Events:** none added. `AIAgentSavingNotification` and `AIPromptSavingNotification` carry
  the new property as part of the entity.
- **Generated clients:** regenerate the Agent and Prompt OpenAPI clients for the new
  properties and query parameters.

Not touched: Copilot Workspace's own section right (separate section, separate policy).
Knowledge set endpoints (read-only today, so the right only hides the menu item). CMS
granular permissions (see Decisions). Search indexing (agents and prompts aren't indexed).
Umbraco.AI.Deploy core connectors (no area right is stored on an entity).

## Data model

Both tables gain one column. No new tables.

```text
umbracoAIAgent
Column               Type            Null  Change
AllowedUserGroupIds  nvarchar(max)   yes   new: JSON array of user group keys

umbracoAIPrompt
Column               Type            Null  Change
AllowedUserGroupIds  nvarchar(max)   yes   new: JSON array of user group keys
```

- **Empty means everyone.** `NULL` and `[]` both mean no limit. Every existing row is `NULL`
  after the migration.
- **Keys are written lowercase, in "D" format,** so the `LIKE '%"<key>"%'` filter matches on
  both SQL Server and SQLite. The filter ORs one `LIKE` per group of the user. A user is in a
  handful of groups.
- **No foreign key to the CMS's `umbracoUserGroup`**, which sits outside the EF context. That
  is also why a deleted group's key stays behind.
- **SQLite:** `TEXT`. Same EF model, separate migration per provider, as for `SurfaceIds`.
