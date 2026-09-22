---
task: design-embeddable-knowledge-sets-for-llm-integration
type: structure-outline
repo: Umbraco.AI
branch: v18/feature/design-embeddable-knowledge-sets-for-llm-integration
sha: f11342e2ac118279b8e74001d072b2ead87bcd2b
supersedes: 04-structure-outline-knowledge-sets.md
---

# Knowledge Sets — realign to async content + Contexts-mirroring UI

This is a **revision** of the in-flight Knowledge Sets implementation (see
`04-structure-outline-knowledge-sets.md`). Phases 1–3 of that outline were built on the feature branch
`v18/feature/design-embeddable-knowledge-sets-for-llm-integration`, but two areas diverged from the
approved design discussion (`03-design-discussion-embeddable-knowledge-sets.md`) and are corrected here:

1. **Content is currently eager** — `AIKnowledgeSetItem` exposes a plain `string Content`, mapped onto the
   built-in `text` resource type with `AIEditableModelResolver.EscapeLiteral(...)` to dodge the `$`-config
   wart. The design calls for **lazy, async content** via `GetContentAsync`, materialised only at format
   time through a **Core-internal `knowledge-content` resource type** — no `text` type, no escape helper.
2. **The backoffice UI does not mirror Contexts** — it is a bespoke table + a detail workspace that dumps
   every item's full markdown inline in `<pre>` blocks. The design calls for a **read-only clone of the
   Context UI**: a card-grid item list (mirroring `<uai-resource-list>`) whose cards open a **read-only
   content modal** (mirroring `<uai-resource-options-modal>`) that **lazily fetches** each item's markdown
   from a new per-item endpoint.

> **Where the code lives.** The feature is on the worktree at
> `/Users/matt/.humanlayer/workspaces/design-embeddable-knowledge-sets-for-llm-integration/Umbraco.AI/Umbraco.AI/`
> (branch above), **not** on `v18/dev`. All paths below are relative to that repo root
> (`src/Umbraco.AI.Core/...`, `src/Umbraco.AI.Web/...`, `src/Umbraco.AI.Web.StaticAssets/Client/...`).

## Desired End State

- `AIKnowledgeSetItem` is `{ Key, Name, Description, GetContentAsync }` — content is a
  `Func<CancellationToken, Task<string>>` fetched only when consumed; a convenience factory wraps a literal
  string for the common static case. No author sees a resource type, settings, or injection mode.
- A **Core-internal `knowledge-content` resource type** is the single seam that materialises content: its
  `ResolveDataAsync((setId, key))` locates the set + item and awaits `GetContentAsync` at format time;
  `FormatDataForLlm` returns the markdown. It is **invisible** to package authors and the Context
  resource-type picker. `AIEditableModelResolver.EscapeLiteral` is gone (it existed only for the abandoned
  eager `text` path and is unreleased).
- `KnowledgeSetContextResolver` emits each item as an `OnDemand` `AIContextResource` with
  `ResourceTypeId = "knowledge-content"` and `Settings = KnowledgeContentRef(set.Id, item.Key)` (a
  *reference*, never the content), with a deterministic GUID derived from `(set.Id, item.Key)`.
- The admin API defers content: `GET /v1/knowledge-sets/{id}` returns item **metadata only**
  (`{ key, name, description }`); a new `GET /v1/knowledge-sets/{id}/item/{key}` awaits `GetContentAsync`
  and returns the markdown, so expensive/computed content is fetched only when an admin actually views it.
- The backoffice mirrors the Context entity UI, read-only: a collection (context-table pattern, no
  create/bulk/selection), a routable read-only workspace (`knowledge-set-workspace-editor` mirroring
  `context-workspace-editor` + a **Details** view + an **Info** view), a `<uai-knowledge-item-list>` card
  grid (read-only mirror of `<uai-resource-list>`), and a `<uai-knowledge-item-modal>` (read-only mirror of
  `<uai-resource-options-modal>`) that renders markdown fetched lazily from the per-item endpoint. The
  inline `<pre>` rendering is removed.
- Runtime behaviour is unchanged for the model: knowledge still surfaces OnDemand via
  `list_context_resources` / `get_context_resource`; only *when* the content is materialised changes
  (deferred to the async `ResolveDataAsync` seam).

## Implementation Overview

