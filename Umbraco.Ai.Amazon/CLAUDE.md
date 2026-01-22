# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.Ai.Amazon provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.Ai.Amazon.sln

# Run tests
dotnet test Umbraco.Ai.Amazon.sln
```

## Architecture Overview

Umbraco.Ai.Amazon is a provider plugin for Umbraco.Ai that enables integration with Amazon Bedrock foundation models. It follows the provider plugin architecture defined by Umbraco.Ai.Core.

### Project Structure

This provider uses a simplified structure (single project):

| Project | Purpose |
|---------|---------|
| `Umbraco.Ai.Amazon` | Provider implementation, capabilities, and settings |

### Provider Implementation

The provider is implemented using the `AiProviderBase<TSettings>` pattern:

```csharp
[AiProvider("amazon", "Amazon Bedrock")]
public class AmazonProvider : AiProviderBase<AmazonProviderSettings>
{
    public AmazonProvider(IAiProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<AmazonChatCapability>();
        WithCapability<AmazonEmbeddingCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`AmazonChatCapability`):
- Extends `AiChatCapabilityBase<AmazonProviderSettings>`
- Creates `IChatClient` instances using AWS Bedrock SDK with M.E.AI integration
- Supports Amazon Nova, Claude via Bedrock, Llama, Mistral models

**Embedding Capability** (`AmazonEmbeddingCapability`):
- Extends `AiEmbeddingCapabilityBase<AmazonProviderSettings>`
- Creates `IEmbeddingGenerator<string, Embedding<float>>` instances
- Supports Amazon Titan Embeddings and Cohere Embed models

### Settings System

Settings use the `[AiField]` attribute for UI generation:

```csharp
public class AmazonProviderSettings
{
    [AiField]
    [Required]
    public string? Region { get; set; }

    [AiField]
    [Required]
    public string? AccessKeyId { get; set; }

    [AiField]
    [Required]
    public string? SecretAccessKey { get; set; }

    [AiField]
    public string? Endpoint { get; set; }
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$AWS:AccessKeyId"`).

### Supported Models

**Chat Models:**
- Amazon Nova family (`amazon.nova-*`)
- Claude via Bedrock (`anthropic.claude-*`)
- Mistral models (`mistral.*`)
- Meta Llama models (`meta.llama*`)

**Embedding Models:**
- Amazon Titan Embeddings (`amazon.titan-embed-*`)
- Cohere Embed models (`cohere.embed-*`)

## Key Namespaces

- `Umbraco.Ai.Amazon` - Root namespace for provider, capabilities, and settings

## Configuration Example

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "AKIA...",
    "SecretAccessKey": "..."
  }
}
```

## Dependencies

- Umbraco CMS 17.x
- Umbraco.Ai 1.x
- AWSSDK.Bedrock
- AWSSDK.BedrockRuntime
- AWSSDK.Extensions.Bedrock.MEAI

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Provider Discovery

The provider is automatically discovered by Umbraco.Ai through:
1. `[AiProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AiProvidersCollectionBuilder`

## Testing

For testing provider implementations, use the test utilities from `Umbraco.Ai.Tests.Common`:
- `FakeAiProvider` - Test double for provider testing
- `AiConnectionBuilder` - Fluent builder for test connections
- `AiProfileBuilder` - Fluent builder for test profiles

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
