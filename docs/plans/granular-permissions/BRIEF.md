# Brief: Granular permissions

Site admins can't split up who does what in Umbraco AI. Anyone with the AI section can change everything in it, and any backoffice user can see and run every agent and prompt.

## Why now
- Customers need to give some people part of the AI section, for example Contexts but not Connections or Profiles.
- Customers need to limit agents to certain people, and have the agent list hide the ones a person can't use.
- Prompts have the same gap as agents, so they're fixed at the same time.

## Done when
- An admin can tick, per user group, which AI areas that group can manage. Each of these gets its own "can manage" right: Connections, Profiles, Contexts, Knowledge sets, Guardrails, Tests, Settings, Agents and Prompts. Audit log and Analytics get a "can view" right.
- One right per area covers create, edit and delete together.
- A user who can manage agents (or prompts) sees and edits every one in the AI section, even ones their own groups can't use.
- Area rights, agent limits and prompt limits ship together in one release.
- A user without the right for an area doesn't see that area's menu item or buttons. Any direct API call to change it is refused.
- An admin can limit an agent to chosen user groups. Users outside those groups don't see it in Copilot or any agent list, it's never picked for them automatically, and running it directly is refused.
- An admin can limit a prompt to chosen user groups, with the same results: hidden from lists and refused if run directly.
- An automation can only run an agent the workspace's service account is allowed to use. Its "Run agent" step is refused for any other agent, even if the person who built the automation could use it.
- The agent picker in an automation step shows only agents the workspace's service account can use, not the agents the person editing the automation can use.
- If an agent's limits change after an automation is saved, and the service account can no longer use it, the "Run agent" step fails with a clear reason in the run log. It doesn't skip quietly.
- After an upgrade, every group that had the AI section can still do everything it could before. No admin action is needed.
- An agent or prompt with no groups chosen stays open to everyone, as it is today.
- Pickers keep working. A user who can manage Contexts but not Profiles can still pick a Profile.

## Must work with
- Umbraco CMS v17, v18 and v19. Each line is built and shipped on its own branch (`v17/dev`, `v18/dev`, and the v19 line while it's in development).
- Umbraco's own user group screen, so admins set these rights where they set every other right.
- Umbraco Automate, where each workspace runs as its own service account (a backoffice user that has user groups).
- Umbraco Deploy, so these settings move between environments with the rest of an agent or prompt.
- No breaking changes to the public API.

## Unknowns
- Whether the CMS user group screen shows rights for a package's own areas cleanly, with good labels, without custom UI.
- Whether a user group's ID is the same in every environment. If not, saved group limits could break when Deploy copies an agent or prompt.
- Every place agents are listed or picked, including Copilot, the Copilot workspace, automatic agent selection, orchestrated agents and Automate. All of them must apply the limit.
- Whether the agent picker in an automation step can tell which workspace, and so which service account, it is for.
- A way around the limit: a user runs an agent they're allowed to use, it starts an automation, and that automation runs an agent the user isn't allowed to use, as the service account. It is unclear whether this is acceptable or must be blocked.

## Not doing
- **Limits on individual items in the management screens.** Area rights apply to a whole area, not to one Context or one Profile.
- **Changing who can read lists.** Pickers depend on them, so reading stays open to backoffice users.
