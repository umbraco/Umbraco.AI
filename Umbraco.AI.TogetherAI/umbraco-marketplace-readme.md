## Umbraco.AI.TogetherAI

Together AI provider for Umbraco.AI — connect to Together AI's serverless inference platform with automatic model discovery.

### Features

- **Open-Source Model Catalog** - Access Llama, Mixtral, Qwen, DeepSeek, Gemma, and more through a single API key
- **Chat Completions** - Streaming and non-streaming chat across every Together-hosted chat model
- **Text Embeddings** - Generate embeddings with BGE, M2-BERT, UAE, and other Together-hosted embedding models
- **Dynamic Model Discovery** - Models are fetched live from Together's `/v1/models` endpoint and filtered by the `type` field, so new models appear without provider updates

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- Together AI API key