- [~] Phase 1: Async content model reaches the LLM (Core + internal resource type + resolver) — automated verification complete, awaiting manual verification
- [~] Phase 2: Lazy admin API — detail returns metadata, new per-item content endpoint — automated verification complete, awaiting manual verification
- [~] Phase 3: Contexts-mirroring read-only UI with click-to-view content modal — automated verification complete, awaiting manual verification

---

## Phase 1: Async content model reaches the LLM (Core + internal resource type + resolver)

The vertical slice that changes *how content is materialised*, end-to-end to the model. Convert the item
to an async producer, introduce the internal `knowledge-content` resource type as the deferred-fetch seam,
and rewire the resolver to emit references instead of baked-in text. After this phase, installing a set
still makes its knowledge available OnDemand, but full content is pulled only at format time
(`get_context_resource`), never eagerly at resolve time.

### File Changes

**The item — from eager string to async producer:**

- **`src/Umbraco.AI.Core/Contexts/KnowledgeSets/AIKnowledgeSetItem.cs`**: replace the eager `Content`
  string with a stable `Key` + async producer, plus a static factory for the literal case.
  ```csharp
  public sealed class AIKnowledgeSetItem
  {
      public required string Key { get; init; }          // stable identity within the set (GUID + API url)
      public required string Name { get; init; }
      public string? Description { get; init; }          // the OnDemand breadcrumb the LLM sees
      public required Func<CancellationToken, Task<string>> GetContentAsync { get; init; }

      // convenience for the common static case — no async ceremony for simple sets
      public static AIKnowledgeSetItem FromContent(string key, string name, string content, string? description = null)
          => new() { Key = key, Name = name, Description = description, GetContentAsync = _ => Task.FromResult(content) };
  }
  ```

**New — the internal deferred-fetch resource type (the one new server-side piece):**

- **`src/Umbraco.AI.Core/Contexts/KnowledgeSets/KnowledgeContentRef.cs`**: tiny settings record carried by
  the emitted resource — `{ string KnowledgeSetId; string ItemKey; }`. Reference only; never the content.
- **`src/Umbraco.AI.Core/Contexts/KnowledgeSets/KnowledgeContentResourceType.cs`**: `internal sealed`
  `IAIContextResourceType` (id `"knowledge-content"`). `ResolveDataAsync` casts settings to
  `KnowledgeContentRef`, `GetById(setId)` → `GetItemsAsync` → find by `ItemKey` → `await GetContentAsync(ct)`;
  `FormatDataForLlm` returns the markdown as-is. Takes `AIKnowledgeSetCollection`.
  - **Deliberately not derived from `AIContextResourceTypeBase`** and **not attribute-decorated**:
    the base ctor requires `[AIContextResourceType]` and routes settings through
    `AIEditableModelResolver.ResolveModel` (the `$`-config path we are escaping *away* from). Implementing
    `IAIContextResourceType` directly keeps content literal and keeps the type out of
    `TypeLoader.GetTypesWithAttribute` discovery. *(Plan detail — visibility: see Open Questions.)*
  - **Graceful degradation** (design "Item content"): a missing set/item or a throwing `GetContentAsync`
    returns an error/empty block rather than failing the whole request. Optionally memoise per
    `(setId, key)` within a request to avoid a double fetch if the same item is materialised twice.
- **`src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs`**: register the internal type
  explicitly (it has no attribute, so it is not auto-discovered), beside the resource-type discovery block
  (~line 233): `builder.AIContextResourceTypes().Add<KnowledgeContentResourceType>();`.

**Resolver — emit a reference, drop the escape hack:**

- **`src/Umbraco.AI.Core/Contexts/KnowledgeSets/KnowledgeSetContextResolver.cs`**: change the emitted
  resource (currently `text` + `TextResourceSettings{ Content = EscapeLiteral(item.Content) }`, GUID from
  `item.Name`) to:
  ```diff
  - ResourceTypeId = "text",
  - Settings = new TextResourceSettings { Content = _modelResolver.EscapeLiteral(item.Content) },
  - Id = CreateResourceId(knowledgeSet.Id, item.Name),
  + ResourceTypeId = "knowledge-content",
  + Settings = new KnowledgeContentRef { KnowledgeSetId = knowledgeSet.Id, ItemKey = item.Key },
  + Id = CreateResourceId(knowledgeSet.Id, item.Key),
  ```
  Remove the `IAIEditableModelResolver _modelResolver` ctor dependency (no longer used). `ContextName` /
  `ContextDescription` grouping stays as-is. `CreateResourceId` now hashes `(set.Id, item.Key)`.

