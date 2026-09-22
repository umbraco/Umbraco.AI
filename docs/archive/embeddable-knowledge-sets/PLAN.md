# Embeddable Knowledge Sets for LLM Integration

> **Status:** Shipped on both lines — PR #252 (v18) and #253 (v17), both merged. Kept here for
> historical reference only. Original ask: "Investigate the idea of knowledge sets. Similar to
> contexts / context resources but purely embeddable by package providers. The idea is that we can
> create knowledge sets for a particular subject (i.e. Umbraco Engage) and this could be used by an
> LLM to understand that product more."

## What problems was I solving

Today the only way to give the LLM reusable, named background knowledge is an **`AIContext`** — a user-authored, database-backed entity an admin builds by hand in the backoffice. A package that wants the AI to simply *understand its product* (e.g. Umbraco Engage — its concepts, terminology, how-tos) has **no first-class way to ship that knowledge**. Its only options are to make the customer recreate a Context by hand, or register a bespoke context resolver in a composer. There is also no notion of knowledge that is *available because a package is installed* — Contexts must be explicitly selected to reach the model.

This introduced **Knowledge Sets**: a code-defined, package-embeddable primitive. A package author drops a single decorated class into their assembly and its knowledge becomes available to the LLM — **no composer, no migration, no DB row, no CRUD UI**. Installing the package *is* the entire enablement action. Once shipped, a customer asking the agent "how do I configure an Engage goal?" gets an accurate answer grounded in the Engage knowledge set, with nobody having hand-built a Context.

The design is deliberately conservative: knowledge is expressed as ordinary `AIContextResource`s flowing through the **existing** resolution → injection → on-demand pipeline, so the only new server-side concept is one Core-internal resource type. Content is materialised **lazily** — only when the model (or an admin) actually reads it.

## What user-facing changes shipped

- `Contexts/KnowledgeSets/IAIKnowledgeSet.cs` — new public primitive a package implements to ship knowledge (Id/Name/Description/Icon + async `GetItemsAsync`).
- `Contexts/KnowledgeSets/AIKnowledgeSetItem.cs` — author-facing item `{ Key, Name, Description, GetContentAsync }` with a `FromContent` literal factory; no resource type, settings, or injection mode to configure.
- `Configuration/UmbracoBuilderExtensions.cs` — knowledge sets auto-discovered via `[AIKnowledgeSet]`; every installed set is auto-active OnDemand.
- Read-only **Management API**: `GET /v1/knowledge-set`, `GET /v1/knowledge-set/{id}`, and `GET /v1/knowledge-set/{id}/item/{key}` (content fetched lazily).
- Read-only **backoffice section** "Knowledge Sets" under AI Configuration — collection listing (with search), a per-set workspace, and a content modal that lazily loads a topic's markdown on open. Items surface to admins as "Topics".
- `Contexts/AIContextProcessor.cs` — on-demand resources are now grouped under a `###` heading per source context, so the model can tell which package a resource belongs to.

## How it was implemented

### Core — the primitive (`Umbraco.AI.Core/Contexts/KnowledgeSets/`)
- `IAIKnowledgeSet.cs`, `AIKnowledgeSetAttribute.cs`, `AIKnowledgeSetBase.cs` (reads its own attribute reflectively), and `AIKnowledgeSetCollection.cs` — an exact mirror of the `IAIContextResourceType` discovery pattern.

### Core — the lazy-content seam
- `KnowledgeContentRef.cs` — the `{ KnowledgeSetId, ItemKey }` pointer carried in settings (never the content).
- `KnowledgeContentResourceType.cs` — the single new server concept. It implements `IAIContextResourceType` **directly** (not via the base class) to keep content literal, out of the `$`-config resolution path, and invisible to authors/pickers. `ResolveDataAsync` re-locates the item and `await`s `GetContentAsync` at format time, degrading gracefully (missing set/item or throwing producer → logged + empty content; `OperationCanceledException` rethrown).

