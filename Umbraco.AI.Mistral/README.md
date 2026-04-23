# Umbraco.AI.Mistral

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.Mistral.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.Mistral/)

Mistral provider plugin for Umbraco.AI, enabling integration with Mistral's chat and embedding models directly via the Mistral API.

## Features

- **Mistral API Support** - Connect directly to Mistral (Mistral Large, Mistral Small, Codestral, Pixtral, Ministral, Magistral, and open-weight models)
- **Chat Capabilities** - Full support for chat completions with streaming
- **Embeddings** - Generate vector embeddings with `mistral-embed`
- **Dynamic Model Discovery** - Automatically fetches available models from the Mistral API
- **Middleware Support** - Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Mistral
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- Mistral API key

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Mistral connection
3. Enter your Mistral API key
4. Create a profile using this connection

### API Configuration

```json
{
    "ApiKey": "..."
}
```

## Supported Models

**Chat Models:**

- Mistral Large / Small / Medium
- Codestral (code completion)
- Pixtral (vision)
- Ministral / Magistral
- Open-weight: open-mistral-*, open-mixtral-*

**Embedding Models:**

- mistral-embed

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details (if available)
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
