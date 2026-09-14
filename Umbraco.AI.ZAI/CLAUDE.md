# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.ZAI provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.ZAI.slnx
```

## Architecture Overview

Umbraco.AI.ZAI is a provider plugin for Umbraco.AI that integrates Z.AI's GLM chat models. Z.AI exposes an OpenAI-compatible REST API, so this provider reuses the official `OpenAI` .NET SDK pointed at `https://api.z.ai/api/paas/v4` rather than introducing a separate vendor SDK.

It does **not** depend on `Umbraco.AI.OpenAI` — it brings its own `Microsoft.Extensions.AI.OpenAI` package reference (which transitively pulls in the OpenAI SDK).

The package/provider is named after the vendor (Z.AI, formerly styled ZAI in code identifiers), not the model family (GLM) it serves — matching the convention of other providers (e.g. Umbraco.AI.OpenAI serves GPT models, Umbraco.AI.Anthropic serves Claude models).

### Project Structure

| Project          | Purpose                                             |
| ---------------- | ---------------------------------------------------- |
| `Umbraco.AI.ZAI` | Provider implementation, capabilities, and settings |

### Provider Implementation

```csharp
[AIProvider("zai", "Z.AI")]
public class ZAIProvider : AIProviderBase<ZAIProviderSettings>
{
    public ZAIProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<ZAIChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`ZAIChatCapability`):

- Extends `AIChatCapabilityBase<ZAIProviderSettings>`
- Creates `IChatClient` via `OpenAIClient.GetChatClient(modelId).AsIChatClient()`
- Uses the OpenAI `/chat/completions` shape
- Discovers models dynamically via `GET /models` and filters with the `^glm-` regex so new model families (e.g. `glm-6-*`) are picked up without code changes

Z.AI's global API (`api.z.ai`) does not expose an embeddings endpoint — embeddings are only available on Zhipu's separate China-only `bigmodel.cn` endpoint, which is out of scope for this provider — so no embedding capability is registered.

### Model Discovery

`GET /api/paas/v4/models` is not documented on Z.AI's public docs site, but was confirmed live to return the standard OpenAI-style `{"object":"list","data":[{"id":"glm-4.5",...}]}` shape. `ZAIProvider.GetAvailableModelIdsAsync` calls it exactly the way `DeepSeekProvider` does, via `OpenAIClient.GetOpenAIModelClient().GetModelsAsync()`, cached for 1 hour. If Z.AI ever removes or changes this undocumented endpoint, model discovery will start failing — there is no hard-coded fallback list by design (see root CLAUDE.md guidance on avoiding hard-coded model lists).

### Settings

```csharp
public class ZAIProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://api.z.ai/api/paas/v4";
}
```

## Key Namespaces

- `Umbraco.AI.ZAI` - Provider, capabilities, and settings
- `Umbraco.AI.Extensions` - Model utilities (display name formatting)

## Configuration Example

```json
{
    "ZAI": {
        "ApiKey": "..."
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
