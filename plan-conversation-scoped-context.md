# Plan: Conversation-scoped context & attachments (Context panel items 6–7)

> **STATUS: BUILT on `v18/feature/copilot-workspace`** (2026-07-22), pending review; not yet ported to
> v17. Backend (hoist, domain, persistence + Sqlite/SqlServer migrations, injection merge, API + client
> regen) and the editable panel are all in place; `dotnet build` + unit tests green; verified live in
> the demo site (add a conversation-scoped context → persists across reload). Composer-attachment
> promotion (Q6) intentionally NOT built. Key files:
> - Core: `AIAttachedResource.cs` (hoisted), `AIConversation.cs` (ContextIds/Resources)
> - Persistence: `AIConversationEntity`, `AIConversationResourceEntity`, `AIConversationEntityFactory`
>   (now instance), `EFCoreAIConversationRepository`, `UmbracoAIConversationsDbContext`, +
>   `UmbracoAIConversations_ConversationContext` migration (both providers)
> - Injection: `ConversationRuntimeContextBuilder.cs` + merge in `StreamConversationAGUIController`
> - API: Conversation request/response models + `ConversationMapDefinition`
> - Frontend: `workspace-context-panel.element.ts` (inherited + this-conversation), `conversation.repository.ts`


Follow-up to the Context sidebar polish pass. Covers the two open questions from the feedback round:

- **(6)** Should chat composer attachments show in the Context panel?
- **(7)** Should we allow attaching contexts/resources to a *single conversation* (not the whole project)?

Both reduce to one decision: **give a conversation its own context layer that stacks on top of the
project's.** The Context panel then reads as: *inherited from project (read-only)* **+** *added to this
conversation (editable)*.

## Where things stand today (grounding)

- **Project is the only context source.** A conversation stores just `ProjectId`
  (`AIConversationEntity` / `AIConversation`); all grounding comes from its project.
- **Injection path.** `StreamConversationAGUIController` (the one endpoint binding a run to a
  conversation) sets
  `AdditionalProperties = await BuildProjectContextAsync(conversation.ProjectId, …)`, which calls
  `ProjectRuntimeContextBuilder.Build(project)`. That builder emits two runtime-context properties:
  - `ContextKeys.AdditionalContextIds` — from `project.ContextIds`, honoured by `ProfileContextResolver`.
  - `ContextKeys.AdditionalResources` — framing + instructions + the project's resources (each an
    `AIContextResolverResource` with a per-resource `InjectionMode`), honoured by
    `AdditionalResourcesContextResolver`.
- **Panel** (`workspace-context-panel.element.ts`) is read-only and resolves the project by
  `conversationId`/`projectId`; contexts render via a readonly `<uai-context-picker>`, resources via a
  readonly `<uui-ref-list>`.
- **Composer attachments** (Agent.UI `input.element.ts`) upload as **temporary files** keyed to the
  AG-UI `threadId` (= conversation id). They are effectively conversation-scoped already, but are
  presented per-message and are **not** persisted as first-class resources or shown in the panel.

## Target model

Add a conversation-owned context layer that mirrors the project's shape and merges *on top* of it.

```
effective context = project (inherited, read-only in panel)
                  + conversation (this conversation only, editable in panel)
```

### 1. Domain + persistence (`Umbraco.AI.Agent.Conversations`)

- **Hoist the resource type (decided).** Rename `AIProjectResource` → shared **`AIAttachedResource`**
  (`Id, ResourceTypeId, Name, Description, Settings, InjectionMode, SortOrder`) used by both projects
  and conversations. Keep `AIProjectResource` as a thin `[Obsolete("Will be removed in v20")]` subclass/
  alias so the public API doesn't break (per repo backwards-compat rule).
- Add to `AIConversation` / `AIConversationEntity`:
  - `IList<Guid> ContextIds` — referenced `AIContext` ids, same as project.
  - `IList<AIAttachedResource> Resources` — directly-attached resources.
