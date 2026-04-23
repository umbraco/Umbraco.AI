# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Mistral provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Mistral.slnx

# Run tests
dotnet test Umbraco.AI.Mistral.slnx
```

## Architecture Overview

Umbraco.AI.Mistral is a provider plugin for Umbraco.AI that enables integration with Mistral's chat and embedding models directly via the Mistral API. It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

Single project:

| Project              | Purpose                                             |
| -------------------- | --------------------------------------------------- |
| `Umbraco.AI.Mistral` | Provider implementation, capabilities, and settings |

### Provider Implementation

```csharp
[AIProvider("mistral", "Mistral")]
public class MistralProvider : AIProviderBase<MistralProviderSettings>
{
    public MistralProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<MistralChatCapability>();
        WithCapability<MistralEmbeddingCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`MistralChatCapability`):

- Extends `AIChatCapabilityBase<MistralProviderSettings>`
- Creates `IChatClient` instances via `MistralClient.Completions`
- Default model: `mistral-large-latest`
- Include patterns: `mistral-`, `open-mistral-`, `open-mixtral-`, `codestral-`, `pixtral-`, `ministral-`, `magistral-`
- Exclude patterns: `embed`, `moderation`, `ocr`

**Embedding Capability** (`MistralEmbeddingCapability`):

- Extends `AIEmbeddingCapabilityBase<MistralProviderSettings>`
- Creates `IEmbeddingGenerator<string, Embedding<float>>` via `MistralClient.Embeddings`
- Default model: `mistral-embed`

### Settings

```csharp
public class MistralProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }
}
```

## Key Namespaces

- `Umbraco.AI.Mistral` - Provider, capabilities, settings
- `Umbraco.AI.Extensions` - `MistralModelUtilities` display-name formatting

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Mistral.SDK (unofficial, MIT-licensed — <https://github.com/tghamm/Mistral.SDK>)

## Target Framework

- .NET 10.0 (`net10.0`)
- Central Package Management via root `Directory.Packages.props`
- Nullable reference types enabled

## Provider Discovery

The provider is automatically discovered by Umbraco.AI through:

1. `[AIProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AIProvidersCollectionBuilder`

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
