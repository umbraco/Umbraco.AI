# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.OpenRouter provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
dotnet build Umbraco.AI.OpenRouter.slnx
```

## Architecture Overview

OpenRouter exposes an OpenAI-compatible Chat Completions API in front of hundreds of models from
dozens of vendors (OpenAI, Anthropic, Google, Meta, Mistral, DeepSeek, and more) behind a single
endpoint and API key. This provider uses the `Microsoft.Extensions.AI.OpenAI` SDK with a custom
endpoint (`https://openrouter.ai/api/v1`) — the same approach as `Umbraco.AI.FireworksAI` and
`Umbraco.AI.TogetherAI`.

### Why OpenRouter's models endpoint is enough

Unlike Fireworks (which needs its *native*, non-OpenAI-compatible endpoint for capability metadata),
OpenRouter's own `GET /models` — reachable at the same OpenAI-compatible base URL — already returns
everything this provider needs per model:

- `id` — the slug to pass to Chat Completions, e.g. `openai/gpt-4o`, `anthropic/claude-3.5-sonnet`
- `name` — a vendor-authored display name, e.g. `Anthropic: Claude v2.0`
- `supported_parameters` — the exact request parameters the model accepts, e.g. `["temperature",
  "top_p", "tools", "tool_choice", "response_format", "reasoning_effort", ...]`

No hardcoded regex model families, and no separate native endpoint call. `GET /models` also only
ever returns chat-completion models — OpenRouter has no embedding models mixed into this listing —
so `OpenRouterChatCapability.GetModelsAsync` does not need an `IsChatModel` filter the way Foundry
and Fireworks do.

### Per-Model Sampling Support

`OpenRouterChatCapability.GetSettingsSupport` checks a model's `supported_parameters` against the
five core sampling keys (`temperature`, `topP`, `topK`, `frequencyPenalty`, `presencePenalty`) and
declares whichever ones are absent as unsupported. This is the same declaration mechanism Foundry
uses (`AIModelSettingsSupport` / `AIProfileSettingKeys.Sampling`), but the predicate comes from data
OpenRouter reports directly instead of a maintained list of vendor families.

**A model absent from the last listing is treated as fully supported** — the opposite of the
single-vendor providers' allow-list philosophy. OpenRouter's catalog is the only source of truth for
its own restrictions; there is no vendor-family fallback to reason from the way Foundry falls back
to OpenAI's or Anthropic's own patterns for an unrecognised deployment. An unknown model here just
means "not yet fetched" or "dropped from the catalog," not "restricted."

Models are cached for one hour, both as the full listing and per-model (keyed by model id), the same
two-tier cache Foundry uses — see `OpenRouterProvider.GetAvailableModelsAsync` /
`TryGetModelInfo`. The chat capability prefetches the (cached) listing in `CreateClientAsync` so the
per-model lookup is warm by the time the base enforces the declaration.

### Capabilities

**Chat** (`OpenRouterChatCapability`)
- `OpenAIClient.GetChatClient(modelId).AsIChatClient()` against the OpenRouter endpoint
- Default model: `openai/gpt-4o-mini`

No embedding capability — see "Not yet supported" below.

### Settings

```csharp
public class OpenRouterProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://openrouter.ai/api/v1";
}
```

## Not yet supported (deliberate scope, not a gap)

- **Provider routing / fallback.** OpenRouter's `provider` request object (order, sort by
  price/throughput, ignore, data_collection) and the top-level `models` fallback array are real
  differentiators, but Umbraco.AI's profile settings have nowhere to put them yet — that needs new
  settings design, not just a new provider file. Sending them at all would require a
  `ChatOptions.RawRepresentationFactory` patch (M.E.AI's `ChatCompletionOptions` has no first-class
  concept of either field), the same technique `Umbraco.AI.OpenAI` uses for the Responses API's
  reasoning options.
- **Reasoning effort.** OpenRouter reports `reasoning_effort` / `reasoning` in `supported_parameters`
  for reasoning models, but wiring it up needs the same raw-request-patch approach and a capability
  settings type (see `OpenAIChatCapabilitySettings.ReasoningEffort` for the pattern). Skipped for v1
  to keep this package chat-only and simple.
- **Embeddings.** OpenRouter added an OpenAI-compatible `/embeddings` endpoint recently. It's new
  enough that it hasn't been exercised here — add `OpenRouterEmbeddingCapability` once it's been
  tested against real traffic, following the `Umbraco.AI.Mistral` or `Umbraco.AI.FireworksAI`
  embedding capability as a template.
- **Structured-output/tools conflicts.** Fireworks AI rejects a request that combines
  `response_format` with `tools`, which is why `FireworksAIStructuredOutputChatClient` exists. This
  has not been confirmed as an OpenRouter (or per-backend) issue — don't pre-emptively copy that
  workaround here without reproducing the failure first.

## Key Namespaces

- `Umbraco.AI.OpenRouter` — Provider, capability, settings, models-response DTOs
- `Umbraco.AI.Extensions` — `OpenRouterModelUtilities` display-name formatter

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Microsoft.Extensions.AI.OpenAI (OpenAI-compatible client)

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management
- Nullable reference types enabled

## Provider Discovery

Auto-discovered via `[AIProvider("openrouter", "OpenRouter")]` and assembly scanning during Umbraco
startup.
