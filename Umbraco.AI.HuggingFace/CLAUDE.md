# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.HuggingFace provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared
> coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.HuggingFace.slnx

# Run tests (no test project exists yet — see "Testing" below)
dotnet test Umbraco.AI.HuggingFace.slnx
```

## Architecture Overview

`Umbraco.AI.HuggingFace` is a chat-only provider plugin that targets the [Hugging Face Inference Providers
router](https://huggingface.co/docs/inference-providers/index). The router exposes a drop-in OpenAI-compatible
`/v1/chat/completions` and `/v1/models` API at `https://router.huggingface.co/v1`, so we reuse
`Microsoft.Extensions.AI.OpenAI` instead of pulling in a vendor-specific SDK.

The package does **not** depend on `Umbraco.AI.OpenAI`; both packages happen to consume the same
`Microsoft.Extensions.AI.OpenAI` NuGet but neither references the other.

### Provider Implementation

```csharp
[AIProvider("huggingface", "Hugging Face")]
public class HuggingFaceProvider : AIProviderBase<HuggingFaceProviderSettings>
{
    public HuggingFaceProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<HuggingFaceChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`HuggingFaceChatCapability`):

- Extends `AIChatCapabilityBase<HuggingFaceProviderSettings>`
- Builds an `IChatClient` via `OpenAIClient.GetChatClient(modelId).AsIChatClient()` (note: NOT
  `GetResponsesClient` — the router does not implement OpenAI's Responses API, only the classic Chat Completions
  API)
- Lists models via `GetOpenAIModelClient().GetModelsAsync()`, cached for one hour per API-key + endpoint pair
- Filters to `vendor/name`-shaped IDs and excludes obvious image/audio/embedding artefacts

### Routing Suffixes

HF model IDs may include a `:suffix` to control routing, e.g. `openai/gpt-oss-120b:fastest`,
`deepseek-ai/DeepSeek-R1:sambanova`. The capability and the model utility both treat the suffix as part of the
model ID; the `/v1/chat/completions` payload passes it through verbatim.

### Settings

```csharp
public class HuggingFaceProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://router.huggingface.co/v1";
}
```

## Future Work

The router endpoint is **chat only**. To add embeddings, image generation, or speech, we would need to either:

- Call Hugging Face's native task-specific Inference API directly (custom HTTP client), or
- Pull in the community `tryAGI/HuggingFace` SDK, which exposes `IEmbeddingGenerator` over HF's TEI endpoints.

Neither is in scope for the initial release.

## Dependencies

- Umbraco CMS 18.x
- Umbraco.AI 18.x
- Microsoft.Extensions.AI.OpenAI

## Provider Discovery

The provider is automatically discovered by Umbraco.AI through:

1. `[AIProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AIProvidersCollectionBuilder`

## Testing

There is no dedicated test project — by convention provider packages are validated manually via the demo site.
The csproj declares `InternalsVisibleTo "Umbraco.AI.HuggingFace.Tests.Unit"` for parity with the other providers.

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for
coding standards.
