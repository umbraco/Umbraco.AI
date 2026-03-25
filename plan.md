# Umbraco.AI Vector Search Plan

## Overview

Integrate Umbraco.AI with Umbraco.Cms.Search to provide vector-based semantic search as a search provider. This enables Umbraco content to be indexed with embeddings and searched semantically, alongside (not replacing) existing search providers like Examine.

The codebase already has embedding generation infrastructure (`IAIEmbeddingService`, `AIEmbeddingService`, provider support for `text-embedding-3-large/small`, etc.). This plan adds the **vector storage**, **text chunking**, **indexing**, and **searching** layers.

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Primary vector store | SQL Server 2025 native `vector` type | Zero extra infrastructure, EF Core 10 built-in `SqlVector<float>` + `VECTOR_DISTANCE()`, DiskANN for ANN |
| Secondary vector store | SQLite via sqlite-vec | Matches Umbraco's existing DB support, `vec0` virtual tables with native cosine/L2 |
| Text chunking | Custom `ITextChunker` | ~100-150 LOC, avoids Semantic Kernel dependency (sunsetting), tailored to Umbraco content |
| VDB abstraction | Drupal-inspired `IVectorStore`/`IVectorCollection` | Pluggable providers, clean separation of index vs collection concerns |
| CMS.Search integration | `IIndexer`/`ISearcher` from day one | `RegisterIndex<VectorIndexer, VectorSearcher>()` on `IndexOptions` |
| Embedding resolution | Default embedding profile from UI settings | No separate `EmbeddingProfileAlias` config — uses `IAIEmbeddingService` which resolves the default |

---

## Architecture

### How It Fits Into Umbraco.Cms.Search

Umbraco.Cms.Search uses a **per-index provider pattern**:
- Each index alias (e.g., `PublishedContentIndex`) maps to one `(IIndexer, ISearcher)` pair
- `IndexOptions.RegisterIndex<TIndexer, TSearcher>(indexAlias)` assigns implementations to a specific index
- Multiple Composers can each register their own indexes independently
- `IIndexerResolver.GetIndexer(alias)` / `ISearcherResolver.GetSearcher(alias)` route by alias at runtime

### Critical DI Constraint: Avoid Becoming the Default Provider

**Problem:** `services.AddTransient<IIndexer, VectorIndexer>()` would make our implementation the last-registered (and therefore default) `IIndexer`/`ISearcher`, overriding Examine or other providers.

**Solution:** Register concrete types only — not the interfaces:

```csharp
// WRONG - becomes the default IIndexer/ISearcher, breaking other providers
services.AddTransient<IIndexer, VectorIndexer>();
services.AddTransient<ISearcher, VectorSearcher>();

// CORRECT - available in DI for RegisterIndex<> resolution, no interface pollution
services.AddTransient<VectorIndexer>();
services.AddTransient<VectorSearcher>();
```

`RegisterIndex<VectorIndexer, VectorSearcher>()` resolves types from the container by concrete type. This keeps Examine (or whatever the user chose) as the default provider, and our vector implementation only activates for the specific index we register it against.

### Composer Ordering

Our Composer must run **after** `AddSearchCore()` has been called. Since the search core setup is the implementor's responsibility (and we don't know their Composer type), we use `[ComposeAfter(typeof(UmbracoAIComposer))]`. Documentation should make clear that `AddSearchCore()` is a prerequisite.

---

## Component Design

### 1. Vector Store Abstraction (Drupal-inspired)

A two-level abstraction for pluggable vector storage backends:

```csharp
/// <summary>
/// Factory for obtaining vector collections by index name.
/// </summary>
public interface IVectorStore
{
    Task<IVectorCollection> GetCollectionAsync(string indexName, CancellationToken ct = default);
    Task DeleteCollectionAsync(string indexName, CancellationToken ct = default);
}

/// <summary>
/// Operations on a single vector collection (index).
/// </summary>
public interface IVectorCollection
{
    Task UpsertAsync(string documentId, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken ct = default);
    Task DeleteAsync(string documentId, CancellationToken ct = default);
    Task DeleteByDocumentPrefixAsync(string prefix, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(ReadOnlyMemory<float> queryVector, int topK = 10, CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
    Task<long> GetDocumentCountAsync(CancellationToken ct = default);
}
```

