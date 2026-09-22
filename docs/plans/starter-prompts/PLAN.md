# Starter Prompts Plan

> Status: partially built — `AIStarterPrompt`/`AIStarterPromptSuggester` + tests exist, but only in
> the `v18-copilot-starter-prompts` worktree (`v18/feature/copilot-starter-prompts`), not yet merged
> to `v18/dev`. Owner: Matt Brailsford. Created 04-08-2026.

## Overview

Give every chat surface a useful empty state by showing clickable starter prompts. Two sources, one
surface:

1. **Agent-defined starters**: authored on the agent definition, shipped/deployed with the agent.
2. **User-saved starters**: a backoffice user saves a prompt they typed so they can re-run it later.
   Private to that user.

This is deliberately **outside** the Copilot Workspace plan
(`copilot-workspace-plan.md`). Starter prompts are a chat-surface feature, consumed by the existing
Copilot sidebar today and by Copilot Workspace when it lands. Neither depends on the other.

### Goals

1. An agent can advertise what it is good at, without the user knowing what to ask.
2. A user can save a prompt they keep retyping, for their own reuse only.
3. Both sets render in the same empty state, clearly separated, and clicking one sends it.
4. Works in any surface that consumes `UAI_CHAT_CONTEXT`: no per-surface code.
5. Cycling/rotation when there are more starters than fit.

### Non-goals (this plan)

