# Draft Sets as an Interim Add-On — CMS Feasibility Investigation

**Can we deliver "Draft Sets" for Umbraco AI / the Umbraco MCP as a standalone package, without core CMS changes?**

- **Status:** Feasibility investigation — findings for AI-team discussion
- **Date:** 2026-07-06
- **Scope of investigation:** Umbraco CMS `main` branch (v18), read-only codebase analysis across five angles
- **Related:** [`2026-07-02-ai-native-content-editing-design.md`](2026-07-02-ai-native-content-editing-design.md) (the broader Draft Sets & Content Validators vision)

---

## 1. Context

The broader vision proposes **Draft Sets** as a core CMS capability: a unit of work grouping many draft content changes that stay inert until applied — an editor equivalent of a git branch.

This document assesses a narrower, tactical question raised in a team discussion:

> Could we build an **interim add-on package** that gives Umbraco AI and the Umbraco MCP a Draft Set abstraction *now* — create a named draft set, make content edits captured **in memory**, then **commit** the whole set to the database — without waiting for core CMS support?

The primary goal is a **safe sandbox for an LLM to define content changes** and apply them atomically, concentrating human approval at the single commit step. A stretch goal is **previewing the site as if a draft set were applied**. Keeping drafts in memory means no backoffice UI work and no schema changes. Shareable drafts are explicitly out of scope for v1 (beyond optionally sharing a draft-set name/id).

This is intended as a **playground to prove the idea** before committing to a core implementation.

---

## 2. Bottom line

**The core mechanism is feasible cleanly with no core changes.** The server-side content-edit path funnels through a single replaceable interface, and Umbraco already ships the exact "buffer-changes-in-memory-then-apply" pattern internally (`ShadowFileSystem`). Capturing edits into a named in-memory set and applying them atomically is well-supported.

**The preview promise bifurcates sharply:**
- Previewing **edits to existing pages** (changed property values) is a clean, supported extension.
- Previewing a **restructured tree** (added / moved / deleted nodes) is a brick wall for an add-on — tree structure and URL routing live in singleton/persisted services with no per-request overlay seam.

**The set is held as in-memory edit operations** — this was always the intended model, not a database transaction. Its terminal operations are **Apply** / **Apply & Publish** (see naming in §6); "commit"/"scope" are deliberately avoided as they are Umbraco's transaction vocabulary.

### Verdict by investigation angle

| # | Angle | Verdict |
|---|-------|---------|
| A | Intercepting/capturing content writes | ✅ **Feasible cleanly** (one caveat: reimplement an `internal sealed` interface) |
| B | Overlaying the published cache for preview | 🟡 **Clean for value edits; hacks for tree changes** |
| C | Routing + per-request preview context | 🟡 **Clean for value edits; brick wall for structural edits** |
| D | Constructing/serialising content in memory | ✅ **Feasible cleanly** |
| E | Prior art / reuse / Library coexistence | ✅ **Strong prior art; preview is the real cost** |

---

## 3. How content writes flow (and where we hook in)

The server-side document write path is single-seamed and DI-overridable:

1. **Management API controllers** (`src/Umbraco.Cms.Api.Management/Controllers/Document/*`) all depend on one interface — e.g. `CreateDocumentController.Create` → `_contentEditingService.CreateAsync(model, userKey)` (`CreateDocumentController.cs:62-66`); update/move/delete/copy/sort mirror this.
2. **`IContentEditingService`** (public interface, `src/Umbraco.Core/Services/IContentEditingService.cs`) is the funnel. Its implementation `ContentEditingService` is `internal sealed` (`src/Umbraco.Core/Services/ContentEditingService.cs:16-17`).
3. It builds/mutates an in-memory `IContent`, then calls `IContentService.Save`, where persistence **and all side effects** happen: `ContentSavingNotification` (cancelable) → repository write → `ContentSavedNotification` → `TreeChangeNotification` → audit → `scope.Complete()` (`PublishableContentServiceBase.cs:573-648`).
4. DI registration uses `AddUnique<IContentEditingService, ContentEditingService>()` (`UmbracoBuilder.cs:321`) — **`AddUnique` means a later registration replaces it.** This is the intended override seam.

**There is no existing deferred/batched/transactional content-set concept** in core (confirmed by search).

### Recommended interception seam

**Replace `IContentEditingService` via DI in an add-on composer.** When an ambient draft set is active, the replacement captures the operation instead of persisting; otherwise it delegates. Because the concrete class is `internal sealed`, we either reimplement the (small) interface using its public collaborators (`IContentService`, `IContentTypeService`, `IDocumentEditingPresentationFactory`, `IContentValidationService`) or capture the prior `ServiceDescriptor` and delegate to it for pass-through and commit-replay.

