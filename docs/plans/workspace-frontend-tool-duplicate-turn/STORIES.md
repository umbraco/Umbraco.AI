# Stories: Workspace frontend-tool turn stored once

**3 stories, 17 criteria.** Story ids are used by `PLAN.md` and the spec files, so never renumber them.

**Store**

- [S3](#s3-rewrite-stored-messages-in-one-step) Rewrite stored messages in one step, 3 criteria

**New turns**

- [S1](#s1-a-frontend-tool-turn-is-stored-once) A frontend-tool turn is stored once, 8 criteria

**Old chats**

- [S2](#s2-an-old-chat-with-a-repeated-turn-repairs-itself) An old chat with a repeated turn repairs itself, 6 criteria

<details>
<summary>Ready and Done</summary>

- **Ready:** role, capability and value stated; Given/When/Then criteria cover the happy path; out of scope is explicit.
- **Done:** every criterion passes as an executable spec; sad paths covered; reviewer PASS; the BRIEF repro passes on the v18 demo site.

</details>

---

## S1: A frontend-tool turn is stored once

As an editor using Copilot Workspace,
I want a turn where the agent used a browser tool to be stored once, with the real tool result,
so that reopening the chat shows it once and the model doesn't get it twice on later runs.

Out of scope: approval resumes; chats stored before the fix (S2).

**Happy path**

```
AC1 — The resent user message is dropped
  Given the store holds user message U, tool call C and the stand-in result for C
  When  a plain run arrives with U (same id), the browser's copy of call C and the real result for C
  Then  U is not passed to the model a second time

AC2 — The browser's copy of the tool call is dropped
  Given the same stored turn and resent turn as AC1
  When  the run is prepared
  Then  the browser's assistant message with call C is not passed to the model

AC3 — The real result replaces the stand-in in the store
  Given the same stored turn and resent turn as AC1
  When  the run is prepared
  Then  the stored result for call C holds the real result, not "Success: Function completed."

AC4 — The stored turn holds no repeat
  Given the same stored turn and resent turn as AC1
  When  the run finishes
  Then  the conversation stores U once, call C once and one result for C

AC5 — A turn with a server tool before the frontend tool
  Given the store holds U, server call S with its real result, frontend call F and the stand-in for F
  When  the browser resends U, calls S and F, and the real results for S and F
  Then  the stored result for S is unchanged and the stored result for F holds the real result

AC6 — New messages after the resent turn are kept
  Given the same stored turn as AC1
  When  the resent turn is followed by a new user message N
  Then  N is passed to the model and stored
```

**Sad path / edges**

```
AC7 — The same text sent twice is two turns
  Given the store holds a finished turn starting with user message U1 "Hello"
  When  a new run sends user message U2 "Hello" with a different id
  Then  U2 is passed to the model and stored as a new turn

AC8 — Approval resumes are untouched
  Given a run paused for an approval
  When  the browser resumes it with an AG-UI resume entry
  Then  the run takes the existing approval path and the merge is not applied
```

## S2: An old chat with a repeated turn repairs itself

As an editor with Copilot Workspace chats from before the fix,
I want a chat that already holds a repeated turn to show and send that turn once,
so that old chats look right and keep working on every provider.

Out of scope: a one-off sweep of every conversation.

**Happy path**

```
AC1 — Opening the chat shows the turn once
  Given the store holds U, call C, the stand-in for C, then U again, call C again and the real result for C
  When  the chat's messages are read for display
  Then  the returned messages hold U once, call C once and the real result for C

AC2 — A run on the chat sends the turn once
  Given the same stored rows as AC1
  When  the chat is loaded for a run
  Then  the model gets U once, call C once and the real result for C

AC3 — The repeated rows are removed from the store
  Given the same stored rows as AC1
  When  the chat is read for display or loaded for a run
  Then  the store no longer holds the second copy of U and C
```

**Sad path / edges**

```
AC4 — A second read changes nothing
  Given a chat already repaired
  When  its messages are read again
  Then  no database write happens

AC5 — A chat with no repeat isn't written to
  Given a chat with no repeated turn
  When  its messages are read
  Then  no database write happens

AC6 — A failed repair leaves the chat as it was
  Given the store fails partway through a repair
  When  the chat's messages are read
  Then  the stored rows are unchanged and the read returns the unrepaired messages
```

## S3: Rewrite stored messages in one step

As the conversation store,
I want to update some messages and delete others in one transaction,
so that a repair can never leave a chat half-fixed.

**Happy path**

```
AC1 — Updates are saved
  Given a conversation with stored messages
  When  a rewrite updates one message's content
  Then  that message holds the new content and keeps its Sequence and DateCreated

AC2 — Deletes are saved
  Given a conversation with stored messages
  When  a rewrite deletes two messages by id
  Then  those messages are gone and the rest keep their order
```

**Sad path / edges**

```
AC3 — A failed rewrite changes nothing
  Given a rewrite with one update and one delete
  When  the delete fails
  Then  the update is not saved either
```