- Persist as JSON columns exactly like the project does (see `AIProjectEntity` + its factory). One
  migration per provider: `UmbracoAIAgent_` prefix, SQLite + SQL Server.
- Repository/service: extend `IAIConversationService` update path to accept the new fields (services
  own their repositories per repo convention — no direct repo access from controllers).

### 2. Injection merge (`Umbraco.AI.Agent.Copilot.Workspace.Web`)

- Add `ConversationRuntimeContextBuilder.Build(conversation)` mirroring `ProjectRuntimeContextBuilder`
  (no framing/instructions synthesis — just `AdditionalContextIds` from the conversation's ids and
  `AdditionalResources` from its resources).
- In `StreamConversationAGUIController.BuildProjectContextAsync` (rename → `BuildRuntimeContextAsync`),
  merge the two property dictionaries:
  - `AdditionalContextIds`: concat + dedupe (project first, then conversation).
  - `AdditionalResources`: project resources first (framing → instructions → project resources), then
    conversation resources appended in author order — preserves the deliberate ordering comment in the
    project builder.
- Per-resource `InjectionMode` stays the single source of truth for always-in-prompt vs.
  fetched-on-demand — no special-casing conversation resources.

### 3. Composer attachments — deferred (Q6 = NO, decided)

**No for now.** Composer file uploads stay transient per-message (threadId-scoped temporary files) and
are **not** surfaced in the Context panel. Rationale kept for the record: surfacing transient
per-message uploads in a panel that reads as persistent state would be misleading, and promoting them to
first-class conversation resources is a separate increment. Revisit after Q7 ships. This means the
Resources block's "this conversation" half (§5) is fed only by the explicit resource picker, not by
composer attachments.

### 4. API (`Umbraco.AI.Agent.Conversations.Web`)

- Extend `UpdateConversationRequestModel` + `ConversationResponseModel` with `contextIds` and
  `resources` (mirror `ProjectRequestModel` / `ProjectResponseModel` and their map definitions).
- Regenerate the OpenAPI client (`npm run generate-client` — needs the demo site running).

### 5. Panel becomes editable at conversation scope (answers Q7)

Keep the **type-based blocks** chosen in the polish pass (Instructions / Contexts / Resources); within
the two attachable blocks, split inherited vs. conversation-own:

- **Instructions** — unchanged (project only; a conversation has no instructions of its own in v1).
- **Contexts** — readonly inherited contexts (as now) **+** an editable `<uai-context-picker multiple>`
  for the conversation's own ids, with an Add control. Writes go through the conversation update
  endpoint.
- **Resources** — readonly inherited `<uui-ref-list>` **+** an editable `<uai-resource-list>` for the
  conversation's own resources (added via the resource picker only — composer attachments are out of
  scope per Q6).

Mirror the project details editor (`project-details-workspace-view.element.ts`) for the editable
controls — it already uses `<uai-context-picker>` and `<uai-resource-list>` with change handlers. The
panel currently loads a read-only `ProjectResponseModel`; it will also need the conversation's own
context/resources (from the conversation response model) to render the editable half.

## Sequencing

1. Hoist `AIAttachedResource` + domain (conversation `ContextIds`/`Resources`).
2. Persistence + migrations (SQLite + SQL Server).
3. Injection merge + `ConversationRuntimeContextBuilder`.
4. API models + mapping + client regen.
5. Panel editable half (contexts + resources).

(Composer attachment promotion — dropped for now per Q6.)

## Decisions locked

- **Shared resource type → HOIST (clean rename).** `AIProjectResource` becomes `AIAttachedResource`.
  No `[Obsolete]` alias: the whole Conversations/Projects domain is unreleased (initial migration
  2026-07-20 on this feature branch), so the backwards-compat rule — which targets *released* public
  API — doesn't apply.
- **Composer attachments (Q6) → NO** for now (see §3).
- **Version lines → v18 only for now.** Build to completion on `v18/feature/copilot-workspace`; port to
  `v17/dev` once the feature is complete (per the repo backport workflow).
