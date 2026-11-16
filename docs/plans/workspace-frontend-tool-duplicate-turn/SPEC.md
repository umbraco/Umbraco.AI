# Spec: Workspace frontend-tool turn stored once

## Endpoints
None new. `GET /umbraco/ai/management/api/v1/conversations/{id}/messages` keeps its shape. It now returns an old chat's repeated turn once (R6).

## Rules

**New turns**
- **R1** When a plain (non-resume) run on a Workspace conversation starts with a user message whose id matches the last stored user message, that message and everything resent after it are treated as a resent turn.
- **R2** In a resent turn, an assistant message is dropped when every tool call in it already has a stored call with the same call id. Any text in it is already in the stored copy.
- **R3** In a resent turn, a tool result whose call id has a stored stand-in result (`"Success: Function completed."`) replaces that stored result before the model runs. The resent copy is then dropped.
- **R4** In a resent turn, a tool result whose call id already has a stored real result is dropped, and the stored result is kept.
- **R5** Messages after the resent turn, such as a new user message, are passed on and stored as usual.

**Old chats**
- **R6** When a conversation is loaded for a run, or its messages are read for display, a repeated turn already in the store is merged: a user message whose id appears earlier, followed by calls with call ids that appear earlier. R2-R4 apply with the later block as the resent turn, and the later block's rows are deleted.
- **R7** A repair writes its updates and deletes in one transaction. If it fails, the chat is left as it was, the error is logged, and the read goes on with the unrepaired rows.
- **R8** A second read of a repaired chat changes nothing.

**Unchanged**
- **R9** A user who sends the same text twice gets two turns. Their messages have different ids.
- **R10** Approval resumes (AG-UI `resume` entries) behave as before.
- **R11** The #375 drop of a resent leading run after a dropped connection still works.

## Targets
- A read of a chat with no repeat does no database writes.
- The merge is one pass over the conversation's stored messages, which are already loaded for a run.
