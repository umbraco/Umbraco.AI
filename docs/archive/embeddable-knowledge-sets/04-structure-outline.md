---
task: design-embeddable-knowledge-sets-for-llm-integration
type: structure-outline
repo: Umbraco.AI
branch: v18/dev
sha: 5aa77cf83eed5c17d6afdc64394f0cae0a909a79
---

# Embeddable Knowledge Sets for LLM Integration

Introduce **Knowledge Sets** — a code-defined, package-embeddable primitive (`IAIKnowledgeSet`) that
lets an add-on ship background knowledge about its product (e.g. "Umbraco Engage") by dropping a single
decorated class into its assembly. The knowledge is discovered at startup, flows to the LLM through the
**existing** context resolution → on-demand pipeline unchanged, and is visible read-only in the
backoffice so admins can audit exactly what a package contributed. No DB, no composer, no CRUD.

## Desired End State

- A package author ships knowledge by adding one `[AIKnowledgeSet(id, name)]`-decorated
  `IAIKnowledgeSet` class — zero registration, no composer, no migration, no DB row (the same
  experience as shipping an `IAIContextResourceType`).
- Every installed knowledge set is auto-active: its items appear in `AIResolvedContext.OnDemandResources`
  on every request, advertised cheaply by name + description, with full markdown pulled by the model via
  the existing `get_context_resource` tool. No per-profile/per-request configuration in v1.
- Knowledge-set items are mapped by the new resolver into real (sealed) `AIContextResource`s with
  `ResourceTypeId = "text"`, `InjectionMode = OnDemand`, and deterministic namespaced GUIDs — so
  `AIContextResolutionService`, `AIContextProcessor`, and the context tools work with **zero changes**.
- Admins get a read-only "Knowledge Sets" section under **AI Configuration**: a listing of installed sets
  and a read-only per-set view showing each item's name/description/content. No create/edit/delete, no
  pickers, no toggles.
- `AIContext` is untouched — user-authored Contexts remain the customer-specific mechanism; Knowledge
  Sets are the package-shipped complement.

## Implementation Overview

