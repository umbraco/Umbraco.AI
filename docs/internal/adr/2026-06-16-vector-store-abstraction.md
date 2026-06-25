# ADR: Vector store abstraction — adopt Microsoft.Extensions.VectorData?

- **Status:** Proposed (decision pending)
- **Date:** 2026-06-16
- **Deciders:** Umbraco.AI maintainers
- **Scope:** `Umbraco.AI.Search` vector storage layer
- **Related:** [[project_meai_adoption_opportunities]]

## Context

`Umbraco.AI.Search` persists and queries embeddings through a hand-rolled abstraction, `IAIVectorStore` (`Umbraco.AI.Search.Core/VectorStore/IAIVectorStore.cs`):

```
UpsertAsync, DeleteAsync, DeleteDocumentAsync, SearchAsync,
GetVectorsByDocumentAsync, ResetAsync, GetDocumentCountAsync
```

Implementations:
- `EFCoreAIVectorStore` (base) — brute-force cosine similarity in .NET via `TensorPrimitives`, over an EF Core entity (`AIVectorEntryEntity`), storing each vector as a JSON array in `nvarchar(max)` / `TEXT`.
- `SqlServerAIVectorStore` — extends the base with native SQL Server 2025 `VECTOR_DISTANCE('cosine', CAST(Vector AS vector(N)), …)`, runtime capability detection (probe query, cached), brute-force fallback for older SQL Server or >1998 dimensions, plus culture filtering.
- `InMemoryAIVectorStore` — test/dev.

Consumers (small, contained surface — 9 files): `AIVectorIndexer`, `AIVectorSearcher`, `SemanticSearchTool`, `AISearchUsageTelemetryProvider`, and DI registration in the two `Db.*` projects.

Meanwhile, `Microsoft.Extensions.VectorData.Abstractions` (currently restored transitively at **9.7.0**) standardizes this space with `VectorStore`, `VectorStoreCollection<TKey,TRecord>`, attribute-based record mapping (`VectorStoreKeyAttribute`/`VectorStoreDataAttribute`/`VectorStoreVectorAttribute`), `IVectorSearchable<T>`, `IKeywordHybridSearchable<T>`, `VectorSearchOptions`/filters, `DistanceFunction`, and `IndexKind`. A connector ecosystem implements it (Qdrant, Azure AI Search, pgvector, SQL Server, Redis, etc.).

The question: should we align our storage layer with `Microsoft.Extensions.VectorData`, and if so, how?

## Decision drivers

1. **Differentiator preservation.** Our real value is the SQL Server 2025 `VECTOR_DISTANCE` light-up *while riding Umbraco's shared EF Core `DbContext`, migrations (`UmbracoAISearch_` prefix, shared `__EFMigrationsHistory`), and connection-string resolution* — zero extra infrastructure for the user. An off-the-shelf VectorData connector manages its *own* schema and connection, which would fragment the Umbraco install model.
2. **Ecosystem demand.** Standard abstractions unlock pluggable backends (Qdrant, Azure AI Search, pgvector) and richer query APIs (hybrid search, structured filters) — *if and when* users actually ask for them.
3. **Maturity / churn risk.** VectorData versions on a separate, still-evolving 9.x line (vs MEAI core 10.x). Betting the storage layer on it now risks API churn.
4. **Migration cost & risk.** Reworking the storage layer touches the two DB projects, migrations, and ~9 consumer files, and risks regressing the bespoke 2025/fallback behavior.
5. **Maintenance burden of the status quo.** Keeping a custom abstraction means we own brute-force search, capability detection, and any future features (hybrid, filters) ourselves.

## Options considered

### Option A — Keep custom `IAIVectorStore` (status quo)
Maintain our abstraction; do not depend on VectorData.

- **Pros:** Zero migration; preserves the SQL 2025 optimization + Umbraco integration exactly; no exposure to VectorData churn; smallest surface to maintain.
- **Cons:** No ecosystem connectors; we build hybrid search / filters ourselves if needed; our abstraction diverges from the emerging .NET standard, so external devs don't get a familiar API.

### Option B — Replace with VectorData + an off-the-shelf connector
Drop our store; adopt `VectorStoreCollection<TKey,TRecord>` backed by a published connector (e.g. the SQL Server connector).