**Remove the now-dead escape helper (unreleased):**

- **`src/Umbraco.AI.Core/EditableModels/IAIEditableModelResolver.cs`** + **`AIEditableModelResolver.cs`**:
  remove `EscapeLiteral` (interface default + override). It was added solely for the eager `text` path,
  which no longer exists, and has never shipped. *(If we would rather keep it as a general-purpose helper,
  leave it but it becomes unused — recommend removal to avoid dead API.)*

**Sample + fixtures — update to the async shape:**

- **`demos/v18/Umbraco.AI.DemoSite/Knowledge/EngageKnowledgeSet.cs`**: give each item a `Key` and build via
  `AIKnowledgeSetItem.FromContent(...)` (or an inline `GetContentAsync`).
- **`tests/Umbraco.AI.Tests.Common/Fakes/FakeKnowledgeSet.cs`**: items now carry `Key` + `GetContentAsync`;
  add a helper to seed an item whose producer throws (for the graceful-degradation test).

### Test File Changes

- **`tests/Umbraco.AI.Tests.Unit/Contexts/KnowledgeSets/KnowledgeSetContextResolverTests.cs`**: update
  assertions — each item becomes an `OnDemand` resource with `ResourceTypeId == "knowledge-content"` and
  `Settings` a `KnowledgeContentRef(set.Id, item.Key)`; GUIDs are stable across two resolves and derive
  from `Key` (rename an item, assert the GUID is unchanged). Drop the `$`-escape assertion.
- **`tests/Umbraco.AI.Tests.Unit/Contexts/KnowledgeSets/KnowledgeContentResourceTypeTests.cs`** (new):
  `ResolveDataAsync` awaits `GetContentAsync` and returns markdown; unknown set/item and a throwing
  producer degrade gracefully; `GetContentAsync` receives the `CancellationToken`; optional per-request
  memoisation fetches once. Follows the directly-constructed-collection pattern over `FakeKnowledgeSet`.
- **`tests/Umbraco.AI.Tests.Unit/EditableModels/AIEditableModelResolverTests.cs`**: delete the
  `EscapeLiteral` cases added for the old path.

### Validation

#### Automated Verification

- [x] `dotnet build Umbraco.AI/Umbraco.AI.slnx`
- [x] `dotnet test Umbraco.AI/Umbraco.AI.slnx` (resolver + new resource-type tests pass; no `EscapeLiteral` refs remain)

#### Manual Verification

- [ ] With the Engage sample set installed, start the demo (`/demo-site-management start`) and ask the agent
      a question answerable only from a set item; confirm it calls `get_context_resource` and answers from
      the item's content (content now materialised lazily at that call, not at resolve time).

---

## Phase 2: Lazy admin API — detail returns metadata, new per-item content endpoint

A thin vertical slice through Web → generated client that makes the admin API match the async model: the
detail endpoint stops returning content, and a new per-item endpoint fetches it on demand. This is what the
Phase 3 modal will consume.

### File Changes

**Detail DTO — metadata only, keyed:**

- **`src/Umbraco.AI.Web/Api/Management/KnowledgeSets/Models/KnowledgeSetDetailResponseModel.cs`**: change the
  nested `KnowledgeSetItemModel` from `{ Name, Description, Content }` to `{ Key, Name, Description }` — drop
  the inline `Content` (items are no longer materialised for the listing).
- **`src/Umbraco.AI.Web/Api/Management/KnowledgeSets/Mapping/KnowledgeSetMapDefinition.cs`**: the
  `AIKnowledgeSetItem → KnowledgeSetItemModel` map now maps `Key` and drops `Content`. The controller keeps
  passing resolved items via `ItemsKey` (map action stays sync).

**New — per-item content endpoint:**

- **`src/Umbraco.AI.Web/Api/Management/KnowledgeSets/Models/KnowledgeSetItemContentResponseModel.cs`** (new):
  `{ string Key; string Content; }` (content ships in the assembly, not secret — fine to return for audit).
- **`src/Umbraco.AI.Web/Api/Management/KnowledgeSets/Controllers/ByKeyKnowledgeSetItemController.cs`** (new):
  `GET {id}/item/{key}` — `GetById(id)` → `GetItemsAsync` → find by `Key` → `await GetContentAsync(ct)` →
  `KnowledgeSetItemContentResponseModel`. 404 (`KnowledgeSetNotFound()` / an item-not-found helper) when the
  set or key is unknown. Mirrors `ByIdKnowledgeSetController` shape;
  `[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]`.
