# Umbraco.AI.FireworksAI

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.FireworksAI.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.FireworksAI/)

Fireworks AI provider plugin for Umbraco.AI — fast open-source model inference (Llama, Qwen, DeepSeek, Mistral, and more).

## Features

- **Fireworks AI API Support** — Connect to Fireworks AI's OpenAI-compatible endpoint
- **Chat Completions** — Full chat support with streaming
- **Text Embeddings** — Vector embeddings for retrieval and search
- **Dynamic Model Discovery** — Models are listed automatically from the Fireworks catalog; new models appear without a package update
- **Middleware Support** — Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.FireworksAI
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- Fireworks AI API key

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Fireworks AI connection
3. Enter your Fireworks AI API key
4. Create a profile using this connection

### Settings

| Field | Required | Description |
|---|---|---|
| `ApiKey` | Yes | Your Fireworks AI API key |
| `AccountId` | No | Account namespace for model discovery. Defaults to `fireworks` (the public catalog). Set to your own account id to expose fine-tuned or private models |
| `Endpoint` | No | Base URL. Defaults to `https://api.fireworks.ai/inference/v1` |

## Documentation

- **[CLAUDE.md](CLAUDE.md)** — Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** — Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** — How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.

