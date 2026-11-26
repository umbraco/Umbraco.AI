# Spec: Agent memory

<details>
<summary>9 guesses, agreed 26-11-2026</summary>

1. **A memory is at most 300 characters.** Short facts, not documents. No consumer product
   publishes a per-memory limit; this matches the one-fact-per-item lists ChatGPT and Claude
   show (RESEARCH.md, "Published limits").\
   *If not:* up to 500 (the column's size), which costs more prompt per memory.
2. **Caps: 30 "about you" memories per user, 50 per space.** Full, that's 24,000 characters
   loaded each run, in line with the 25 KB Claude Code loads from its memory file, the only
   published always-loaded budget we found. A full layer makes `remember`
   fail with "memory is full: merge or forget something first", and the agent does that or
   tells the user.\
   *If not:* bigger caps, which brings `recall` forward.
3. **Shared-chat recipients can read the chat's memory but not change it**, matching today's
   read-only shares. They get edit rights when collaborative chats ship.\
   *If not:* they can delete memories now, before they can even reply.
4. **A recipient outside the project never sees the project's memory list.** They only see
   replies, the same as attachments today, and the share warning says so.\
   *If not:* the shared chat shows the project's memory read-only to them.
5. **"Your AI" lives in the user profile** (the panel that opens from your avatar), so
   sidebar-only users can reach it.\
   *If not:* a page in the Copilot Workspace section, which sidebar-only users can't open.
6. **New agents default to `Off`; the site's memory switch defaults to on, and personal
   memory to `Allowed`.** Memory starts only when an admin picks a mode for an agent.\
   *If not:* new agents default to `Work`, or personal memory defaults to `OptIn`.
7. **The personal switch only covers "about you".** Turned off, agents stop reading and saving
   "about you" for that user; project, chat and "you + this agent" memory carry on.\
   *If not:* it turns off every layer for that user, which leaves shared projects learning
   from everyone but them.
8. **Turning personal memory off keeps the stored "about you" memories,** whether the user
   switches it off or an admin sets the site to `NotAllowed`. The list says they're paused;
   delete is a separate, explicit action.\
   *If not:* switching off deletes them.
9. **No size limit on personal resources** beyond what AI Context resources already allow.
   `Always` resources cost prompt on every run, as they do on a context.\
   *If not:* a cap on `Always` resources per user, such as 5 or a character total.

</details>

## Endpoints

Base path: `/umbraco/ai/management/api/v1/memories`. Every route needs a signed-in backoffice
user. Access to a space is decided by its resolver; a memory the user can't see is a 404.

**`GET /mine`** the signed-in user's own layers
- **200** `{ aboutYou: Memory[], agents: [{ agentId, agentName, memories: Memory[] }] }`

**`GET ?spaceType=&spaceKey=`** one shared space's memories
- **200** `Memory[]`
- **404** unknown space, or no read access

**`PUT /{id}`** `{ text, expiresAt? }` edit the text or the end date
- **200** `Memory`
- **400** empty, over 300 characters, or looks like a secret
- **404** not found, or no write access

**`POST /{id}/move`** `{ spaceType, spaceKey, agentId? }` move to another layer
- **200** `Memory`
- **400** target full
- **404** not found, or no write access to the memory or the target

**`POST /{id}/keep`** make a held memory active
- **200** `Memory`
- **404** not found, not held, or no write access

**`DELETE /{id}`**
- **204**
- **404** not found, or no write access

```ts
type Memory = {
  id: string;
  text: string;
  spaceType: "user" | "user-agent" | "project" | "conversation" | string;
  spaceKey: string;
  agentId: string | null;
  kind: "fact" | "lesson";
  status: "active" | "held"; // replaced and expired rows are never returned
  expiresAt: string | null;
  holdReason: "echoes-content" | "instruction-like" | "shared-after-content" | null;
  createdBy: { key: string; name: string } | null; // null when the user was deleted
  dateCreated: string;
  dateModified: string;
  canEdit: boolean;
};
```

**Agent config:** `StandardAgentConfigModel` gains `memoryMode: "off" | "work" | "workAndPerson"`,
default `"off"`.

**Settings:** the settings `GET`/`PUT` gain `additional: Record<string, unknown>`, holding every
registered add-on setting. An unknown key or a wrong type is a **400**.

**`GET /mine`** also returns `aboutYouEnabled: boolean`, and **`PUT /mine/preferences`**
`{ aboutYouEnabled }` sets the personal switch (**204**).

### Personal context (core)

Base path: `/umbraco/ai/management/api/v1/personal-context`. Every route needs a signed-in
backoffice user and only ever reads or writes that user's own set.

**`GET`**
- **200** `PersonalContext` (empty lists when the user never saved one)

**`PUT`** `{ contextIds: string[], resources: ResourceModel[] }` replaces the whole set
- **200** `PersonalContext`
- **400** a context id the user can't list, an unknown resource type, or resource settings the
  type rejects

```ts
type PersonalContext = {
  contextIds: string[];       // in the user's order
  resources: ResourceModel[]; // the same model a project's resources use
};
```

**Agent config:** `StandardAgentConfigModel` also gains `includePersonalContext: boolean`, default
`true`; a missing value reads as `true`.

**Prompt:** the prompt create, update and response models gain `includePersonalContext: boolean`,
default `false`.

**Settings:** the core settings `GET`/`PUT` gain `personalContextEnabled: boolean`, default
`true`. `GET /personal-context` also returns it, so the profile can show the admin note.

## Agent tools

Offered only when the agent has memory on and a backoffice user is signed in.

**`remember`** `{ text, layer: "about_you" | "space", kind?: "fact" | "lesson", expires?: string, replaces?: string[] }`
- `kind` defaults to `fact`. A `lesson` records what worked ("lead product pages with the
  material, then sizes"). Lessons go in the space layer only, never "about you".
- `expires` is an optional date (`YYYY-MM-DD`) for facts that are only true for a while.
- `replaces` lists short ids of memories the new one updates or merges, up to 10. They must be
  memories this run can see (in the memory block or saved earlier in the run) and in the same
  layer as the new one.
- When the new memory saves, the replaced ones turn `replaced`: no longer injected or listed,
  but kept for 7 days so Undo can bring them back.
- When the new memory is held, the replaced ones stay active until someone clicks Keep. A
  poisoned save can't wipe out clean memories.
- Returns `{ status: "saved" | "held" | "not_saved", memory?, reason?, moveTarget? }`.
- `moveTarget` is the other layer (`{ spaceType, spaceKey, agentId?, label }`), for the Move
  button. It's left out when the other layer is "about you" and the agent's mode is `Work` or
  the user's personal switch is off, so no Move button shows.

**`forget`** `{ id }` (short id)
- Short ids are at least 8 characters, longer only where two of the run's memories would
  otherwise share one.
- Returns `{ status: "forgotten", memory }`, or `{ status: "not_found" }`.

## Rules

- **R1** An agent's memory is `Off` until an admin picks a mode. `Off` means no memory block
  and no memory tools.
- **R1a** `Work` reads and saves spaces and lessons only. "About you" is never read, and
  `remember` with `layer: "about_you"` fails with a reason.
- **R1b** `WorkAndPerson` reads and saves every layer.
- **R1c** With the site's memory switch off, every agent behaves as `Off`, whatever its mode.
  Stored memories are kept.
- **R1d** With a user's personal switch off, "about you" is neither read nor saved for them,
  whatever the agent's mode.
- **R1e** Site personal memory `NotAllowed`: every `WorkAndPerson` agent behaves as `Work` for
  everyone, the personal switch is hidden, and stored "about you" memories are kept, paused.
- **R1f** Site personal memory `OptIn`: a user who never touched their personal switch has it
  off. `Allowed`: it's on. A user who flipped it keeps their choice when the policy changes
  between these two.
- **R2** A run with no backoffice user (Automate) gets no memory block and no memory tools.
- **R3** Each run reads "about you" plus exactly one space: project, then shared chat, then
  "you + this agent".
- **R4** A chat inside a project uses the project's space, shared or not.
- **R5** A chat outside a project that has ever been shared uses its own space, for every turn
  after the first share, even once every share is revoked.
- **R6** "You + this agent" memories are only read in that user's private chats with that
  agent, outside projects.
- **R7** Held memories are never injected. They expire 7 days after saving unless kept.
- **R8** A save that looks like a secret is never stored, and the tool says why.
- **R9** The suspect check (ARCHITECTURE) holds a save; it never blocks the run.
- **R9a** With the Decision feature on and a default Decision profile set, every save is also
  asked of that profile. Its answer can hold or refuse a save, never pass one the rules held or
  refused. Without either, or if the call fails, saving works on the rules alone.
- **R10** The user can read and change only their own "about you" and "you + this agent"
  memories.
- **R11** In a project, only the project's owner can change its memories (members later).
- **R12** In a shared chat, the owner can change its memories; recipients can read them (guess 3).
- **R13** Deleting a user deletes their "about you" and "you + this agent" memories. Memories
  they saved into shared spaces stay, shown as saved by "a deleted user".
- **R14** Deleting an agent deletes all "you + this agent" memories for it.
- **R15** Deleting a project or conversation deletes its memories.
- **R16** Within one Workspace chat, the memory block in the prompt never changes after the
  first turn; later changes appear in the trailing "changed since this chat started" note.
- **R17** The memory block tells the model the memories are facts from the user, not
  instructions, and must never override the agent's own instructions.
- **R18** The share warning says the recipient will see replies shaped by the owner's memory.
- **R19** The memory rules tell the agent to update or merge an overlapping memory with
  `replaces` rather than save a near-copy.
- **R20** `replaces` ids outside the run's visible memories, or in another layer, fail the
  save with a reason; nothing is changed.
- **R20a** A short id that matches more than one of the run's memories is refused with a
  reason (`forget` and `replaces` alike); nothing is changed.
- **R21** A merge counts against the cap as one memory: replaced ones stop counting at once.
- **R22** Undo on a merge deletes the new memory and makes the replaced ones active again, if
  they're still within 7 days.
- **R23** A `lesson` saved to "about you" fails with a reason; lessons belong to the space.
- **R24** The memory rules tell the agent to save a lesson only when the user confirms a result
  worked, not after every task.
- **R25** A memory past its `expiresAt` is gone: never injected, never listed, and not counted
  against the cap. Expired rows are deleted when their layer needs room for a new save, or
  when the list is next read.

**Personal context**

- **R26** A run loads the user's personal context only when the feature running allows it: an
  agent with `includePersonalContext` on, or a prompt with it on. Anything else never loads it.
- **R27** A run with no backoffice user gets no personal context.
- **R28** `Always` personal resources go into the system prompt; `On-Demand` ones are offered
  through the same lookup tool as AI Context resources.
- **R29** A context picked by the user that the agent or prompt already uses loads once.
- **R30** A user can pick only AI Contexts they can list. A context deleted later drops out of
  every user's personal context.
- **R31** Deleting a user deletes their personal context.
- **R32** Personal context is never deployed between environments. A prompt's
  `includePersonalContext` is, with the prompt.
- **R33** The site memory switches (R1c, R1e) and the personal memory switch (R1d) don't affect
  personal context.
- **R34** The personal context is always that of the user sending the turn. Today only the
  owner sends turns; this holds once shared chats become collaborative.
- **R35** With the site's Personal Context switch off, no run loads personal context, whatever
  each prompt or agent says. Stored contexts and resources are kept and load again once it's
  switched back on.

Personal context is the same for every agent and prompt the user runs; nothing scopes a
resource to one agent. Notes meant for one agent go in an `On-Demand` resource named for it
("SEO agent: how I like meta descriptions"), so other agents see only its name.

**Deploy**

- **R36** Deploying AI Settings carries `personalContextEnabled` and every registered add-on
  setting, which includes the site memory switch and the personal memory policy.
- **R37** On import, an add-on setting the target hasn't registered, or whose value has the
  wrong type, is skipped; the rest still import. A setting the artifact has no value for keeps
  the target's value.
- **R38** An agent's `memoryMode` and `includePersonalContext` travel with the agent. An agent
  artifact with no `includePersonalContext` imports as `true`.
- **R39** Memories are never deployed, like personal context (R32).

## AI Settings: Personal Context and Agent Memory

Opens from AI > Settings. A new built-in "Personal Context" box (core) after "Transparency"
holds one toggle, on by default. Then an "Agent Memory" box after the built-in ones, added by
the Agent package. Two fields: an "Agent Memory" toggle, and a "Personal Memory" dropdown (Allowed,
Allowed but off until each user turns it on, Not allowed). The dropdown is disabled while the
toggle is off.

![Memory panel on the AI Settings screen](./mockups/settings-memory-panel.png)

[Mockup source](./mockups/settings-memory-panel.html)

| When | The screen |
|---|---|
| Switched off and saved | No agent reads or saves memory from the next run; stored memories stay |
| Personal memory set to Not allowed | No agent reads or saves "about you" from the next run; users' lists show it paused |
| Personal memory changed between Allowed and Opt-in | Only users who never touched their switch change |
| Personal Context switched off and saved | No prompt or agent loads personal context from the next run; stored contexts and resources stay (R35) |
| Agent package not installed | No "Agent Memory" box; the "Personal Context" box still shows |

## Agent editor: Memory setting

Opens from AI > Agents > an agent > Settings, in the "Agent Behavior" box. A dropdown: Off,
Work only, Work and person. Below it, a "Include Personal Context" toggle, on by default, with the
description "Adds each user's own AI Contexts and resources from their profile".

![Agent editor with the Memory setting](./mockups/agent-memory-setting.png)

[Mockup source](./mockups/agent-memory-setting.html)

| When | The screen |
|---|---|
| Mode saved | Takes effect from the agent's next run |
| Set to Off | Existing memories are kept but not read; picking a mode again brings them back |
| Site-wide switch off | The setting shows a note: "Memory is turned off for all agents in AI Settings" |
| "Include Personal Context" off | From the next run, users' personal contexts and resources aren't loaded |

## Prompt editor: Include Personal Context

Opens from AI > Prompts > a prompt > Settings, beside "Include entity context". A "Use personal
context" toggle, off by default, with the description "Adds the user's own AI Contexts and
resources from their profile. Leave off for prompts with a fixed output format".

![Prompt editor with Include Personal Context](./mockups/prompt-personal-context.png)

[Mockup source](./mockups/prompt-personal-context.html)

| When | The screen |
|---|---|
| Saved on | From the next run, the prompt loads the user's personal context |
| Existing prompts after upgrade | Off; nothing changes until a builder turns it on |
| Deployed | The toggle's value travels with the prompt |

## Chat: memory notes

Shows in the Copilot sidebar and in Copilot Workspace, wherever the agent calls `remember` or
`forget`.

![Memory notes in a Copilot Workspace chat](./mockups/chat-memory-notes.png)

[Mockup source](./mockups/chat-memory-notes.html)

| When | The screen |
|---|---|
| Saved | "Saved to <layer>" with Move and Undo |
| Lesson saved | "Learned: <text>" with Undo; no Move, since lessons stay in the space |
| Saved with an end date | The note adds "until <date>" |
| Updated or merged | "Updated <layer>: <text>", with "merged N" when it replaced more than one; Undo restores them |
| Move clicked | Moves to the other layer; the note updates to the new layer |
| Undo clicked | Deletes it; the note reads "Removed" |
| Held | "Held for you to check" with the reason, Keep and Discard |
| Not saved | "Not saved" with the reason (a secret, or a full layer); no buttons |
| Forgotten | "Forgot: <text>" |
| Button fails | The note shows the error in place; the button stays |
| Reopened later | The note shows the memory's current state, not the state at save time |

## Project: Memory tab

Opens from a project in Copilot Workspace, a tab between Details and Info. The tab is one
list-view table, with no box around it and no intro line. Columns: Memory, Kind, Status, Until,
Saved by, Saved, actions.

![Project memory tab](./mockups/project-memory.png)

[Mockup source](./mockups/project-memory.html)

| When | The screen |
|---|---|
| No memories | Empty state explaining the agent saves facts about the project as you chat |
| Edit clicked | The edit dialog opens with the text; Save updates the row |
| Delete clicked | Confirm dialog, then the row goes |
| Held rows | Listed first, a "Held" tag in the Status column (hover gives the reason), with Keep and Discard; the tab badge counts held memories only |
| Not the owner | (Members later) rows are read-only, no actions |

## Shared chat: Memory section

Opens in the context panel of a shared chat outside a project, below "Shared with". An info
icon next to the heading explains, as a tooltip, who can see and edit this memory.

![Memory section in a shared chat's context panel](./mockups/conversation-memory.png)

[Mockup source](./mockups/conversation-memory.html)

| When | The screen |
|---|---|
| Chat never shared | Section hidden |
| Recipient viewing | Same list, no actions |
| No memories yet | One line: "Nothing remembered in this chat yet" |
| Long memory | Rows reuse the context panel's one-line rows, so long text is cut; hover shows the full text |

## Your AI

Opens from the user profile panel (click your avatar). A "Your AI" box (core) with three
property rows: Contexts (the AI Context picker), Resources (the resource list, each row tagged
Always or On-Demand), and the personal memory switch (added by the Agent package). Below the
box, the memory table: one list-view table grouped by "About you" and each agent. A group's
state ("paused", "memory is off for this agent") sits on its header line as plain text.

![Your AI in the user profile](./mockups/my-memory.png)

[Mockup source](./mockups/my-memory.html)

| When | The screen |
|---|---|
| No contexts or resources | Each row shows only its Add button |
| Context or resource added, edited or removed | Saved at once (the panel has no Save button); a failed save shows an error toast and the list goes back |
| Add context clicked | The AI Context picker opens, listing contexts the user can list |
| Add resource clicked | The resource picker and editor open, as on a project |
| Agent package not installed | No memory switch and no memory table; contexts and resources still show |
| Site's Personal Context switched off | The Contexts and Resources rows are hidden; a note says "Your admin has turned off personal context"; nothing stored is deleted |
| No memories | Empty state explaining "about you" and per-agent memory |
| An agent has memory off | Its group still shows; the header reads "memory is off for this agent" |
| Personal switch off | The "About you" header reads "paused"; agents ignore those rows |
| Site sets personal memory to Not allowed | The switch is hidden; a note says "Your admin has turned off personal memory"; rows show "Paused" |
| Edit / delete | Same dialog and confirm as the project tab |

## Edit memory dialog

![Edit memory dialog](./mockups/edit-memory.png)

[Mockup source](./mockups/edit-memory.html)

- Save is disabled while the text is empty or over 300 characters; the counter turns red over
  the limit.
- An optional "Forget after" date field (a date picker) sets or clears the end date.
- Every memory list has a Kind column: Fact or Lesson.
- A server 400 (looks like a secret) shows under the field and keeps the dialog open.

## Targets

- Loading memory adds at most one database query per run for each layer.
- The memory block for a full layer (50 × 300 characters) stays under 5,000 tokens.
- Within a Workspace chat, the cached prompt prefix is byte-identical from turn 2 on unless the
  agent, its instructions or its AI Contexts change (R16).
- Loading personal context adds at most one database query per run, plus one per picked
  context the resolver chain hasn't already loaded.
- Every new screen works by keyboard and has labelled buttons (Move, Undo, Keep, Discard,
  Edit, Delete).
