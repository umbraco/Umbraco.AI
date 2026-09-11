# Umbraco.AI.OpenRouter

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.OpenRouter.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.OpenRouter/)

OpenRouter provider plugin for Umbraco.AI — one connection to hundreds of chat models from dozens of vendors (OpenAI, Anthropic, Google, Meta, Mistral, DeepSeek, and more), through OpenRouter's OpenAI-compatible API.

## Features

- **OpenRouter API Support** — Connect to OpenRouter's OpenAI-compatible endpoint
- **Chat Completions** — Full chat support with streaming
- **Dynamic Model Discovery** — Models are listed automatically from the OpenRouter catalog; new models appear without a package update
- **Per-Model Sampling Support** — OpenRouter reports which request parameters each model accepts, so the profile editor only shows sampling settings (temperature, top-p, top-k, frequency/presence penalty) a model actually honours

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.OpenRouter
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- OpenRouter API key

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new OpenRouter connection
3. Enter your OpenRouter API key
4. Create a profile using this connection

### Settings

| Field | Required | Description |
|---|---|---|
| `ApiKey` | Yes | Your OpenRouter API key |
| `Endpoint` | No | Base URL. Defaults to `https://openrouter.ai/api/v1` |

## Not yet supported

This first release is chat-only. Two OpenRouter features are deliberate future work rather than gaps in this release:

- **Provider routing / fallback** — OpenRouter can prefer providers by price or speed and fall back to alternate models automatically. Umbraco.AI's profile settings have no place for these yet.
- **Embeddings** — OpenRouter added an embeddings endpoint recently; skipped for now until it has a track record.

## Documentation

- **[CLAUDE.md](CLAUDE.md)** — Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** — Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** — How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
