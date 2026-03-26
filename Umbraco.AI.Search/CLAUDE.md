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
| `Umbraco.AI.Search.SqlServer` | SQL Server EF Core DbContext, migrations, vector store |
| `Umbraco.AI.Search.Sqlite` | SQLite EF Core DbContext, migrations, vector store |
| `Umbraco.AI.Search.Startup` | Umbraco Composer for DI registration |
| `Umbraco.AI.Search` | Meta-package that bundles all components |

### Key Services

- `AIVectorIndexer` - Extracts text from content fields, chunks, embeds, and stores vectors
- `AIVectorSearcher` - Embeds queries and performs cosine similarity search
- `IAIVectorStore` - Vector storage abstraction (SQL Server, SQLite, or custom)
- `SemanticSearchTool` - AI agent tool for semantic search with text query or document similarity modes

### Database

Uses EF Core with provider-specific migrations. Migration prefix: `UmbracoAISearch_`.

SQL Server store detects version at runtime:
- SQL Server 2025+: native `VECTOR_DISTANCE()` with `CAST`
- Older versions: brute-force cosine similarity in .NET

Column type is `varbinary(max)` for compatibility across all SQL Server versions.

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
