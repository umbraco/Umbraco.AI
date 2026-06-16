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

## Decision (recommended)

**Adopt Option A (keep custom `IAIVectorStore`) now; treat Option C as the deferred end-state, revisited on concrete demand.** Do **not** adopt Option B.

Rationale: our integration with Umbraco's DbContext/migrations plus the SQL 2025 light-up *is* the product value here, and Option B sacrifices exactly that for an ecosystem we have no current demand for. Option C is the right principled target, but doing it speculatively — against a still-evolving 9.x abstraction, before any user needs Qdrant/Azure AI Search or hybrid search — is YAGNI and exposes a working subsystem to churn for no near-term gain. Staying on a small, well-contained custom abstraction keeps optionality cheap: the ~9-file consumer surface and the clean `IAIVectorStore` seam mean a later move to Option C is tractable when justified.

## Consequences

- **Now:** no code change; we continue maintaining `IAIVectorStore` and the SQL 2025 path. Keep the abstraction deliberately thin and connector-agnostic so Option C stays a realistic future move.
- **We accept:** building any hybrid-search / structured-filter capability ourselves in the interim, and a surface that differs from the emerging .NET standard.
- **Guard:** when touching the store, avoid leaking storage-specific assumptions into the ~9 consumers beyond the `IAIVectorStore` contract, to preserve the seam.

## Revisit triggers

Re-open this ADR (toward Option C) when any of:
1. A concrete user/requirement needs a backend we don't have (Qdrant, Azure AI Search, pgvector).
2. We need hybrid (vector + keyword) search or rich metadata filtering that VectorData provides off the shelf.
3. `Microsoft.Extensions.VectorData` reaches a stable 10.x aligned with MEAI core and ships a connector that can ride an existing EF Core `DbContext` (removing the Option B integration objection).
4. Maintaining our own brute-force/capability-detection code becomes a material burden.

## Notes for a future Option C spike (not now)

- Map `AIVectorEntryEntity` → a `[VectorStoreKey]`/`[VectorStoreData]`/`[VectorStoreVector]`-annotated record.
- Implement `VectorStoreCollection<string, TRecord>` + `IVectorSearchable<TRecord>` over `EFCoreAIVectorStore`, keeping the SQL 2025 `VECTOR_DISTANCE` path inside `SearchAsync`.
- Translate `VectorSearchOptions`/filters to our culture/index filtering; decide hybrid-search support.
- Keep `IAIVectorStore` as an internal adapter or retire it once consumers move to the collection API.
