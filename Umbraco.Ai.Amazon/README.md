# Umbraco.Ai.Amazon

[![NuGet](https://img.shields.io/nuget/v/Umbraco.Ai.Amazon.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.Ai.Amazon/)

Amazon Bedrock provider plugin for Umbraco.Ai, enabling integration with AWS Bedrock foundation models.

## Features

- **Amazon Bedrock Support** - Connect to AWS Bedrock foundation models
- **Multi-Model Support** - Access Amazon Nova, Claude via Bedrock, Llama, Mistral, and more
- **Chat Capabilities** - Full support for chat completions with streaming
- **Embedding Capabilities** - Generate text embeddings using Titan and Cohere models
- **Model Configuration** - Configure temperature, max tokens, and other model parameters
- **Middleware Support** - Compatible with Umbraco.Ai's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.Ai monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.Ai.Amazon
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Ai 17.0.0+
- .NET 10.0
- AWS Account with Bedrock access

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Amazon Bedrock connection
3. Enter your AWS credentials (Region, Access Key ID, Secret Access Key)
4. Create a profile using this connection

### AWS Configuration

```json
{
  "Region": "us-east-1",
  "AccessKeyId": "AKIA...",
  "SecretAccessKey": "..."
}
```

You can also reference configuration values using the `$` prefix (e.g., `$AWS:AccessKeyId`).

## Supported Models

**Chat Models:**
- Amazon Nova family (`amazon.nova-lite-v1:0`, `amazon.nova-pro-v1:0`, etc.)
- Claude via Bedrock (`anthropic.claude-3-sonnet`, `anthropic.claude-3-haiku`, etc.)
- Mistral models (`mistral.mistral-large`, etc.)
- Meta Llama models (`meta.llama3-70b-instruct`, etc.)

**Embedding Models:**
- Amazon Titan Embeddings (`amazon.titan-embed-text-v2:0`, etc.)
- Cohere Embed models (`cohere.embed-english-v3`, etc.)

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