- [ ] Phase 1: Core primitive + resolver — knowledge reaches the LLM end-to-end
- [x] Phase 2: Read-only listing — API + backoffice section (admin sees installed sets)
- [ ] Phase 3: Read-only detail — API + workspace (admin audits a set's items)

---

## Phase 1: Core primitive + resolver — knowledge reaches the LLM end-to-end

The vertical slice that delivers the actual value: define the discovered primitive, wire attribute
discovery exactly like `IAIContextResourceType`, and add the resolver that maps each item into the
shared context pipeline. After this phase, installing a package that ships an `IAIKnowledgeSet` makes its
knowledge available to chat/agent requests via the existing on-demand tools.

All new Core code lives under `Umbraco.AI/src/Umbraco.AI.Core/Contexts/KnowledgeSets/` (beside
`Contexts/ResourceTypes/`, its closest sibling).

> **Scope change during implementation — context attribution in the on-demand listing (agreed with
> product owner).** The original outline promised "no changes to the resolution service, processor,
> middleware, or tools." During Phase 1 we found the on-demand breadcrumb list `AIContextProcessor`
> injects into the system prompt renders each resource by bare **name + description only**, with no
> indication of *which context* it came from. With multiple installed knowledge sets (e.g. Engage +
> Commerce) a bare item like "Goals" is ambiguous, and the model has no signal for whether a group is
> worth a `get_context_resource` call. We therefore extended Phase 1 to **group the on-demand listing by
> context**, rendering a `### {ContextName}` heading plus an optional context description per group.
> This deliberately widens the slice beyond "zero pipeline changes":
> - **New (additive, non-breaking) field `ContextDescription`** on `AIContextResolverResource` and
>   `AIResolvedResource`, mapped through in `AIContextResolutionService`. `ContextName` already existed
>   end-to-end and was simply not rendered.
> - **`KnowledgeSetContextResolver`** populates `ContextDescription` from the set's `Description`. The
>   existing `ProfileContextResolver`/`ContentContextResolver` leave it `null` (no `AIContext.Description`
>   field exists), so the heading shows the context **name** only for user-authored contexts.
> - **`AIContextProcessor`** groups `OnDemandResources` by `ContextName` (stable first-seen order) and
>   emits the heading + description before each group's items.
> - This improves attribution for **all** on-demand contexts, not just knowledge sets, and is covered by
>   new `AIContextProcessorTests` plus an extended resolver test asserting `ContextDescription` flows
>   through.

### File Changes

**New — the primitive (author-facing surface):**

- **`Contexts/KnowledgeSets/IAIKnowledgeSet.cs`**: `IDiscoverable` marker interface + metadata +
  an async item producer. Mirrors `IAIContextResourceType`'s shape.
  ```csharp
  public interface IAIKnowledgeSet : IDiscoverable
  {
      string Id { get; }
      string Name { get; }
      string? Description { get; }
      string? Icon { get; }
      Task<IReadOnlyList<AIKnowledgeSetItem>> GetItemsAsync(CancellationToken cancellationToken = default);
  }
  ```
- **`Contexts/KnowledgeSets/AIKnowledgeSetAttribute.cs`**: `[AttributeUsage(Class, Inherited=false)]`,
  ctor `(string id, string name)` + settable `Description`/`Icon`. Copy of `AIContextResourceTypeAttribute`.
- **`Contexts/KnowledgeSets/AIKnowledgeSetItem.cs`**: the sealed author-facing record — **no** type, id,
  settings, or injection mode (locking OnDemand structurally, per design).
  ```csharp
  public sealed class AIKnowledgeSetItem
  {
      public required string Name { get; init; }
      public string? Description { get; init; }   // the OnDemand breadcrumb the LLM sees
      public required string Content { get; init; } // markdown, injected as-is
  }
  ```
- **`Contexts/KnowledgeSets/AIKnowledgeSetBase.cs`**: abstract base reading its attribute reflectively in
  the ctor (throw if missing), exposing `Id`/`Name`/`Description`/`Icon`; author overrides
  `GetItemsAsync`. Direct analogue of `AIContextResourceTypeBase`'s ctor (`AIContextResourceTypeBase.cs:74-86`).

**New — discovery wiring (mirror of resource types):**

- **`Contexts/KnowledgeSets/AIKnowledgeSetCollection.cs`**: `BuilderCollectionBase<IAIKnowledgeSet>`
  with `GetById(string id)` (case-insensitive), copying `AIContextResourceTypeCollection`.
- **`Contexts/KnowledgeSets/AIKnowledgeSetCollectionBuilder.cs`**:
  `LazyCollectionBuilderBase<AIKnowledgeSetCollectionBuilder, AIKnowledgeSetCollection, IAIKnowledgeSet>`.
- **`Configuration/UmbracoBuilderExtensions.Context.cs`**: add `AIKnowledgeSets()` extension returning
  `builder.WithCollectionBuilder<AIKnowledgeSetCollectionBuilder>()` (next to `AIContextResourceTypes()` at lines 34-35).
- **`Configuration/UmbracoBuilderExtensions.cs`** (in `AddUmbracoAICore`, beside resource-type discovery at 233-236):
  ```csharp
  builder.AIKnowledgeSets()
      .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIKnowledgeSet, AIKnowledgeSetAttribute>(cache: true));
  ```

**New — the resolver (mapping into the existing pipeline):**

- **`Contexts/KnowledgeSets/KnowledgeSetContextResolver.cs`**: `internal sealed : IAIContextResolver`.
  Iterates **all** discovered sets (auto-active, no filtering in v1), calls `GetItemsAsync`, and maps each
  item to an `AIContextResolverResource` tagged with the set's name as `ContextName`. It is the **sole**
  constructor of the real resource shape (design "Content model"):
  It takes `IAIEditableModelResolver` (see next item) purely to escape literal content:
  ```csharp
  // per item -> the resolver bakes in text type + OnDemand + a deterministic GUID
  new AIContextResolverResource {
      Id = DeterministicGuid(set.Id, itemKey),      // namespaced UUIDv5-style hash; stable across restarts
      ResourceTypeId = "text",
      Name = item.Name,
      Description = item.Description,
      Settings = new TextResourceSettings { Content = _modelResolver.EscapeLiteral(item.Content) },
      InjectionMode = AIContextResourceResourceInjectionMode.OnDemand,
      ContextName = set.Name,
  };
  ```
  - `DeterministicGuid` = name-based hash of `set.Id + itemKey` (item key derived from `item.Name`), so
    dedup, caching, and `get_context_resource` keep working and namespace-collisions with user contexts
    are impossible (design "Resource identity"). *Plan detail:* confirm whether `Guid.CreateVersion5`
    (net10) is available or add a small SHA1-based helper.
  - **Escape literal content via the EditableModel feature (decision — supersedes the design doc's
    "accepted papercut").** `Content` routes through `AIEditableModelResolver.ResolveModel`, which runs
    `$`-config resolution on **every** public string property — it is **not** gated by `[AIField]` or
    `IsSensitive` (`AIEditableModelResolver.cs:99-123`; `IsSensitive` only restricts *secret-prefixed* keys,
    lines 161-168). A value whose first char is `$` that doesn't match an allowed prefix **throws** at
    request time (`AIEditableModelResolver.cs:149-157`) — so package markdown like `$5 per month` or `$ref`
    would hard-fail context injection. Because knowledge-set content is always literal (never a config
    reference), the resolver escapes it before constructing the resource.

**New — escape helper owned by the EditableModel feature (so escape co-evolves with the `$`/`$$` syntax):**

- **`EditableModels/IAIEditableModelResolver.cs` + `AIEditableModelResolver.cs`**: add
  `string? EscapeLiteral(string? value)` next to the existing `$`/`$$` logic and the `ConfigPrefix`
  constant (`AIEditableModelResolver.cs:24`, un-escape at `138-141`). It is the inverse of the un-escape
  step — prefixing one `$` to any value already starting with `$`:
  ```csharp
  // co-located with ConfigPrefix + the $$ un-escape so a syntax change updates both together
  public string? EscapeLiteral(string? value)
      => value is not null && value.StartsWith(ConfigPrefix, StringComparison.Ordinal) ? ConfigPrefix + value : value;
  ```
  This is lossless — round-trips `$5` → `$$5` → `$5`, and even `$$x$$` → `$$$x$$` → `$$x$$` (the un-escape
  strips exactly one leading `$`) — and invisible to package authors. Owning it here (rather than the
  knowledge-set resolver hardcoding `"$" + content`) means the escape and the syntax it reverses live in
  one place and cannot drift. *Reused by:* `KnowledgeSetContextResolver`; available to any future
  literal-content producer.
- **`Configuration/UmbracoBuilderExtensions.cs`**: append the resolver **after** Content so the chain is
  Profile → Content → KnowledgeSet (design "Resolver ordering"), extending lines 246-249:
  ```csharp
  builder.AIContextResolvers()
      .Append<ProfileContextResolver>()
      .Append<ContentContextResolver>()
      .Append<KnowledgeSetContextResolver>();
  ```

**New — test fixture (so discovery + resolver are automatically verifiable):**

- **`tests/Umbraco.AI.Tests.Common/Builders/AIKnowledgeSetBuilder.cs`** (or a `Fakes/FakeKnowledgeSet.cs`):
  a fluent builder / fake `IAIKnowledgeSet` yielding known items, mirroring `AIContextBuilder`.

### Test File Changes

- **`tests/Umbraco.AI.Tests.Unit/Contexts/KnowledgeSets/KnowledgeSetContextResolverTests.cs`**: new — follow
  `ProfileContextResolverTests` (real `AIRuntimeContext([])`, mocked `IAIRuntimeContextAccessor`, mocked
  `AIKnowledgeSetCollection` via directly-constructed collection over fakes, Shouldly). Assert: every item
  becomes an `OnDemand` resource with `ResourceTypeId="text"`, correct `Content`/`Name`/`Description`,
  `ContextName == set.Name`, and that GUIDs are **stable across two resolves** and namespaced. Include a
  content-starting-with-`$` item and assert it is escaped so it survives `AIContextProcessor` round-trip
  (i.e. `ProcessResourceForLlmAsync` returns the original literal, not a thrown/substituted value).
- **`tests/Umbraco.AI.Tests.Unit/EditableModels/AIEditableModelResolverTests.cs`**: add cases for
  `EscapeLiteral` — `null`/no-`$` unchanged; `$x` → `$$x`; `$$x$$` → `$$$x$$`; and a round-trip test asserting
  `ResolveModel` on a model whose escaped value un-escapes back to the original literal.
- **`tests/Umbraco.AI.Tests.Unit/Contexts/KnowledgeSets/AIKnowledgeSetBaseTests.cs`**: new — attribute
  reflected into `Id`/`Name`/`Description`/`Icon`; missing attribute throws (mirrors resource-type base behavior).
- **`tests/Umbraco.AI.Tests.Unit/Contexts/KnowledgeSets/AIKnowledgeSetCollectionTests.cs`**: new —
  `GetById` case-insensitive lookup over a directly-constructed collection (the `ServiceResolutionTests`
  pattern that bypasses `TypeLoader`).

### Validation

#### Automated Verification

- [x] `dotnet build Umbraco.AI/Umbraco.AI.slnx`
- [x] `dotnet test Umbraco.AI/Umbraco.AI.slnx` (new resolver/base/collection tests pass)

#### Manual Verification

- [ ] Add a sample knowledge set to the demo site (e.g. a small `DemoKnowledgeSet : AIKnowledgeSetBase`
      with 2-3 items) and start it via `/demo-site-management start`.
- [ ] In the agent chat, ask a question answerable only from the sample content; confirm the model calls
      `list_context_resources` / `get_context_resource` and answers from the knowledge-set item, with no
      Context authored by hand.

---

## Phase 2: Read-only listing — API + backoffice section (admin sees installed sets)

A thin vertical slice through Web → generated client → backoffice: expose `GET /v1/knowledge-sets` and a
read-only "Knowledge Sets" section that lists installed sets. Mirrors the read-only `context-resource-type`
Management API and the `audit-log` read-only frontend (root workspace + collection, no per-entity editing).

### File Changes

**Backend — Management API (mirror `Api/Management/ContextResourceTypes/`):**

- **`Umbraco.AI.Web/Constants.cs`**: add a `Feature.KnowledgeSets` block
  (`RouteSegment = "knowledge-sets"` — plural, matching sibling routes like `context-resource-types`;
  `GroupName = "Knowledge Sets"`), beside `ContextResourceTypes` (175-186).
- **`Api/Management/KnowledgeSets/Controllers/KnowledgeSetControllerBase.cs`**: abstract base with
  `[ApiExplorerSettings(GroupName = …KnowledgeSets.GroupName)]` +
  `[UmbracoAIVersionedManagementApiRoute(…KnowledgeSets.RouteSegment)]`, deriving
  `UmbracoAICoreManagementControllerBase`. Copy of `ContextResourceTypeControllerBase`.
- **`Api/Management/KnowledgeSets/Controllers/AllKnowledgeSetController.cs`**: `GET` returning
  `IEnumerable<KnowledgeSetResponseModel>` from `AIKnowledgeSetCollection` via `IUmbracoMapper.MapEnumerable`.
  Copy of `AllContextResourceTypeController` (`[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]`).
- **`Api/Management/KnowledgeSets/Models/KnowledgeSetResponseModel.cs`**: list DTO
  `{ string Id; string Name; string? Description; string? Icon; int ItemCount; }` (design's list shape).
- **`Api/Management/KnowledgeSets/Mapping/KnowledgeSetMapDefinition.cs`**: `IMapDefinition` mapping
  `IAIKnowledgeSet → KnowledgeSetResponseModel` (`ItemCount` computed from `GetItemsAsync`; *plan detail:*
  decide whether to compute count eagerly in the controller and pass via `MapperContext`, since map
  actions are synchronous). Follow `ContextResourceTypeMapDefinition`.
- **`Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs`**: register the map definition in
  `AddUmbracoAIMapDefinitions` (add `.Add<KnowledgeSetMapDefinition>()` beside line 69).

**Frontend — read-only listing (model on `context-resource-type` repos + `audit-log`/`context` listing):**

- **`Client/src/knowledge-set/{entity.ts,constants.ts,types.ts,type-mapper.ts,index.ts,manifests.ts}`**:
  entity types (`UAI_KNOWLEDGE_SET_ENTITY_TYPE`, `UAI_KNOWLEDGE_SET_ROOT_ENTITY_TYPE`), UI models,
  response→model mapper, feature barrel.
- **`Client/src/knowledge-set/repository/collection/*`**: `UaiKnowledgeSetCollectionRepository`
  (`UmbRepositoryBase`, `UmbCollectionRepository`) + server data source calling the generated
  `KnowledgeSetsService.getAllKnowledgeSets(...)` wrapped in `tryExecute`, mapping via the type-mapper
  (copy of `context-collection.*`).
- **`Client/src/knowledge-set/collection/*`**: `collection` (kind `default`) + `collectionView` (table)
  manifests + a table view element (columns: icon, name, description, item count) — **no** create/bulk-delete
  actions (unlike Context).
- **`Client/src/knowledge-set/menu/manifests.ts`**: `menuItem` (kind `entityContainer`,
  `menus: [UAI_CONFIGURATION_MENU_ALIAS]`, `childEntityTypes: [UAI_KNOWLEDGE_SET_ENTITY_TYPE]`), copy of
  `context/menu/manifests.ts`.
- **`Client/src/knowledge-set/workspace/knowledge-set-root/manifests.ts`**: root `workspace` (kind
  `default`) + `workspaceView` (kind `collection`, `collectionAlias`), copy of `context-root` — no entity
  actions.
- **`Client/src/lang/en.ts`**: add a `uaiKnowledgeSet` localization group (label, description, empty-list,
  item-count, "surfaced on demand" note).
- **`Client/src/manifests.ts`**: spread `...knowledgeSetManifests` into the root array.

### Test File Changes

- No frontend test harness exists for this client (research §7); rely on build/typecheck + manual verification.
- Optionally add **`tests/Umbraco.AI.Tests.Unit/…/KnowledgeSetMapDefinitionTests.cs`** if the item-count
  mapping logic warrants it (no existing map-definition tests, so optional).

### Validation

#### Automated Verification

- [x] `dotnet build Umbraco.AI/Umbraco.AI.slnx`
- [ ] `npm run generate-client` (against a running demo site) — confirm a `KnowledgeSetService` appears in `api/sdk.gen.ts`
      — **not run**: requires a running demo site, which this session was instructed not to start. Deferred to
      manual verification.
- [ ] `npm run build:core` (frontend typecheck/build passes) — **fails as expected**: the new frontend code
      references the not-yet-generated `KnowledgeSetService`/`KnowledgeSetResponseModel` symbols (per plan
      instructions). Confirmed via a temporary, reverted stub of the generated client that the rest of the new
      code type-checks cleanly once those symbols exist — the only errors are the two unresolved-import errors
      and their two cascading type-inference errors. Re-run `npm run generate-client` then `npm run build:core`
      once a demo site is available to confirm cleanly.

#### Manual Verification

- [ ] With the demo site running and the Phase 1 sample set installed, `GET /ai/management/api/v1/knowledge-sets`
      returns the set with `itemCount`.
- [ ] In the backoffice, **AI Configuration → Knowledge Sets** shows a table listing the sample set (name,
      description, item count); confirm there are no create/delete/bulk actions.

---

## Phase 3: Read-only detail — API + workspace (admin audits a set's items)

Complete the transparency story: `GET /v1/knowledge-sets/{id}` returns full item content, and a read-only
routable workspace renders each item's name/description/markdown so an admin can audit exactly what the LLM
can see, with a note that items are surfaced OnDemand.

### File Changes

**Backend:**

- **`Api/Management/KnowledgeSets/Controllers/ByIdKnowledgeSetController.cs`**: `GET {id}` (string id),
  `AIKnowledgeSetCollection.GetById(id)` → 404 via a `KnowledgeSetNotFound()` helper (add to
  `UmbracoAICoreManagementControllerBase` alongside `ResourceTypeNotFound()` at 68-73, or reuse a generic
  not-found). Returns a detail DTO. Copy of `ByIdContextResourceTypeController`.
- **`Api/Management/KnowledgeSets/Models/KnowledgeSetDetailResponseModel.cs`**: metadata +
  `IEnumerable<KnowledgeSetItemModel> Items` where `KnowledgeSetItemModel = { Name, Description?, Content }`
  (design's detail shape — full content returned inline; it ships in the assembly, not secret).
- **`Api/Management/KnowledgeSets/Mapping/KnowledgeSetMapDefinition.cs`**: add the
  `IAIKnowledgeSet → KnowledgeSetDetailResponseModel` map (items resolved via `GetItemsAsync`; *plan detail:*
  resolve items in the controller and pass through `MapperContext` since the map action is sync).

**Frontend — read-only per-set workspace (model on `context/workspace/context/`, stripped to read-only):**

- **`Client/src/knowledge-set/repository/detail/*`**: `UaiKnowledgeSetDetailRepository` +
  server data source calling `KnowledgeSetService.getKnowledgeSetById(...)`, mapping to a detail model
  (pattern of `context-resource-type/repository/detail/*` — read-only, no create/save).
- **`Client/src/knowledge-set/workspace/knowledge-set/*`**: routable `workspace` (kind `routable`) +
  `workspace context` (load-only; no `submit`/save action, unlike `UaiContextWorkspaceContext`) +
  a single read-only `workspaceView` element rendering metadata + an items list (each item's `Name`,
  `Description`, and `Content` markdown) + the "surfaced OnDemand" note. **No** `workspaceAction` (Save),
  **no** entity actions, **no** property editors.
- **`Client/src/knowledge-set/workspace/manifests.ts`** + `paths.ts`: barrel + path patterns (copy of
  `context/workspace/**`, dropping create/save/delete wiring).
- **`Client/src/lang/en.ts`**: add detail-view terms (item heading, content label, on-demand note).

### Test File Changes

- Build/typecheck + manual verification (no frontend test harness).

### Validation

#### Automated Verification

- [x] `dotnet build Umbraco.AI/Umbraco.AI.slnx`
- [ ] `npm run generate-client` (against a running demo site) — confirm `getKnowledgeSetById` appears in
      `api/sdk.gen.ts` and `KnowledgeSetDetailResponseModel`/`KnowledgeSetItemModel` in `api/types.gen.ts`
      — **not run**: requires a running demo site, which this session was instructed not to start. Deferred to
      manual verification.
- [ ] `npm run build:core` — **not run cleanly for the same reason**: the new detail/workspace code references
      the not-yet-generated `KnowledgeSetsService.getKnowledgeSetById` / `KnowledgeSetDetailResponseModel` /
      `KnowledgeSetItemModel` symbols. Confirmed via a temporary, reverted stub of the generated client that the
      rest of the new Phase 3 code type-checks cleanly once those symbols exist — the only errors are the
      unresolved-import errors and their cascading type-inference errors. Re-run `npm run generate-client` then
      `npm run build:core` once a demo site is available to confirm cleanly.

#### Manual Verification

- [ ] `GET /ai/management/api/v1/knowledge-sets/{id}` returns metadata + items with full `content`; unknown id → 404.
- [ ] In the backoffice, clicking the sample set opens a read-only workspace showing its metadata and each
      item's name/description/rendered markdown, plus the OnDemand note — with no editable fields, save, or delete.

---

## Cross-Version Note

Per CLAUDE.md "Keep Active Versions in Sync": this is a new feature developed on `v18/dev`. Once merged,
confirm with the user whether it should be ported to `v17/dev` (active support) via the Backport Workflow.

## Open Questions

- **Item-count / item resolution in map definitions**: `IUmbracoMapper` map actions are synchronous but
  `GetItemsAsync` is async. Preferred resolution (to confirm in the plan): the controller awaits
  `GetItemsAsync` and passes the resolved items/count into the `MapperContext`, keeping map definitions
  sync — rather than making `IAIKnowledgeSet` expose a synchronous item accessor.
- **Deterministic GUID mechanism**: confirm `Guid.CreateVersion5` availability on `net10.0`; if unavailable,
  add a small internal name-based (SHA1) GUID helper under `Contexts/KnowledgeSets/`.
- **Item key for GUID derivation**: use `item.Name` as the per-item key (documented constraint: item names
  should be unique within a set), or introduce an optional stable `Key`/ordinal — decide in the plan.
