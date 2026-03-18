# Semantic Search: Migration to Umbraco.Cms.Search

This document describes the current semantic search architecture and how it maps to `Umbraco.Cms.Search` when that package becomes available.

## Current Architecture

```
ISemanticIndexSource (content/media)        <-- Provides entries to index
    |
    v
IAISemanticSearchService                    <-- Orchestrates indexing + search
    |
    v
IAIEmbeddingsRepository                    <-- Stores/queries vector embeddings
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `ISemanticIndexSource` | Extracts indexable entries from CMS entities (content, media) |
| `IAISemanticSearchService` | Coordinates indexing (single entity + full reindex) and similarity search |
| `IAIEmbeddingsRepository` | Persists embedding vectors and performs similarity queries |
| `SemanticIndexEntry` | DTO: entity key, type, alias, name, text, date modified |
| `IContentTextExtractor` | Extracts plain text from published content properties |
| Notification handlers | React to CMS events (publish, unpublish, delete, media save/delete) to trigger re-indexing |
| `SemanticSearchStartupHandler` | Checks if index is empty on startup and triggers full reindex |
| `AISemanticSearchOptions` | Configuration: enabled flag, max text length, batch size |

### Data Flow

1. **On entity change**: Notification handler queues background work item
2. **Background worker**: Calls `IAISemanticSearchService.IndexEntityAsync()` which:
   - Finds the matching `ISemanticIndexSource` by entity type
   - Calls `GetEntryAsync()` to extract text
   - Generates embedding via configured profile
   - Stores via `IAIEmbeddingsRepository`
3. **On search**: `IAISemanticSearchService.SearchAsync()` generates query embedding and delegates similarity search to repository

## Mapping to Umbraco.Cms.Search

When `Umbraco.Cms.Search` is available, the mapping should be:

| Current (Umbraco.AI) | Future (Umbraco.Cms.Search) | Notes |
|-----------------------|-----------------------------|-------|
| `ISemanticIndexSource` | `IIndexPopulator` | Populates the index with entries from CMS entities |
| `GetAllEntriesAsync()` | `IIndexPopulator.PopulateAsync()` | Full reindex — paged iteration is already in place |
| `GetEntryAsync()` | Per-entity update in index populator | Single entity re-index on change |
| Notification handlers | `IIndexPopulator` event hooks or `TransformingIndexEventManager` | CMS Search provides its own event wiring |
| `SemanticSearchStartupHandler` | Built-in index rebuild on empty | CMS Search handles index lifecycle |
| `IAIEmbeddingsRepository` | Custom `IIndex` implementation | Vector storage + similarity search |
| `IAISemanticSearchService.SearchAsync()` | `ISearcher` query API | Search interface provided by CMS Search |
| `AISemanticSearchOptions` | Index configuration in CMS Search | Configuration moves to index-level settings |

## Migration Checklist

### Step 1: Implement IIndex for embeddings

- Create an `AIEmbeddingIndex` implementing `Umbraco.Cms.Search.IIndex`
- Wrap `IAIEmbeddingsRepository` as the backing store
- Register via CMS Search's index collection builder

### Step 2: Implement IIndexPopulator

- Create `ContentEmbeddingIndexPopulator` and `MediaEmbeddingIndexPopulator`
- Move text extraction logic from `ISemanticIndexSource` implementations
- `GetAllEntriesAsync()` paged query logic transfers directly to `PopulateAsync()`

### Step 3: Implement ISearcher

- Create `AIEmbeddingSearcher` implementing `Umbraco.Cms.Search.ISearcher`
- Wrap the similarity search from `IAIEmbeddingsRepository`

### Step 4: Remove replaced components

- Delete `ISemanticIndexSource` and its implementations (`ContentSemanticIndexSource`, `MediaSemanticIndexSource`)
- Delete all notification handlers (`ContentPublished/Unpublished/Deleted`, `MediaSaved/Deleted`)
- Delete `SemanticSearchStartupHandler` (CMS Search manages index lifecycle)
- Simplify `IAISemanticSearchService` to delegate to CMS Search APIs

### Step 5: Update DI registration

- Remove notification handler registrations from `UmbracoBuilderExtensions.cs`
- Register new index, populators, and searcher via CMS Search builder APIs

## What Stays

- `IAIEmbeddingsRepository` — vector storage is AI-specific, not replaced by CMS Search
- `IContentTextExtractor` — text extraction logic is reusable
- `AISemanticSearchOptions` — may be simplified but concept remains
- Embedding generation via M.E.AI profiles — orthogonal to search infrastructure

## What Gets Deleted

- `ISemanticIndexSource` interface and implementations
- All five notification handlers
- `SemanticSearchStartupHandler`
- Recursive/paged tree walking (replaced by CMS Search populator lifecycle)
- `SemanticIndexEntry` DTO (replaced by CMS Search index entry types)
