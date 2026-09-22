---
name: vector-store-abstraction
description: Decision to keep the custom IAIVectorStore abstraction over adopting Microsoft.Extensions.VectorData, and the adapter path for when external backends are needed
type: decision
---

**Decision:** Keep the custom `IAIVectorStore` abstraction (`Umbraco.AI.Search.Core/VectorStore/`)
as-is for now, rather than adopting `Microsoft.Extensions.VectorData`. Decided 2026-06-16, scope:
`Umbraco.AI.Search` vector storage layer. When external backends (Qdrant, Azure AI Search,
pgvector) become a real need, prefer an **adapter** (`MeaiVectorStoreAdapter : IAIVectorStore`
delegating to a MEAI `VectorStoreCollection` connector) over replacing or wrapping the abstraction
in VectorData's own interface.

**Why:**
- The real differentiator is the SQL Server 2025 `VECTOR_DISTANCE` light-up while riding Umbraco's
  shared EF Core `DbContext`, migrations (`UmbracoAISearch_` prefix, shared
  `__EFMigrationsHistory`), and connection-string resolution — zero extra infrastructure for the
  user. An off-the-shelf VectorData connector manages its own schema/connection, which would
  fragment that install model.
- `Microsoft.Extensions.VectorData` is still on a separate, evolving 9.x line (vs MEAI core 10.x) —
  betting the storage layer on it now risks API churn.
- Small consumer surface (4 files, 6 methods actually used: `AIVectorIndexer`, `AIVectorSearcher`,
  `SemanticSearchTool`, `AISearchUsageTelemetryProvider`) means the migration cost is avoidable
  work, not a forced move.
- A follow-up effort assessment (2026-06-16) confirmed deferring: implementing VectorData's full
  `VectorStoreCollection<TKey,TRecord>` contract (~14 members) over the existing EF store is a
  medium-large, multi-day refactor for a standard *interface* only — it does **not** by itself
  unlock the connector ecosystem (that requires also adopting a connector, i.e. Option B, which
  sacrifices the SQL 2025 + Umbraco-DbContext integration). Real impedance mismatches: no `Count`
  in the VectorData contract (two consumers need it), VectorData deletes are key-based vs our
  predicate-based `DeleteDocumentAsync`/`ResetAsync`, and our composite identity
  (indexName+documentId+culture+chunkIndex) needs synthesizing into a single key.

**Options considered (for full context if revisiting):**
- **A — keep custom (chosen).** Zero migration, preserves the differentiator, no VectorData churn
  exposure.
- **B — replace with VectorData + an off-the-shelf connector.** Rejected: loses the differentiator
  (connector owns schema/connection), largest migration/regression risk.
- **C — implement `VectorStoreCollection` ourselves over the existing EF store.** Rejected as not
  worth it standalone: gives the standard *interface* but not the connector ecosystem, and most of
  the mapping work would be discarded if a connector (Option D) is adopted later anyway.
- **D — additive adapter (`IAIVectorStore` facade over a MEAI connector), opt-in per backend.**
  This is the path for *when* external backends are needed — not adopted now, no current demand.

**How to apply:**
- Don't propose replacing `IAIVectorStore` or adding a VectorData dependency without a concrete
  trigger below — this has already been assessed twice (2026-06-16 initial + effort review).
- When touching the vector store, keep the abstraction thin and connector-agnostic (avoid leaking
  storage-specific assumptions into the ~9 consumers) so Option D stays a realistic future move.
- **Revisit triggers:** (1) a concrete requirement needs a backend `IAIVectorStore` doesn't have
  (Qdrant, Azure AI Search, pgvector); (2) hybrid vector+keyword search or rich metadata filtering
  is needed and VectorData provides it off the shelf; (3) `Microsoft.Extensions.VectorData` reaches
  a stable 10.x aligned with MEAI core and ships a connector that can ride an existing EF Core
  `DbContext`; (4) maintaining the brute-force/capability-detection code becomes a material burden.
  On any of these, go to **Option D** (the adapter), not B or C.
