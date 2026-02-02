# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.MicrosoftFoundry provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.MicrosoftFoundry.sln

# Run tests
dotnet test Umbraco.AI.MicrosoftFoundry.sln
```

## Architecture Overview

Umbraco.AI.MicrosoftFoundry is a provider plugin for Umbraco.AI that enables integration with Microsoft AI Foundry (Azure AI Inference). It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

This provider uses a simplified structure (single project):

| Project | Purpose |
|---------|---------|
| `Umbraco.AI.MicrosoftFoundry` | Provider implementation, capabilities, and settings |

### Provider Implementation

The provider is implemented using the `AIProviderBase<TSettings>` pattern:

```csharp
[AIProvider("microsoft-foundry", "Microsoft AI Foundry")]
public class MicrosoftFoundryProvider : AIProviderBase<MicrosoftFoundryProviderSettings>
{
    public MicrosoftFoundryProvider(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        WithCapability<MicrosoftFoundryChatCapability>();
        WithCapability<MicrosoftFoundryEmbeddingCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`MicrosoftFoundryChatCapability`):
- Extends `AIChatCapabilityBase<MicrosoftFoundryProviderSettings>`
- Creates `IChatClient` instances using Azure.AI.Inference with Microsoft.Extensions.AI.AzureAIInference
- Returns empty model list (users specify model names in profiles)
- Default model: `gpt-4o`

**Embedding Capability** (`MicrosoftFoundryEmbeddingCapability`):
- Extends `AIEmbeddingCapabilityBase<MicrosoftFoundryProviderSettings>`
- Creates `IEmbeddingGenerator<string, Embedding<float>>` instances
- Returns empty model list (users specify model names in profiles)
- Default model: `text-embedding-3-small`

### Settings System

Settings use the `[AIField]` attribute for UI generation:

```csharp
public class MicrosoftFoundryProviderSettings
{
    [AIField]
    [Required]
    public string? Endpoint { get; set; }  // e.g., https://your-resource.services.ai.azure.com/

    [AIField]
    [Required]
    public string? ApiKey { get; set; }
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$MicrosoftFoundry:ApiKey"`).

### Microsoft AI Foundry Model Access

Microsoft AI Foundry provides a unified endpoint for multiple model providers:
- **OpenAI models**: GPT-4o, GPT-4, GPT-3.5-turbo, text-embedding-3-*
- **Mistral models**: mistral-large, mistral-small
- **Llama models**: llama-3-70b, llama-3-8b
- **Cohere models**: command-r, embed-v3
- **Phi models**: phi-3-medium, phi-3-small
- And more as deployed in your Azure AI hub

One endpoint + one API key provides access to all deployed models. Users specify the model name in their profile.

## Key Namespaces

- `Umbraco.AI.MicrosoftFoundry` - Root namespace for provider, capabilities, and settings

## Configuration Examples

### Microsoft AI Foundry

```json
{
  "MicrosoftFoundry": {
    "Endpoint": "https://your-resource.services.ai.azure.com/",
    "ApiKey": "..."
  }
}
```

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Azure.AI.Inference
- Microsoft.Extensions.AI.AzureAIInference

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Provider Discovery

The provider is automatically discovered by Umbraco.AI through:
1. `[AIProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AIProvidersCollectionBuilder`

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
