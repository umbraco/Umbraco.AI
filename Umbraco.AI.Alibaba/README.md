# Umbraco.AI.Alibaba

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.Alibaba.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.Alibaba/)

Alibaba Cloud provider plugin for Umbraco.AI, enabling integration with Alibaba Cloud Model Studio's Qwen chat and text embedding models via its OpenAI-compatible API.

## Features

- **Alibaba Cloud Model Studio Support** - Connect to Qwen chat and text embedding models via the OpenAI-compatible endpoint
- **Chat Capabilities** - Full support for chat completions with streaming and tool calls
- **Embedding Capabilities** - Text embedding support via `text-embedding-v4` and related models
- **Dynamic Model Discovery** - Automatically fetches available models from Model Studio
- **Custom Endpoints** - Override the base URL for the China (Beijing) region or a workspace-scoped endpoint
- **Middleware Support** - Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Alibaba
```

## Requirements

- Umbraco CMS 18.0.0+
- Umbraco.AI 18.0.0+
- .NET 10.0
- Alibaba Cloud Model Studio API key (from <https://bailian.console.alibabacloud.com/>)

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Alibaba Cloud connection
3. Enter your Alibaba Cloud API key
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