**Bonus — side-effect-free trial applies:** `ScopedNotificationPublisher` (`src/Umbraco.Core/Events/ScopedNotificationPublisher.cs:95-135`) *defers* all non-cancelable notifications (`Saved`, `TreeChange`, and therefore cache refreshers and Examine indexing) until scope completion, and exposes an official `Suppress()`. So edits can be trial-applied inside a scope that is never completed — the DB rolls back and no side effects fire — which is useful for validation without persistence.

**Caveat — publishing is separate.** `IContentEditingService` covers save/move/delete/sort only. Publish/unpublish live in `IContentPublishingService` (`src/Umbraco.Core/Services/ContentPublishingService.cs`). If a draft set must stage publish intent, a second parallel seam is required (same pattern).

---

## 4. Capturing and committing a set (the recommended core mechanism)

### In-memory construction is a first-class capability

`IContentService.Create()` explicitly returns a **non-persisted** `IContent` and never touches the DB — its own doc-comment says so (`ContentService.cs:328-330`); the body just constructs `new Content(...)` (`:357-373`). The object is freely mutable in memory: `SetValue(alias, value, culture, segment)` (`ContentBase.cs:491-506`), `SetCultureName` for variants (`:330-367`). Persistence happens **only** on `Save()`. So content can be created and mutated indefinitely without persisting.

### Capture format: the Management API editing DTOs

Do **not** serialise the live `IContent` graph (its `[Serializable]` attributes are vestigial; it holds event handlers, back-references, and change-tracking state). Instead capture the operation as the Management API's own editing DTOs — plain, DB-independent, Guid-keyed POCOs:

- `ContentCreateModel` / `ContentUpdateModel` — carry `Key`, `ContentTypeKey`, `ParentKey`, `TemplateKey`, `Properties`, `Variants` (`src/Umbraco.Core/Models/ContentEditing/`).
- `PropertyValueModel` = `{ Alias, object? Value, Culture, Segment }` (`PropertyValueModel.cs:6-27`).
- `VariantModel` = `{ Culture, Segment, Name }` (`VariantModel.cs:6-22`).

This format already has a **built-in capture → validate → replay lifecycle** in `ContentEditingService`:
- **Validate without persisting:** `ValidateCreateAsync` / `ValidateUpdateAsync` (`ContentEditingService.cs:87-118`) — gives the AI early feedback.
- **Replay at commit:** `CreateAsync` / `UpdateAsync` (`:121-189`).

It is also the natural output of an MCP/AI tool, and it serialises to JSON trivially (the only nuance: `object? Value` needs an untyped converter, and Umbraco already has `JsonObjectConverter`).

**Block editors** fit recursively: a Block List/Grid value is a JSON string deserialising to `BlockValue` (`Layout` + `ContentData`/`SettingsData`, each ultimately the same alias/value/culture/segment shape — `BlockValue.cs:9-59`). Capturing a block value is capturing a string; the AI must emit valid block JSON, but that burden is identical under any approach.

### Apply

