# Architecture

## Extension points

**An ordered collection of agent selectors, in `Umbraco.AI.Agent.Core`.**

```csharp
namespace Umbraco.AI.Agent.Core.Agents.Selection;

public interface IAIAgentSelector
{
    /// Return null for "no opinion" - the next selector in the collection decides.
    Task<AIAgentSelectionResult?> SelectAgentAsync(
        AIAgentSelectionRequest request,
        CancellationToken cancellationToken = default);
}

public class AIAgentSelectorCollectionBuilder
    : OrderedCollectionBuilderBase<AIAgentSelectorCollectionBuilder, AIAgentSelectorCollection, IAIAgentSelector>
{
    protected override AIAgentSelectorCollectionBuilder This => this;
}
```

Registered through `builder.AIAgentSelectors()` (a new `UmbracoBuilderExtensions.Selectors.cs`,
same shape as `UmbracoBuilderExtensions.Surfaces.cs`). Developers add their own with
`Append` / `InsertBefore<LLMAgentSelector, MySelector>()`.

This copies the closest sibling: `IAIGuardrailResolver` + `AIGuardrailResolverCollectionBuilder`
in Core, an ordered chain where each entry adds to (or skips) the result. Using the same Umbraco
collection-builder mechanism means ordering, `Exclude<T>`, and DI lifetimes all work the way
Umbraco developers already expect.

**Default registration (at the time this feature shipped):** `LLMAgentSelector` only.
`StickyAgentSelector` ships but is **not** registered, so out-of-the-box behaviour is unchanged.
Turning it on was one line:

```csharp
builder.AIAgentSelectors().InsertBefore<LLMAgentSelector, StickyAgentSelector>();
```

> **Superseded:** `decision-capability-release` (now `docs/archive/decision-capability-release/`) added `DecisionAgentSelector`,
> registered by default *before* `LLMAgentSelector` (decision 7). `InsertBefore<LLMAgentSelector, …>`
> now lands a selector after Decision, which would override it — so the guidance for registering
> `StickyAgentSelector` first in the chain became `builder.AIAgentSelectors().Insert<StickyAgentSelector>()`.

### Types

```csharp
public sealed class AIAgentSelectionRequest
{
    // Active + scope-available agents, in the same order SelectAgentForPromptAsync used.
    // Selectors may only return one of these.
    public required IReadOnlyList<AIAgent> CandidateAgents { get; init; }

    // Full conversation as M.E.AI messages (history + attachments), converted once.
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    // Surface / section / entity type (what scope rules use).
    public required AgentAvailabilityContext AvailabilityContext { get; init; }

    // Raw request context items (entity key, content type, etc.) - everything the frontend
    // sent, not just the three fields AvailabilityContext extracts. This is what lets
    // business rules see "the context" without us guessing which fields they need.
    public required IReadOnlyList<AIRequestContextItem> ContextItems { get; init; }

    public required string SurfaceId { get; init; }

    public required IReadOnlyList<Guid> UserGroupIds { get; init; }

    public IReadOnlyList<AIFrontendTool> FrontendTools { get; init; } = [];

    // The agent picked on the previous turn, resolved against CandidateAgents.
    // Null if the browser sent nothing, sent garbage, or the agent is no longer allowed.
    public AIAgent? PreviousAgent { get; init; }
}

public sealed record AIAgentSelectionResult(AIAgent Agent, string SelectorId, string? Reason);
```

### Orchestration: `IAIAgentSelectionService`

A small new service owns the chain, so `AIAgentService` (already large) doesn't grow further.

```csharp
public interface IAIAgentSelectionService
{
    // Null only when there are no candidate agents at all.
    Task<AIAgentSelectionResult?> SelectAgentAsync(
        AIAgentSelectionInput input,
        CancellationToken cancellationToken = default);
}
```

`AIAgentSelectionInput` is what callers have before filtering (surface, availability context,
messages, context items, frontend tools, optional previous agent ID). The service turns it into an
`AIAgentSelectionRequest`:

1. Get agents for the surface, filter to active + `AIAgentScopeValidator.IsAgentAvailable`. This is
   today's logic, moved, not changed. **Filtering always runs before any selector.**
2. 0 candidates -> `null`.
3. 1 candidate -> that agent, `SelectorId = "only-candidate"`. No selector runs.
4. Resolve user group IDs once. Resolve `PreviousAgent` against the candidates (an ID not in
   the list becomes `null`).
5. Run selectors in collection order. For each result:
   - `null` -> next selector.
   - Agent **not** in `CandidateAgents` -> log a warning, ignore, next selector.
   - Otherwise -> return it.
   - Selector throws (other than cancellation) -> log the error, next selector.
6. Nobody decided -> first candidate, `SelectorId = "fallback"`. Same agent today's code
   falls back to, but now recorded.
7. Publish `AIAgentSelectedNotification` with the final result (see below), then return it.
   This covers every outcome from steps 3-6 and never runs for step 2 (no candidates).

