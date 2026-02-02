# Umbraco.AI.Amazon

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.Amazon.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.Amazon/)

Amazon Bedrock provider plugin for Umbraco.AI, enabling integration with AWS Bedrock foundation models.

## Features

- **Amazon Bedrock Support** - Connect to AWS Bedrock foundation models
- **Multi-Model Support** - Access Amazon Nova, Claude via Bedrock, Llama, Mistral, and more
- **Chat Capabilities** - Full support for chat completions with streaming
- **Embedding Capabilities** - Generate text embeddings using Titan and Cohere models
- **Model Configuration** - Configure temperature, max tokens, and other model parameters
- **Middleware Support** - Compatible with Umbraco.AI's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Amazon
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- AWS Account with Bedrock access

## AWS Setup

Before using this provider, you need to create IAM credentials with Bedrock permissions.

### 1. Create an IAM User

1. Go to the [AWS IAM Console](https://console.aws.amazon.com/iam/)
2. Navigate to **Users** → **Create user**
3. Enter a username (e.g., `umbraco-ai-bedrock`)
4. Click **Next**

### 2. Attach Bedrock Permissions

Attach the following AWS managed policy to the user:

- `AmazonBedrockLimitedAccess` - Grants limited access to Amazon Bedrock

### 3. Create Access Keys

1. Select your user → **Security credentials** tab
2. Under **Access keys**, click **Create access key**
3. Choose **Application running outside AWS**
4. Save both the **Access Key ID** and **Secret Access Key**

> **Note:** All Bedrock models are available through the API once you have the correct IAM permissions. You do not need to enable access to specific models in the Bedrock console.

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

## Supported Models

**Chat Models:**
- Amazon Nova family (`amazon.nova-lite-v1:0`, `amazon.nova-pro-v1:0`, etc.)
- Claude via Bedrock (`anthropic.claude-3-sonnet`, `anthropic.claude-3-haiku`, etc.) This requires permission from Anthropic to use.
- Mistral models (`mistral.mistral-large`, etc.)

**Embedding Models:**
- Amazon Titan Embeddings (`amazon.titan-embed-text-v2:0`, etc.)
- Cohere Embed models (`cohere.embed-english-v3`, etc.)

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
