# Umbraco.AI.ZAI

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.ZAI.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.ZAI/)

Z.AI provider plugin for Umbraco.AI, enabling integration with Z.AI's GLM chat models via their OpenAI-compatible API.

## Features

- **Z.AI API Support** - Connect to Z.AI's OpenAI-compatible chat completions API
- **Chat Capabilities** - Full support for chat completions with streaming and tool calls
- **Dynamic Model Discovery** - Automatically fetches available GLM models from the Z.AI API
- **Custom Endpoints** - Override the base URL for proxies or alternative endpoints
- **Middleware Support** - Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.ZAI
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 17.0.0+
- .NET 10.0
- Z.AI API key (from <https://z.ai/model-api>)

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Z.AI connection
3. Enter your Z.AI API key
4. Create a profile using this connection

### API Configuration

```json
{
    "ApiKey": "..."
}
```

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
