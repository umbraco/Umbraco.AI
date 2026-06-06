# Umbraco.AI.HuggingFace

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.HuggingFace.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.HuggingFace/)

Hugging Face provider plugin for Umbraco.AI, giving access to hundreds of open-weights models served by the
[Hugging Face Inference Providers](https://huggingface.co/docs/inference-providers/index) router.

## Features

- **Inference Providers Router** — single endpoint (`https://router.huggingface.co/v1`) routes to Cerebras, Together, Fireworks, SambaNova, Groq, Replicate, and more
- **Chat Completions** — streaming and non-streaming chat against any conversational model on the Hub
- **Model Discovery** — fetches the list of available chat models directly from `/v1/models`
- **Routing Suffixes** — supports `model-id:fastest`, `:cheapest`, `:preferred`, or `:provider-name` to control how the request is routed
- **Custom Endpoint** — point at a self-hosted OpenAI-compatible gateway if needed

The provider has no dependency on `Umbraco.AI.OpenAI`; it talks to the Hugging Face router via the OpenAI-compatible
schema using `Microsoft.Extensions.AI.OpenAI`.

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup
instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.HuggingFace
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- A [Hugging Face access token](https://huggingface.co/settings/tokens) with the
  *Make calls to Inference Providers* permission

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Hugging Face connection
3. Paste your Hugging Face access token
4. Create a profile that uses this connection

### API Configuration

```json
{
    "ApiKey": "hf_..."
}
```

## Supported Models

The full list comes back live from `GET /v1/models` on the router and varies as Hugging Face partners add or
retire models. Examples that have been broadly available:

- `openai/gpt-oss-120b`
- `meta-llama/Meta-Llama-3.1-70B-Instruct`
- `deepseek-ai/DeepSeek-R1`
- `Qwen/Qwen2.5-72B-Instruct`
- `mistralai/Mistral-Small-24B-Instruct-2501`

Append a routing suffix to influence provider selection, e.g. `openai/gpt-oss-120b:fastest` or
`deepseek-ai/DeepSeek-R1:sambanova`.

## Documentation

- **[CLAUDE.md](CLAUDE.md)** — Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** — Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** — How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.

