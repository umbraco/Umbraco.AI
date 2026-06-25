# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.FireworksAI provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
dotnet build Umbraco.AI.FireworksAI.slnx
```

## Architecture Overview

Fireworks AI exposes an OpenAI-compatible API, so this provider uses the `Microsoft.Extensions.AI.OpenAI` SDK with a custom endpoint (`https://api.fireworks.ai/inference/v1`). The OpenAI SDK handles chat and embedding requests; a small hand-rolled HTTP call queries Fireworks' **native** models endpoint (`/v1/accounts/{AccountId}/models`) to get model metadata — `conversationConfig`, `kind`, `supportsServerless` — which drives chat-vs-embedding classification.

### Why the native models endpoint

Fireworks' OpenAI-compatible `/inference/v1/models` returns only `{id, object, created, owned_by}` — no capability info. Without capability metadata we'd need hardcoded regex patterns that would go stale the moment Fireworks ships a new family. The native endpoint returns `gatewayModel` objects with:

- `conversationConfig` — present iff the model accepts Chat Completions
- `kind` — `HF_BASE_MODEL`, `EMBEDDING_MODEL`, `FIRE_AGENT`, …
- `supportsServerless` — whether the user can hit it via the shared API without dedicated deployment

Classification: **chat** = `conversationConfig != null`, **embedding** = `kind == "EMBEDDING_MODEL"`. Only serverless-capable models are surfaced (dedicated-deployment-only models would 404 for most users).

### Account id

The `AccountId` setting defaults to `fireworks` — the public catalog. Advanced users with their own Fireworks account can override this to expose fine-tuned or private models. The id flows into both the models URL and the `accounts/{AccountId}/models/{name}` model id passed to the chat/embedding client.

### Capabilities

**Chat** (`FireworksAIChatCapability`)
- `OpenAIClient.GetChatClient(modelId).AsIChatClient()` against the Fireworks endpoint
- Model id format: `accounts/{AccountId}/models/{model-name}` — Fireworks requires the full path

**Embedding** (`FireworksAIEmbeddingCapability`)
- `OpenAIClient.GetEmbeddingClient(modelId).AsIEmbeddingGenerator()`
- Same model id format

### Settings

```csharp
public class FireworksAIProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? AccountId { get; set; } = "fireworks";

    [AIField]
    public string? Endpoint { get; set; } = "https://api.fireworks.ai/inference/v1";
}
```

## Key Namespaces

- `Umbraco.AI.FireworksAI` — Provider, capabilities, settings
- `Umbraco.AI.Extensions` — `FireworksAIModelUtilities` display-name formatter

## Dependencies

- Umbraco CMS 18.x
- Umbraco.AI 18.x
- Microsoft.Extensions.AI.OpenAI (OpenAI-compatible client)

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management
- Nullable reference types enabled

## Provider Discovery

Auto-discovered via `[AIProvider("fireworks-ai", "Fireworks AI")]` and assembly scanning during Umbraco startup.
