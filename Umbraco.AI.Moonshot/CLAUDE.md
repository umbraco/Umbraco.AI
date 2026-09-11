# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Moonshot provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Moonshot.slnx
```

## Architecture Overview

Umbraco.AI.Moonshot is a provider plugin for Umbraco.AI that integrates Moonshot AI's Kimi chat models. Moonshot exposes an OpenAI-compatible REST API, so this provider reuses the official `OpenAI` .NET SDK pointed at `https://api.moonshot.ai/v1` rather than introducing a separate vendor SDK.

It does **not** depend on `Umbraco.AI.OpenAI` — it brings its own `Microsoft.Extensions.AI.OpenAI` package reference (which transitively pulls in the OpenAI SDK).

### Project Structure

This provider uses a simplified structure (single project):

| Project               | Purpose                                             |
| ---------------------- | --------------------------------------------------- |
| `Umbraco.AI.Moonshot` | Provider implementation, capabilities, and settings |

### Provider Implementation

```csharp
[AIProvider("moonshot", "Moonshot AI")]
public class MoonshotProvider : AIProviderBase<MoonshotProviderSettings>
{
    public MoonshotProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<MoonshotChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`MoonshotChatCapability`):

- Extends `AIChatCapabilityBase<MoonshotProviderSettings>`
- Creates `IChatClient` via `OpenAIClient.GetChatClient(modelId).AsIChatClient()`
- Uses the OpenAI `/chat/completions` shape
- Discovers models dynamically via `GET /models` and filters with the `^kimi-` regex so new model families are picked up without code changes

Moonshot does not document an embeddings endpoint, so no embedding capability is registered.

### Settings

```csharp
public class MoonshotProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://api.moonshot.ai/v1";
}
```

### Models

Live model IDs are fetched dynamically from `GET /models`, not hard-coded. As of September 2026 the current Kimi lineup is `kimi-k3` (flagship, 1M context — the default model), `kimi-k2.7-code`, `kimi-k2.7-code-highspeed`, and `kimi-k2.6`. Earlier `moonshot-v1-*` and `kimi-k2`/`kimi-k2.5` families have been retired by Moonshot and return HTTP 404 — the `^kimi-` filter still matches their IDs mechanically, but the live `/models` endpoint won't list them.

## Key Namespaces

- `Umbraco.AI.Moonshot` - Provider, capabilities, and settings
- `Umbraco.AI.Extensions` - Model utilities (display name formatting)

## Configuration Example

```json
{
    "Moonshot": {
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
