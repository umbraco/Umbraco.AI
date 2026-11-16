---
name: workspace-resent-turn-merge
description: Why Copilot Workspace merges a browser-resent frontend-tool turn on the server, and how old chats repair themselves
type: decision
---

**Decision:** After a frontend tool (e.g. `get_page_info`), the browser resends the whole turn
(#381) and the server stores each turn once by merging it. `ConversationChatHistoryProvider`
matches the resent turn by the user message id plus the tool call ids, keeps the server's own copy
of the user message and call, and writes the real tool result over the stored stand-in before
the model runs. Old chats that already hold a repeated turn are repaired when they're read (before
a run, and on the messages endpoint). No migration, no background job. Accepted 16-11-2026 (#751,
shipped in #755 v18, #756 v17, #757 v19).

**Why:**
- Message ids alone don't match: the browser keeps the user message id but gives the call and
  result its own ids. Tool call ids come from the provider and match on both sides.
- The server copy is kept because it holds the provider message id and thinking content, which
  Anthropic needs inside a tool loop.
- The real result is written before the run because MAF puts stored history in front of the
  request, so the model must see the real result there.
- The stand-in is spotted by Microsoft.Extensions.AI's empty-result text, `"Success: Function
  completed."` (`AIResentTurnMerger.StandInResultText`), because frontend tools return nothing on
  purpose and old chats already hold that text.

**Rejected:** not storing the first run (loses the provider id and thinking, and a reload would
lose the question); changing the browser to stop resending; a one-off migration per version line.

**Revisit if:** the browser stops resending the whole turn after a frontend tool; an
M.E.AI upgrade changes the empty-result text (the merger would stop recognising the stand-in,
and specs in `AIResentTurnMergerTests` should catch it); or a provider starts rejecting gaps in
`Sequence` (repairs delete rows without renumbering).
