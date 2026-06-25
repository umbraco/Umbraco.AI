# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.DeepSeek provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.DeepSeek.slnx
```

## Architecture Overview

Umbraco.AI.DeepSeek is a provider plugin for Umbraco.AI that integrates DeepSeek's chat models. DeepSeek exposes an OpenAI-compatible REST API, so this provider reuses the official `OpenAI` .NET SDK pointed at `https://api.deepseek.com` rather than introducing a separate vendor SDK.

It does **not** depend on `Umbraco.AI.OpenAI` — it brings its own `Microsoft.Extensions.AI.OpenAI` package reference (which transitively pulls in the OpenAI SDK).

### Project Structure

| Project               | Purpose                                             |
| --------------------- | --------------------------------------------------- |
| `Umbraco.AI.DeepSeek` | Provider implementation, capabilities, and settings |

### Provider Implementation

```csharp
[AIProvider("deepseek", "DeepSeek")]
public class DeepSeekProvider : AIProviderBase<DeepSeekProviderSettings>
{
    public DeepSeekProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<DeepSeekChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`DeepSeekChatCapability`):

- Extends `AIChatCapabilityBase<DeepSeekProviderSettings>`
- Creates `IChatClient` via `OpenAIClient.GetChatClient(modelId).AsIChatClient()`
- Uses the OpenAI `/chat/completions` shape (DeepSeek does **not** implement OpenAI's `/responses` API)
- Discovers models dynamically via `GET /models` and filters with the `^deepseek-` regex so new model families are picked up without code changes

DeepSeek does not expose an embeddings endpoint, so no embedding capability is registered.

### Settings

```csharp
public class DeepSeekProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://api.deepseek.com";
}
```

## Key Namespaces

- `Umbraco.AI.DeepSeek` - Provider, capabilities, and settings
- `Umbraco.AI.Extensions` - Model utilities (display name formatting)

## Configuration Example

```json
{
    "DeepSeek": {
        "ApiKey": "sk-..."
    }
}
```

## Dependencies

- Umbraco CMS 18.x
- Umbraco.AI 18.x
- Microsoft.Extensions.AI.OpenAI (transitively brings the `OpenAI` .NET SDK)

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
