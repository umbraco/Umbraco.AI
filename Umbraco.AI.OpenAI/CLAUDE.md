# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.Ai.OpenAi provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.Ai.OpenAi.sln

# Run tests
dotnet test Umbraco.Ai.OpenAi.sln
```

## Architecture Overview

Umbraco.Ai.OpenAi is a provider plugin for Umbraco.Ai that enables integration with OpenAI and Azure OpenAI Service. It follows the provider plugin architecture defined by Umbraco.Ai.Core.

### Project Structure

This provider uses a simplified structure (single project):

| Project | Purpose |
|---------|---------|
| `Umbraco.Ai.OpenAi` | Provider implementation, capabilities, and settings |

### Provider Implementation

The provider is implemented using the `AiProviderBase<TSettings>` pattern:

```csharp
[AiProvider("openai", "OpenAI")]
public class OpenAiProvider : AiProviderBase<OpenAiSettings>
{
    public OpenAiProvider(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        WithCapability<OpenAiChatCapability>();
        WithCapability<OpenAiEmbeddingCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`OpenAiChatCapability`):
- Extends `AiChatCapabilityBase<OpenAiSettings>`
- Creates `IChatClient` instances using Microsoft.Extensions.AI.OpenAI
- Supports both OpenAI API and Azure OpenAI endpoints
- Handles model configuration (GPT-4, GPT-3.5-turbo, etc.)

**Embedding Capability** (`OpenAiEmbeddingCapability`):
- Extends `AiEmbeddingCapabilityBase<OpenAiSettings>`
- Creates `IEmbeddingGenerator<string, Embedding<float>>` instances
- Supports text-embedding-3-large, text-embedding-3-small, text-embedding-ada-002

### Settings System

Settings use the `[AiField]` attribute for UI generation:

```csharp
public class OpenAiSettings
{
    [AiField("provider-type", "Provider Type", AiFieldType.Select)]
    public string ProviderType { get; set; } = "openai"; // or "azure"

    [AiField("api-key", "API Key", AiFieldType.Password)]
    public string ApiKey { get; set; } = string.Empty;

    [AiField("endpoint", "Azure Endpoint", AiFieldType.Text)]
    public string? AzureEndpoint { get; set; }

    // ... other settings
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$OpenAI:ApiKey"`).

### Supported Models

**Chat Models:**
- `gpt-4o`
- `gpt-4-turbo`
- `gpt-4`
- `gpt-3.5-turbo`

**Embedding Models:**
- `text-embedding-3-large`
- `text-embedding-3-small`
- `text-embedding-ada-002`

## Key Namespaces

- `Umbraco.Ai.OpenAi` - Root namespace for provider, capabilities, and settings
- `Umbraco.Ai.OpenAi.Chat` - Chat capability implementation
- `Umbraco.Ai.OpenAi.Embeddings` - Embedding capability implementation

## Configuration Examples

### OpenAI API

```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-...",
    "OrganizationId": "org-..."
  }
}
```

### Azure OpenAI

```json
{
  "OpenAI": {
    "Azure": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "...",
      "DeploymentName": "gpt-4"
    }
  }
}
```

## Dependencies

- Umbraco CMS 17.x
- Umbraco.Ai 1.x
- Microsoft.Extensions.AI.OpenAI
- Azure.AI.OpenAI (for Azure support)

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