- **Pros:** Maximum ecosystem; standard surface; hybrid/filter APIs for free; least *bespoke* code to maintain long-term.
- **Cons:** **Loses our differentiator** — the connector owns its schema/connection, breaking the "rides the Umbraco DbContext, no extra infra" model and likely the `UmbracoAISearch_`/shared-history migration story; the bespoke "works on any SQL Server, light up on 2025, brute-force fallback, >1998-dim handling, culture filter" behavior would have to be re-established or abandoned; largest migration and regression risk; ties us to the connector's release cadence.

### Option C — Adopt VectorData abstractions, keep our EF store behind a custom `VectorStoreCollection`
Expose `Microsoft.Extensions.VectorData` as the public surface, but implement `VectorStoreCollection<TKey,TRecord>` (and `IVectorSearchable<T>`) over our existing EF Core + SQL 2025 store.

- **Pros:** Keeps the SQL 2025 optimization and Umbraco integration; gives consumers the standard interface; lets *other* backends be swapped in later via published connectors; principled long-term end-state.
- **Cons:** Highest implementation effort — we must satisfy the full collection contract (record mapping, filters, search options, possibly hybrid search) over our model; exposes us to VectorData API churn at the public boundary; most of the ecosystem benefit only materializes once we also wire alternate connectors (i.e. a lot of work before the payoff).

### Option D — `IAIVectorStore` adapter over a MEAI `VectorStoreCollection` connector (additive, opt-in)
Keep `IAIVectorStore` as the public surface; add an **alternative implementation** (`MeaiVectorStoreAdapter : IAIVectorStore`) that delegates to a MEAI `VectorStoreCollection` from a published connector (Qdrant, Azure AI Search, pgvector…). The existing EF/SQL-2025 store remains the **default** impl; the adapter is opt-in (a separate connector package per backend, mirroring our provider model, e.g. `Umbraco.AI.Search.Db.Qdrant`).

Mapping our 6 used methods onto a collection (validated 2026-06-16):
- `UpsertAsync` (composite identity → synthesized string key) ✓; `SearchAsync` ✓ (gains native **ANN** vs our brute-force); `GetVectorsByDocumentAsync` → filtered `GetAsync` ✓; `DeleteDocumentAsync`/`ResetAsync` → filter-keys-then-`DeleteAsync(keys)` ✓ (two-step, fine for deletes).
- **One real gap:** `GetDocumentCountAsync` — there is **no `Count` in the VectorData abstraction**. Resolve via the connector's native client through `VectorStoreCollection.GetService(...)` (Qdrant/Azure expose count), or relax that method to approximate/optional.
- Arbitrary `IDictionary<string,object>` metadata → a single serialized **JSON data-property** on the record (portable across connectors).