- **`src/Umbraco.AI.Web/Api/Management/Common/Controllers/UmbracoAICoreManagementControllerBase.cs`**: add a
  `KnowledgeSetItemNotFound()` helper beside `KnowledgeSetNotFound()` (or reuse a generic not-found).

### Test File Changes

- No existing map-definition/controller test harness for this area (research §7 / §5); rely on build +
  generate-client + manual API checks. Optionally add a `KnowledgeSetMapDefinitionTests` if the keyed-item
  mapping warrants it.

### Validation

#### Automated Verification

- [x] `dotnet build Umbraco.AI/Umbraco.AI.slnx`
- [x] `npm run generate-client` (against a running demo site) — confirm `getKnowledgeSetItemContent` (or the
      generated name for `{id}/item/{key}`) appears in `api/sdk.gen.ts`, `KnowledgeSetItemContentResponseModel`
      in `api/types.gen.ts`, and that `KnowledgeSetItemModel` no longer carries `content`.

#### Manual Verification

- [ ] `GET /ai/management/api/v1/knowledge-sets/{id}` returns items as `{ key, name, description }` with **no**
      content; unknown id → 404.
- [ ] `GET /ai/management/api/v1/knowledge-sets/{id}/item/{key}` returns the item's markdown; unknown set or
      key → 404.

---

## Phase 3: Contexts-mirroring read-only UI with click-to-view content modal

