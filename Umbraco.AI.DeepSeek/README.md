# Umbraco.AI.DeepSeek

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.DeepSeek.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.DeepSeek/)

DeepSeek provider plugin for Umbraco.AI, enabling integration with DeepSeek's chat models via their OpenAI-compatible API.

## Features

- **DeepSeek API Support** - Connect to DeepSeek's OpenAI-compatible chat completions API
- **Chat Capabilities** - Full support for chat completions with streaming and tool calls
- **Dynamic Model Discovery** - Automatically fetches available models from the DeepSeek API
- **Custom Endpoints** - Override the base URL for proxies or alternative endpoints
- **Middleware Support** - Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.DeepSeek
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- DeepSeek API key (from <https://platform.deepseek.com/>)

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new DeepSeek connection
3. Enter your DeepSeek API key
4. Create a profile using this connection

### API Configuration

```json
{
    "ApiKey": "sk-..."
}
```

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