The user-facing operation is **Apply** (deliberately *not* "commit" — that word is Umbraco's scope/transaction abstraction, see §6). Applying replays the captured operations through the real `CreateAsync`/`UpdateAsync` inside **one short-lived scope** opened at apply time. This gives atomicity for the write, fires all normal side effects (indexing, cache refresh, notifications, audit) exactly as a human edit would, and keeps the long-lived AI session entirely off the database.

A second operation, **Apply & Publish**, applies the set and then drives the publish pipeline (`IContentPublishingService`) for the affected content, publishing the whole set as one unit. This makes the publish seam (§3) a v1 requirement rather than an optional extra.

**Intra-set references:** newly-created content has no key until saved. The store must model temp keys for parents created earlier in the same set and resolve them to real keys during **ordered** replay.

### Prior art confirms the pattern

`ShadowFileSystem` (`src/Umbraco.Core/IO/ShadowFileSystem.cs:5-11`) is Umbraco's own in-memory-overlay-then-commit implementation for files: *"tracks changes without modifying the original… captures all file operations in memory and can later apply them."* A Draft Set is the content-layer equivalent. Content scaffolding (`DeepCloneWithResetIdentities`, `ContentScaffoldedNotification`) and Content Blueprints show the framework routinely builds detached in-memory content graphs.

---

## 5. Preview — feasible for value edits, brick wall for structural changes

### How `IPublishedContent` is served (v18 HybridCache)

- A request reaches content via `IUmbracoContext.Content` → `CacheManager.Content` → **`IPublishedContentCache`** (`UmbracoContext.cs:154`, `ICacheManager.cs:18`). `IPublishedContentCache` is implemented by `DocumentCache` (public sealed), delegating to the `internal sealed` `DocumentCacheService`.
- **Preview is a per-request `bool`**, threaded explicitly into every read (`GetById(bool preview, …)`), not a global state. It selects published vs. the persisted **draft** DB version. Preview mode is derived from the signed `UMB_PREVIEW` cookie + a backoffice identity (`UmbracoContext.cs:195-221`, `PreviewService.cs`, `PreviewAuthenticationMiddleware.cs`).
- **Critically, tree structure is NOT in the content node.** `IPublishedContent` carries only its own data; parent/children/ancestors come from a *separate* singleton **`IDocumentNavigationQueryService`** (returns ordered Guid keys), re-resolved through the cache. Route→key resolution uses a *persisted* **`DocumentUrlService`** URL-segment table keyed by draft/published (`DocumentUrlService.cs:900+`).

### Value edits on existing nodes — clean

1. **Ambient draft-set accessor:** mirror Umbraco's `HybridAccessorBase` (AsyncLocal + request-cache) — the same pattern used for `IUmbracoContextAccessor`/`IVariationContextAccessor`, so it flows through async and MCP calls. Carry the set id via a cookie/token using the existing preview middleware pattern so preview links work.
2. **Custom `IContentFinder`** (a clean, ordered, first-class seam — `IContentFinder.cs:6`, ordered collection at `UmbracoBuilder.Collections.cs:41-46`): when a set is active, resolve the route normally and return the node wrapped in **`PublishedContentWrapped`** (public decorator base, `PublishedContentWrapped.cs:20`) with the set's edited values overlaid. Let the real value converters run on the substituted raw values for rendering fidelity.

**Isolation rule:** the overlay must sit *in front of* the shared singleton cache and never write into its L0/L1/L2 tiers, or one user's draft leaks into everyone's published output.

### Structural changes (add / move / delete / reparent) — brick wall

Because structure and URLs live outside `IPublishedContent`:
- Route→key resolution walks the **persisted** `DocumentUrlService` table — an unpersisted new/moved node has no row, so its URL won't resolve and a moved node still resolves at its old path.
- Tree structure comes from the **singleton** `IDocumentNavigationQueryService` — no per-request overlay hook.
- Outbound URL generation (`IPublishedUrlProvider`/`DefaultUrlProvider`) re-derives from the same services, so an in-memory node yields no valid URL.

Supporting structural preview means shadowing/replacing several core singletons per-request and keeping them mutually consistent — invasive, fragile against core changes, and clearly "hacks" territory. **Recommendation: exclude structural-change preview from v1.** (It is arguably the strongest argument for eventually doing Draft Sets *in core*.)

The internal node/factory/model builder types (`PublishedContentFactory`, `PublishedContent`, `ContentNode`, etc.) are `internal` to the HybridCache assembly, so fabricating genuinely new nodes from an add-on requires hand-implementing `IPublishedContent`/`IPublishedProperty` — another reason to keep v1 to wrapping existing nodes.

### If we want structural preview: proposed CMS extension points

The structural wall is *not* fundamental — it comes down to two choke points, and small **additive, non-breaking** seams at each would let an add-on preview a restructured tree. This is the concrete ask to take to the CMS team; it phases cleanly (reorganising existing content needs far less than adding new content).

**Why it's tractable — two convergence points:**

- **Routing and outbound URLs both derive tree *structure* from a single interface, `IDocumentNavigationQueryService`.** Inbound resolution (`DocumentUrlService.GetDocumentKeyByRoute`, `DocumentUrlService.cs:935,1028-1036`) and outbound generation (`DefaultUrlProvider` → `DocumentUrlService.GetLegacyRouteFormat`, `DocumentUrlService.cs:1114`) both walk *that* for structure, reading only a per-node URL *segment string* from a separate store — and those segments are **parent-independent and already persisted** (`DocumentUrlService.cs:450-466`). So moving/reparenting/re-sorting/deleting an *existing* node changes only what navigation reports.
- **The published-content pipeline is already ~90% public.** The data DTOs (`ContentCacheNode`, `ContentData`, `PropertyData`), content-type lookup (`IPublishedContentTypeCache`), the value-converter machinery (`IPublishedPropertyType.ConvertSourceToInter/ConvertInterToObject`), `CreateModel(...)` (ModelsBuilder typed models), and `PublishedContentWrapped` (the public "wrap a node" story) are all public. Only the *assembler* that builds a node from raw data is `internal`.

**Proposed extension points (all additive; each mirrors an existing Umbraco pattern):**

| # | Extension point | Attaches to | Unblocks | Cost |
|---|-----------------|-------------|----------|------|
| 1 | **Navigation overlay seam** — inject `IEnumerable<INavigationOverlayProvider>` consulted inside the `TryGet*` methods, or officially bless decoration of the interface | `IDocumentNavigationQueryService` (registered `UmbracoBuilder.cs:382`) | **move / reparent / re-sort / delete** of existing nodes — for *both* inbound routing and outbound URLs at once | Additive; potentially **zero-code** if decoration is blessed |
| 2 | **Public node builder** — expose a Core `IPublishedContentBuilder`, or make the existing `IPublishedContentFactory` public | HybridCache factory (`Factories/IPublishedContentFactory.cs:9`) | **fabricating new nodes** that run the real value converters + typed models — the "build" counterpart to the public "wrap" (`PublishedContentWrapped.cs:20`) | Near one-liner; additive |
| 3 | **URL-segment overlay hook** — optional provider consulted by segment lookups for nodes with no persisted row | `DocumentUrlService.GetUrlSegment` / `GetChildWithUrlSegment` (`DocumentUrlService.cs:450,1038`) | giving a **new** node a routable URL segment | Additive (matches the default-interface-method evolution style already used on `IDocumentUrlService`) |
| 4 | **Preview-participation tolerances** — keep the preview-unfiltered filtering branch a documented contract; make `IsPublished`/`IsDraft` tolerant of keys unknown to the DB | `IPublishedContentStatusFilteringService` (`PublishedContentStatusFilteringService.cs:52-57`), `IDocumentPublishStatusQueryService` (used at `PublishedElement.cs:213-220`) | synthetic nodes surviving tree-query filtering and returning sane publish state under preview | Small/additive |
| — | *Already open — just keep them* | `IContentFinder`, `IUrlProvider` ordered collections (`UmbracoBuilder.Collections.cs:41-46,161`) | the entry points to apply the above | Zero |

**How it phases:**

- **Tier 1 — reorganising existing content** (move, reparent, re-sort, delete): needs **#1 alone** (existing nodes keep their persisted segments and are already real cache nodes). If blessing decoration counts, this is *zero core code* — just a supported contract. Covers a large share of "sweeping content changes."
- **Tier 2 — adding new content**: adds **#2 + #3 + #4**. Still all additive; #2 is essentially a make-public.

**Cross-cutting design notes (to include in any proposal):**

1. **Everything gates on a per-request ambient signal the add-on owns** — built exactly like `HybridUmbracoContextAccessor` (`AsyncLocal` + `IRequestCache`). Core need only *consult injected providers*; when none is active, behaviour is byte-for-byte unchanged, so nothing leaks into the Delivery API, Examine, sitemaps, or the backoffice.
2. **Read-time layering, never mutation** of the singleton navigation tree.
3. **Layers on top of the existing `preview`/`isDraft` bool** — same mental model, no new global state.

One optional refactor worth flagging: having `DocumentNavigationService` derive ancestors/descendants/siblings/level from the three primitives (parent / ordered children / roots) would shrink an overlay's surface from ~10 methods to 3 and guarantee coherence between the inbound and outbound walks.

**Net ask to the CMS team:** essentially *one navigation-overlay seam* (does most of the work, both routing directions) *+ one make-public* (node fabrication) *+ two small tolerances* — a far more fundable proposal than "make structural preview work," and the natural bridge from this interim add-on toward first-class core Draft Sets.

---

## 6. Coexistence and naming (from the prior-art scout)

- **The Library section is real in v18** — a shipping section (`Umb.Section.Library`) hosting reusable global **Elements**. `IElement : IPublishableContentBase` — Elements are first-class *publishable content* with their own versioning, publish pipeline, and controllers. A Draft Set spanning "all content" therefore inherently touches Elements; they use the same edit/publish pipeline, so they are in-scope by default (and must be handled, not ignored).
- **Variants (culture/segment)** are pervasive — capture must key edits by `(entity, culture, segment)`, and rollback/validation are per-culture.
- **Naming — avoid "scope".** "Scope" is Umbraco's unit-of-work/transaction abstraction (`ICoreScope`, `IScope`, `ScopeProvider`, ambient scope stack). Using it for this feature collides semantically and in code. **"Draft Set" / "Change Set" reads cleanly** and has no existing content-domain collision. Avoid overloading "Element" too (now the Library entity).
- **Existing preview infra cannot help directly:** `/umbraco/preview` sets a cookie so rendering uses each item's *persisted* draft version — there is no hook to inject uncommitted, in-memory state. The overlay (§5) is the only route, confirming preview is the real engineering cost.

### Naming & terminology (decided)

- **Feature name: "Draft Sets"** — package/namespace `Umbraco.Cms.DraftSets`. Chosen because it (a) extends a concept editors already know — today a node has one *draft*; a Draft Set makes drafts groupable and lets a node carry drafts in multiple named sets; (b) fits Umbraco's descriptive house style (Content Blueprints, Content Apps, Variants, Segments); (c) is free as a compound identifier (`DraftSet` — zero collisions in the CMS codebase); and (d) reads as an editor feature, which matters because the name will carry into the marketplace and potentially into core. The `Umbraco.Cms.*` prefix (rather than `Umbraco.AI.*`) reflects that this is general CMS infrastructure with multiple consumers (Umbraco.AI **and** the Umbraco MCP), not an AI-specific package.
- **Alternatives considered:** *Changeset* (zero collision and the most literal description of the mechanism, but developer/VCS-flavoured and cold for editors — held in reserve if the feature is ever repositioned as pure dev/MCP infrastructure); *Workspace* (**ruled out** — the backoffice editing surface is already "the workspace", 1,057 frontend references); *Branch* (**ruled out** — collides with content-tree "branch"/subtree and the git meaning); *Sandbox* (implies throwaway, undersells apply-to-live).
- **Apply verbs (decided):** the two terminal operations are **Apply** (materialise the set's changes as drafts) and **Apply & Publish** (apply *and* publish the whole set as one unit). "Apply" is used in preference to "commit" both to avoid the scope/transaction overload and because it reads more naturally to editors. Discarding a set is **Discard**.

---

## 7. Recommended v1 package shape

A **safe LLM content sandbox**, shipped as an add-on with no core changes:

1. **Draft Set** = a named, optionally JSON-persistable list of editing-DTO operations (`ContentCreateModel`/`ContentUpdateModel`), keyed by Guid, held in an in-memory (or DB-backed) store behind an ambient accessor. Never called "scope".
2. **Capture** by replacing `IContentEditingService`; **validate** on capture via `ValidateCreate/UpdateAsync`; **Apply** by atomic replay in one short-lived scope; **Discard** by dropping the set.
3. **Preview** = value-edits-on-existing-nodes only (accessor + custom `IContentFinder` + `PublishedContentWrapped` overlay). Structural-tree preview explicitly out of scope.
4. **MCP/AI tools:** `create-draft-set`, edit content (auto-captured while a set is active), `list-changes`, `preview`, `apply`, `apply-and-publish`, `discard`.
5. **Publishing:** in scope for v1 via **Apply & Publish** — a set can be applied as drafts (`Apply`) or applied and published as a unit (`Apply & Publish`), the latter using the `IContentPublishingService` seam (§3).

This delivers the transformational property — *a safe place to be wrong*, with Apply as the single human approval gate — and is buildable today.

---

## 8. Open questions / risks to carry into design

- **Persistence of the set:** pure in-memory is single-instance and lost on restart; persisting the captured DTOs as JSON gives named, restart-surviving, name-shareable sets on one instance (multi-instance/load-balanced would need shared storage). Which does v1 want?
- **Concurrent sets touching the same node**, and **trunk drift** (live content changing under an open set) — the merge/conflict questions from the core vision still apply at **Apply** time even in the interim package; v1 can likely block-on-conflict rather than merge.
- **Reimplementing `IContentEditingService`** vs. delegate-via-captured-`ServiceDescriptor` — pick one; both are ordinary decorator work but the interface surface must be tracked against core updates.
- **Value-converter fidelity** for previewed block/complex properties — validate rendering matches a real save.
- **Temp-key resolution** for intra-set parent/child references on ordered replay.

---

## Appendix: investigation method

Five parallel read-only investigations of the CMS `main` (v18) codebase:

- **A — Write interception seam:** the content edit pipeline and where to divert writes.
- **B — Published cache overlay:** HybridCache and overlaying `IPublishedContent` for preview.
- **C — Routing + preview context:** request routing, preview-mode plumbing, ambient context injection.
- **D — Content model construction:** building and serialising content in memory for capture and replay.
- **E — Prior art / adjacent features:** reusable patterns, the Library section, naming collisions.

File/line references throughout point to `/Users/matt/Documents/Work/Umbraco/Umbraco.Cms` on `main` and may drift with core changes.