`DeleteByDocumentPrefixAsync` supports chunk cleanup — when re-indexing document `{guid}`, delete all `{guid}:chunk-*` entries first.

**Implementations:**

| Provider | Backend | Search Strategy | Notes |
|----------|---------|----------------|-------|
| `SqlServerVectorStore` | SQL Server 2025 `vector(N)` columns | `VECTOR_DISTANCE()` exact + `VECTOR_SEARCH()` DiskANN approximate | EF Core 10 `SqlVector<float>`, native |
| `SqliteVecVectorStore` | sqlite-vec `vec0` virtual tables | `MATCH` KNN (indexed) or `vec_distance_cosine` (brute force) | Requires sqlite-vec native extension |
| `InMemoryVectorStore` | `ConcurrentDictionary` | `TensorPrimitives.CosineSimilarity` brute force | Dev/testing only |

### 2. SQL Server 2025 Vector Store

Uses EF Core 10's built-in vector support — no plugins needed.

**Entity model:**
```csharp
public class VectorEntry
{
    public long Id { get; set; }
    public string IndexName { get; set; }
    public string DocumentId { get; set; }  // "{guid}:chunk-{n}" for chunked docs

    [Column(TypeName = "vector(1536)")]
    public SqlVector<float> Vector { get; set; }

    public string? Metadata { get; set; }  // JSON
}
```

**Search via EF Core:**
```csharp
var results = await db.VectorEntries
    .Where(e => e.IndexName == indexName)
    .OrderBy(e => EF.Functions.VectorDistance("cosine", e.Vector, queryVector))
    .Take(topK)
    .Select(e => new VectorSearchResult(e.DocumentId, EF.Functions.VectorDistance("cosine", e.Vector, queryVector), e.Metadata))
    .ToListAsync(ct);
```

**Key features:**
- `VECTOR_DISTANCE("cosine", a, b)` for exact search (GA in SQL Server 2025)
- `VECTOR_SEARCH()` + DiskANN index for approximate search (preview — behind `PREVIEW_FEATURES` config)
- Supports cosine, euclidean, and dot product distance metrics
- Vector dimensions configurable via `[Column(TypeName = "vector(N)")]`

### 3. SQLite Vector Store (sqlite-vec)

Uses the `sqlite-vec` extension's `vec0` virtual tables for native vector search.

**SQL operations:**
```sql
-- Create vector table
CREATE VIRTUAL TABLE vec_search USING vec0(
    document_id TEXT,
    embedding float[1536],
    +index_name TEXT,
    +metadata TEXT
);

-- Insert
INSERT INTO vec_search(document_id, embedding, index_name, metadata)
VALUES (?, vec_f32(?), ?, ?);

-- KNN search (indexed, fast)
SELECT document_id, distance
FROM vec_search
WHERE embedding MATCH vec_f32(?)
  AND k = ?
ORDER BY distance;

-- Or brute-force with specific metric
SELECT document_id, vec_distance_cosine(embedding, vec_f32(?)) as distance
FROM vec_search
WHERE index_name = ?
ORDER BY distance
LIMIT ?;
```

**Key features:**
- `vec0` virtual tables with SIMD-accelerated distance computation (AVX/NEON)
- Supports float32, int8, and bit vector types
- Distance metrics: L2 (euclidean), cosine, L1 (manhattan), hamming
- KNN via `MATCH` or brute-force via `vec_distance_*` functions
- Chunked memory-efficient storage

**NuGet status:** No official standalone NuGet package yet (as of early 2026). Options:
1. Bundle the native binary via a runtime-specific NuGet package we publish
2. Use `Microsoft.SemanticKernel.Connectors.SqliteVec` (preview) — but adds SK dependency
3. Raw ADO.NET with `connection.LoadExtension("vec0")` and manual binary distribution

