## Umbraco.AI

The official AI integration layer for Umbraco CMS - connect to any AI provider through a unified, provider-agnostic interface.

### Features

- **Multi-Provider Support** - Connect to OpenAI, Anthropic, Google, Amazon Bedrock, and Azure AI through installable provider packages
- **Connection & Profile Management** - Configure multiple AI connections and create reusable profiles with model settings
- **AI Contexts** - Define brand voice, guidelines, and reference materials that enrich AI operations
- **Chat & Embeddings** - Full support for chat completions (streaming) and text embeddings
- **Usage Analytics** - Track AI usage with dashboards showing requests, tokens, and costs by provider/profile/user
- **Audit Logging** - Complete governance trail of all AI operations for compliance
- **Extensible Middleware** - Add logging, caching, rate limiting, or custom processing to the AI pipeline
- **Built-in Tools** - Content search, media retrieval, web page fetching, and context resource tools
- **Backoffice UI** - Full management interface for connections, profiles, contexts, and testing

### Requirements

- Umbraco CMS 17.0.0+
- .NET 10.0
- At least one provider package (e.g., Umbraco.AI.OpenAI)
