# Umbraco.AI.Search

AI-powered semantic vector search for Umbraco CMS content and media. Built on [Umbraco.Cms.Search](https://github.com/umbraco/Umbraco.Cms.Search) and Umbraco.AI's embedding capabilities, it registers as a search provider alongside Examine and other CMS Search providers.

## Features

- **Semantic Search** - Find content by meaning, not just keywords ("audio shows" finds Podcasts)
- **SQL Server 2025+ Native Vectors** - Automatic `VECTOR_DISTANCE()` with brute-force fallback for older versions
- **CMS Search Provider** - Registers as an `IIndexer`/`ISearcher` via `Umbraco.Cms.Search`, works alongside Examine
- **Content & Media Indexing** - Indexes both document and media content on publish
- **Text Chunking** - Configurable chunking with overlap for long documents
- **MinScore Filtering** - Filters irrelevant results below a configurable similarity threshold
- **Agent Tool** - Semantic search tool for AI agents with text query and document similarity modes
- **Custom Vector Store** - Implement `IAIVectorStore` for alternative storage backends

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Search
```

This meta-package includes all required components. For more control, install individual packages:

| Package                          | Description                                              |
| -------------------------------- | -------------------------------------------------------- |
| `Umbraco.AI.Search.Core`        | Domain models, services, indexer, searcher, tools        |
| `Umbraco.AI.Search.Db`          | Shared EF Core DbContext and base vector store           |
| `Umbraco.AI.Search.Db.SqlServer`| SQL Server migrations + native VECTOR_DISTANCE override  |
| `Umbraco.AI.Search.Db.Sqlite`   | SQLite migrations                                        |

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Cms.Search 1.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- An embedding-capable AI provider (e.g. Umbraco.AI.OpenAI with text-embedding-3-small)

## Configuration

```json
{
    "Umbraco": {
        "AI": {
            "Search": {
                "ChunkSize": 512,
                "ChunkOverlap": 50,
                "DefaultTopK": 100,
                "MinScore": 0.3
            }
        }
    }
}
```

| Option       | Default | Description                                                  |
| ------------ | ------- | ------------------------------------------------------------ |
| `ChunkSize`  | `512`   | Maximum tokens per text chunk                                |
| `ChunkOverlap` | `50` | Token overlap between consecutive chunks                     |
| `DefaultTopK` | `100`  | Max candidates from vector similarity search                 |
| `MinScore`   | `0.3`   | Minimum cosine similarity score (0.0-1.0) to include a result |

## Known Issues

### AddSearchCore() ordering with site code

This package calls `builder.AddSearchCore()` during its Composer if no other package has already registered search core services. If your site code or another package also calls `AddSearchCore()`, it must do so **before** this package's Composer runs — otherwise `AddSearchCore()` will be called twice, which can result in duplicate service registrations because it is not idempotent in `Umbraco.Cms.Search` 1.0.0-beta.2.

**Workaround:** If you need to call `AddSearchCore()` in your own Composer, ensure it runs before `UmbracoAISearchComposer` using `[ComposeBefore]`. Alternatively, let this package handle it automatically.

**Fix:** [umbraco/Umbraco.Cms.Search#109](https://github.com/umbraco/Umbraco.Cms.Search/pull/109) makes `AddSearchCore()` idempotent. Once a new version of `Umbraco.Cms.Search` is released with this fix, calling order will no longer matter.

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide, architecture, and technical details for this package
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