**Recommendation:** Raw ADO.NET with the native binary bundled. Avoids SK dependency. This is what the Semantic Kernel connector does internally anyway.

### 4. Text Chunking

Custom `ITextChunker` implementation — avoids Semantic Kernel dependency (SK is sunsetting in favor of Microsoft Agent Framework, and TextChunker hasn't been migrated).

```csharp
public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text, TextChunkingOptions? options = null);
}

public record TextChunk(string Text, int Index);

public sealed class TextChunkingOptions
{
    public int MaxTokensPerChunk { get; set; } = 512;
    public int OverlapTokens { get; set; } = 50;
}
```

**Algorithm:** Split on paragraph → sentence → word boundaries, respect max token count, add overlap. ~100-150 lines.

**Lessons from Drupal's chunking pain points:**
- Long titles crash chunking when overlap > chunk size — guard against this
- Multi-field content forced through rendered HTML — chunk per-field, not rendered output
- Re-indexing appends instead of replacing — use `DeleteByDocumentPrefixAsync` before re-indexing
- Broken BPE tokenization — split on word boundaries, not mid-token
- Timeouts on large content — async/batched embedding generation

**Indexing with chunks:**
```
Document {guid} → ITextChunker.Chunk(text) → [chunk-0, chunk-1, chunk-2]
                                              ↓
                         UpsertAsync("{guid}:chunk-0", embedding0)
                         UpsertAsync("{guid}:chunk-1", embedding1)
                         UpsertAsync("{guid}:chunk-2", embedding2)
```

**Search result deduplication:** Multiple chunks from the same document may match. The searcher groups by document GUID and returns the best chunk score per document.

### 5. VectorIndexer (implements IIndexer)

Responsible for converting Umbraco content into vector embeddings and storing them.

**Indexing flow:**
1. Receives content fields from the Umbraco indexing pipeline
2. Extracts text via `ExtractTextFromFields` (all text-ranked fields)
3. Chunks text via `ITextChunker`
4. Deletes existing chunks for the document (`DeleteByDocumentPrefixAsync`)
5. Generates embeddings via `IAIEmbeddingService.GenerateEmbeddingAsync()` for each chunk
6. Stores chunk vectors in the vector store

**Key considerations:**
- Batch embedding support for rebuild scenarios (embed multiple chunks in one API call)
- HTML stripping before chunking (property-aware, avoids Drupal's rendered HTML issue)
- Handles embedding dimension differences across providers/models

### 6. VectorSearcher (implements ISearcher)

Responsible for semantic search queries against the vector store.

**Search flow:**
1. Receives search query text
2. Generates query embedding via `IAIEmbeddingService.GenerateEmbeddingAsync()`
3. Performs similarity search against vector store
4. Deduplicates results by document GUID (best chunk score wins)
5. Returns results mapped to Umbraco content IDs with relevance scores

### 7. Registration (Composer)

```csharp
[ComposeAfter(typeof(UmbracoAIComposer))]
public sealed class UmbracoAISearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Concrete types only — NOT as IIndexer/ISearcher
        builder.Services.AddTransient<VectorIndexer>();
        builder.Services.AddTransient<VectorSearcher>();

        // Text chunking
        builder.Services.AddSingleton<ITextChunker, TextChunker>();

        // Vector store — resolved by database provider
        // SQL Server → SqlServerVectorStore, SQLite → SqliteVecVectorStore
        builder.AddUmbracoAISearchPersistence();

        // Configuration
        builder.Services.AddOptions<VectorSearchOptions>()
            .BindConfiguration("Umbraco:AI:Search");

        // Register named index
        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterIndex<VectorIndexer, VectorSearcher>("VectorContentIndex"));
    }
}
```

---

## Project Structure

Separate `Umbraco.AI.Search` add-on package — depends on `Umbraco.AI.Core` (for embeddings) and `Umbraco.Cms.Search.Core` (for provider interfaces). Users install only if they want vector search.

```
Umbraco.AI.Search/
├── src/
│   ├── Umbraco.AI.Search.Core/              # Domain: IVectorStore, IVectorCollection,
│   │   ├── VectorIndexer.cs                 #   ITextChunker, VectorSearcher, VectorIndexer
│   │   ├── VectorSearcher.cs
│   │   ├── Chunking/
│   │   │   ├── ITextChunker.cs
│   │   │   ├── TextChunker.cs
│   │   │   ├── TextChunk.cs
│   │   │   └── TextChunkingOptions.cs
│   │   ├── VectorStore/
│   │   │   ├── IVectorStore.cs
│   │   │   ├── IVectorCollection.cs
│   │   │   ├── VectorSearchResult.cs
│   │   │   └── InMemoryVectorStore.cs
│   │   └── Configuration/
│   │       └── VectorSearchOptions.cs
│   ├── Umbraco.AI.Search.EfCore/            # Shared EF Core DbContext, migrations base
│   │   ├── UmbracoAISearchDbContext.cs
│   │   └── VectorStore/
│   │       ├── AIVectorEntryEntity.cs
│   │       └── EfCoreVectorStore.cs         # → Becomes SqlServerVectorStore
│   ├── Umbraco.AI.Search.SqlServer/         # SQL Server migrations + SqlVector mapping
│   ├── Umbraco.AI.Search.Sqlite/            # sqlite-vec implementation + SQLite migrations
│   │   └── VectorStore/
│   │       └── SqliteVecVectorStore.cs      # Raw ADO.NET with vec0 virtual tables
│   ├── Umbraco.AI.Search.Startup/           # Composer, DI registration
│   └── Umbraco.AI.Search/                   # Meta-package
├── tests/
│   └── Umbraco.AI.Search.Tests.Unit/
├── Umbraco.AI.Search.slnx
└── CLAUDE.md
```

---

## Technology Details

### SQL Server 2025

- **Data type:** `vector(N)` — stores N-dimensional float32 vectors in optimized binary format
- **Max dimensions:** 1998 (covers all standard embedding models)
- **EF Core 10:** `SqlVector<float>` maps natively, no plugins needed (replaces `EFCore.SqlServer.VectorSearch`)
- **Exact search:** `VECTOR_DISTANCE('cosine'|'euclidean'|'dot', a, b)` — GA
- **Approximate search:** `VECTOR_SEARCH()` with DiskANN index — preview (behind `PREVIEW_FEATURES`)
- **DiskANN:** Graph-based ANN using SSDs, 95%+ recall with sub-10ms latency at scale
- **Hybrid search:** Can combine `FreeTextTable` + vector search with Reciprocal Rank Fusion

### sqlite-vec

- **Extension:** `vec0` virtual table module for SQLite
- **Distance metrics:** L2 (euclidean), cosine, L1 (manhattan), hamming
- **Vector types:** float32, int8, bit
- **KNN:** `WHERE embedding MATCH ? AND k = ?` (indexed) or `ORDER BY vec_distance_cosine(...)` (brute force)
- **Performance:** SIMD-accelerated (AVX/NEON), chunked memory-efficient storage
- **License:** MIT (Apache 2.0 dual-licensed)
- **.NET integration:** No official NuGet yet — load via `connection.LoadExtension("vec0")` with bundled native binary

### Text Chunking (Custom)

- **Strategy:** Paragraph → sentence → word boundary splitting
- **Parameters:** `maxTokensPerChunk` (default 512), `overlapTokens` (default 50)
- **Token counting:** Word-count estimator by default (1 word ≈ 1.3 tokens), optional tiktoken for accuracy
- **Guard rails:** Overlap must be < chunk size, minimum chunk size enforcement
- **HTML handling:** Strip before chunking (per-field, not rendered output)

---

## Implementation Phases

### Phase 1: Core + SQL Server

**Goal:** Working vector search with SQL Server 2025 backend.

- [ ] Refactor `IVectorStore` to two-level `IVectorStore`/`IVectorCollection` pattern
- [ ] Implement `SqlServerVectorStore` using EF Core 10 `SqlVector<float>` + `VECTOR_DISTANCE()`
- [ ] Update `AIVectorEntryEntity` to use `SqlVector<float>` column type
- [ ] Regenerate SQL Server EF Core migrations with vector column
- [ ] Implement `ITextChunker` + `TextChunker` with paragraph/sentence/word splitting
- [ ] Update `VectorIndexer` to chunk text and store multiple vectors per document
- [ ] Update `VectorSearcher` to deduplicate chunk results by document GUID
- [ ] Update unit tests

### Phase 2: Umbraco.Cms.Search Integration

**Goal:** Full integration with search framework, content indexing pipeline.

- [x] Register with `IndexOptions.RegisterIndex<VectorIndexer, VectorSearcher>("VectorContentIndex")`
- [ ] Integrate with content change notification handlers (publish, unpublish, delete)
- [ ] Batch rebuild support for full re-indexing
- [ ] Index lifecycle management (create/rebuild/delete via backoffice)
- [ ] HTML stripping in text extraction (property-aware)
- [ ] Culture/variation-aware indexing

### Phase 3: SQLite + UI

**Goal:** SQLite support and backoffice UI for vector search.

- [ ] Implement `SqliteVecVectorStore` with raw ADO.NET + sqlite-vec extension
- [ ] Bundle sqlite-vec native binary (runtime-specific)
- [ ] Auto-detect database provider and register appropriate vector store
- [ ] Backoffice UI for index management (rebuild, status, document count)
- [ ] Configuration UI for chunking parameters and distance metric

### Phase 4: Future Providers + Hybrid Search

**Goal:** Extensibility for external vector databases and hybrid search.

- [ ] External provider support (Qdrant, Pinecone, Azure AI Search, etc.)
- [ ] Hybrid search combining full-text + vector results (RRF scoring)
- [ ] Agent tool integration (`search.semantic` tool)
- [ ] DiskANN approximate search support when SQL Server preview features stabilize

---

## Configuration

```json
{
  "Umbraco": {
    "AI": {
      "Search": {
        "DefaultTopK": 100,
        "Chunking": {
          "MaxTokensPerChunk": 512,
          "OverlapTokens": 50
        },
        "DistanceMetric": "cosine"
      }
    }
  }
}
```

The embedding profile is resolved from the default UI settings (no separate configuration needed).

---

## What's Been Built So Far

| Component | Status | Notes |
|-----------|--------|-------|
| Project structure | Done | `Umbraco.AI.Search/` with Core, EfCore, SqlServer, Sqlite, Startup |
| `IVectorStore` (flat) | Done | Single-level interface — needs refactoring to `IVectorStore`/`IVectorCollection` |
| `InMemoryVectorStore` | Done | For dev/testing |
| `EfCoreVectorStore` | Done | **Brute-force** — loads all vectors into memory, no native vector support. Needs replacement with `SqlServerVectorStore` using `SqlVector<float>` |
| `VectorIndexer` | Done | **No chunking** — single embedding per document. Needs chunking support |
| `VectorSearcher` | Done | **No deduplication** — doesn't handle multiple chunks per document |
| `VectorSearchOptions` | Done | `DefaultTopK` only (removed `EmbeddingProfileAlias`) |
| EF Core migrations | Done | SQL Server + SQLite, but using `byte[]` blobs not native vector types |
| `RegisterIndex` call | Done | Registered on `IndexOptions` in Composer |
| Unit tests | Done | InMemoryVectorStore tests (10 passing) |
| Text chunking | **Not started** | |
| SQL Server native vectors | **Not started** | Need `SqlVector<float>` + `VECTOR_DISTANCE()` |
| sqlite-vec integration | **Not started** | Need native extension loading + `vec0` virtual tables |
| Content change handlers | **Not started** | Publish/unpublish/delete notifications |
| Backoffice UI | **Not started** | |
