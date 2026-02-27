# Umbraco.AI.OpenAI

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.OpenAI.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.OpenAI/)

OpenAI provider plugin for Umbraco.AI, enabling integration with OpenAI's GPT models and Azure OpenAI Service.

## Features

- **OpenAI API Support** - Connect to OpenAI's API (GPT-4, GPT-4o, GPT-3.5-turbo, etc.)
- **Azure OpenAI Support** - Connect to Azure OpenAI Service deployments
- **Chat Capabilities** - Full support for chat completions with streaming
- **Embedding Capabilities** - Generate text embeddings for semantic search
- **Model Configuration** - Configure temperature, max tokens, and other model parameters
- **Middleware Support** - Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.OpenAI
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new OpenAI connection
3. Choose between OpenAI API or Azure OpenAI
4. Enter your API key or Azure credentials
5. Create a profile using this connection

### OpenAI API

```json
{
    "ApiKey": "sk-...",
    "OrganizationId": "org-..." // Optional
}
```

### Azure OpenAI

```json
{
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-api-key",
    "DeploymentName": "your-deployment-name"
}
```

## Supported Models

**Chat Models:**

- GPT-4o
- GPT-4 Turbo
- GPT-4
- GPT-3.5 Turbo

**Embedding Models:**

- text-embedding-3-large
- text-embedding-3-small
- text-embedding-ada-002

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details (if available)
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