### Core — the resolver + wiring
- `KnowledgeSetContextResolver.cs` — the sole constructor of the real sealed `AIContextResource`, always `OnDemand`, tagged with the set as `ContextName`/`ContextDescription`. Each item gets a deterministic namespaced UUIDv5 over `"{setId}\0{itemKey}"` — stable across restarts/renames, never colliding with user context GUIDs.
- `UmbracoBuilderExtensions.cs` registers discovery, `.Add<KnowledgeContentResourceType>()` (explicit, no attribute), and `.Append<KnowledgeSetContextResolver>()` after Content.
- Attribution plumbing: an optional `ContextDescription` added to `AIContextResolverResource` / `AIResolvedResource` and flowed through `AIContextResolutionService` into the grouped processor output.

### Web — read-only Management API (`Umbraco.AI.Web/Api/Management/KnowledgeSets/`)
- `AllKnowledgeSetController`, `ByIdKnowledgeSetController` (metadata only), `ByKeyKnowledgeSetItemController` (lazy content), and `KnowledgeSetMapDefinition` — mirroring the `context-resource-type` controllers.

### Core/Web — keep the internal type out of author-facing surfaces
- A non-breaking `IsInternal` default-interface property added to `IAIContextResourceType` (defaults to `false`); `KnowledgeContentResourceType` sets it `true`. The `AllContextResourceTypeController` filters internal types out of the listing and `ByIdContextResourceTypeController` treats them as not-found — so the `knowledge-content` seam is registered and resolvable at runtime but never appears in the resource-type API or the Context picker.

### Frontend — read-only Contexts UI clone (`Client/src/knowledge-set/`)
~40 Lit/TypeScript files cloning the Context entity UI with all mutation removed: menu item, collection (+ search), workspace, and a repurposed content modal (edit → view). Labels centralised in the `uaiKnowledgeSet` localization group, plus a regenerated OpenAPI client.

## Deviations from the plan

Compared against the design discussion and structure outlines (see `04-structure-outline.md`, superseded by `05-structure-outline-async-and-ui-sync.md` in this folder).

### Implemented as planned
- `IAIKnowledgeSet` / attribute / base / collection — exact mirror of the resource-type discovery pattern.
- `AIKnowledgeSetItem` final shape `{ Key, Name, Description, GetContentAsync }` + `FromContent` factory, per the 05 async realignment.
- Core-internal `KnowledgeContentResourceType` implements the interface directly, registered explicitly (no attribute), with graceful degradation. The 04 `text`-type eager-content path was fully removed.
- The internal `knowledge-content` type is hidden from the resource-type listing API and Context resource-type picker via a new non-breaking `IsInternal` default-interface flag — 05's flagged open question, now resolved.
- `KnowledgeSetContextResolver` with reference-only settings, OnDemand mode, and the deterministic namespaced GUID.
- Context-attribution grouping (`ContextName`/`ContextDescription`) shipped as designed.
- Read-only Management API (three controllers) and Contexts-mirroring read-only backoffice, including the repurposed content modal.
- Test coverage: base, collection, resolver, resource-type, processor, plus `FakeKnowledgeSet`.

### Deviations / surprises
- **Info workspace view removed after being built.** 05 called for both a Details view *and* an Info view. Both were implemented, then the Info view + its `workspaceView` manifest entry were deliberately deleted to simplify the read-only UI. Only the Details view ships.
- Stale XML doc on `ByIdKnowledgeSetController` — its `<summary>` still says "including its items and their full content", a holdover from the 04 eager design. Behaviour is correct (metadata only); doc-only drift.

### Additions not in the plan
- Search/filter toolbar on the collection — a UX refinement not in either design doc.
- "Topics" presentation label — domain/API terminology stays "item", but the backoffice surfaces items as "Topics", centralised into localization.
- Menu-item ordering fix — a small weight adjustment so the menu item sits above default AI Configuration entries.

### Items planned but not implemented
- Per-request content memoisation in `ResolveDataAsync` — explicitly optional in the design; each resolution re-invokes `GetContentAsync`.