Replace the bespoke knowledge-set frontend with a read-only clone of the Context entity UI, and wire the
item cards to a content modal that lazily fetches markdown from the Phase 2 endpoint. Structure and manifest
kinds mirror `Client/src/context/*`; every mutation affordance is stripped (design "Backoffice — closely
mirror the Contexts UI, read-only").

### File Changes

**Collection — align to the context-table pattern (read-only):**

- **`Client/src/knowledge-set/collection/views/table/knowledge-set-table-collection-view.element.ts`**:
  mirror `context-table-collection-view.element.ts` (columns: icon, name, description, item count) but keep
  `allowSelection: false` and no bulk actions. Name links to the set's read-only workspace route.
- Collection/root-workspace manifests stay `kind: "default"` with a `collectionView` (table) — no
  `collectionAction` (create) and no `bulk-action`, unlike Context.

**Routable read-only workspace — mirror `context/workspace/context/*`:**

- **`Client/src/knowledge-set/workspace/knowledge-set/knowledge-set-workspace.context.ts`**: keep it a
  **non-submittable** routable context (load-only; `submit` is a no-op / absent), unlike
  `UaiContextWorkspaceContext` which extends `UmbSubmittableWorkspaceContextBase`. It loads the set via the
  detail repository (metadata + item list) and exposes it as observable state. No command store, no
  validation context, no save.
- **`Client/src/knowledge-set/workspace/knowledge-set/knowledge-set-workspace-editor.element.ts`**: mirror
  `context-workspace-editor.element.ts` but strip the editable name `<uui-input>` / alias `<uui-input-lock>`
  / async uniqueness — render a read-only title + back button inside `<umb-workspace-editor>`.
- **`.../views/knowledge-set-details-workspace-view.element.ts`**: mirror
  `context-details-workspace-view.element.ts` — a `<uui-box>` wrapping the new `<uai-knowledge-item-list>`.
  **Replaces** the current inline `<pre>` rendering.
- **`.../views/knowledge-set-info-workspace-view.element.ts`** (new): mirror
  `context-info-workspace-view.element.ts` but Info-only (Id / metadata) — **no** `<uai-version-history>`
  (code-defined items have no versions).
- Workspace manifests gain the **Info** `workspaceView` and drop any Save `workspaceAction`.

**New components — item list + content modal (mirror `resource-list` + `resource-options-modal`, read-only):**

- **`Client/src/knowledge-set/components/knowledge-item-list/knowledge-item-list.element.ts`** (new):
  `<uai-knowledge-item-list>`, read-only mirror of `<uai-resource-list>`. Renders each item as a
  `<uui-card-block-type>` (name + description, item icon), **no** "Add" button, **no** remove action, **no**
  injection-mode tag editing. Clicking a card (`@open`) opens the content modal (below) — the exact mirror
  point where Context opens the *edit* options modal.
- **`.../components/knowledge-item-list/knowledge-item-modal.token.ts`** + **`knowledge-item-modal.element.ts`**
  (new): `<uai-knowledge-item-modal>`, read-only mirror of `<uai-resource-options-modal>`. Data:
  `{ knowledgeSetId, item: { key, name, description } }`. On open it calls the Phase 2 per-item endpoint via
  the detail repository/data source and renders the returned markdown read-only (e.g. `<umb-code-block>` /
  markdown render). Static Name/Description; **no** `<uai-model-editor>`, **no** injection-mode select, **no**
  Save — Close only. This dovetails with lazy fetch: content loads only when the modal opens.
- **`.../components/index.ts`** + **`.../components/knowledge-item-list/index.ts`**: barrel registration
  (Lessons Learned: export through `index.ts`, never import components by path).
- **`Client/src/knowledge-set/components/manifests.ts`** (new): register the modal (`modal` manifest), spread
  into `knowledge-set/manifests.ts`.

**Repository — read path + per-item content fetch:**

- **`Client/src/knowledge-set/repository/detail/knowledge-set-detail.server.data-source.ts`**: `read()` maps
  the metadata-only detail (items now `{ key, name, description }`); add a `readItemContent(id, key)` method
  calling the generated per-item endpoint, mapped to a content model. Keep the repo read-only (no
  create/save/delete overrides).
- **`Client/src/knowledge-set/types.ts`** + **`type-mapper.ts`**: item model gains `key`, loses inline
  `content`; add a `UaiKnowledgeSetItemContentModel` + `toItemContentModel`.

**Localization:**

- **`Client/src/lang/en.ts`** (`uaiKnowledgeSet` group): add modal terms (content label, close, on-demand
  note), item-list empty state; drop keys tied to the removed inline detail view.

### Test File Changes

- No frontend test harness exists for this client (research §7) — rely on build/typecheck + manual verification.

### Validation

#### Automated Verification

- [x] `npm run build:core` (after Phase 2's `generate-client`) — the new UI type-checks against the generated
      per-item endpoint + keyed item model.

#### Manual Verification

- [ ] **AI Configuration → Knowledge Sets** lists installed sets (name, description, item count); no
      create/delete/bulk/selection affordances.
- [ ] Opening a set shows a read-only workspace mirroring the Context editor: a **Details** view with an item
      **card grid** (not `<pre>` dumps) and an **Info** view (Id/metadata, no version history).
- [ ] Clicking an item card opens the read-only content modal, which fetches and renders that item's markdown
      (network call to `{id}/item/{key}` fires on open, not before) — with no editable fields or Save.

---

## Cross-Version Note

Per CLAUDE.md "Keep Active Versions in Sync": developed on the `v18` line. Once merged, confirm with the user
whether Knowledge Sets should be ported to `v17/dev` (active support) via the Backport Workflow. See
`backport-note.md`.

## Open Questions

- **Internal resource-type visibility.** ✅ Resolved via **option (b)**: added a non-breaking default
  interface member `bool IsInternal => false;` on `IAIContextResourceType`; `KnowledgeContentResourceType`
  overrides it to `true`. `AllContextResourceTypeController` filters out `IsInternal` types before mapping
  (which is the sole data source for the Context resource-type picker, so it is hidden there too), and
  `ByIdContextResourceTypeController` returns 404 for internal types for a consistent author-visible view.
  Chosen over (a) because the flag is self-documenting on the type and scales cleanly if a second internal
  type appears.
- **Per-request content memoisation.** Confirm whether to cache `GetContentAsync` results per `(setId, key)`
  within a request (the design lists it as optional) — worthwhile if an item can be materialised more than
  once per request (e.g. listed then fetched), otherwise skip for v1 simplicity.
- **Item `Key` on the demo/sample sets.** `Key` is now required and drives the deterministic GUID + API url.
  Confirm the Engage sample uses stable, url-safe keys (e.g. `"goals"`, `"segments"`) distinct from the
  display `Name`.
- **Markdown rendering in the modal.** Confirm the backoffice component to render markdown read-only
  (`<umb-code-block>` for raw, or a markdown renderer) — mirror whatever `<uai-model-editor>`/core uses so the
  modal stays consistent with the rest of the UI.