- **Pros:** **No public-API break, no consumer churn** (interface unchanged); actually delivers external-service backends (which C does *not*); keeps our SQL-2025/EF store as default — external backends are purely additive/opt-in; smaller than C (we adapt *our* 6 methods one-way, not implement VectorData's full 14-member contract).
- **Cons:** each backend needs its connector NuGet (opt-in packages); the `Count` workaround; per-backend config (dimensions/distance/filter support); we don't get the SQL-2025 optimization on the *external* path (acceptable — those backends have their own ANN indexes).

## Decision (recommended)

**Adopt Option A (keep custom `IAIVectorStore`) now.** When external backends become a concrete need, prefer **Option D** (adapter behind `IAIVectorStore`) over B or C. Do **not** adopt B (sacrifices our integration) or pursue C speculatively (no connector payoff standalone).

Rationale: our integration with Umbraco's DbContext/migrations plus the SQL 2025 light-up *is* the product value, so the default store stays. Option D is the key insight from this review: it delivers the actual goal (plug into external vector services) **additively and without a public break**, because `IAIVectorStore` is a small, well-contained seam — so there's no need to break it or expose VectorData to consumers. There's still no current demand, so we build nothing now; but D, not B/C, is the path when demand arrives. (Option C remains only relevant if we ever want *consumers* to speak the VectorData interface directly — a separate, weaker motivation.)

## Consequences

- **Now:** no code change; we continue maintaining `IAIVectorStore` and the SQL 2025 path. Keep the abstraction deliberately thin and connector-agnostic so Option C stays a realistic future move.
- **We accept:** building any hybrid-search / structured-filter capability ourselves in the interim, and a surface that differs from the emerging .NET standard.
- **Guard:** when touching the store, avoid leaking storage-specific assumptions into the ~9 consumers beyond the `IAIVectorStore` contract, to preserve the seam.

## Revisit triggers

Re-open this ADR (toward Option D — the adapter — for external backends) when any of:
1. A concrete user/requirement needs a backend we don't have (Qdrant, Azure AI Search, pgvector).
2. We need hybrid (vector + keyword) search or rich metadata filtering that VectorData provides off the shelf.
3. `Microsoft.Extensions.VectorData` reaches a stable 10.x aligned with MEAI core and ships a connector that can ride an existing EF Core `DbContext` (removing the Option B integration objection).
4. Maintaining our own brute-force/capability-detection code becomes a material burden.

## Option C effort assessment (investigated 2026-06-16)

Desk investigation of the real effort, prompted by the v17 major-alignment review. **Conclusion: confirms defer** — medium-large effort, leaky "standard" surface, and no connector payoff standalone.

### Consumer surface (small)
Only 4 consumers, 6 methods used — so keeping `IAIVectorStore` as a facade over a new impl means ~zero consumer churn:
- `AIVectorIndexer` → `UpsertAsync`, `DeleteDocumentAsync`, `ResetAsync`, `GetDocumentCountAsync`
- `AIVectorSearcher` → `SearchAsync`
- `SemanticSearchTool` → `GetVectorsByDocumentAsync`
- `AISearchUsageTelemetryProvider` → `GetDocumentCountAsync`

### Contract to implement
`VectorStoreCollection<TKey,TRecord>` (~14 members: `UpsertAsync`/`DeleteAsync`/`GetAsync` singular+batch, filtered `GetAsync`, `SearchAsync`, `CollectionExists`/`EnsureCollection*`), plus `VectorStore` (~8: `GetCollection`, `CollectionExistsAsync`, `EnsureCollectionDeletedAsync`, `ListCollectionNamesAsync`), plus a `[VectorStoreKey]`/`[VectorStoreData]`/`[VectorStoreVector]` record type + mapping to/from `AIVectorEntryEntity`.

### Impedance mismatches (the crux — 3 of 6 used methods)
1. **No `Count` in the VectorData contract**, but `GetDocumentCountAsync` is used by two consumers. → keep custom (defeats "standard surface") or enumerate (perf hit). Sharpest friction.
2. **VectorData deletes are key-based; ours are predicate-based** (`DeleteDocumentAsync`, `ResetAsync`). → query-keys-then-delete, or keep custom.
3. **Composite identity** (indexName+documentId+culture+chunkIndex) → synthesize a string key; `indexName` ⇒ "collection".
4. **JSON-in-nvarchar + runtime SQL 2025 dimension detection** vs connectors' typed vector columns → only preserved if we implement our *own* collection (not use a connector).

### Benefit reality-check
A custom `VectorStoreCollection` over our EF store yields the **standard interface only — NOT the connector ecosystem**. Connectors (Qdrant/Azure) = Option B = abandon the SQL 2025 + Umbraco-DbContext integration. So Option C standalone = "standard surface + future optionality"; if alternate backends later become a real need, you'd adopt a connector and most of Option C's mapping work is discarded.

### Effort
Medium-large: record+mapping (S), 14-member collection contract incl. **re-homing the SQL 2025 `VECTOR_DISTANCE` path into `SearchAsync`** (L), store/factory (M), `IAIVectorStore` facade to avoid consumer churn (S), cross-provider + VectorData conformance tests (M–L), 2-project DI (S), plus the count/delete impedance workarounds. A multi-day refactor — not an interface swap.

### Bottom line
Not justified now, and this investigation also reframes the *future* path: the effort of implementing VectorData's full contract (Option C) isn't worth it. When external backends are wanted, **Option D (adapter behind `IAIVectorStore`)** is the lighter, non-breaking route — adapt our 6 methods one-way onto a connector's `VectorStoreCollection` rather than implement the 14-member contract or break the public interface. Option C only matters if we ever want *consumers* to speak VectorData directly.
