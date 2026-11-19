# Stories: Granular permissions

**17 stories, 86 criteria.** Story ids (`GP-n`) are used by `PLAN.md` and the spec files, so
never renumber them.

> [!IMPORTANT]
> **1 guess needs your OK.**
>
> 1. The Ready and Done rules below are proposed defaults, not yet agreed.\
>    *If not:* tell me what to add or drop, and every story picks it up.

"AI section user" means a backoffice user whose group has the AI section. "Right" means one of
the 11 `Uai.*` verbs in [SPEC.md](./SPEC.md#rights). "Limited" means an agent or prompt with at
least one user group chosen.

**Area rights (Umbraco.AI)**

- [GP-1](#gp-1-ai-rights-on-the-user-group-screen) AI rights on the user group screen, 4 criteria
- [GP-2](#gp-2-write-endpoints-need-the-areas-right) Write endpoints need the area's right, 7 criteria
- [GP-3](#gp-3-rollback-needs-the-areas-right) Rollback needs the area's right, 4 criteria
- [GP-4](#gp-4-logs-and-analytics-need-a-view-right) Logs and analytics need a view right, 6 criteria
- [GP-5](#gp-5-the-backoffice-hides-an-area-without-its-right) The backoffice hides an area without its right, 6 criteria
- [GP-6](#gp-6-the-upgrade-keeps-every-group-working) The upgrade keeps every group working, 7 criteria

**Agent limits (Umbraco.AI.Agent, Copilot Workspace)**

- [GP-7](#gp-7-choose-who-can-use-an-agent) Choose who can use an agent, 7 criteria
- [GP-8](#gp-8-only-change-an-agents-groups-within-your-own) Only change an agent's groups within your own, 7 criteria
- [GP-9](#gp-9-limited-agents-are-hidden-from-people-outside-them) Limited agents are hidden from people outside them, 7 criteria
- [GP-10](#gp-10-limited-agents-refuse-to-run-for-people-outside-them) Limited agents refuse to run for people outside them, 4 criteria
- [GP-11](#gp-11-copilot-workspace-chats-with-an-agent-you-lost) Copilot Workspace chats with an agent you lost, 2 criteria

**Prompt limits (Umbraco.AI.Prompt)**

- [GP-12](#gp-12-choose-who-can-use-a-prompt) Choose who can use a prompt, 6 criteria
- [GP-13](#gp-13-limited-prompts-are-hidden-and-refused) Limited prompts are hidden and refused, 5 criteria

**Automate (Umbraco.AI.Automate)**

- [GP-14](#gp-14-the-run-agent-picker-shows-the-service-accounts-agents) The "Run agent" picker shows the service account's agents, 4 criteria
- [GP-15](#gp-15-publishing-catches-an-agent-the-service-account-cant-use) Publishing catches an agent the service account can't use, 2 criteria
- [GP-16](#gp-16-a-run-agent-step-fails-clearly-at-run-time) A "Run agent" step fails clearly at run time, 3 criteria

**Deploy (Umbraco.AI.Agent.Deploy, Umbraco.AI.Prompt.Deploy)**

- [GP-17](#gp-17-user-groups-travel-with-agents-and-prompts) User groups travel with agents and prompts, 5 criteria

<details>
<summary>Ready and Done</summary>

**Ready:** the role, what they want and why are stated. Given/When/Then covers the happy path.
What's out of scope is named.

**Done:** every criterion passes as an executable spec, sad paths included. Every touched
product builds (`dotnet build <Product>/<Product>.slnx`, `npm run build:<target>`). Every real
entry point is proven by its wire task through the running demo site. Reviewer PASS. The v17
and v19 ports are tracked.

</details>

---

## GP-1: AI rights on the user group screen

As a **site admin**,
I want to tick AI rights on a user group where I set every other right,
so that I can split up who manages what in the AI section without a separate screen.

**Happy path**

- AC1 — one AI box
  Given Umbraco.AI, Umbraco.AI.Agent and Umbraco.AI.Prompt are installed
  When I open Users > User groups > a group
  Then the group shows one **AI** box with 11 rights, grouped under Configuration, Monitoring
  and Add-ons, with the labels in SPEC.md
- AC2 — saved rights stick
  Given I tick "Manage contexts" on a group and save
  When I reopen the group
  Then "Manage contexts" is ticked
- AC3 — the right is stored as its verb
  Given I tick "Manage contexts" on a group and save
  When the group is read on the server
  Then `IReadOnlyUserGroup.Permissions` contains `Uai.Context.Manage`

**Edge cases**

- AC4 — rights only for installed products
  Given Umbraco.AI.Agent isn't installed
  When I open a user group
  Then the AI box has no "Manage agents" row

Out of scope: our own rights screen. Rights on single items.

---

## GP-2: Write endpoints need the area's right

As a **site admin**,
I want the API to refuse any change to an AI area from a user without its right,
so that hiding a button isn't the only thing between a user and a change.

**Happy path**

- AC1 — with the right, writes work
  Given an AI section user whose group has `Uai.Connection.Manage`
  When they call `POST connections` with a valid body
  Then the response is 201
- AC2 — every listed write endpoint has its right
  Given each write endpoint listed under "Endpoints that now need a right" in SPEC.md (connections,
  profiles, contexts, guardrails, `PUT settings`, audit log delete and cleanup, tests and test
  runs, agents, prompts)
  When it is called by an AI section user whose group has only that endpoint's right
  Then the response isn't 403
- AC3 — read endpoints stay open
  Given an AI section user with no AI rights
  When they call `GET profiles`
  Then the response is 200

**Sad path**

- AC4 — refused without the right
  Given an AI section user whose group has every AI right except `Uai.Connection.Manage`
  When they call `POST connections`
  Then the response is 403
- AC5 — a refused write changes nothing
  Given an AI section user without `Uai.Connection.Manage`
  When they call `DELETE connections/{alias}` for a connection that exists
  Then the connection still exists
- AC6 — the right doesn't replace the section
  Given a user whose group has `Uai.Connection.Manage` but not the AI section
  When they call `POST connections`
  Then the response is 403
- AC7 — audit log delete needs Manage settings (spec guess 1)
  Given an AI section user with `Uai.AuditLog.View` but not `Uai.Settings.Manage`
  When they call the audit log delete endpoint
  Then the response is 403

Out of scope: Copilot Workspace's own section right. Knowledge set endpoints (read-only today).

---

## GP-3: Rollback needs the area's right

As a **site admin**,
I want rolling back an entity to need the same right as saving it,
so that rollback isn't a way to change an area without its right.

**Happy path**

- AC1 — rollback with the right
  Given an AI section user with `Uai.Profile.Manage` and a profile with an older version
  When they call `POST versions/{profile entity type}/{id}/{version}/rollback`
  Then the response is 200

**Sad path**

- AC2 — refused without the area's right
  Given an AI section user with every AI right except `Uai.Profile.Manage`
  When they roll back a profile
  Then the response is 403
- AC3 — a refused rollback changes nothing
  Given an AI section user without `Uai.Profile.Manage`
  When they roll back a profile
  Then the profile's current version is unchanged
- AC4 — the right follows the entity type
  Given an AI section user with `Uai.Profile.Manage` but not `Uai.Agent.Manage`
  When they roll back an agent
  Then the response is 403

---

## GP-4: Logs and analytics need a view right

As a **site admin**,
I want viewing the audit log and analytics to need their own rights,
so that I can keep prompts, answers and usage figures from people who don't need them.

**Happy path**

- AC1 — log list with the right
  Given an AI section user with `Uai.AuditLog.View`
  When they call the audit log list endpoint
  Then the response is 200
- AC2 — analytics with the right
  Given an AI section user with `Uai.Analytics.View`
  When they call the usage summary endpoint
  Then the response is 200

**Sad path**

- AC3 — log list refused
  Given an AI section user without `Uai.AuditLog.View`
  When they call the audit log list endpoint
  Then the response is 403
- AC4 — log detail refused
  Given an AI section user without `Uai.AuditLog.View`
  When they call the audit log detail endpoint
  Then the response is 403
- AC5 — analytics refused
  Given an AI section user without `Uai.Analytics.View`
  When they call the usage summary, time series or breakdown endpoint
  Then the response is 403
- AC6 — menu item hidden
  Given an AI section user without `Uai.AuditLog.View`
  When they open the AI section
  Then there is no "Audit log" menu item

---

## GP-5: The backoffice hides an area without its right

As an **AI section user with some rights**,
I want to see only the areas I can manage,
so that I'm not offered buttons that then fail.

**Happy path**

- AC1 — area shown with its right
  Given an AI section user with `Uai.Context.Manage`
  When they open the AI section
  Then the Contexts menu item, its create button and the editor's save button are shown
- AC2 — pickers keep working (R4)
  Given an AI section user with `Uai.Context.Manage` but not `Uai.Profile.Manage`
  When they open the profile picker in a context
  Then the picker lists the site's profiles

**Sad path**

- AC3 — menu item hidden
  Given an AI section user without `Uai.Connection.Manage`
  When they open the AI section
  Then there is no Connections menu item
- AC4 — entity actions hidden
  Given an AI section user without `Uai.Connection.Manage`
  When they look for create, delete or bulk actions on connections
  Then none are shown
- AC5 — direct URL to the list doesn't open
  Given an AI section user without `Uai.Connection.Manage`
  When they go to the connections collection URL directly
  Then the collection doesn't render
- AC6 — direct URL to the editor doesn't open
  Given an AI section user without `Uai.Connection.Manage`
  When they go to a connection's editor URL directly
  Then the editor doesn't render

Out of scope: a read-only view of an area. The menu updating without a reload.

---

## GP-6: The upgrade keeps every group working

As a **site admin upgrading**,
I want every group that had the AI section to keep doing what it did,
so that the upgrade needs no action from me.

**Happy path**

- AC1 — AI section groups get the core rights
  Given a group with the AI section before the upgrade
  When the Umbraco.AI migration runs
  Then the group has all 9 core rights
- AC2 — admin group gets them too
  Given the admin group's AI section row is added in the same upgrade
  When the Umbraco.AI migration runs
  Then the admin group has all 9 core rights
- AC3 — each product grants its own (R6)
  Given a site with the AI section groups already upgraded, then Umbraco.AI.Agent installed
  When the Umbraco.AI.Agent migration runs
  Then every group with the AI section has `Uai.Agent.Manage`
- AC4 — the prompt product does the same
  Given Umbraco.AI.Prompt is installed
  When its migration runs
  Then every group with the AI section has `Uai.Prompt.Manage`

**Edge cases**

- AC5 — a right turned off stays off (R7)
  Given the migration already ran and an admin turned off `Uai.Connection.Manage` on a group
  When the grant step runs again
  Then the group still lacks `Uai.Connection.Manage`
- AC6 — nothing is added twice (R7)
  Given the migration already ran
  When the grant step runs again
  Then each group holds each AI verb at most once
- AC7 — later groups start with none (R8)
  Given a group given the AI section after the upgrade
  When it is first saved
  Then it has no AI rights

---

## GP-7: Choose who can use an agent

As an **AI section user with Manage agents**,
I want to pick the user groups that can use an agent,
so that an agent built for one team isn't offered to everyone.

**Happy path**

- AC1 — empty by default (R9)
  Given an agent that existed before the upgrade
  When I open its Availability view
  Then the Access field has no groups
- AC2 — choose opens the CMS picker
  Given the Access field on an agent I may change (GP-8)
  When I click Choose
  Then the CMS user group picker modal opens
- AC3 — saved groups stick
  Given I choose the "Editors" group and save
  When I reopen the agent
  Then the Access field shows "Editors"
- AC4 — the API takes the list
  Given an AI section user with `Uai.Agent.Manage`
  When they call `PUT agents/{alias}` with `allowedUserGroupIds: ["<editors key>"]`
  Then `GET agents/{alias}` returns those `allowedUserGroupIds`
- AC5 — rollback restores the list
  Given an agent saved with no groups, then saved with "Editors"
  When I roll it back to the first version
  Then its `allowedUserGroupIds` is empty

**Edge cases**

- AC6 — a deleted group stays (R14)
  Given an agent limited to a group that was then deleted
  When I open its Availability view
  Then the field shows "Deleted user group" with a danger Deleted tag
- AC7 — a deleted group matches no one
  Given an agent limited only to a deleted group
  When any non-manager calls `GET agents`
  Then the agent isn't listed

---

## GP-8: Only change an agent's groups within your own

As a **site admin**,
I want non-admins to only add or remove groups they belong to, on agents they can already use,
so that nobody can give themselves an agent they can't use.

**Happy path**

- AC1 — admins change freely
  Given an admin
  When they set an agent's groups to any groups and save
  Then the save succeeds
- AC2 — a member can add their own group
  Given a non-admin with `Uai.Agent.Manage` in "Editors", and an unlimited agent
  When they add "Editors" and save
  Then the save succeeds
- AC3 — picker shows only their groups
  Given a non-admin with `Uai.Agent.Manage` in "Editors" and "Writers"
  When they click Choose
  Then the picker lists only "Editors" and "Writers"

**Sad path**

- AC4 — can't add someone else's group
  Given a non-admin in "Editors" only
  When they call `PUT agents/{alias}` adding "Translators"
  Then the response is 403
- AC5 — can't edit an agent they can't use
  Given an agent limited to "Translators" and a non-admin manager not in it
  When they open its Availability view
  Then the Access field is read-only
- AC6 — only an admin empties the list (spec guess 5)
  Given an agent limited to "Editors" and a non-admin manager in "Editors"
  When they remove "Editors" and save
  Then the response is 403

- AC7 — a refused save changes nothing
  Given a non-admin in "Editors" only
  When they call `PUT agents/{alias}` adding "Translators" and renaming the agent
  Then the agent keeps its old name and groups

---

## GP-9: Limited agents are hidden from people outside them

As a **backoffice user outside an agent's groups**,
I want not to see that agent anywhere,
so that I'm only offered agents I'm allowed to use.

**Happy path**

- AC1 — listed for members
  Given an agent limited to "Editors" and a user in "Editors"
  When they call `GET agents`
  Then the agent is listed
- AC2 — managers see all in the AI section (R12)
  Given a user with `Uai.Agent.Manage` not in "Editors"
  When they call `GET agents?includeRestricted=true`
  Then the agent limited to "Editors" is listed

**Sad path**

- AC3 — hidden from others
  Given an agent limited to "Editors" and a user not in it
  When they call `GET agents`
  Then the agent isn't listed
- AC4 — includeRestricted ignored without the right
  Given a user without `Uai.Agent.Manage` and not in "Editors"
  When they call `GET agents?includeRestricted=true`
  Then the agent limited to "Editors" isn't listed
- AC5 — detail hidden as not found (spec guess 4)
  Given a user without `Uai.Agent.Manage` and not in "Editors"
  When they call `GET agents/{alias}` for the limited agent
  Then the response is 404
- AC6 — never picked automatically (R11)
  Given an agent limited to "Editors" that best matches the request
  When Copilot picks an agent for a user not in "Editors"
  Then a different agent, or none, is picked
- AC7 — limit and Scope both apply (R13)
  Given an agent the user may use but out of scope for the current surface
  When the surface lists agents
  Then the agent isn't listed

The list filter runs in the same database query as today (SPEC Targets).

---

## GP-10: Limited agents refuse to run for people outside them

As a **site admin**,
I want a run of a limited agent by anyone outside its groups to be refused,
so that hiding it isn't the only guard.

**Happy path**

- AC1 — members can run it
  Given an agent limited to "Editors" and a user in "Editors"
  When they call `POST agents/{alias}/run`
  Then the run starts

**Sad path**

- AC2 — refused on every run endpoint
  Given an agent limited to "Editors" and a user not in it
  When they call `run`, `stream`, `stream-agui`, `suggest-starters` or
  `GET suggest-starters/availability`
  Then the response is 403 `ProblemDetails` titled "Agent not available"
- AC3 — managers are refused too (R10)
  Given a user with `Uai.Agent.Manage` not in "Editors"
  When they run the agent limited to "Editors"
  Then the response is 403
- AC4 — test runs check the runner (architecture guess 5)
  Given a test of an agent limited to "Editors" and a user with `Uai.Test.Manage` not in it
  When they run the test
  Then the agent run is refused

---

## GP-11: Copilot Workspace chats with an agent you lost

As a **Copilot Workspace user removed from an agent's groups**,
I want my old chat to stay readable,
so that I don't lose history, while I can't keep using the agent.

**Happy path**

- AC1 — history still opens (R15)
  Given a saved conversation with an agent the user can no longer use
  When they open the conversation
  Then its messages show

**Sad path**

- AC2 — new message refused
  Given that conversation
  When the user sends a new message
  Then the stream endpoint returns 403 `ProblemDetails` titled "Agent not available"

---

## GP-12: Choose who can use a prompt

As an **AI section user with Manage prompts**,
I want to pick the user groups that can use a prompt,
so that a prompt built for one team isn't offered to everyone.

**Happy path**

- AC1 — empty by default (R9)
  Given a prompt that existed before the upgrade
  When I open its Availability view
  Then the Access field has no groups
- AC2 — saved groups stick
  Given I choose "Editors" and save
  When I reopen the prompt
  Then the Access field shows "Editors"
- AC3 — rollback restores the list
  Given a prompt saved with no groups, then with "Editors"
  When I roll it back to the first version
  Then its `allowedUserGroupIds` is empty

**Sad path**

- AC4 — can't add someone else's group (R12)
  Given a non-admin with `Uai.Prompt.Manage` in "Editors" only
  When they call `PUT prompts/{alias}` adding "Translators"
  Then the response is 403
- AC5 — only an admin empties the list (spec guess 5)
  Given a prompt limited to "Editors" and a non-admin manager in "Editors"
  When they remove "Editors" and save
  Then the response is 403
- AC6 — a deleted group shows as deleted (R14)
  Given a prompt limited to a deleted group
  When I open its Availability view
  Then the field shows "Deleted user group" with a danger Deleted tag

---

## GP-13: Limited prompts are hidden and refused

As a **content editor outside a prompt's groups**,
I want not to be offered that prompt,
so that I'm only shown prompts I'm allowed to run.

**Happy path**

- AC1 — listed for members
  Given a prompt limited to "Editors" and a user in "Editors"
  When the prompt property action loads its prompts
  Then the prompt is offered

**Sad path**

- AC2 — hidden from others
  Given a prompt limited to "Editors" and a user not in it
  When they call `GET prompts`
  Then the prompt isn't listed
- AC3 — detail hidden as not found
  Given that user, without `Uai.Prompt.Manage`
  When they call `GET prompts/{alias}`
  Then the response is 404
- AC4 — execute refused
  Given that user
  When they call `POST prompts/{alias}/execute`
  Then the response is 403 `ProblemDetails` titled "Prompt not available"
- AC5 — managers see all in the AI section
  Given a user with `Uai.Prompt.Manage` not in "Editors"
  When they call `GET prompts?includeRestricted=true`
  Then the limited prompt is listed

---

## GP-14: The "Run agent" picker shows the service account's agents

As an **automation builder**,
I want the "Run agent" step's picker to list what the workspace's service account can use,
so that I can't pick an agent that will fail when the automation runs.

**Happy path**

- AC1 — lists the service account's agents (R16)
  Given a workspace whose service account is in "Automation", an agent limited to "Automation",
  and a builder not in "Automation"
  When the builder opens the "Run agent" picker
  Then the agent limited to "Automation" is listed
- AC2 — hides what the service account can't use
  Given an agent limited to "Editors", and the builder in "Editors" but the service account not
  When the builder opens the picker
  Then that agent isn't listed

**Sad path**

- AC3 — workspace the caller can't access
  Given a caller without access to workspace W
  When they call `GET agents?onBehalfOf=automate-workspace:W`
  Then the response is 403
- AC4 — unknown kind or no service account
  Given `onBehalfOf=unknown:1`, or a workspace with no service account
  When `GET agents` is called with it
  Then the response is 400

---

## GP-15: Publishing catches an agent the service account can't use

As an **automation builder**,
I want publishing to fail when a "Run agent" step uses an agent the service account can't use,
so that I find out while building, not at run time.

**Happy path**

- AC1 — usable agent publishes
  Given a "Run agent" step using an agent the service account can use
  When I publish the automation
  Then it publishes

**Sad path**

- AC2 — unusable agent blocks publish (R17)
  Given a "Run agent" step using an agent limited to a group the service account isn't in
  When I publish the automation
  Then publishing fails with an error naming the step and the agent

---

## GP-16: A "Run agent" step fails clearly at run time

As an **automation owner**,
I want a step to fail with a clear reason when the service account loses an agent,
so that I see the problem instead of a step that skips quietly.

**Happy path**

- AC1 — usable agent runs
  Given a published automation whose service account can use its agent
  When the automation runs
  Then the "Run agent" step succeeds

**Sad path**

- AC2 — lost access fails the step (R18)
  Given the agent was limited after publishing, to a group the service account isn't in
  When the automation runs
  Then the step fails with category `Authentication` and the reason "The workspace's service
  account can't use agent '<alias>'. Add one of its user groups to the agent, or pick another
  agent."
- AC3 — no service account fails closed (R19)
  Given a workspace with no service account and a limited agent
  When the automation runs
  Then the step fails and the agent isn't run

---

## GP-17: User groups travel with agents and prompts

As a **site admin using Deploy**,
I want an agent's and prompt's user groups to move between environments,
so that a limit set on one site holds on the next.

**Happy path**

- AC1 — exported by alias (R20)
  Given an agent limited to the custom group "Translators"
  When it is exported
  Then the artifact lists the group by the alias `translators`
- AC2 — imported by alias
  Given the target site has a "Translators" group with a different key
  When the agent is imported
  Then its `allowedUserGroupIds` holds the target site's "Translators" key
- AC3 — prompts do the same
  Given a prompt limited to "Translators"
  When it is exported and imported
  Then its `allowedUserGroupIds` holds the target site's "Translators" key

**Sad path**

- AC4 — missing group fails the import (R21, spec guess 3)
  Given the target site has no "translators" group
  When the agent is imported
  Then the import fails with a message naming `translators`
- AC5 — a failed import changes nothing
  Given that failed import
  When the target site's agent is read
  Then it is unchanged
