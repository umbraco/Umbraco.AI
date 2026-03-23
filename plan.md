# Umbraco.AI Vector Search Plan

## Overview

Integrate Umbraco.AI with Umbraco.Cms.Search to provide vector-based semantic search as a search provider. This enables Umbraco content to be indexed with embeddings and searched semantically, alongside (not replacing) existing search providers like Examine.

The codebase already has embedding generation infrastructure (`IAIEmbeddingService`, `AIEmbeddingService`, provider support for `text-embedding-3-large/small`, etc.). This plan adds the **indexing** and **searching** layers that plug into Umbraco.Cms.Search's provider model.

---

## Architecture

### How It Fits Into Umbraco.Cms.Search

Umbraco.Cms.Search uses a **per-index provider pattern**:
- Each index alias (e.g., `PublishedContentIndex`) maps to one `(IIndexer, ISearcher)` pair
- `RegisterIndex<TIndexer, TSearcher>()` assigns implementations to a specific index
- Multiple Composers can each register their own indexes independently

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

Our Composer must use `[ComposeAfter(typeof(SearchCoreComposer))]` (or equivalent) to ensure `AddSearchCore()` has already run before we register our provider.

---

## Component Design

### 1. VectorIndexer (implements IIndexer)

Responsible for converting Umbraco content into vector embeddings and storing them.

**Indexing flow:**
1. Receives content from the Umbraco indexing pipeline (content saves, publish events)
2. Extracts text fields from content (name, properties marked for indexing)
3. Calls `IAIEmbeddingService.GenerateEmbeddingAsync()` to generate vector embeddings
4. Stores vectors in a vector store (abstracted — see Vector Store Abstraction below)

**Key considerations:**
- Batch indexing support for rebuild scenarios
- Configurable text extraction (which fields to embed, concatenation strategy)
- Handles embedding dimension differences across providers/models

### 2. VectorSearcher (implements ISearcher)

Responsible for semantic search queries against the vector store.

**Search flow:**
1. Receives search query text
2. Generates query embedding via `IAIEmbeddingService.GenerateEmbeddingAsync()`
3. Performs similarity search against vector store (cosine similarity / ANN)
4. Returns results mapped to Umbraco content IDs with relevance scores

### 3. Vector Store Abstraction

An abstraction layer for the actual vector storage backend, allowing pluggable implementations.

```csharp
public interface IVectorStore
{
    Task UpsertAsync(string indexName, string documentId, float[] vector, IDictionary<string, object>? metadata = null, CancellationToken ct = default);
    Task DeleteAsync(string indexName, string documentId, CancellationToken ct = default);
    Task<IEnumerable<VectorSearchResult>> SearchAsync(string indexName, float[] queryVector, int topK = 10, CancellationToken ct = default);
    Task DeleteIndexAsync(string indexName, CancellationToken ct = default);
}

public record VectorSearchResult(string DocumentId, double Score, IDictionary<string, object>? Metadata);
```

**Initial implementation options:**
- In-memory (for dev/testing)
- SQLite-based (lightweight, no external dependencies)
- Future: Qdrant, Pinecone, Azure AI Search, etc.

### 4. Registration (Composer)

```csharp
[ComposeAfter(typeof(SearchCoreComposer))]
public class VectorSearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register concrete types only — NOT as IIndexer/ISearcher
        builder.Services.AddTransient<VectorIndexer>();
        builder.Services.AddTransient<VectorSearcher>();
        builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>(); // or configurable

        // Register for a specific index (e.g., a new "VectorContentIndex")
        builder.RegisterIndex<VectorIndexer, VectorSearcher>("VectorContentIndex");
    }
}
```

---

## Project Placement

This could live in:

**Option A: Inside `Umbraco.AI/` (Core)**
- Pros: Direct access to `IAIEmbeddingService`, ships with core
- Cons: Adds Umbraco.Cms.Search dependency to core

**Option B: New add-on package `Umbraco.AI.VectorSearch/`**
- Pros: Optional dependency, clean separation, users opt-in
- Cons: More project scaffolding

**Recommendation: Option B** — A separate `Umbraco.AI.VectorSearch` package that depends on both `Umbraco.AI.Core` (for embeddings) and `Umbraco.Cms.Search` (for the provider interfaces). Users install it only if they want vector search.

### Proposed Structure

```
Umbraco.AI.VectorSearch/
├── src/
│   ├── Umbraco.AI.VectorSearch.Core/
│   │   ├── VectorIndexer.cs
│   │   ├── VectorSearcher.cs
│   │   ├── VectorStore/
│   │   │   ├── IVectorStore.cs
│   │   │   ├── VectorSearchResult.cs
│   │   │   └── InMemoryVectorStore.cs
│   │   └── Configuration/
│   │       └── VectorSearchOptions.cs
│   ├── Umbraco.AI.VectorSearch.Startup/
│   │   └── VectorSearchComposer.cs
│   └── Umbraco.AI.VectorSearch/          # Meta-package
├── tests/
│   └── Umbraco.AI.VectorSearch.Tests.Unit/
├── Umbraco.AI.VectorSearch.slnx
└── CLAUDE.md
```

---

## Open Questions

1. **Index strategy:** Do we create a new dedicated index (e.g., `VectorContentIndex`), or allow re-registering an existing index like `PublishedContentIndex` with our vector provider?
2. **Vector store default:** What's the right default vector store for v1? SQLite-backed would be zero-config for most users.
3. **Text extraction:** How should we determine which content fields to embed? Configuration per content type? All text fields by default?
4. **Hybrid search:** Should we support combining vector similarity scores with traditional full-text relevance in a single query?
5. **Embedding model configuration:** Should this reuse existing Profiles/Connections, or have its own configuration for the embedding model to use?

---

## Implementation Phases

### Phase 1: Foundation
- Create `Umbraco.AI.VectorSearch` project structure
- Implement `IVectorStore` abstraction + in-memory implementation
- Implement `VectorIndexer` and `VectorSearcher`
- Register as Umbraco.Cms.Search provider (concrete types only, per constraint above)

### Phase 2: Content Indexing
- Text extraction from Umbraco content nodes
- Integration with `IAIEmbeddingService` for vector generation
- Batch rebuild support
- Index lifecycle management (create/rebuild/delete)

### Phase 3: Search Integration
- Semantic search query handling
- Result mapping back to Umbraco content
- Score normalization and relevance tuning

### Phase 4: Agent Tool Integration
- Implement `search.semantic` tool (referenced in agents design doc)
- Replace/augment existing `SearchUmbracoTool` (Examine-based) with semantic option

### Phase 5: Persistent Vector Store
- SQLite or database-backed vector store implementation
- Migration support (EF Core pattern matching existing products)
