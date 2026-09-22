# Brief

## Problem

Implement a prompt suggestions feature for Copilot to provide meaningful prompt suggestions. This
should take two forms:

1. On agent definitions, allow defining a number of simpler starter prompts.
2. Within a conversation, allow converting a posted prompt into a user-stored prompt with a custom
   label. These will also need to allow management features when presented.

Prompts need to be presented in the Copilot start view — an empty chat gave the user a blank box
and nothing to click.

**Success looks like:** an agent author can add, reorder and remove up to four starter prompts on
an agent, and can press "Suggest starters" to draft them from the agent's `Instructions`. Opening
an empty chat in Copilot or Copilot Workspace shows those starters as chips; clicking one sends it
immediately and pins the conversation to that starter's agent. A surface that doesn't supply
starters renders today's empty state unchanged — this is additive, never a breaking change.

## Non-goals

- **User-saved prompts (form 2 above) — cut from v1, 16-09-2026.** The seams that keep this
  additive are in place (every new context member is optional, `AIStarterPrompt` is an object
  rather than a bare string), but the per-user table, its API, the save button, and the inline
  management UI are deferred to a follow-up. Full design preserved in
  `03-design-discussion.md#deferred-user-saved-prompts`.
- Team/organisation-shared prompt libraries.
- Auto-detecting repeated prompts and offering to save them.
- Follow-up suggestions generated after each assistant reply — a different feature, different
  lifetime.
- Reusing `AIPrompt` (`Umbraco.AI.Prompt`) entities — those are field-level property actions, not
  chat conversation starters.
