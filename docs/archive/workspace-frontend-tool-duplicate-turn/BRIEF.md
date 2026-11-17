# Brief: Workspace frontend-tool turn stored once

In Copilot Workspace, a turn where the agent calls a frontend tool (a tool that runs in the browser, such as `get_page_info`) is stored twice. Editors see the turn repeated when they reopen the chat, and the model gets the repeated turn on every later run. Tracked in [#751](https://github.com/umbraco/Umbraco.AI/issues/751).

## Why now
- Found while fixing #748. Every Workspace chat that uses a frontend tool gets a duplicated turn.
- The duplicate reaches the model on every later run, so it costs tokens and may confuse replies.
- It needs porting to v17 and v19, which have the same code.

## Done when
- After a frontend-tool round trip, the conversation's stored messages hold the user message, one tool call and one tool result, and the result is the real one from the browser, not `"Success: Function completed."`.
- Reopening the chat shows the turn once.
- The model in the follow-up run gets each tool call and result once, with no repeated `tool_use` id.
- The stored tool call is the server's own copy, which keeps the provider's message id and any thinking content. Anthropic needs that inside a tool loop.
- The same holds for a turn with several frontend tool calls, and for a frontend tool followed by more server tool calls.
- Chats already stored with a repeated turn behave as if it were stored once: the model gets each tool call once, and reopening the chat shows the turn once.
- Those old chats clean themselves up: the repeated rows are removed from the database the next time the chat is loaded for a run or opened, with no migration, no one-off sweep and no editor action.
- A test covers the frontend-tool resume and fails without the fix. Another covers an old chat with a repeated turn.
- Ported to `v17/dev` and `v19/dev`.

## Must work with
- Umbraco CMS 18 (`v18/dev`) first, then CMS 17 and 19 (same code on those lines).
- SQL Server and SQLite.
- Anthropic with extended thinking, plus the other chat providers.
- The browser is not changed. It keeps resending the whole turn after a frontend tool, as #381 made it do.
- The #375 drop of resent leading messages after a dropped connection must keep working.
- Approval (HITL) resumes must keep working.

## Unknowns
- What the model receives in the follow-up run today. Both copies of the tool call carry the same `tool_use` id, but Anthropic accepted the run. Something may already filter one copy out.
- Where `"Success: Function completed."` comes from. It isn't in this repo's source, so it's probably Microsoft.Extensions.AI or the Microsoft Agent Framework ending a run at a frontend tool.
- Whether the conversation store can update or delete a stored message today, or only add new ones.
- How to tell an old repeated turn apart from a user genuinely asking the same thing twice. The repeat carries the same tool call id, which a real second ask never would.
- Whether another provider (such as OpenAI) rejects a chat that repeats a tool call id. If so, old chats fail today and the hiding is urgent, not just tidy.

## Not doing
- **Stop storing run 1 and keep only the resent turn.** Rejected by the maintainer. The browser's copy of the tool call loses the provider's message id and thinking content, which Anthropic needs inside a tool loop. A reload between the two runs would also lose the user's question.
- **Fix the attachment thread id.** Already fixed in #750 (#748). Different cause.

## Repro
1. On the v18 demo site, use an Anthropic chat profile and a standard agent on the `copilot-workspace` surface with the navigation and content scopes.
2. Start a new Workspace chat and ask "Check what page I'm on, then tell me something about it", so the agent calls `get_page_info`.
3. Read `umbracoAIConversationsMessage` for that conversation, ordered by `Sequence`.

Today:
- Rows 0-2 hold the user message, the tool call and the placeholder result.
- Rows 3-5 repeat the turn with client message ids and the real result.

After the fix:
- One user message, one tool call and one real result.
- Then the assistant's reply.

Proof and stored row ids: [Notes for the fix on #751](https://github.com/umbraco/Umbraco.AI/issues/751#issuecomment-6911893552).
