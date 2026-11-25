[Plan folder](https://github.com/umbraco/Umbraco.AI/tree/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions) | [Build log](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/BUILD-LOG.md) | [Decision log](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/DECISION-LOG.md)

Docs: [umbraco/UmbracoDocs PR](https://github.com/umbraco/UmbracoDocs/pull/8823) (link added once opened)

> [!IMPORTANT]
> **4 choices were made without you.** Please check each one.
>
> **1. Design: Automate reads Agent internals**\
> Plan said: Nothing about Automate using Agent's internal types. It planned `InternalsVisibleTo` only from Core to Agent, Prompt and Copilot Workspace.\
> Built: [Agent.Core gives `InternalsVisibleTo` to `Umbraco.AI.Automate`](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Umbraco.AI.Agent.Core.csproj#L55), so the Run agent step uses the internal `IAIAgentUnrestrictedReader` to tell "missing" from "limited" and get the alias for the R18 message. Automate's Agent floor is raised to cover it.
>
> - **A. Keep it.** Same pattern as the other internal contracts in this PR; the floor stops a mismatched install. *(recommended)*
> - B. Add a public alias lookup on `IAIAgentService` and drop the `InternalsVisibleTo`. One more public member to support, but no hidden contract.
>
> **2. Design: New public API surface**\
> Plan said: Public verb constants, policy names and `IAIAgentUserGroupResolver`. Everything else internal.\
> Built: Also public: [`IAIAgentUserGroupResolutionService`](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/IAIAgentUserGroupResolutionService.cs#L16) (public controllers take it), `AIAgentNotAvailableException`, `AIPromptNotAvailableException`, new `includeRestricted`, explicit-group and `Get…ForUseAsync` members on `IAIAgentService` / `IAIPromptService` (fail-closed defaults), and frontend exports `withAreaPermission`, `createAreaPermissionCondition`, `createRegisteredGroupPermissionCondition`, `UAI_AREA_GATED_MANIFEST_TYPES`, the weight constants and `UaiStreamRequestOptions`.
>
> - **A. Keep them public.** Add-ons need the frontend helpers, and the service members are the only way for a caller to get a 403 instead of a 404. *(recommended)*
> - B. Make the resolution service internal again; controllers resolve it through `StaticServiceProvider`. Keeps the rest.
>
> **3. Behavior: Copilot no longer retries a refused stream forever**\
> Plan said: Nothing about the stream client. A refused run was only meant to return 403.\
> Built: [The shared Agent transport makes any 4xx final](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/transport/stream-request-options.ts#L43) and shows the ProblemDetails text. Before, the generated client retried any non-OK stream forever, so a refusal left Copilot on "Thinking..." and re-sent the turn 50+ times. 5xx and network errors still retry with no cap.
>
> - **A. Keep it in this PR**, and port it with the feature. Without it, every refusal this PR adds hangs the chat. *(recommended)*
> - B. Split it into its own PR so v17 and v19 get it sooner, then rebase this one on it.
>
> **4. Behavior: An old Deploy artifact clears a local limit**\
> Plan said: Groups travel by alias; a missing group fails the import (R20, R21). Nothing about artifacts that predate the field.\
> Built: [`AllowedUserGroups` defaults to empty](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI.Agent.Deploy/src/Umbraco.AI.Agent.Deploy/Artifacts/AIAgentArtifact.cs#L61), so importing an artifact made before this release onto a site that set a limit opens the agent or prompt to everyone. "Source wins", like every other field.
>
> - **A. Keep "source wins".** Old artifacts only exist until the source site upgrades, and Deploy rights already decide who moves changes. *(recommended)*
> - B. Treat a missing field as "keep the target's list", like a `PUT` without `allowedUserGroupIds`.

## Why the change

Anyone with the AI section could change everything in it, and every backoffice user could see and run every agent and prompt; now admins can grant each AI area per user group and limit each agent and prompt to chosen groups.

## What it looks like

**Where to find it:**
- Users › User Groups › a group › **AI** box (Configuration, Monitoring, Add-ons)
- AI › any area: menu items, create, delete and save hide without that area's right
- AI › Add-ons › Agents › an agent › Availability › **Access**
- AI › Add-ons › Prompts › a prompt › Availability › **Access**
- AI › Add-ons › Agents › an agent › Governance › User Group Permissions (rows for other groups read-only)
- Automation › a workspace › an automation › Run AI Agent step › Agent picker

**Who can see it:**
- The AI rights box: anyone who can edit user groups (Users section).
- Each AI area: users with the AI section **and** that area's right. After the upgrade, every group that had the AI section has every right.
- The Access box: users with Manage agents or Manage prompts. A non-admin can only change groups they belong to, on an item they can already use.

**User group screen**

| Before | After |
|---|---|
| ![User group before](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/user-group-before.png?raw=true) | ![User group after](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/user-group-after.png?raw=true) |

The Add-ons group, with both add-ons installed:

![Add-ons rights](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t33-user-group-manage-prompts.png?raw=true)

**AI section for a user with only Manage contexts**

| Before | After |
|---|---|
| ![AI menu before](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/ai-menu-limited-before.png?raw=true) | ![AI menu after](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/ai-menu-limited-t12-after.png?raw=true) |

**Agent Access box**

| Before | After |
|---|---|
| ![Agent availability before](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/agent-availability-before.png?raw=true) | ![Agent access as admin](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t25-agent-access-admin.png?raw=true) |

<details>
<summary>Access box states (non-admin, read-only, deleted group, last group, no Choose)</summary>

| Non-admin | Read-only |
|---|---|
| ![Non-admin](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t25-agent-access-non-admin.png?raw=true) | ![Read-only](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t25-agent-access-readonly.png?raw=true) |

| Deleted group | Last group |
|---|---|
| ![Deleted group](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t25-agent-access-deleted.png?raw=true) | ![Last group](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t25-agent-access-last-group.png?raw=true) |

No group the non-admin may add, so Choose is hidden:

![No Choose](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/access-box-no-choose.png?raw=true)

</details>

**Prompt Access box**

| Before | After |
|---|---|
| ![Prompt availability before](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/prompt-availability-before.png?raw=true) | ![Prompt access as admin](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t33-prompt-access-admin.png?raw=true) |

**Agent tool rules per group, as a non-admin**

| Before | After |
|---|---|
| ![Governance before](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/agent-governance-before.png?raw=true) | ![Governance non-admin](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t48-agent-governance-non-admin.png?raw=true) |

**Where a limit shows up for users**

<details>
<summary>Copilot, Copilot Workspace, prompt property action, Automate</summary>

Copilot hides an agent the user can't use:

![Copilot agents](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t27-copilot-agents-non-admin.png?raw=true)

A Workspace chat whose agent was limited away refuses the next message:

![Workspace refusal](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t36-workspace-refusal.png?raw=true)

Prompt property action, member vs outsider:

| Member | Outsider |
|---|---|
| ![Member](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t34-prompt-property-action-member.png?raw=true) | ![Outsider](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/t34-prompt-property-action-outsider.png?raw=true) |

Automate step picker lists only the service account's agents:

![Automate picker](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/automate-picker-filtered.png?raw=true)

Publish refused:

![Automate publish refused](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/automate-publish-refused.png?raw=true)

Run refused after the agent was limited:

![Automate run refused](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/screenshots/automate-run-refused.png?raw=true)

</details>

## How to verify

On the v18 demo site (admin@example.com / password1234):

1. Start the site on an existing database. Open Users › User Groups › Administrators.\
   **See:** an AI box with 6 Configuration, 3 Monitoring and 2 Add-ons rights, all on.
2. Create a group "Context managers" with the AI and Content sections and only **Manage contexts** on. Add a test user to it and log in as them.\
   **See:** AI shows only Configuration › Contexts. No Monitoring or Add-ons heading. Creating, saving and deleting a context works.
3. As that user, call `POST /umbraco/ai/management/api/v1/connections`.\
   **See:** 403, nothing created. `GET profiles` still returns 200.
4. As admin, turn on **Manage agents** for Context managers. As the test user, open an agent's Settings and the profile picker.\
   **See:** every profile is listed, though they can't manage profiles.
5. As admin, open AI › Add-ons › Agents › Legal Specialist › Availability › Access › Choose › Editors. Save and reload.\
   **See:** Editors is kept.
6. As admin (not in Editors), open Copilot in Content.\
   **See:** Legal Specialist isn't offered. There is no admin bypass. Running it directly gives 403 "Agent not available".
7. As the test user (Manage agents, not in Editors), open Legal Specialist.\
   **See:** the agent opens, the Access box is read-only. Add the test user to Editors: they now see it in Copilot.
8. On the Governance tab as the test user, look at User Group Permissions.\
   **See:** only rows for their own groups can be edited or removed. Add lists only their own groups.
9. Limit the Summarize prompt to Editors. As a user outside Editors, open a TextBox property's actions.\
   **See:** Summarize isn't offered. `POST prompts/summarize/execute` gives 403 "Prompt not available".
10. In a Copilot Workspace chat that picked an agent, limit that agent away from the user and send a message.\
    **See:** "The specified agent is not available to you.", one request, no retry, "Thinking..." clears.
11. Put an Automate workspace's service account in a group "Automation" only. Limit one Automations-surface agent to Automation and another to Editors.\
    **See:** the Run AI Agent picker lists only the first. Publishing with the second fails, naming the step and agent. Limiting the first to Editors after publish makes the run fail with category Authentication and the R18 text.

## Good to know

- **Upgrade grants every AI right** to each group that has the AI section (three package migrations, one per product). Re-runs add nothing and never turn a right back on.
- **Two new columns:** `AllowedUserGroupIds` on the agent and prompt tables. Backports must copy these migration files, not regenerate them.
- **Release notes:** the old `IAIAgentService` and `IAIPromptService` getters now hide items the current user can't use. Headless callers see only unlimited ones.
- **Version floors** for Core, Agent, Prompt and Deploy are set to the next patch for the internal contracts. `/release-prepare` must leave them at this feature's release version.
- **Umbraco.AI.Automate needs Umbraco.Automate 18.5.0+**, for `workspaceId` in field editors and the publish check.
- **Known gap:** a direct URL to an area the user can't manage shows the CMS's endless spinner, not a "no access" page.
- **Automate resolver copies Automate's access rule** (admin, or a shared group) because `WorkspaceAccessHandler` is internal. If Automate changes its rule, this must follow.
- **No automated test for R15** (old Workspace chat stays readable). Covered by a structural guard and a live smoke; the staged spec stays in `specs/`. An issue gets raised at feature close.
- **Pre-existing, not from this PR:** on SQLite, a Deploy disk import of any AI entity fails with "database table is locked" when AI shares the CMS connection.
- **Pre-existing follow-up:** `AITestRunner` still grades an error transcript, so a test can pass when the agent never ran.
- **Ports to v17 and v19** follow once this PR is approved, reusing the same migration IDs.

## Change outline

How every request finds out what the user may do. Highlighted boxes are new or changed.

![Access check flow](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/diagrams/access-check-flow.svg?raw=true)

The 11 rights and their verbs are listed in [SPEC.md › Rights](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/docs/plans/granular-permissions/SPEC.md#rights). Each product owns its own verbs and policies.

**Data model.** Both tables gain one column. `NULL` and `[]` mean everyone.

**Table `umbracoAIAgent`** (migration `UmbracoAIAgent_AddAllowedUserGroupIds`)

| Column | Type | Change | Note |
|---|---|---|---|
| `AllowedUserGroupIds` | `nvarchar(max)?` | **New** | JSON array of group keys |

**Table `umbracoAIPrompt`** (migration `UmbracoAIPrompt_AddAllowedUserGroupIds`)

| Column | Type | Change | Note |
|---|---|---|---|
| `AllowedUserGroupIds` | `nvarchar(max)?` | **New** | JSON array of group keys |

**Endpoints.** All routes start with `/umbraco/ai/management/api/v1` and still need the AI section.

- **Changed** create, update, delete of connections, profiles, contexts, guardrails, tests, settings: need that area's manage right
- **Changed** `POST connections/{idOrAlias}/test`, test runs, set baseline: need the area's manage right
- **Changed** audit log list and detail: need View logs. Delete and cleanup need Manage settings
- **Changed** analytics usage endpoints: need View analytics
- **Changed** `POST versions/{entityType}/{id}/{version}/rollback`: needs the right the entity's adapter declares; none declared means refused
- **Changed** version snapshot and compare: 404 for an agent or prompt the user can't see
- **Changed** `GET agents`: new `includeRestricted` and `onBehalfOf` query values
- **Changed** `GET agents/{idOrAlias}`: new `onBehalfOf`; 404 when the user can't use it and can't manage agents
- **Changed** `POST`/`PUT agents`: `allowedUserGroupIds`; `PUT` without it keeps the saved list; 403 on an R12 or R22 breach
- **Changed** agent run, stream, stream-agui, suggest-starters, Workspace stream: 403 "Agent not available"
- **Changed** `GET`/`POST`/`PUT prompts`: same as agents, without `onBehalfOf`
- **Changed** `POST prompts/{idOrAlias}/execute`: 403 "Prompt not available"

**Server code.** Where the new parts live.

```diff
 Umbraco.AI/src/
 ├── Umbraco.AI.Core/
+│   ├── Security/AIPermissions.cs               # new: core verb constants
+│   ├── Security/AIPermissionService.cs         # new, internal: HasPermission, CanUse
+│   ├── Security/AIUserGroupChangeRule.cs       # new, internal: R12/R22 save rule
+│   ├── Migrations/GrantAIPermissionsToSectionGroups.cs  # new: upgrade grant
+│   ├── Migrations/AIPermissionGrant.cs         # new, internal: shared grant SQL
 │   └── Versioning/IAIVersionableEntityAdapter.cs  # changed: RollbackPermission
 └── Umbraco.AI.Web/Authorization/
     ├── AIAuthorizationPolicies.cs              # changed: one policy per right
+    ├── AIPermissionRequirement.cs              # new
+    └── AIPermissionAuthorizationHandler.cs     # new
 Umbraco.AI.Agent/src/
 ├── Umbraco.AI.Agent.Core/
+│   ├── Security/AIAgentPermissions.cs          # new: Uai.Agent.Manage
 │   ├── Agents/AIAgentService.cs                # changed: filter, refuse, save rule
+│   ├── Agents/IAIAgentUserGroupResolver.cs     # new, public: onBehalfOf kinds
+│   ├── Agents/IAIAgentUnrestrictedReader.cs    # new, internal
+│   ├── Agents/IAIAgentUnrestrictedWriter.cs    # new, internal: Deploy import only
+│   └── Migrations/GrantAIAgentPermissions.cs   # new
 ├── Umbraco.AI.Agent.Persistence/Agents/AIAgentUserGroupFilter.cs  # new: LIKE filter
 └── Umbraco.AI.Agent.Web/Authorization/AIAgentAuthorizationPolicies.cs  # new
 Umbraco.AI.Prompt/src/                          # same shape as Agent, no resolver
 Umbraco.AI.Agent.Copilot.Workspace/src/         # changed: stream refuses with 403
 Umbraco.AI.Deploy/src/…/UmbracoAIUserGroupMapper.cs  # new, internal: key ↔ alias
 Umbraco.AI.Agent.Deploy/src/…/AIAgentArtifact.cs     # changed: AllowedUserGroups
 Umbraco.AI.Prompt.Deploy/src/…/AIPromptArtifact.cs   # changed: AllowedUserGroups
 Umbraco.AI.Automate/src/Umbraco.AI.Automate/
 ├── Actions/RunAgentAction.cs                   # changed: check, publish validation
+├── Security/AgentAccessChecker.cs              # new
+└── Security/AutomateWorkspaceAgentUserGroupResolver.cs  # new: automate-workspace
```

**Backoffice registrations**

| Change | Type | Name | Where it appears |
|---|---|---|---|
| **New** | User permission (x11) | AI rights | User group screen, AI box |
| **Changed** | Menus, collections, workspaces, actions | Each AI area | Hidden without its right |
| **New** | Condition | Permission group | AI sidebar headings |
| **New** | Element | Access box | Agent and prompt › Availability |
| **Changed** | Property | User Group Permissions | Agent › Governance |
| **Changed** | Property editor | Agent picker | Automate Run AI Agent step |
| **Changed** | Dashboard | AI welcome | Create a Connection needs its right |

**Public service members.** Added with defaults that fail closed, so other implementations keep compiling.

```diff
 public interface IAIAgentService
 {
     Task<AIAgent?> GetAgentAsync(Guid id, …);   // changed: hides limited agents
+    Task<AIAgent?> GetAgentAsync(Guid id, bool includeRestricted, …);
+    Task<AIAgent?> GetAgentAsync(Guid id, IReadOnlyCollection<Guid> userGroupIds, …);
+    Task<AIAgent?> GetAgentForUseAsync(Guid id, …);        // throws when limited
+    Task<AIAgent?> GetAgentForUseByAliasAsync(string alias, …);
     // same pattern for by-alias and paged reads; IAIPromptService mirrors it
 }
```

<details>
<summary>Checked and fine (15)</summary>

- `PUT` without `allowedUserGroupIds` keeps the saved list; signed off 25-11-2026 ([AgentMapDefinition.cs:75](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web/Api/Management/Agent/Mapping/AgentMapDefinition.cs#L75)).
- Deploy imports save user groups and tool rules whoever is signed in; signed off 25-11-2026 ([IAIAgentUnrestrictedWriter.cs:8](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/IAIAgentUnrestrictedWriter.cs#L8)).
- Access box is our own element, not `umb-user-group-input`; keyboard and screen-reader parity checked, signed off 25-11-2026 ([user-group-access-input.element.ts](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/core/components/user-group-access-input/user-group-access-input.element.ts)).
- All 10 guesses in SPEC.md and ARCHITECTURE.md approved as built, 25-11-2026.
- Agent and Prompt rights and policies live in their own packages, registered through public `AIAuthorizationPolicies.AddPermissionPolicy`, per the no-hand-kept-lists rule ([AIAuthorizationPolicies.cs:88](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI/src/Umbraco.AI.Web/Authorization/AIAuthorizationPolicies.cs#L88)).
- Rollback needs the right each adapter declares; none means refused ([IAIVersionableEntityAdapter.cs:37](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI/src/Umbraco.AI.Core/Versioning/IAIVersionableEntityAdapter.cs#L37)).
- Grant migrations skip a group holding any of their verbs and clear only user caches, not the content cache ([GrantAIPermissionsToSectionGroups.cs:27](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI/src/Umbraco.AI.Core/Migrations/GrantAIPermissionsToSectionGroups.cs#L27)).
- Version history snapshot and compare now 404 for a hidden agent or prompt, closing a read leak found in review.
- Old public controller actions kept as `[NonAction]` `[Obsolete]` overloads (v20); `dotnet pack` package validation clean.
- `onBehalfOf` needs only the AI section; the resolver checks workspace access ([AutomateWorkspaceAgentUserGroupResolver.cs:77](https://github.com/umbraco/Umbraco.AI/blob/7917211e5f65b9e08885bc212f70bead1b2ef026/Umbraco.AI.Automate/src/Umbraco.AI.Automate/Security/AutomateWorkspaceAgentUserGroupResolver.cs#L77)).
- The group-list rule limits both adding and removing groups, as SPEC R12 says.
- Tests area hides whole without Manage tests; audit-log bulk delete needs View logs and Manage settings, matching the server.
- The AI welcome dashboard shows "Create a Connection" only with Manage connections.
- New test-only package `Umbraco.Cms.Persistence.Sqlite` in `Umbraco.AI/tests/Directory.Packages.props`; no shipped project references it.
- `AIPromptUserGroupFilter` is a copy of the agent one (~50 lines) rather than a new cross-product dependency.

</details>

🤖 Generated with [Claude Code](https://claude.com/claude-code)