### `AIAgentSelectedNotification`

```csharp
public sealed class AIAgentSelectedNotification : StatefulNotification
{
    public AIAgentSelectedNotification(
        AIAgentSelectionResult selection,
        AIAgentSelectionRequest request,
        EventMessages messages);

    public AIAgentSelectionResult Selection { get; }   // agent, selector ID, reason
    public AIAgentSelectionRequest Request { get; }    // exactly what the selectors saw
    public EventMessages Messages { get; }
}
```

- Lives next to `AIAgentExecutingNotification` / `AIAgentExecutedNotification` in
  `Umbraco.AI.Agent.Core.Agents` and copies their shape: a `StatefulNotification` carrying
  `EventMessages`, published through `IEventAggregator.PublishAsync` by the selection service.
- **Observation only, not cancelable.** Handlers can log, count, or react. They can't change the
  pick, because selectors already do that.
- For the `only-candidate` case the service still builds an `AIAgentSelectionRequest` (it just
  runs no selectors), so `Request` is never null.
- "Selected" does not mean "ran": `AIAgentExecutingNotification` fires afterwards and can still
  cancel the run. The XML docs must say this.
- A throwing handler propagates, the same as the other agent notifications. No special
  swallowing.

`AIAgentService.SelectAgentForPromptAsync` stays, builds an input from its arguments (one user
message), calls the new service, and returns `.Agent`. Marked
`[Obsolete("Use IAIAgentSelectionService.SelectAgentAsync. Will be removed in v20")]`.

### Built-in selectors

- **`LLMAgentSelector`** (`SelectorId = "llm"`, registered by default) - today's classifier,
  moved as is. Same prompt, same input (last user message text only), same classifier profile.
  The one change: where it used to return the first agent on a failure (no profile, unparseable
  reply, unknown GUID), it now returns `null` and the service's `fallback` records it.
  Same agent picked, but the reason is now visible.
- **`StickyAgentSelector`** (`SelectorId = "sticky"`, opt-in) - returns `PreviousAgent` if set,
  otherwise `null`. No config.

## Data model & persistence

Selection itself is computed per request and never stored. For the plain Copilot surface, the
"previous pick" travels from the browser on each request (see Key decisions). The selection reason
is written into the agent run's existing audit log `Metadata` dictionary, with no schema change
there.

**Copilot Workspace (persisted conversations): one new nullable column.** `AIMessage`/
`AIMessageEntity` gains `AgentId` (`Guid?`), filled only on assistant messages with the agent that
produced them. Existing rows stay null. It needs SQLite and SQL Server migrations in the Workspace
persistence projects, following the existing Workspace migration naming. This column is:

- **Workspace's previous pick:** the agent on the newest assistant message in the conversation.
  Regenerate and truncate behave naturally, because removing the last reply makes the reply before
  it the previous pick.
- **Agent names in reopened chats:** the history API returns each message's agent ID, and the client
  maps it to a name.

## Copilot Workspace

Workspace has its own stream endpoint (`StreamConversationAGUIController`). It runs a conversation
by ID, not by `auto`, and resolves the agent server-side on every turn from
`conversation.AgentIdOrAlias`. When that is `auto` (or the agent is missing or inactive), it now uses
`IAIAgentSelectionService` exactly like the plain endpoint:

```
StreamConversationAGUIController (auto path)
  previous = last assistant message's AgentId in this conversation       (new repo query)
  input    = { surface: copilot-workspace, messages: this turn's converted messages
               (regenerate: falls back to the last persisted user message text, as today),
               frontend tools, previousAgentId: previous }
  selection = IAIAgentSelectionService.SelectAgentAsync(input)
  run with AIAgentExecutionOptions { ConversationHistory, AdditionalProperties, Selection }
  prepend agent_selected (same payload as the plain endpoint)
```

- **The message saver stamps the agent.** `ConversationChatHistoryProvider` already runs inside the
  agent's runtime scope, which holds `Constants.ContextKeys.AgentId`. When it persists an assistant
  message, it writes that ID to the new column. This works for every Workspace run, explicit or auto.
- **The frontend needs no change for the live pick.** `UaiRunController` already shows the agent name
  from `agent_selected`. Workspace never got one before, so it never showed a name.
- **Reopened chats:** the message mapper sets `agentName` from the message's agent ID, using the
  agent list Workspace already loads for its picker. An unknown or deleted agent shows no name.
- **Pickers see only this turn's messages in Workspace,** not the persisted history. This is a known
  gap, logged as a follow-up (see DECISION-LOG).

## Connected systems