- Team/organisation-shared prompt libraries. Deliberately deferred: see [Later, not now](#later-not-now).
- Auto-detecting repeated prompts and offering to save them.
- Follow-up suggestions generated after each assistant reply. Different feature, different lifetime.
- Reusing `AIPrompt` (Umbraco.AI.Prompt) entities. Those are field-level property actions, not chat
  conversation starters.

---

## Prior art

Full survey lives in `docs/internal/agent/research/cms-ai-copilot-patterns.md`. Condensed, for the
decisions below:

| Platform                    | Where starters live                          | User can add their own?              |
| --------------------------- | -------------------------------------------- | ------------------------------------ |
| Teams agents                | App manifest, max 12, explicitly not dynamic | No                                   |
| Copilot Studio              | Agent definition, max 10, can auto-generate  | No                                   |
| OpenAI custom GPTs          | GPT config ("conversation starters")         | Only by making their own private GPT |
| Slack agents                | Pushed at runtime per conversation, via API  | No                                   |
| M365 Copilot Prompt Gallery | Separate pool: vendor / user / team / tenant | Yes: "Your prompts", private        |
| VS Code Copilot             | User-profile prompt files, run as `/name`    | Yes: personal, syncs across devices |
| Gemini Gems                 | A saved instruction set as a mini agent      | Yes: "Only me" by default           |

Takeaways applied here:

- Starters shown on an empty chat are **authored**, never inferred from usage.
- The personal layer is a **separate store**, not an override of the agent's list.
- Usage data is for ranking and reporting, not for defining the list.

---

## How long are prompts really

Two different populations, and conflating them is the main design risk in this plan.

**Agent-defined starters are seeds.** One line, roughly 40 to 120 characters, phrased as a task:
"Summarise this page and suggest three SEO improvements." The published examples bear this out - the
Teams docs' own sample starters are single sentences, and OpenAI's conversation starters are short
enough that the starter text *is* the payload, with no separate label.

**Personal saved prompts skew long.** People save the prompt that is tedious to retype, which means
structure, constraints, tone rules and output format. Hundreds to a couple of thousand characters,
often multi-line. VS Code's personal prompts are entire markdown files, which is why it displays them
by *filename* and runs them as `/name`.

What every platform does about it:

| Platform             | Shown in the list                              | Actually sent      |
| -------------------- | ---------------------------------------------- | ------------------ |
| M365 org prompts     | "display prompt", max 132 chars (title max 35) | prompt, max 8,000  |
| Teams agents         | title + description                            | prompt, max 4,000  |
| Slack agents         | title                                          | message            |
| VS Code Copilot      | file name, run as `/name`                      | whole file         |
| OpenAI GPTs          | the starter text itself                        | the same text      |

The pattern is unambiguous: **the moment a platform allows a long payload, it stops showing the
payload.** Only OpenAI shows the raw text, and only because its starters are always short.

One rule cannot serve both populations. D3 therefore splits them: agent starters are capped short and
carry no label, saved prompts may be long and carry an optional one.

Consequences carried into the phases below:

- Agent starters cap at 200 chars. The cap is the design, not a limitation - depth belongs in the
  agent's `Instructions`.
- Saved prompts allow the full 4,000 and never truncate what is *sent*, only what is *shown*.
- A long saved prompt shows its opening line, which is usually the instruction anyway ("Rewrite this
  page for a Danish audience..." then 800 characters of rules), until the user renames it.
- Saved prompts are capped at 10 per agent so the whole list can always be shown, which removes the
  need for a manage screen entirely (Phase 5). Editing a long one happens in a sidebar modal.

---

## Design decisions

**D1: Starters live top-level on `AIAgent`, not inside `Config`.**
`Config` is per-agent-type (`AIStandardAgentConfig` / `AIOrchestratedAgentConfig`). Starters apply to
both types, so putting them in `Config` means duplicating the property and the editor. Top-level
costs one migration; that is the cheaper trade.

**D2: Stored as a JSON string column, matching `Scope` / `GuardrailIds` / `SurfaceIds`.**
`AIAgentEntity.StarterPrompts` as `string?`. No new table, no relational shape to maintain, same
pattern reviewers already know.

**D3: Agent starters are prompt-only. `Label` exists only on user-saved prompts, and is optional there.**
The label solves one problem: a prompt too long to show. That problem only exists in the personal
layer, so that is the only place the field goes.

Why agent starters do not need one:

- They are seeds by design - one line, 40 to 120 chars (see
  [How long are prompts really](#how-long-are-prompts-really)). A seed is already its own best label.
- **An agent already has a home for long text: its `Instructions`.** A starter that needs a briefing
  is a starter doing the agent's job. Capping it short pushes depth to where it belongs.
- OpenAI's conversation starters work exactly this way, with the starter text as the payload.
- One less field the author can leave inconsistent with the prompt beside it.

So: `AIStarterPrompt` is `{ prompt }`, max 10 per agent, prompt max 200 chars. A hard-ish cap here, not
the 4000 used for saved prompts, because the cap *is* the design.

`AIUserStarterPrompt` keeps `Label?` (max 100 chars), because a user cannot edit the agent's
instructions and so genuinely does need somewhere to keep a long prompt. It is never asked for at save
time; the row falls back to the front of the prompt, and the label is set in the edit modal (Phase 5),
where the full text is readable.

Both stay one-field-or-more **objects** in JSON, so adding `Label` to agent starters later is additive
with no data migration. If an author ever makes a real case for a long agent starter, that is the door.

Rendering is uniform either way: the merged chat entry carries a display string, set or derived, so the
chip component does not care which source it came from.

**D4: User-saved prompts are a new entity and table, never merged into the agent definition.**
Different lifecycle (per user, no versioning, no Deploy), different permissions (any user who can
chat may save; only agent managers may edit agent starters). Merging them would leak one user's
prompts into a deployable artifact.

**D5: User-saved prompts are scoped to `(UserId, AgentId?)`, but the UI always sets an agent.**
The model and API keep `AgentId` nullable, where null means "show for any agent". The UI never writes
null in v1: saving always assigns the agent that was in use, with no toggle to decide. Prompts are
written for a specific agent's tools, so agent-scoped is the honest default, and it keeps the save
flow to a single click with nothing to choose (D3).

The null path stays implemented and tested, because it is the shape a future shared/global layer
needs and it costs nothing to honour on read. It is simply unreachable from the backoffice for now.

**D6: Read path piggybacks the existing agent load; saved prompts get their own endpoint.**
Agent starters ride on `AgentResponseModel` and `AgentItemResponseModel`, so the chat context already
has them the moment an agent is selected: no extra request. Saved prompts get a small
current-user-scoped CRUD controller.

**D7: Rendering lives in `uai-chat`'s empty state, fed by the chat context.**
`UAI_CHAT_CONTEXT` gains a `starterPrompts$` observable that merges agent starters and the current
user's saved prompts. `uai-chat` renders them; the Copilot sidebar (and later Copilot Workspace)
inherits it for free.

**D8: No usage-driven behaviour in v1.**
Click tracking and "you have asked this 4 times, save it?" are explicitly Phase 6+. Nothing about
the v1 data model blocks them.

**D9: Cycling for agent starters. A plain list for saved prompts.**
Agent starters render as chips, 4 at a time in definition order, with a small rotate control if there
are more. Saved prompts render as rows under their own heading, all of them, most-recently-used first,
scrolling if needed. Chips suit short seeds; rows suit longer text and need somewhere to hang the edit
and delete actions. No paging anywhere.

---

## Auto mode

The Copilot picker gains an "Auto" entry whenever more than one agent is available
(`copilot.context.ts`, the `agentItems$` observer). Picking Auto sends the alias `auto`;
`StreamAgentAGUIController` then calls `AIAgentService.SelectAgentForPromptAsync`, which scope-filters
the surface's agents and, if more than one survives, asks an LLM classifier to choose. The chosen
agent comes back to the UI as an `agent_selected` AG-UI event.

That raises two questions this plan has to answer.

**D10: In auto mode the empty state aggregates starters from every available agent.**
The list to aggregate already exists: `UaiCopilotAgentRepository.agentItems$` is filtered by surface
(`copilot`) and by the live entity context, using the same allow/deny scope rules the server applies.
So no new availability logic - just read starters off the items we already have. Rules:

- Dedupe on prompt text. Two agents can legitimately ship the same starter.
- Interleave round-robin, one per agent per pass, so an agent with 10 starters cannot crowd out an
  agent with 2.
- Label each chip with its agent name in auto mode only. This is the discovery win: the user learns
  the roster by reading the empty state. In single-agent mode the label is noise, so hide it.
- The window stays at 4 with the rotate control from D9. Aggregation makes overflow the normal case,
  so rotation stops being an edge feature.

**D11: Clicking a starter pins its agent. The classifier is not consulted.**
The prompt was authored against one agent's instructions and tools, so re-deciding at click time is
both slower (an extra LLM round trip) and riskier (it can route to a different agent than the author
intended). Clicking sets the selected agent to the starter's agent and then sends.

This is cheap because of how `run.controller.ts` already works: `setAgent()` recreates the client for
one `agentId` and calls `resetConversation()`. Starters are only ever clicked on an **empty** thread,
so the reset is a no-op. Consequences, stated plainly:

- The picker visibly moves from "Auto" to that agent. The user can flip back to Auto whenever.
- The rest of that thread stays with the pinned agent, rather than being re-classified per message
  the way an auto thread is today. For a thread that started from a purpose-written prompt, that is
  the behaviour we want.
- A per-send override that keeps the thread on Auto is the alternative. It needs a new `agentId`
  argument threaded through `sendUserMessage` / `UaiAgentClient`, for no user-visible gain. Rejected
  for v1; see [Open questions](#open-questions).

**D12: User-saved prompts follow the same rule, and in v1 they always have an agent to pin.**
- Agent-scoped saved prompt (every one the UI creates): shows only when that agent is in the available
  list; clicking pins it, same as an agent-defined starter.
- Saving while in auto mode records the agent the classifier resolved to, taken from the
  `agent_selected` event. There is always one by the time a message has been sent.
- `AgentId = null` (API-only, per D5): always shows, and clicking leaves the selection on Auto so the
  classifier routes it. The only path where a starter click costs an LLM call. Implemented and tested,
  not reachable from the UI.

**Fallbacks.**
- Agent inactive or scoped out of the current context: its starters simply are not in the list, since
  the list is derived from available agents.
- Agent-scoped saved prompt whose agent is unavailable right now: hide the prompt rather than show a
  chip that cannot run. Do not delete it - the user may navigate somewhere it applies again.
- Agent deleted: agent-scoped saved prompts are deleted with it (Phase 4), so nothing dangles.
- Exactly one agent available: no Auto entry exists, so this is just that agent's starters plus the
  user's own. No aggregation, no labels.

---

## Architecture: core chat feature, not a slot

**A core feature of `uai-chat`, behind an optional context capability.** Not a slot each surface fills,
and not a manifest extension point.

### Why

The *behaviour* is surface-agnostic: merge, dedupe, rotate, send, pin, save, edit. Only the *inputs*
are surface-specific (which agents are available, how selection changes), and those already flow
through `UAI_CHAT_CONTEXT`. A slot would mean the Copilot sidebar and Copilot Workspace each build
their own empty state and then drift apart.

An `umb-extension-slot` with its own manifest kind was considered and rejected for v1. The repo already
uses that pattern where it earns its keep - `uai-agent-tool-renderer` exists because third parties
genuinely need to render arbitrary tool output. Nobody outside this repo needs to inject empty-state
widgets, and a kind means a registry, a schema and a public contract to keep stable. Promote it later if
real demand appears.

### Three layers

| Layer                                        | Holds                                                                                                       |
| -------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| **Data** - `@umbraco-ai/agent`               | Generated client for the saved-prompt endpoints, plus `UaiUserStarterPromptRepository`. Agent starters need nothing new: they ride on the agent item (D6). |
| **Behaviour** - `@umbraco-ai/agent-ui`       | `UaiStarterPromptsController` (an `UmbControllerBase`) that merges the two sources and exposes `starterPrompts$`; the dumb `uai-starter-prompts` element; the edit modal. |
| **Inputs** - the surface (Copilot, Workspace) | Supplies the available-agent list and the selection setter used for pinning (D11). Copilot already has both. |

`agent-ui` depends on `@umbraco-ai/agent`, so the behaviour layer can reach the repository directly.
No plumbing through the surface for data.

### The contract

Two **optional** additions to `UAI_CHAT_CONTEXT`:

```ts
starterPrompts$?: Observable<UaiStarterPromptEntry[]>;
sendStarterPrompt?(entry: UaiStarterPromptEntry): void;
```

- `uai-chat` renders `<uai-starter-prompts>` in its empty state **only when both are present**. A
  surface that provides neither keeps today's empty state, byte for byte.
- Optional keeps this additive. `UAI_CHAT_CONTEXT` is exported public API through the agent-ui rollup,
  so a required member would be a breaking change for anyone implementing it.
- The Copilot context gets these by instantiating `UaiStarterPromptsController` and delegating. Roughly
  three lines, and Copilot Workspace will do the same.

### Escape hatches: two narrow slots, not one wide one

The empty state does two unrelated jobs - it greets you and it suggests what to ask. One slot over the
whole region couples them: a surface that only wants to reword the greeting would be forced to
re-supply the starters too, and would silently lose them the moment the default changes.

**PR #292 has already added the first of these slots**, and it was renamed to `empty-state-message`
there (commit `5a43dd4`) while still in draft. It wraps only the greeting paragraph:

```html
<slot name="empty-state-message">
    <p>Start a conversation with ${this._agentName || "an agent"}</p>
</slot>
```

and the Copilot sidebar fills it with a contextual `.copilot-intro`. That is the message seam, and it is
the right shape. Starters get a **sibling** slot, never the same one:

```html
<slot name="empty-state-message">     <!-- #292: the greeting. Untouched by this plan. -->
<slot name="empty-state-suggestions"> <!-- new: default <uai-starter-prompts>, when the context supplies it -->
```

- Filling one leaves the other on its default. Native slot behaviour, no wiring needed.
- Deliberately **no** coarse whole-region slot. Two overlapping slots would need a precedence rule,
  which is a bug waiting to happen. A surface that wants to replace everything fills both.
- No manifest, no registry. If third parties ever need to *contribute* rather than *replace*, that is
  when an extension kind earns its keep.

---

## Sequencing against open PRs

Three open PRs land in the same code. Checked on 04-08-2026:

| PR       | What                                                        | Base                            | State |
| -------- | ----------------------------------------------------------- | ------------------------------- | ----- |
| **#255** | Copilot Workspace: persisted conversations + projects       | `v18/dev`                       | open  |
| **#259** | The same, v17 backport                                      | `v17/dev`                       | open  |
| **#292** | Contextual copilot refocus: trigger, edit lock, per-node history | `v17/feature/copilot-workspace` (stacked on #259) | draft |

### What collides

Only the chat UI. The backend does not overlap at all.

- **`chat/components/chat.element.ts`** is edited by both #255 and #292 - and #292 is the one that adds
  the `empty-state` slot. This plan rewrites the same block. Highest conflict risk in the whole plan.
- **`copilot/copilot.context.ts`** is substantially rewritten by #292 (FAB, per-node history, section
  registry). Phase 3 needs to add controller wiring to exactly that file.
- **#255 adds a third chat surface**, `Umbraco.AI.Agent.Copilot.Workspace`, with its own
  `copilot-workspace-chat.context.ts` and `workspace-agent.repository.ts`. Building before it merges
  means Copilot Workspace ships without starters and someone has to retrofit them.
- **#255 adds `conversation-strategy.ts` and pending-first-message handling.** A starter click must go
  through the same send path as typing, or a persisted conversation will not be created correctly.
- **#292 gives every node its own thread**, so the empty state appears far more often. That makes
  starters more valuable and changes what "a fresh chat" means. Worth designing against the new
  behaviour rather than today's.

### What does not collide

- `AIAgent.cs`, the persistence entity, the migrations, the Web models and mapping, the Deploy artifact,
  the agent editor views, and the whole saved-prompt table and API. None of the three PRs touch any of it.
- `AIAgentService.cs` is touched by #255 and by Phase 1 (validation), but in different methods. Trivial.
- Our agent migration is in a different `DbContext` from the Conversations migrations, so no ordering
  problem.

### So: split the start

1. **Start now** - Phases 1, 2 and 4. Backend, agent editor, saved-prompt table and API. That is the
   bulk of the work and it cannot conflict.
2. **Hold** Phases 3 and 5 until the **v18** versions of #255 and #292 have landed. #292 is v17-based
   and still a draft, so its v18 twin is the real gate for this plan.
3. Slot naming is settled: #292 now exposes `empty-state-message` (commit `5a43dd4`).

---

## Phase 0: Worktree setup

Target line is **`v18/dev`**, with a backport to `v17/dev` in Phase 6.

1. Branch from `v18/dev`, not the current checkout. This plan was drafted while sitting on `v17/dev`,
   so the doc itself needs to land on `v18/dev` too.
2. `EnterWorktree` with name `starter-prompts`.
3. `npm install` in the worktree.
4. `TaskCreate`: title `Worktree: starter-prompts`, description `Path: <abs path> | Branch: v18/feature/starter-prompts`.
5. All work and commits happen in that worktree.

---

## Phase 1: Agent-defined starters (backend)

### Files

| File                                                                    | Change                                                                  |
| ----------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| `Umbraco.AI.Agent.Core/Agents/AIStarterPrompt.cs`                       | **New.** `Prompt` only, max 200 chars (D3).                             |
| `Umbraco.AI.Agent.Core/Agents/AIAgent.cs`                               | Add `IReadOnlyList<AIStarterPrompt> StarterPrompts { get; set; } = [];`  |
| `Umbraco.AI.Agent.Core/Agents/AIAgentVersionableEntityAdapter.cs`       | Include `StarterPrompts` in snapshot + restore.                         |
| `Umbraco.AI.Agent.Core/Agents/AIAgentService.cs`                        | Validate count/length on save.                                          |
| `Umbraco.AI.Agent.Persistence/Agents/AIAgentEntity.cs`                  | Add `string? StarterPrompts`.                                           |
| `Umbraco.AI.Agent.Persistence/Agents/AIAgentEntityFactory.cs`           | Serialize/deserialize the JSON both ways.                               |
| `Umbraco.AI.Agent.Persistence/UmbracoAIAgentDbContext.cs`               | Register the property.                                                  |
| `Umbraco.AI.Agent.Persistence.SqlServer/Migrations/…AddStarterPrompts`  | **New migration.** Copy `20260316100000_UmbracoAIAgent_AddGuardrailIds` as the template. |
| `Umbraco.AI.Agent.Persistence.Sqlite/Migrations/…AddStarterPrompts`     | **New migration.** Same.                                                |
| `Umbraco.AI.Agent.Web/…/Models/AgentResponseModel.cs`                   | Add starters.                                                           |
| `Umbraco.AI.Agent.Web/…/Models/AgentItemResponseModel.cs`               | Add starters (this is what the chat picker reads).                      |
| `Umbraco.AI.Agent.Web/…/Models/Create/UpdateAgentRequestModel.cs`       | Add starters.                                                           |
| `Umbraco.AI.Agent.Web/…/Mapping/*`                                      | Map both directions.                                                    |
| `Umbraco.AI.Agent.Deploy/Artifacts/AIAgentArtifact.cs`                  | Add starters so they deploy with the agent.                             |
| `Umbraco.AI.Agent.Deploy/Connectors/ServiceConnectors/UmbracoAIAgentServiceConnector.cs` | Map artifact to/from entity.                                  |

### Acceptance

- Existing agents load with an empty list; no null blow-ups on rows written before the migration.
- Save/reload round-trips 10 starters intact, on SQLite and SQL Server.
- Over-limit input is rejected with a clear validation message, not silently truncated.
- Agent version history shows a starter change as a change.
- Deploy artifact includes starters; a Deploy round-trip preserves them.

> Note found while planning: `AIAgentVersionableEntityAdapter.CreateSnapshot` already omits
> `GuardrailIds` and `Scope`. That looks like a pre-existing bug. Out of scope here, but worth a
> separate issue.

---

## Phase 2: Agent editor UI

### Files

- `Umbraco.AI.Agent.Web.StaticAssets/Client/src/agent/workspace/agent/views/agent-details-workspace-view.element.ts`
 : add a "Starter prompts" section. Repeatable single-input rows, add/remove/reorder, with a char
   counter against the 200 cap.
- `agent/types.ts`, `agent/type-mapper.ts`: carry the new field.
- `agent/repository/*`: nothing beyond the generated client refresh.
- `lang/*`: labels and help text.

### Acceptance

- Editor can add, edit, reorder and remove starters, and the order is what the chat shows.
- Client-side limit matches server-side limit, with the same message.
- The 200-char cap is visible as a counter, and the help text points long text at the agent's
  `Instructions` instead.
- Works for both Standard and Orchestrated agents.

---

## Phase 3: Render in the chat empty state

### Files

Layering follows [Architecture](#architecture-core-chat-feature-not-a-slot).

- `Umbraco.AI.Agent.UI/Client/src/chat/context.ts`: add the two **optional** members
  (`starterPrompts$`, `sendStarterPrompt`) and the `UaiStarterPromptEntry` type - prompt text, display
  string, source (agent or saved), and the agent to pin.
- `Umbraco.AI.Agent.UI/Client/src/chat/services/starter-prompts.controller.ts`: **new.** The behaviour
  layer: merges agent starters with the user's saved prompts, dedupes, applies the D10 round-robin,
  derives display strings, and pins on send. Surfaces instantiate it; they do not reimplement it.
- `Umbraco.AI.Agent.UI/Client/src/chat/components/chat.element.ts`: add a second slotted region beside
  #292's `empty-state` greeting slot - `empty-state-suggestions`, falling back to
  `<uai-starter-prompts>` and rendered only when the context provides both optional members. Apply this
  on top of the merged #292 shape, not today's.
- `Umbraco.AI.Agent.UI/Client/src/chat/components/starter-prompts.element.ts`: **new**, and dumb -
  takes entries, emits a select event. Renders the chips window of 4 plus the saved-prompt rows, clamps
  to two lines with a full-text tooltip, handles the rotate control, groups "Suggested" vs "Saved
  prompts", shows the agent name per chip in auto mode only.
- `Umbraco.AI.Agent/Client/src/agent/repository/user-starter-prompt.repository.ts`: **new.** Data layer
  over the Phase 4 endpoints, exported through the package's `exports.ts` (never imported by path).
- `Umbraco.AI.Agent.Copilot/Client/src/copilot/repository/copilot-agent.repository.ts`: include
  starters in the `UaiCopilotAgentItem` projection (the push at line ~84). Without this the aggregated
  list is empty in auto mode.
- `Umbraco.AI.Agent.Copilot/Client/src/copilot/copilot.context.ts`: instantiate the controller, feed it
  the available-agent list, and delegate the two optional members to it. Three lines, not a
  reimplementation.

### Acceptance

- Empty chat with no starters looks exactly like today (no empty box, no dead control).
- Clicking a starter sends it immediately and the empty state disappears.
- Switching the selected agent swaps the starters.
- More than 4 starters shows the rotate control; fewer hides it.
- A 1,500-character saved prompt renders as a two-line row, not a wall of text, and still sends in
  full.
- Auto mode shows starters from every available agent, deduped, no single agent dominating, each chip
  naming its agent.
- Clicking an auto-mode starter runs it on that starter's agent with no classifier call, and the
  picker reflects the switch.
- Navigating to a context where an agent is scoped out removes that agent's starters from the list.
- Copilot sidebar picks this up with only the two wiring changes above; no new components there.
- A surface that does not provide the two optional context members compiles and renders today's empty
  state unchanged. No breaking change to `UAI_CHAT_CONTEXT`.
- Filling `empty-state-message` leaves the starter prompts on their default, and filling
  `empty-state-suggestions` leaves the greeting on its default. Neither slot drags the other with it.

---

## Phase 4: User-saved prompts (backend)

### Files

| File                                                                | Change                                                                                     |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `Umbraco.AI.Agent.Core/StarterPrompts/AIUserStarterPrompt.cs`        | **New.** `Id`, `UserId`, `AgentId?`, `Prompt`, `Label?`, `DateCreated`, `DateLastUsed?`.      |
| `Umbraco.AI.Agent.Core/StarterPrompts/IAIUserStarterPromptService.cs` + impl | **New.** Current-user scoped; resolves user via `IBackOfficeSecurityAccessor`.      |
| `Umbraco.AI.Agent.Core/StarterPrompts/IAIUserStarterPromptRepository.cs` | **New.** `internal` per repo convention.                                               |
| `Umbraco.AI.Agent.Persistence/StarterPrompts/*`                     | **New** entity + factory + `DbSet`, table `umbracoAIAgentUserStarterPrompt`.               |
| `…Persistence.SqlServer` / `…Persistence.Sqlite` migrations         | **New table.** Index on `(UserId, AgentId)`.                                               |
| `Umbraco.AI.Agent.Web/…/UserStarterPrompt/*`                        | **New** controllers: list (mine), create, update, delete, mark-used.                       |

### Rules

- The service **never** takes a user id from the request. It reads the current user, always.
- Cap at 10 per user, per agent, so the whole list always fits in the chat surface (Phase 5). Over the
  cap, the API rejects with a message telling them to remove one.
- Deleting an agent deletes its scoped saved prompts (notification handler, mirrors existing
  `AIProfileDeletingAgentNotificationHandler`).
- Not versioned. Not deployed. Not in the Deploy artifact.

### Acceptance

- User A never sees user B's saved prompts, even with a hand-crafted request.
- A user with chat access but no agent-management permission can still save and delete their own.
- Deleting an agent leaves no orphan rows.

---

## Phase 5: Save UX

### Files

- `chat/components/message.element.ts`: add a "Save as starter" action next to the existing copy
  button, on **user** messages. One click, no modal, no title to invent (D3). Toast confirms, with an
  undo.
- `chat/components/starter-prompts.element.ts`: saved-prompt rows get pencil and trash actions.
- `chat/modals/saved-prompt-editor/*`: **new** modal element + `UmbModalToken`, registered through
  `chat/manifests/`. Mirrors the existing `UAI_TOOL_PERMISSIONS_OVERRIDE_EDITOR_MODAL` pattern
  (`type: "sidebar"`), so it looks like the rest of the backoffice.
- `lang/*`: strings.

### There is no separate manage screen

Editing happens where the prompts already are, using standard backoffice pieces. No drill-down panel,
no dedicated management view.

```
Suggested                                    ← agent starters, chips, rotate control
┌──────────────────┐ ┌──────────────────┐
│ Audit this page… │ │ Draft a summary… │
└──────────────────┘ └──────────────────┘

Saved prompts                                ← the user's own, as rows with actions
  Rewrite this page for a Danish audience…      ✎  🗑
  Check every link on this page and report…     ✎  🗑
```

- **Two treatments, on purpose.** Agent starters stay chips: short, nothing to manage. Saved prompts
  become a compact list, because rows take a clamp better than chips do and they need room for actions.
- **Click the row text** to send it, same as clicking a chip, with the same agent pinning (D11).
- **Trash** deletes inline, with an undo toast. The common case never opens a modal.
- **Pencil** opens the sidebar modal: the full prompt in a textarea, plus the optional `Label` field
  (D3). This is where a long prompt is readable and editable, and the only place the label is asked for.
- **The list shows everything, so nothing needs a "see all".** That is why the per-agent cap drops from
  25 to 10 (Phase 4): a list that always fits is simpler than any paging or overflow affordance. The
  group scrolls if it has to.
- **No search, no manual ordering, in v1.** Ten rows ordered most-recently-used is findable by eye.
- **Empty state.** The "Saved prompts" heading only appears once the user has one. No empty group.

Scope on save follows D5: always the agent in use, or Auto's resolved agent if the thread was in auto
mode. No toggle, no choice to make. Saving never asks for a label; renaming happens later in the
manage view, where the user can see whether the derived one reads well.

### Acceptance

- Saving from a sent message is one click. Nothing to fill in, nothing to retype.
- A long saved prompt can be given a label afterwards, from the manage view, and the chip updates.
- Saving twice from the same text does not create a duplicate.
- A saved prompt appears in the next fresh chat's empty state under "Yours".
- Clicking a saved prompt bumps `DateLastUsed`, which drives its order.
- Removing it is possible without leaving the chat.
- With 10 saved prompts, all 10 are reachable without paging, and the sidebar stays usable.
- Deleting is one click plus undo, and never opens a modal.
- A 4,000-character prompt is fully readable and editable in the pencil modal.

---

## Phase 6: Docs and port

- `Umbraco.Docs`: starter prompts on the agent page, plus a short user-facing note on saving your own.
- Port the whole feature to the other active version line per the Backport Workflow in `CLAUDE.md`.
  Both v17 and v18 are in active support, so both get it.

---

## Later, not now

Ordered by how much I think they are worth:

1. **Generate starters from the agent's instructions.** Copilot Studio does this and it removes the
   blank-page problem for the person creating the agent. Cheap: one AI call, prefill the editor rows.
2. **Promotion path.** Mine → shared with a user group → promoted into the agent's starters. This is
   Microsoft's mine/team/org ladder. Needs a sharing model, so it is its own plan.
3. **Click tracking and ordering.** Record which starters get used, order by that, report on it.
   Ranking only: it must never change the list on its own.
4. **Repeat detection.** "You have asked this 4 times, save it?" Nothing surveyed does this. Genuinely
   novel, but only worth it once the personal layer is proven.
5. **Runtime/contextual starters.** A developer-facing hook to replace the list per conversation, the
   way Slack does. Wait for a real request before building it.

---

## Open questions

1. **Should "generate from instructions" be pulled into v1?** It is small and it directly improves the
   odds that agents actually ship with starters.
2. **Pin for the thread, or pin for one message?** D11 pins for the thread because it is nearly free.
   Keeping the thread on Auto after a starter click needs an `agentId` argument plumbed through
   `sendUserMessage` and `UaiAgentClient`. Only worth doing if thread-level pinning tests badly.
3. **Agent name on chips in auto mode.** D10 says show it. It aids discovery but costs vertical space
   in a narrow sidebar, so it is worth a look at the real thing before committing.
4. **Is 200 chars the right cap on agent starters?** D3 treats the cap as the design, on the basis that
   depth belongs in the agent's `Instructions`. If authors keep hitting it for good reasons, the fix is
   to add `Label` to agent starters (additive, no migration) rather than to raise the cap.
