# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Alibaba provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Alibaba.slnx
```

## Architecture Overview

Umbraco.AI.Alibaba is a provider plugin for Umbraco.AI that integrates Alibaba Cloud Model Studio's Qwen chat and text embedding models. Model Studio (formerly DashScope) exposes an OpenAI-compatible REST API, so this provider reuses the official `OpenAI` .NET SDK pointed at Model Studio's endpoint rather than introducing a separate vendor SDK — the same approach `Umbraco.AI.DeepSeek` takes.

It does **not** depend on `Umbraco.AI.OpenAI` — it brings its own `Microsoft.Extensions.AI.OpenAI` package reference (which transitively pulls in the OpenAI SDK).

### Project Structure

| Project              | Purpose                                             |
| -------------------- | ---------------------------------------------------- |
| `Umbraco.AI.Alibaba` | Provider implementation, capabilities, and settings |

### Provider Implementation

```csharp
[AIProvider("alibaba", "Alibaba Cloud Model Studio")]
public class AlibabaProvider : AIProviderBase<AlibabaProviderSettings>
{
    public AlibabaProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<AlibabaChatCapability>();
        WithCapability<AlibabaEmbeddingCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`AlibabaChatCapability`):

- Extends `AIChatCapabilityBase<AlibabaProviderSettings>`
- Creates `IChatClient` via `OpenAIClient.GetChatClient(modelId).AsIChatClient()`
- Uses the OpenAI `/chat/completions` shape
- Discovers models dynamically via `GET /models` and filters with the `^qwen` include regex, so
  new model families (e.g. a future `qwen4-*`) are picked up without code changes. Model Studio
  also fronts other vendors' models (DeepSeek, Kimi, GLM, MiniMax) behind the same endpoint —
  those are filtered out here since dedicated providers already cover some of them.
- Also excludes Qwen-prefixed models that don't speak chat/completions — image generation
  (`qwen-image-*`), speech (`qwen-audio-*`, `qwen3-asr-*`, `qwen3-tts-*`), translation
  (`qwen-mt-*`, `qwen3-livetranslate-*`), speech-to-speech (`qwen3-s2s-*`), realtime WebSocket
  variants (`*-realtime`), and the `qwen3.7-text-embedding` embedding model. Confirmed against a
  live 165-model catalog during development — the plain `^qwen` regex alone was too broad.
- Default model: `qwen-plus`

**Embedding Capability** (`AlibabaEmbeddingCapability`):

- Extends `AIEmbeddingCapabilityBase<AlibabaProviderSettings>`
- Creates `IEmbeddingGenerator<string, Embedding<float>>` via `OpenAIClient.GetEmbeddingClient(modelId).AsIEmbeddingGenerator()`
- Discovers models dynamically, filtered with an `embedding` substring match — not all of
  Alibaba's embedding models are `text-embedding-*` prefixed (`qwen3.7-text-embedding` isn't)
- Default model: `text-embedding-v4`

### The "thinking" quirk (`AlibabaDisableThinkingPolicy`)

Qwen's hybrid models emit a `reasoning_content` field when "thinking" is on, which
`Microsoft.Extensions.AI`'s `ChatMessage` doesn't round-trip across turns — the same problem
`Umbraco.AI.DeepSeek` has. DashScope's OpenAI-compatible endpoint additionally rejects
non-streaming calls to reasoning-capable models unless `enable_thinking` is passed explicitly.

`AlibabaDisableThinkingPolicy` injects `enable_thinking: false` into every `/chat/completions`
request, **except** when the model ID contains `"thinking"` or `"instruct"` — per Alibaba's docs,
`-thinking` models are thinking-only (they reject `enable_thinking: false`) and `-instruct` models
don't support the toggle at all.

Live testing against `qwen-plus`, `qwen3-max`, and `qwen3-max-preview` found calls succeed
identically with or without `enable_thinking` — the documented "non-streaming fails without it"
failure mode didn't reproduce for those models on the current API version. The policy is kept
anyway since it's harmless where it isn't needed and guards against the documented failure mode
for models/API versions where it does apply. The `thinking`/`instruct` skip list itself is still
based on documentation rather than a live-tested matrix across the full model catalog — widen it
if another family turns out to reject the parameter.

### Model discovery — confirmed against a live key

`GET {endpoint}/models` is undocumented for Model Studio's OpenAI-compatible mode, but it works:
confirmed live during development against both the flat international endpoint
(`dashscope-intl.aliyuncs.com`) and a workspace-scoped endpoint, both returning an identical
165-model OpenAI-style list (`{"object": "list", "data": [...]}`). Chat completions and embeddings
were also confirmed live. The catalog mixes Qwen models with several other vendors' and several
non-chat Alibaba capabilities (see the exclude regex above) — plan for it to keep growing and
changing shape as Alibaba ships new model families.

### Settings

```csharp
public class AlibabaProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1";
}
```

The default endpoint is Model Studio's international (Singapore) region. Override `Endpoint` for
the China (Beijing) region (`https://dashscope.aliyuncs.com/compatible-mode/v1`) or a
workspace-scoped endpoint.

## Key Namespaces

- `Umbraco.AI.Alibaba` - Provider, capabilities, and settings
- `Umbraco.AI.Extensions` - Model utilities (display name formatting)

## Configuration Example

```json
{
    "Alibaba": {
        "ApiKey": "sk-..."
    }
}
```

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 17.x
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
