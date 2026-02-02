# Umbraco.AI.Anthropic

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.Anthropic.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.Anthropic/)

Anthropic provider plugin for Umbraco.AI, enabling integration with Anthropic's Claude models.

## Features

- **Anthropic API Support** - Connect to Anthropic's API (Claude 3.5 Sonnet, Claude 3 Opus, etc.)
- **Chat Capabilities** - Full support for chat completions with streaming
- **Advanced Features** - Support for Claude's unique capabilities like extended context windows
- **Model Configuration** - Configure temperature, max tokens, and other model parameters
- **Middleware Support** - Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Anthropic
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Anthropic connection
3. Enter your Anthropic API key
4. Create a profile using this connection

### API Configuration

```json
{
  "ApiKey": "sk-ant-..."
}
```

## Supported Models

**Chat Models:**
- Claude 3.5 Sonnet
- Claude 3 Opus
- Claude 3 Sonnet
- Claude 3 Haiku

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details (if available)
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
