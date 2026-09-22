[HumanLayer Task](https://app.humanlayer.com/tasks/01a0a508-5bbf-7377-b154-804c10aafdff)

## Why the change

An empty chat gave the user a blank box and nothing to click, so agents can now carry up to four author-written starter prompts that render as clickable chips in every chat surface's empty state.

## Special things to note

- **Read only the last 7 commits** (`dce6fa1d`..`dfb43048`). The branch was cut from `v18/release/2026.08.1`, because that is the only branch that has both the `empty-state-message` slot and the `Umbraco.AI.Agent.Copilot.Workspace` surface this feature builds on. It targets `v18/dev`, so everything before `dce6fa1d` is the release branch and will disappear from the diff once 2026.08.1 merges down. Nothing here should land on the release branch.
- A paired SQL Server and SQLite migration adds one nullable `StarterPrompts` JSON column. An agent saved before the migration reads back as an empty list, and bad JSON in the column also reads back as an empty list rather than throwing.
- Two pieces of the original plan are deliberately not here: **user-saved prompts** were cut from v1 (the seams that keep them additive are in place — every new context member is optional, and `AIStarterPrompt` is an object rather than a bare string), and **docs plus the v17 port** are still outstanding.

## Change outline

One new column on the existing agent table, stored the same way as the three JSON blobs already there.

```diff
 umbracoAIAgent
   Id                 uniqueidentifier primary key
   Config             nvarchar(max)        # existing JSON blob
   GuardrailIds       nvarchar(4000)       # existing JSON array
   Scope              nvarchar(max)        # existing JSON object
+  StarterPrompts     nvarchar(max)        + JSON array of { prompt }, null when empty
```

The starters ride the agent models both chat surfaces already load, so the chat makes no extra request. Only the **Suggest starters** feature adds endpoints.

```diff
  GET  /umbraco/ai/management/api/v1/agents            # item + detail responses
+      { ..., starterPrompts: [{ prompt: string }] }   # also on create/update requests

+ GET  /umbraco/ai/management/api/v1/agents/{idOrAlias}/suggest-starters/availability
+      200  true | false      # can a profile serve this? lets the editor disable up front
+      404  ProblemDetails

+ POST /umbraco/ai/management/api/v1/agents/{idOrAlias}/suggest-starters
+      200  { starters: string[] }   # clamped to 4 items x 200 chars, never persisted
+      400  ProblemDetails           # no profile, or nothing usable came back
+      404  ProblemDetails
```

The chat contract gains two **optional** members. A surface that supplies neither renders today's empty state unchanged, which is what keeps this safe for every other consumer of `UaiChatContextApi`.

```diff
 interface UaiChatContextApi extends UmbContextMinimal {
   …existing members unchanged…
+  starterPrompts$?: Observable<UaiStarterPromptEntry[]>;
+  sendStarterPrompt?(entry: UaiStarterPromptEntry): void;
 }

+export interface UaiStarterPromptEntry {
+    prompt: string;       // what gets sent, in full
+    display: string;      // chip label; equal to prompt in v1
+    agentId?: string;     // the agent this chip pins the conversation to
+    agentName?: string;   // set only when the list spans more than one agent
+}
```

The empty state gains a sibling slot, never a replacement for the existing one, so both surfaces keep their current greeting.

```diff
 <uai-chat>  _messages.length === 0
   <div class="empty-state">
     <slot name="empty-state-message">        # unchanged; Copilot fills it with .copilot-intro
       <uui-icon name="icon-chat">
       <p>Start a conversation with {agent}</p>
+    <slot name="empty-state-suggestions">    + new sibling slot
+      <uai-starter-prompts>                  + rendered only when BOTH context members exist
+        .entries / @select
   <uai-chat-input>
```

Clicking a chip pins first, then sends down the same path as typing. That matters because Copilot Workspace turns a pending first message into a persisted conversation, and any other send path would skip it.

```text
click chip
  sendStarterPrompt(entry)                     UaiStarterPromptsController
    if entry.agentId !== currently selected
      chatContext.selectAgent(entry.agentId)   # the picker moves Auto -> that agent
    chatContext.sendUserMessage(entry.prompt)  # same call the composer makes
```

Which starters show up depends on the picker. Auto is what most people have selected when they open Copilot, so it merges rather than showing nothing.

```diff
 starterPrompts$ =
+  a real agent is selected -> that agent's starters, in authoring order   # capped at 4, always one page
+  Auto (or nothing yet)    -> merge across every available agent
+    dedupe on exact prompt text            # two agents may ship the same seed
+    round-robin, one per agent per pass    # a 4-starter agent can't bury a 1-starter one
+    tag each entry with its agent name
+    drop every tag again if the survivors all came from one agent
```

Availability needs no new rules: the merge reads the agent list each surface's own repository already filters by surface and live entity scope.

Backend and Deploy. Deploy clamps an over-long artifact to the first four rather than throwing, so a transfer never fails the whole pass over a presentation rule.

```diff
 Umbraco.AI.Agent/src/
 ├── Umbraco.AI.Agent.Core/Agents/
+│   ├── AIStarterPrompt.cs                     + the record
+│   ├── AIStarterPromptSuggester.cs            + one structured-output chat call, clamps the result
+│   ├── AIAgent.cs                             ~ + StarterPrompts
+│   ├── AIAgentService.cs                      ~ count (4) and length (200) guards on save
+│   └── AIAgentVersionableEntityAdapter.cs     ~ snapshot, restore and compare the new field
 ├── Umbraco.AI.Agent.Persistence{,.SqlServer,.Sqlite}/
+│                                              ~ entity, factory, DbContext + the migration pair
 └── Umbraco.AI.Agent.Web/Api/Management/Agent/
+     ├── Controllers/SuggestStartersAgentController.cs   + the two endpoints above
+     └── Models/, Mapping/                     ~ starters on all four models, both directions

 Umbraco.AI.Agent.Deploy/
+├── Artifacts/AIAgentArtifact.cs               ~ + StarterPrompts
+└── …/UmbracoAIAgentServiceConnector.cs        ~ export on read, clamp to 4 on import
```

Frontend. The editor is a thin wrapper over the CMS's own repeatable-string list, so add, remove and drag-to-reorder come for free.

```diff
 Umbraco.AI.Agent.Web.StaticAssets/…/agent/
+├── components/agent-starter-prompts-editor/   + wrapper over <umb-input-multiple-text-string max=4>
+├── property-actions/                          + "Suggest starters" as a property action, not a button
+│                                                custom menu item, so it can be disabled with a reason
+│                                                and show its own loading state
+└── workspace/…/agent-details-workspace-view.element.ts
+                                               ~ Starter prompts property inside the Agent Behavior box

 Umbraco.AI.Agent.UI/…/chat/
+├── services/starter-prompts.controller.ts     + all the behaviour above, plus the chip paging window
+├── components/starter-prompts.element.ts       + dumb: entries in, select out
+└── components/chat.element.ts                 ~ the sibling slot

 Umbraco.AI.Agent.Copilot/…  +  Umbraco.AI.Agent.Copilot.Workspace/…
+└── agent repository + chat context            ~ carry starters in the projection, delegate 2 members
```

The chips are container-responsive rather than viewport-responsive: `:host` is an inline-size container, so they fill the width on Copilot's narrow sidebar and hug their own text in Workspace's wider column, from one stylesheet.

Tests: `vitest` is new to `Umbraco.AI.Agent.UI` (matching the Copilot package's happy-dom setup, wired into the root `test` script) and covers the aggregation logic. The .NET side adds serialization, service-guard, version-adapter, API-mapping, suggester and Deploy round-trip tests.
