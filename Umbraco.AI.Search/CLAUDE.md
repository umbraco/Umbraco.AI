# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Search add-on package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Search.slnx

# Run tests
dotnet test Umbraco.AI.Search.slnx
```

## Testing

```bash
# Run all tests
dotnet test Umbraco.AI.Search.slnx

# Run specific test project
dotnet test tests/Umbraco.AI.Search.Tests.Unit/Umbraco.AI.Search.Tests.Unit.csproj
```

## Architecture Overview

Umbraco.AI.Search provides AI-powered semantic vector search for Umbraco CMS content and media. It integrates with the CMS Search framework (`Umbraco.Cms.Search.Core`) and uses embedding models via Umbraco.AI's `IAIEmbeddingService`.

### Project Structure

| Project | Purpose |
| --- | --- |
| `Umbraco.AI.Search.Core` | Domain models, services, interfaces, indexer, searcher, tools |
| `Umbraco.AI.Search.Db` | Shared EF Core DbContext, entity, base vector store (brute-force search) |
| `Umbraco.AI.Search.Db.SqlServer` | SQL Server migrations + native `VECTOR_DISTANCE` search override |
| `Umbraco.AI.Search.Db.Sqlite` | SQLite migrations |
| `Umbraco.AI.Search.Startup` | Umbraco Composer for DI registration |
| `Umbraco.AI.Search` | Meta-package that bundles all components |

### Key Services

- `AIVectorIndexer` - Extracts text from content fields, chunks, embeds, and stores vectors
- `AIVectorSearcher` - Embeds queries and performs cosine similarity search
- `IAIVectorStore` - Vector storage abstraction (SQL Server, SQLite, or custom)
- `SemanticSearchTool` - AI agent tool for semantic search with text query or document similarity modes

### Database

Uses EF Core with provider-specific migrations. Migration prefix: `UmbracoAISearch_`.

Vectors are stored as JSON arrays in `nvarchar(max)` (SQL Server) / `TEXT` (SQLite) columns. This enables SQL Server 2025's native `VECTOR_DISTANCE()` via nvarchar-to-vector CAST at query time.

SQL Server store detects capabilities at runtime:
- SQL Server 2025+ with ≤1998 dimensions: native `VECTOR_DISTANCE()` for server-side cosine similarity
- Older versions or >1998 dimensions: brute-force cosine similarity in .NET via `TensorPrimitives`

### CMS Search Integration

Registered via `RegisterContentIndex<AIVectorIndexer, AIVectorSearcher, IPublishedContentChangeStrategy>` for content indexing on publish.

Media indexing uses a notification handler workaround (`MediaIndexingNotificationHandler`) because `IPublishedContentChangeStrategy` does not handle media. See: https://github.com/umbraco/Umbraco.Cms.Search/issues/108

### Configuration

Options bound from `Umbraco:AI:Search` in `appsettings.json`:

| Option | Default | Description |
| --- | --- | --- |
| `ChunkSize` | `512` | Maximum tokens per text chunk |
| `ChunkOverlap` | `50` | Token overlap between chunks |
| `DefaultTopK` | `100` | Max candidates from vector search |
| `MinScore` | `0.3` | Minimum cosine similarity score (0.0–1.0) to include a result |
