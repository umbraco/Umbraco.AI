# Umbraco.AI.TogetherAI

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.TogetherAI.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.TogetherAI/)

Together AI provider plugin for Umbraco.AI, enabling integration with the
hundreds of open-source models hosted on Together AI's serverless inference
platform (Llama, Mixtral, Qwen, DeepSeek, Gemma, and more).

## Features

- **Chat Completions** - Streaming and non-streaming chat with all Together-hosted chat models
- **Text Embeddings** - Generate embeddings with BGE, M2-BERT, UAE and other Together-hosted embedding models
- **Dynamic Model Discovery** - Models are fetched live from Together's `/v1/models` endpoint and filtered by Together's declared `type`, so new models appear automatically without provider updates
- **OpenAI-Compatible Wire Format** - Reuses the OpenAI .NET SDK pointed at `https://api.together.xyz/v1`

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.TogetherAI
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- Together AI API key (sign up at [together.ai](https://together.ai))

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Together AI connection
3. Enter your Together AI API key
4. Optionally override the endpoint (defaults to `https://api.together.xyz/v1`)
5. Create a profile using this connection

```json
{
    "ApiKey": "..."
}
```

## Supported Models

Together AI hosts a catalog of open-weight models that grows over time. The
provider lists every chat or embedding model your account has access to,
sourced live from `/v1/models`. A few representative examples:

**Chat Models:**

- `meta-llama/Llama-3.3-70B-Instruct-Turbo`
- `mistralai/Mixtral-8x7B-Instruct-v0.1`
- `Qwen/Qwen2.5-72B-Instruct-Turbo`
- `deepseek-ai/DeepSeek-V3`
- `google/gemma-2-27b-it`

**Embedding Models:**

- `BAAI/bge-large-en-v1.5`
- `BAAI/bge-base-en-v1.5`
- `togethercomputer/m2-bert-80M-32k-retrieval`
- `WhereIsAI/UAE-Large-V1`

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
