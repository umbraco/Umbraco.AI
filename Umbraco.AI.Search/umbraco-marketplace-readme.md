## Umbraco.AI.Search

AI-powered semantic vector search for Umbraco CMS - find content by meaning, not just keywords. Built on Umbraco.Cms.Search, it works alongside Examine and other search providers.

### Features

- **Semantic Search** - Search by meaning: "audio shows" finds Podcasts, "CMS" finds Umbraco content
- **Automatic Indexing** - Content and media are indexed on publish via the CMS Search framework
- **SQL Server 2025+ Native Vectors** - Uses VECTOR_DISTANCE() for server-side similarity, with automatic brute-force fallback
- **Text Chunking** - Splits long documents into overlapping chunks for better retrieval
- **Configurable Thresholds** - MinScore filtering removes irrelevant results
- **Agent Integration** - Semantic search tool for AI agents with text query and document similarity modes
- **Extensible** - Implement IAIVectorStore for custom storage backends

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Cms.Search 1.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- An embedding-capable AI provider (e.g. Umbraco.AI.OpenAI)
