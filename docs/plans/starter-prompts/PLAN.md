---
task: copilot-prompt-suggestions-and-storage-system-yi81tz
type: structure-outline
repo: Umbraco.AI
branch: v17/dev
sha: 0ae8ebeae50ecabf27b1679c395cb1bb4ee48b10
base: v18/release/2026.08.1 @ 2fa3e7ac4866bc3067592ea14092a5134497f19e
---

> **Status:** Open PR — [#407](https://github.com/umbraco/Umbraco.AI/pull/407), targeting `v18/dev`.
> This supersedes the earlier draft plan (04-08-2026), which is fully consolidated into
> `03-design-discussion.md` in this folder — see that file's own header for what changed.

# Starter prompts for the chat empty state

Give every chat surface a useful empty state by showing clickable starter prompts, authored on the agent definition. They render through `UAI_CHAT_CONTEXT`, so Copilot and Copilot Workspace both get them with a few lines of wiring each.

Built on `v18/release/2026.08.1`, because that is the only branch where the `empty-state-message` slot and the `Umbraco.AI.Agent.Copilot.Workspace` surface exist.

> **Scope cut, 16-09-2026.** v1 is **agent starters only**. User-saved prompts are deferred to a follow-up; the design for them is preserved in the [design discussion appendix](03-design-discussion.md#deferred-user-saved-prompts). The four seams that keep that half additive are carried in Phases 2 and 3 and cost under ten lines. The phases that built it — the per-user table, its API, the save button and the inline management UI — are gone from this outline.

## Desired End State

- An agent author can add, reorder and remove up to four starter prompts on an agent, and can press **Suggest starters** to draft them from the agent's `Instructions`.
- Starters are versioned, travel through Deploy, and ride the existing agent list response, so the chat needs no extra request.
- Opening an empty chat in Copilot or Copilot Workspace shows those starters as chips; clicking one sends it immediately and pins the conversation to that starter's agent.
- In Auto mode the chips aggregate across every available agent, deduped and round-robin interleaved, each tagged with its agent name when the list spans more than one agent.
- A surface that supplies neither of the two new optional context members renders today's empty state, byte for byte.

## Implementation Overview

- [x] Phase 0: Worktree off the release branch
- [ ] Phase 1: Agent starters — authored, stored, versioned, deployed
- [x] Phase 2: Starters render and send in both chat surfaces
- [x] Phase 3: Auto mode — aggregate, tag, rotate, pin
- [x] Phase 4: Suggest starters from the agent's instructions (automated verification; manual verification pending)
- [ ] Phase 5: Docs and port to v17

---

## Phase 0: Worktree off the release branch

Everything is built on `v18/release/2026.08.1` and merged into `v18/dev` **after** that release branch merges down. Nothing lands on the release branch itself.

```text
EnterWorktree  name: starter-prompts
  branch from origin/v18/release/2026.08.1   (not v18/dev, not the current v17/dev checkout)
  branch name  v18/feature/starter-prompts
npm install                                   (root, never inside a nested Client/)
TaskCreate  "Worktree: starter-prompts"  Path: <abs> | Branch: v18/feature/starter-prompts
```

> **Done 16-09-2026.** The worktree already existed as `v18-copilot-starter-prompts` on branch
> **`v18/feature/copilot-starter-prompts`**, originally cut from `v18/dev`. Since `v18/dev` has neither
> the `empty-state-message` slot nor the `Umbraco.AI.Agent.Copilot.Workspace` surface that Phases 2–3
> require, the branch was reset onto `origin/v18/release/2026.08.1` (tip `e0af97f5`, three commits past
> the `2fa3e7ac` recorded in this outline's front matter). It had no unique commits, so nothing was lost.
> **The branch name is `v18/feature/copilot-starter-prompts`, not `v18/feature/starter-prompts`.**

### Validation

#### Automated Verification

- [x] `git branch --show-current` reports `v18/feature/copilot-starter-prompts`, based on `origin/v18/release/2026.08.1`
- [x] `Umbraco.AI.Agent.Copilot.Workspace` and the `empty-state-message` slot both present on the base
- [x] `dotnet build Umbraco.AI.Agent/Umbraco.AI.Agent.slnx` succeeds on the untouched branch (0 errors)
- [x] `npm install` clean at the root

---

## Phase 1: Agent starters — authored, stored, versioned, deployed

The first vertical slice: a new collection field on `AIAgent` that an author can edit in the backoffice, which survives save/reload, shows up in version history, and round-trips through Deploy. Nothing renders in the chat yet — that is Phase 2.

The record stays an object rather than a bare string (seam 4), so an optional `Label` can be added later with no data migration.

```csharp
// Umbraco.AI.Agent.Core/Agents/AIStarterPrompt.cs  — new
public sealed record AIStarterPrompt
{
    public required string Prompt { get; init; }   // max 200 chars
}

// AIAgent.cs
public IReadOnlyList<AIStarterPrompt> StarterPrompts { get; set; } = [];   // max 4 per agent
```

The cap of **4** is a service guard and an editor rule, never a database constraint — the column is a single JSON blob and could never count rows anyway. Four is also the chip window, so a single selected agent now fits on one page. Caps are cheap to raise and painful to lower, so it starts where the UI actually is.

Storage matches the three JSON blobs already on the entity, so the column is additive and `null` means "none".

```diff
 umbracoAIAgent
   Id                 uniqueidentifier primary key
   Config             nvarchar(max)        # existing JSON blob
   GuardrailIds       nvarchar(4000)       # existing JSON array
   Scope              nvarchar(max)        # existing JSON object
+  StarterPrompts     nvarchar(max)        + JSON array of { prompt }, null when empty
```

Backend files. `AIAgentService.SaveAgentAsync` validates with guard clauses that throw, because that is what the rest of that method does — there is no `Attempt`/`OperationStatus` wrapper in this service.

```diff
 Umbraco.AI.Agent/src/
 ├── Umbraco.AI.Agent.Core/Agents/
+│   ├── AIStarterPrompt.cs                        + the record above
+│   ├── AIAgent.cs                                ~ + StarterPrompts
+│   ├── AIAgentService.cs                         ~ count (4) + length (200) guards in SaveAgentAsync
+│   └── AIAgentVersionableEntityAdapter.cs        ~ snapshot, restore and compare the new field
 ├── Umbraco.AI.Agent.Persistence/
+│   ├── Agents/AIAgentEntity.cs                   ~ + string? StarterPrompts
+│   ├── Agents/AIAgentEntityFactory.cs            ~ Serialize/DeserializeStarterPrompts, [] on bad JSON
+│   └── UmbracoAIAgentDbContext.cs                ~ register the column (no max length, like Scope)
+├── Umbraco.AI.Agent.Persistence.SqlServer/Migrations/2026…_UmbracoAIAgent_AddStarterPrompts{,.Designer}.cs
+├── Umbraco.AI.Agent.Persistence.Sqlite/Migrations/2026…_UmbracoAIAgent_AddStarterPrompts{,.Designer}.cs
+│   └── (both ModelSnapshot.cs files updated)     ~ timestamps must sort after 20260316100000
 ├── Umbraco.AI.Agent.Web/Api/Management/Agent/
+│   ├── Models/AgentResponseModel.cs              ~ + starterPrompts
+│   ├── Models/AgentItemResponseModel.cs          ~ + starterPrompts   ← what the chat will read
+│   ├── Models/CreateAgentRequestModel.cs         ~ + starterPrompts
+│   ├── Models/UpdateAgentRequestModel.cs         ~ + starterPrompts
+│   └── Mapping/AgentMapDefinition.cs             ~ all four maps, both directions
 └── Umbraco.AI.Agent.Web.StaticAssets/Client/src/
+    ├── api/**                                    ~ regenerated, committed (never hand-edited)
+    ├── agent/types.ts                            ~ + starterPrompts on detail and item models
+    ├── agent/type-mapper.ts                      ~ + both directions
+    ├── agent/components/agent-starter-prompts-editor/   + thin wrapper over the CMS list input
+    ├── agent/workspace/agent/views/agent-details-workspace-view.element.ts
+    │                                             ~ new "Starter prompts" uui-box section
+    └── lang/en.ts                                ~ labels, help text, validation message
```

Deploy carries them so a transfer does not silently drop an agent's starters.

```diff
 Umbraco.AI.Agent.Deploy/
+├── Artifacts/AIAgentArtifact.cs                              ~ + IEnumerable<AIStarterPrompt> StarterPrompts
+└── Connectors/ServiceConnectors/UmbracoAIAgentServiceConnector.cs
+                                                              ~ export in GetArtifactAsync, import in Pass3Async
+                                                              ~ import clamps to the first 4, never throws
```

Clamping rather than throwing on import is deliberate: `Pass3Async` goes through `SaveAgentAsync`, so an artifact carrying five starters — from another version line, or after a future cap change — would otherwise fail the whole transfer over a presentation rule.

The rows are **not** hand-rolled. `<umb-input-multiple-text-string>` is the CMS's own repeatable-string list — the component behind the Repeatable Text String property editor — and it is publicly exported from `@umbraco-cms/backoffice/components`, an import path this repo already uses in eleven places. It brings add, remove, drag-to-reorder (`UmbSorterController`) and a `max` item-count validator with no code from us.

The property editor UI that wraps it, `umb-property-editor-ui-multiple-text-string`, is **not** exported from any entry point — it is only reachable through its manifest alias inside a document-type property context, so it cannot be used here. The inner component is the reusable piece.

```text
uai-agent-starter-prompts-editor          (thin wrapper, value in / change out)
  <umb-input-multiple-text-string
      max=4                                # enforced by the component's own rangeOverflow validator
      .items=${prompts.map(p => p.prompt)}
      @change>                             # items back out as string[]
  -> map to AIStarterPrompt[] and dispatch UaiPartialUpdateCommand onto the workspace context
```

The wrapper exists for three reasons, all of which stay small: it maps `string[]` to `AIStarterPrompt[]` and back (seam 4 keeps the stored shape an object), it enforces the 200-character cap the CMS component does not know about, and in Phase 4 it is where the **Suggest starters** button lives.

Two divergences from the CMS component worth knowing before the work starts, neither a blocker:

- **No character limit.** `umb-input-multiple-text-string` has no `maxlength` and no per-item counter. The 200-char cap is checked in the wrapper's change handler and surfaced as a validation message on the `umb-property-layout`, with the service guard from `SaveAgentAsync` as the real enforcement.
- **Delete opens a confirm dialog.** The component calls `umbConfirmModal` before removing a row. Every other list in this repo removes immediately. Accepted as the cost of reuse rather than worked around.

When an optional `Label` is added to `AIStarterPrompt` later, this component no longer fits and the wrapper's insides become hand-rolled rows — a UI change with no data change, which is the whole point of seam 4.

Tests follow the existing unit-test shapes in `Umbraco.AI.Agent.Tests.Unit`.

```diff
 Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Agents/
+├── AIStarterPromptSerializationTests.cs     + round trip, malformed JSON -> [], null column -> []
+├── AIAgentServiceTests.cs                   ~ + over-4 and over-200 rejected with a clear message
+├── AIAgentVersionableEntityAdapterTests.cs  + first coverage of this class; snapshot/restore/compare
 Umbraco.AI.Agent/tests/… /Api/
+└── AgentMapDefinitionTests.cs               + starters survive create -> AIAgent -> response AND item model
 Umbraco.AI.Agent.Deploy/tests/… /
+└── UmbracoAIAgentServiceConnectorTests.cs   ~ + export/import round trip preserves starters
+                                             ~ + a 5-starter artifact imports 4, not an exception
```

The mapping test earns its place: a silent drop in `AgentMapDefinition` is the most likely failure mode, and `AgentItemResponseModel` is what every later phase reads.

### Validation

#### Automated Verification

- [x] `dotnet build Umbraco.AI.Agent/Umbraco.AI.Agent.slnx`
- [x] `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx`
- [x] `dotnet test Umbraco.AI.Agent.Deploy/Umbraco.AI.Agent.Deploy.slnx`
- [x] `npm run build:core && npm run build:agent`

#### Manual Verification

- [ ] `/demo-site-management start` on SQLite, then a second run against SQL Server: add 4 starters to an agent, save, reload — all 4 come back in order
- [ ] An existing agent created before the migration opens with an empty list and saves without error
- [ ] A fifth row is refused by the component's own max validator
- [ ] Rows drag-reorder, and the new order is what saves
- [ ] Pasting a 250-character starter is rejected with a readable message, not truncated
- [ ] Version history lists a starter change as a change
- [ ] Works on both Standard and Orchestrated agents

---

## Phase 2: Starters render and send in both chat surfaces

Now the field becomes visible. This slice spans the agent client, the behaviour layer in `agent-ui`, the dumb element, and the wiring in both surfaces. Scope is deliberately narrow: **the selected agent's** starters only, as chips. Auto-mode aggregation is Phase 3.

Two optional members go on `UaiChatContextApi`. Optional is seam 1: the interface is public API through the agent-ui rollup, so the deferred write members can land later as pure additions.

```diff
 interface UaiChatContextApi extends UmbContextMinimal {
   …existing members unchanged…
+  starterPrompts$?: Observable<UaiStarterPromptEntry[]>;
+  sendStarterPrompt?(entry: UaiStarterPromptEntry): void;
 }
```

`<uai-chat>` renders the element only when **both** are present.

One entry type, shaped so a second source is additive later. `source` keeps both union members (seam 2) and `display` stays separate from `prompt` (seam 3) even though v1 only ever sets `display = prompt`.

```ts
export interface UaiStarterPromptEntry {
    prompt: string;              // what gets sent, in full
    display: string;             // label, or the clamped front of the prompt — seam 3
    agentId?: string;            // the agent to pin
    agentName?: string;          // shown as a tag only when the list spans >1 agent
}
```

> **Decision 16-09-2026: seam 2 dropped.** `source` is **not** added in v1. Adding an optional
> `source?: "agent" | "saved"` later is a one-line widening and is not a breaking change, so carrying
> it now buys nothing. Do not add the field.

The empty state gains a **sibling** slot. Filling one leaves the other on its default — native slot behaviour, no precedence rule to get wrong.

```diff
 <uai-chat>  _messages.length === 0
   <div class="empty-state">
     <slot name="empty-state-message">        # exists today; Copilot fills it with .copilot-intro
       <uui-icon name="icon-chat">
       <p>Start a conversation with {agent}</p>
+    <slot name="empty-state-suggestions">    + new sibling, never the same slot
+      <uai-starter-prompts>                  + only when starterPrompts$ AND sendStarterPrompt exist
   <uai-chat-input>
```

No group heading in v1 — with one source a heading labels nothing, and adding one later is a render change with no contract change.

```diff
 Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client/
+├── vitest.config.ts                              + copied from the Copilot package (happy-dom)
+├── package.json                                  ~ + vitest devDep and "test" script
 └── src/chat/
+    ├── context.ts                                ~ + the two optional members, + UaiStarterPromptEntry
+    ├── services/starter-prompts.controller.ts    + UaiStarterPromptsController: the behaviour layer
+    ├── services/starter-prompts.controller.test.ts  + pure-logic coverage
+    ├── components/starter-prompts.element.ts     + dumb: entries in, select event out
+    ├── components/chat.element.ts                ~ + the sibling slot, gated on both members
+    └── index.ts / exports.ts                     ~ barrel chain; entry type + controller are public
```

The starters themselves need no new request — they ride the agent item both surfaces already load.

```diff
 Umbraco.AI.Agent.Copilot/…/copilot/
+├── types.ts                                      ~ UaiCopilotAgentItem + starterPrompts
+├── repository/copilot-agent.repository.ts        ~ include starters in the projection (~line 84)
+└── copilot.context.ts                            ~ instantiate the controller, delegate 2 members

 Umbraco.AI.Agent.Copilot.Workspace/…/chat/
+├── workspace-agent.repository.ts                 ~ same projection change (~line 27)
+└── copilot-workspace-chat.context.ts             ~ same delegation
```

Pinning goes through `chatContext.selectAgent(agentId)`, **not** `runController.setAgent(...)`. Two reasons found while checking the release branch: `setAgent` there takes a `UaiAgentItem` and deliberately preserves the conversation (it no longer resets), and in Workspace the run controller's agent is the *conversation* (`conversation:{id}`), not the agent. `selectAgent` is already on the shared interface and is what both surfaces implement.

```text
click chip
  sendStarterPrompt(entry)                 UaiStarterPromptsController
    if entry.agentId && entry.agentId !== current
      chatContext.selectAgent(entry.agentId)     # picker moves Auto -> that agent
    chatContext.sendUserMessage(entry.prompt)    # same path as typing; keeps pending-first-message intact
```

`sendUserMessage` is the single send path on purpose — Workspace creates a persisted conversation from a pending first message, and bypassing it would skip that.

`message.element.ts` is untouched in v1. Its assistant-only guard in `#renderActions()` only changes when the deferred save action arrives.

### Validation

#### Automated Verification

- [x] `npm run build:core && npm run build:agent && npm run build:agent-ui && npm run build:copilot && npm run build:copilot-workspace`
- [x] `npm run test:agent-ui` (new script, wired into the root `test`) — entries derive a display string, missing context members yield no render
- [x] `npm run test:copilot && npm run test:copilot-workspace` still pass

#### Manual Verification

Approved by the user on 21-09-2026, after the presentation fixes recorded under Phase 3.

- [x] Copilot sidebar on an agent with starters: chips show under the greeting; clicking one sends it and the empty state disappears
- [x] Copilot Workspace: same chips, same behaviour, with no code beyond the two wiring changes
- [x] An agent with no starters looks exactly like today — no empty box, no dead control
- [x] Switching the selected agent swaps the chips
- [x] The Copilot sidebar's existing `.copilot-intro` greeting is unchanged

---

## Phase 3: Auto mode — aggregate, tag, rotate, pin

Auto is what most users have selected when they open Copilot, so without this the empty state stays empty for the majority. The controller starts reading starters off *every* available agent rather than just the selected one. No new availability logic: both surface repositories already filter by surface and by live entity scope using the same allow/deny rules the server applies.

```diff
 starterPrompts$ =
-  selectedAgent.starterPrompts
+  if a single agent is selected -> that agent's starters, in authoring order  # never more than 4
+  if Auto                       -> merge across all available agents
+    dedupe on exact prompt text                  # two agents may ship the same seed
+    round-robin, one per agent per pass          # a 4-starter agent cannot crowd out a 1-starter one
+    tag each entry with its agent name
+  suppress the agent tag whenever the merged list spans exactly one agent
```

The merge step is also where the deferred saved-prompt source plugs in later — it is not speculative, Auto mode needs it now.

The cap of 4 means a single selected agent always fits the window, so the rotate control only ever appears in Auto mode with more than one agent supplying starters. It still has to exist: three agents at four each is twelve entries.

Everything lands in two files plus their tests; no new layers.

```diff
 Umbraco.AI.Agent.UI/…/chat/
+├── services/starter-prompts.controller.ts        ~ aggregation, dedupe, round robin, tag suppression
+├── services/starter-prompts.controller.test.ts   ~ + the five cases below
+└── components/starter-prompts.element.ts         ~ window of 4 + rotate control + agent attribution
```

> **Decision 21-09-2026: the agent name is a header, not a trailing tag.** `<uui-tag look="secondary">`
> at the end of the chip text was unreadable — on the Copilot sidebar's surface the tag's own background
> is the same colour as the chip's. It is replaced by the same `.agent-attribution` treatment used above
> an assistant message in `message.element.ts` (`icon-bot` + name, 0.75rem, `--uui-color-text-alt`, 0.8
> opacity), sitting above the prompt text inside the chip.
>
> **Decision 21-09-2026: chips are container-responsive.** `:host` becomes an inline-size query
> container, so the chips size to the space the surface gives them rather than to the viewport: below
> 560px (the Copilot sidebar) each chip is `flex: 1 1 100%` and fills the width; at or above it they
> revert to `flex: 0 1 auto` and hug their own text, which is what Copilot Workspace's 860px column
> already showed.
>
> Two follow-ups fell out of verifying that on the demo site:
>
> - **The host needs an explicit `width: 100%` / `align-self: stretch`.** It is a flex item of
>   `.empty-state`, which centres its children, so it was sized shrink-to-fit — and inline-size
>   containment makes a shrink-to-fit box report *no* intrinsic width, which collapsed every chip to
>   one word per line. The container query is only meaningful once the host has a definite width.
> - **The rotate control gets its own row.** `.starter-prompts` is now a centred column holding a
>   `.chips` row-wrap box and the rotate button beneath it, rather than one flat wrap where the button
>   trailed the last chip. Chips in a row also `align-items: stretch` so a two-line prompt no longer
>   leaves its neighbour short. Top margin raised from `--uui-size-space-5` to `-6`.

This is the phase that earns the vitest install from Phase 2 — the logic is pure and cheap to pin down:

- dedupe on identical prompt text across two agents
- round-robin so a 4-starter agent cannot crowd out a 1-starter one
- agent tag suppressed when the merged list spans one agent
- rotate control hidden at 4 or fewer entries, shown above — so never for a single selected agent
- clicking pins the entry's agent and never consults the classifier

### Validation

#### Automated Verification

- [x] `npm run test:agent-ui` covers all five cases above
- [x] `npm run build:agent-ui && npm run build:copilot && npm run build:copilot-workspace`

#### Manual Verification

Approved by the user on 21-09-2026.

- [x] With three copilot agents, Auto shows a mix from all three, each chip tagged with its agent
- [x] Clicking an Auto chip switches the picker to that agent and answers as that agent, with no classifier delay
- [x] Navigating to a section where an agent is scoped out removes that agent's chips
- [x] With one agent available there is no Auto entry, no agent tags and no rotate control
- [x] Two agents with four starters each shows the rotate control and reaches all eight

---

## Phase 4: Suggest starters from the agent's instructions

The main threat to this whole feature is agents shipping with no starters. The scope cut sharpens that: agent starters are now the *only* source, so an agent with none means an empty state with nothing in it. One button on the editor turns the agent's `Instructions` into draft rows the author edits or discards. It never saves on its own.

There is no "generate a list and prefill rows" pattern in the repo yet, so this combines two that do exist: the structured-output chat call from `AIPromptService`, and the inline button-with-loading-state from the connection editor's **Test connection**.

```csharp
// one chat call, structured output, no agent run
var response = await _chatService.GetChatResponseAsync(chat =>
{
    chat.WithAlias("agent-suggest-starters");
    if (agent.ProfileId.HasValue) chat.WithProfile(agent.ProfileId.Value);  // else falls back to
    chat.WithOutputSchema(AIOutputSchema.FromType<SuggestedStartersResponse>()); // the default chat profile
}, messages, cancellationToken);

response.TryGetResult<SuggestedStartersResponse>(out var parsed);   // { IReadOnlyList<string> Starters }
```

```text
POST /umbraco/ai/management/api/v1/agents/{agentIdOrAlias}/suggest-starters
  response 200  { starters: string[] }        each already clamped to 200 chars, max 4
  response 400  ProblemDetails                no profile, or the model returned nothing usable
```

```diff
 Umbraco.AI.Agent/src/
+├── Umbraco.AI.Agent.Core/Agents/AIStarterPromptSuggester.cs   + prompt + schema + clamping
 ├── Umbraco.AI.Agent.Web/Api/Management/Agent/
+│   ├── Controllers/SuggestStartersAgentController.cs          + same base/policy as RunAgentController
+│   └── Models/SuggestStartersResponseModel.cs
 └── Umbraco.AI.Agent.Web.StaticAssets/Client/src/
+    ├── api/**                                                 ~ regenerated, committed
+    └── agent/components/agent-starter-prompts-editor/…         ~ + Suggest starters button
```

The button sits in the wrapper, below `<umb-input-multiple-text-string>`, since the CMS component owns its own add control and we do not reach inside it. Drafts replace the `items` array only on success; a failed call surfaces a message and leaves existing rows alone.

> **Decision 16-09-2026: disable the button when no profile can serve it.** Rather than letting the
> click fail with a 400, the editor resolves whether a profile is available — the agent's own
> `ProfileId`, else a default chat profile — and renders the button `disabled` with an explanatory
> `title` tooltip when neither exists. The 400 path stays implemented server-side as the backstop, but
> the common case never reaches it.

> **Decision 21-09-2026: it is a property action, not an inline button.** The button below the list is
> replaced by a `propertyAction` extension, so it appears in the `...` menu against the property label
> — where the CMS puts Clear, Copy and Paste. `umb-property-action-menu` filters on nothing but the
> string handed to `.propertyEditorUiAlias`, so the property declares a namespaced alias of its own
> (`Uai.PropertyEditorUi.AgentStarterPrompts`) and the action registers against that; no document-type
> property stack is involved. The action writes through `UAI_AGENT_WORKSPACE_CONTEXT.handleCommand`,
> which leaves `uai-agent-starter-prompts-editor` a plain wrapper over the CMS list again.
>
> The default property-action element can carry neither the disabled tooltip nor an in-flight state, so
> the menu item is a custom element in the same shape as the CMS's own sort-mode action — the decision
> above is preserved verbatim, as a disabled `uui-menu-item` with the same tooltip, plus the menu
> item's own loading indicator while the model is being asked.
>
> ```diff
>  Umbraco.AI.Agent.Web.StaticAssets/Client/src/agent/
> +├── property-actions/constants.ts                              + synthetic UI alias + action alias
> +├── property-actions/suggest-starter-prompts.property-action.ts        + availability + execute
> +├── property-actions/suggest-starter-prompts.property-action.element.ts + disabled/loading menu item
> +├── property-actions/{manifests,index}.ts
> +├── manifests.ts / index.ts                                    ~ register and export
> +├── components/agent-starter-prompts-editor/…                  ~ button, availability and API calls removed
> +└── workspace/agent/views/agent-details-workspace-view.element.ts
> +                                                               ~ umb-property-action-menu in the action-menu slot
> ```

### Validation

#### Automated Verification

- [x] `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx` — with a mocked `IAIChatService`: results are clamped to 4 × 200 chars even when the model returns more, an empty or malformed model response returns a 400 rather than empty rows, and nothing is persisted
- [x] `dotnet build Umbraco.AI.Agent/Umbraco.AI.Agent.slnx && npm run build:agent`

#### Manual Verification

- [x] On an agent with real instructions, the action fills the rows with plausible one-line starters and saves nothing until the author hits save — verified 21-09-2026 on the demo site
- [ ] With no default chat profile configured, the failure is a readable message and existing rows survive
- [ ] The menu item shows a loading state and cannot be double-fired
- [x] The `...` menu renders against the "Starter prompts" property label and holds the action — verified 21-09-2026

---

## Phase 5: Docs and port to v17

```text
Umbraco.Docs
  agent page      + Starter prompts: what they are, the cap of 4 × 200 chars, why depth belongs
                    in Instructions
                  + Suggest starters, and that it never saves on its own

v17 line
  port the whole feature per the Backport Workflow in CLAUDE.md
  base on v17/release/2026.08.1 (or v17/dev once that has merged down — whichever the timing favours)
  migrations authored on that line's own branch; never forward-merge support lines
```

Both v17 and v18 are in active support, so both get the feature. Confirm the port before treating the work as done.

### Validation

#### Automated Verification

- [ ] `dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx` and `npm run test` both green on the v17 branch
- [ ] Migrations apply cleanly on a fresh v17 demo site, SQLite and SQL Server

#### Manual Verification

- [ ] The docs pages read correctly against the shipped UI
- [ ] A v17 demo site shows starters in Copilot with the same behaviour as v18

---

## Deferred to a follow-up

Cut on 16-09-2026 and **not built here**. The design is finished and preserved in the [design discussion appendix](03-design-discussion.md#deferred-user-saved-prompts).

```text
user-saved prompts
  umbracoAIAgentUserStarterPrompt table + migration pair
  AIUserStarterPrompt entity, service, internal repository, cascade notification handler
  5 current-user-scoped controllers + map definition + frontend repository
  saveStarterPrompt / updateStarterPrompt / deleteStarterPrompt on UaiChatContextApi
  "Save as starter" action on user messages (the per-role branch in message.element.ts)
  "Saved prompts" rows, the sidebar edit modal, undo toast, DateLastUsed ordering
  the ownership test suite
```

What v1 carries so that half stays additive: every new context member is optional, `source` is already a two-member union, `display` is already separate from `prompt`, and `AIStarterPrompt` is already a JSON object rather than a bare string.

## Open Questions

- ~~**Merge target.**~~ **Confirmed 16-09-2026:** branch *from* `v18/release/2026.08.1`, merge *into* `v18/dev` once that release branch has merged down. Nothing lands on the release branch itself, so the feature never enters the 2026.08.1 release or its `release-manifest.json`.
- ~~**Seam 2.**~~ **Resolved 16-09-2026: dropped.** `source` is not added in v1. Three seams carried, not four. Re-adding it later is an optional-field widening, not a breaking change.
- ~~**Phase 4 profile.**~~ **Resolved 16-09-2026: disable the button.** When neither the agent's `ProfileId` nor a default chat profile is available, the **Suggest starters** button renders disabled with an explanatory tooltip. The server-side 400 remains as a backstop.
