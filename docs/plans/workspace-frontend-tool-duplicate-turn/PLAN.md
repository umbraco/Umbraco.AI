# Plan: Workspace frontend-tool turn stored once

6 tasks. T1 and T2 can run together. T3 and T4 both edit `ConversationChatHistoryProvider.cs`, so they run one after the other.

All paths are under `Umbraco.AI.Agent.Copilot.Workspace/`.

## Setup

- [ ] **T0** - story: S1, S2, S3 (all criteria). Generate the pending executable specs from `STORIES.md` with `bdd-specs`, in the feature worktree:
  - S1 and S2 go in `tests/Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/`.
  - S3 goes in `tests/Umbraco.AI.Agent.Copilot.Workspace.Tests.Integration/`.
  - Use the existing xUnit + Shouldly + Moq style, as in `StreamConversationAutoSelectionTests.cs`.
  Acceptance: the specs compile and are marked pending (skipped). `dotnet test Umbraco.AI.Agent.Copilot.Workspace.slnx` passes with them skipped.
  depends-on: none

## Store

- [ ] **T1** - story: S3 (AC1-AC3). Add `RewriteMessagesAsync(Guid conversationId, IReadOnlyList<AIMessage> updates, IReadOnlyList<Guid> deleteIds, CancellationToken)` to the internal `IAIConversationRepository`, and implement it in `src/Umbraco.AI.Agent.Conversations.Persistence/Conversations/EFCoreAIConversationRepository.cs`:
  - Use one `IEFCoreScope`, following `DeleteMessagesFromAsync`.
  - An update sets only `ContentJson` and `ContentText` on the row with the matching `Id`. `Sequence`, `DateCreated` and the token columns stay as they are.
  - Deletes go by `Id`.
  - No schema change (ARCHITECTURE "Data model").
  Acceptance: S3 specs green on SQLite in the integration test project.
  depends-on: T0. parallel-group: A

## Core

- [ ] **T2** - story: S1 (AC1-AC7), S2 (AC1, AC4, AC5). Add the internal, pure `AIResentTurnMerger` in `src/Umbraco.AI.Agent.Conversations.Core/Conversations/AIResentTurnMerger.cs` (ARCHITECTURE "Decisions"). It has two entry points:
  - **Resent turn** (R1-R5, R9). Input: the stored `ChatMessage`s and the incoming candidates. Output: the candidates to pass on, plus the stored result messages to update. The anchor is a candidate user message whose `MessageId` equals the last stored user message's. Assistant messages are dropped when every call id is already stored. A result replaces a stored stand-in (`"Success: Function completed."`) and is otherwise dropped.
  - **Repeated block in stored history** (R6, R8). Input: stored `AIMessage`s. Output: the rows to update and the row ids to delete. It reuses the same rules with the later block as the resent turn, and returns nothing for a history with no repeat.
  - Put the stand-in text in one named constant, with a comment saying it comes from Microsoft.Extensions.AI's empty-result text.
  Acceptance: S1 AC1-AC7 and S2 AC1, AC4, AC5 specs green against the merger directly.
  depends-on: T0. parallel-group: A

- [ ] **T3** - story: S1 (AC1-AC6, AC8). Use the merger in `ConversationChatHistoryProvider.DropAlreadyPersistedLeadingMessagesAsync`:
  - First keep the existing #375 leading-run drop (R11).
  - Then apply the resent-turn merge.
  - Write the real results over the stand-ins through `RewriteMessagesAsync` before returning, so `ProvideChatHistoryAsync` hands the model the real result.
  - Log once at information level with the conversation id and counts.
  - The AG-UI resume branch in `AGUIStreamingService` is not changed (R10).
  Acceptance: S1 specs green through the provider, with a stubbed repository holding the stored turn. The existing `ConversationChatHistoryProvider` tests still pass.
  depends-on: T1, T2

- [ ] **T4** - story: S2 (AC1-AC6). Repair old repeats when a chat is read:
  - Add an internal `RepairRepeatedTurnsAsync(Guid conversationId, CancellationToken)` next to the merger. It loads stored rows, asks the merger for a repair, and applies it through `RewriteMessagesAsync`.
  - On failure it logs a warning and returns without throwing (R7).
  - Call it at the start of `ConversationChatHistoryProvider.ProvideChatHistoryAsync` (before a run) and of `AIConversationService.GetMessagesPagedAsync` (display), after the ownership check.
  - It writes nothing when there's no repeat (Targets).
  Acceptance: S2 specs green, with S2 AC6 covered by a repository that throws.
  depends-on: T1, T2, T3

## Wire

- [ ] **T5** - **wire: the merge and repair into the Workspace stream endpoint and messages endpoint.** story: S1 (AC4), S2 (AC1, AC3). Smoke on the v18 demo site:
  - **New turn:** run BRIEF's repro in a new chat, then read `umbracoAIConversationsMessage`. There should be one user message, one call and the real result.
  - **Old chat:** open an old chat that holds a repeated turn, such as `d419d8be…`, `2bb58a3c…` or `cc3f417f…` in the v18 demo DB, through the Workspace UI. Confirm the turn shows once and the repeated rows are gone from the table.
  - Send one more message in that old chat and confirm it gets a reply.
  Acceptance: the real requests above pass on the running site.
  depends-on: T3, T4
