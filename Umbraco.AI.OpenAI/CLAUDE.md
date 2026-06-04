# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.OpenAI provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.OpenAI.slnx

# Run tests
dotnet test Umbraco.AI.OpenAI.slnx
```

## Architecture Overview

Umbraco.AI.OpenAI is a provider plugin for Umbraco.AI that enables integration with OpenAI and Azure OpenAI Service. It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

This provider uses a simplified structure (single project):

| Project             | Purpose                                             |
| ------------------- | --------------------------------------------------- |
| `Umbraco.AI.OpenAI` | Provider implementation, capabilities, and settings |

### Provider Implementation

The provider is implemented using the `AIProviderBase<TSettings>` pattern:

```csharp
[AIProvider("openai", "OpenAI")]
public class OpenAIProvider : AIProviderBase<OpenAISettings>
{
    public OpenAIProvider(IAIProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        WithCapability<OpenAIChatCapability>();
        WithCapability<OpenAIEmbeddingCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`OpenAIChatCapability`):

- Extends `AIChatCapabilityBase<OpenAISettings>`
- Creates `IChatClient` instances using Microsoft.Extensions.AI.OpenAI
- Supports both OpenAI API and Azure OpenAI endpoints
- Handles model configuration (GPT-4, GPT-3.5-turbo, etc.)

**Embedding Capability** (`OpenAIEmbeddingCapability`):

- Extends `AIEmbeddingCapabilityBase<OpenAISettings>`
- Creates `IEmbeddingGenerator<string, Embedding<float>>` instances
- Supports text-embedding-3-large, text-embedding-3-small, text-embedding-ada-002

### Settings System

Settings use the `[AIField]` attribute for UI generation:

```csharp
public class OpenAISettings
{
    [AIField("provider-type", "Provider Type", AIFieldType.Select)]
    public string ProviderType { get; set; } = "openai"; // or "azure"

    [AIField("api-key", "API Key", AIFieldType.Password)]
    public string ApiKey { get; set; } = string.Empty;

    [AIField("endpoint", "Azure Endpoint", AIFieldType.Text)]
    public string? AzureEndpoint { get; set; }

    // ... other settings
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$Umbraco:AI:Secrets:OpenAIApiKey"`). Resolution is default-deny — only keys under `AIOptions.AllowedConfigurationKeyPrefixes` (default `Umbraco:AI:Secrets` / `Umbraco:AI:Variables`) resolve, and secret keys only into `IsSensitive` fields. See the core docs for the rationale.

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

- `Umbraco.AI.OpenAI` - Root namespace for provider, capabilities, and settings
- `Umbraco.AI.OpenAI.Chat` - Chat capability implementation
- `Umbraco.AI.OpenAI.Embeddings` - Embedding capability implementation

## Configuration Examples

Place config-reference targets under the allow-listed `Umbraco:AI:Secrets` (sensitive) and `Umbraco:AI:Variables` (non-sensitive) sections so `$` references resolve by default. Then reference them from connection settings, e.g. API Key = `$Umbraco:AI:Secrets:OpenAIApiKey`, Endpoint = `$Umbraco:AI:Variables:OpenAIEndpoint`.

### OpenAI API

```json
{
    "Umbraco": {
        "AI": {
            "Secrets": { "OpenAIApiKey": "sk-proj-..." },
            "Variables": { "OpenAIOrganizationId": "org-..." }
        }
    }
}
```

### Azure OpenAI

```json
{
    "Umbraco": {
        "AI": {
            "Secrets": { "AzureOpenAIApiKey": "..." },
            "Variables": {
                "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com/",
                "AzureOpenAIDeploymentName": "gpt-4"
            }
        }
    }
}
```

To keep using an existing section such as `OpenAI:ApiKey` instead, add its prefix to `Umbraco:AI:AllowedConfigurationKeyPrefixes`.

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Microsoft.Extensions.AI.OpenAI
- Azure.AI.OpenAI (for Azure support)

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Provider Discovery

The provider is automatically discovered by Umbraco.AI through:

1. `[AIProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AIProvidersCollectionBuilder`

## Testing

For testing provider implementations, use the test utilities from `Umbraco.AI.Tests.Common`:

- `FakeAIProvider` - Test double for provider testing
- `AIConnectionBuilder` - Fluent builder for test connections
- `AIProfileBuilder` - Fluent builder for test profiles

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