| System | Applies? | Why |
|--------|----------|-----|
| Audit log | **Yes** | Selection reason goes in `AIAuditLog.Metadata` via runtime context `LogKeys` (the same route `RunId`/`ThreadId` take). No schema change. |
| AG-UI `agent_selected` event | **Yes** | Gains `selectorId` and `reason`. Additive, so existing listeners keep working. |
| Agent UI library (`Umbraco.AI.Agent.UI`) | **Yes** | Sends the previous pick in `forwardedProps`, widens the `resolvedAgent$` type. |
| Public docs (Umbraco.Docs) | **Yes** | A developer extension point is useless if nobody can find it. Add an "Extending > Agent selection" page. |
| v17 backport | **Yes** | Both lines are in active support. Port after v18 lands. |
| Persistence / migrations | **Yes (Workspace)** | Nullable `AgentId` on Workspace messages, with SQLite and SQL Server migrations. |
| Deploy connectors | No | No new entity. Conversations are per-user data, not deployed. |
| Version history | No | No new entity. |
| Copilot Workspace | **Yes** | Its own stream endpoint uses the selection service, emits `agent_selected` and shows agent names in reopened chats. |
| Notifications | **`*ed` only** | `AIAgentSelectedNotification` after every `auto` pick, for watching it. No cancelable `*ing` version, because selectors are already the way to change the pick. |
| Management API / OpenAPI client | **Regen (Workspace)** | Plain endpoint: no new route or DTO. Workspace: the message response model gains `agentId`, so the Workspace client is regenerated. |
| Localization | No | No new UI text. |

## Key decisions

- **Ordered collection builder over a single replaceable service.** Rejected: one
  `IAIAgentSelector` swapped via DI (only one package can win, and nothing composes); replacing
  `IAIAgentService` (forces copying the whole service); a notification handler (no clear ordering,
  and notifications are for watching or cancelling, not deciding).
- **Scope filtering happens in the service, before the chain, and results are checked against
  the candidates.** A third-party selector can never reach an agent the surface or context ruled
  out. Rejected: letting selectors see all agents and trusting them to filter.
- **Selectors return `null` for "no opinion"; the service owns the fallback.** Each selector stays
  single-purpose, and the fallback is recorded in one place. Rejected: a `Continue`/`Stop` result
  enum (more ceremony, no extra power).
- **A selector that throws is skipped, not fatal.** A buggy business rule shouldn't take Copilot
  down, and skipping can't break scope because filtering already ran. Cancellation still
  propagates.
- **Previous pick travels in AG-UI `forwardedProps.previousAgentId`.** The server stores no
  conversations on this line, so the browser is the only thing that knows. `forwardedProps` is the
  AG-UI spec's slot for extra data, is never shown to the model, and is already used (`resume`).
  Treated as an untrusted hint: resolved only against the candidates. Rejected: a context item
  (these can end up in the model's prompt); a new typed request field (steps outside the AG-UI
  spec).
- **Selection info reaches the audit log through a typed `AIAgentExecutionOptions.Selection`.**
  The controller calls the existing options overload of `StreamAgentAGUIAsync` with
  `new AIAgentExecutionOptions { Selection = result }`. This is the same as the default
  `new AIAgentExecutionOptions()` the plain overload already passes, plus the selection. The
  service writes `Umbraco.AI.Agent.SelectorId` and `Umbraco.AI.Agent.SelectionReason` into the
  run's additional properties and adds them to `LogKeys`. Rejected: a generic
  `AdditionalProperties` bag on options (untyped, and leaks internal context-key plumbing to
  callers).
- **The LLM classifier keeps its exact input and prompt.** Out-of-the-box picks must not change.
  Richer classifier input is a separate, behaviour-changing feature.
- **Only a `Selected` notification, no `Selecting`.** Developers can watch picks (logging,
  analytics) without writing a selector. A cancelable `Selecting` notification was rejected: it
  would be a second way to change the pick, with unclear precedence against the selector chain.
- **Sticky ships opt-in, not on by default.** Keeps today's re-pick-every-turn behaviour for
  existing sites, and still proves the previous-pick input works.
- **`abortRun()` keeps the previous pick; only `resetConversation()` clears it.** (Resolved during
  T2 build, formerly a TODO below.) The conversation is still live after an abort, so the next
  turn should keep sending `previousAgentId` and sticky shouldn't lose its memory. Only starting a
  genuinely new conversation should forget the pick.

- **Workspace's previous pick comes from an agent ID on each assistant message**, not a
  `LastAgentId` on the conversation and not the browser. Per-message data also lets reopened chats
  show which agent answered, and it handles regenerate without special cases. Rejected:
  `LastAgentId` (last turn only, needs resetting on regenerate); session state (meant for the AI
  framework's own state, and loaded later in the run); the browser (Workspace isn't `auto` on the
  wire, and the pick would be lost on reopen).

## TODO

- **Starter prompts interaction.** The starter-prompts feature (planned, not on `v18/dev` yet)
  pins a conversation to the starter's agent. That pin should send the explicit agent ID and skip
  `auto` entirely, so the two shouldn't conflict. Re-check when starter prompts land.
