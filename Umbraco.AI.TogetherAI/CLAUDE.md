# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.TogetherAI provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
dotnet build Umbraco.AI.TogetherAI.slnx
```

## Architecture Overview

Umbraco.AI.TogetherAI integrates Together AI's serverless inference platform with Umbraco.AI. Together AI is fully OpenAI-compatible at `https://api.together.xyz/v1`, so this provider reuses `Microsoft.Extensions.AI.OpenAI` (which transitively pulls the OpenAI 2.x SDK) pointed at Together's endpoint — no Together-specific SDK is needed.

### Project Structure

Single-project layout (matches the simpler providers like Anthropic and OpenAI):

| File | Purpose |
| --- | --- |
| `TogetherAIProvider.cs` | `[AIProvider("togetherai", "Together AI")]`. Owns the `OpenAIClient` factory and the cached `/v1/models` fetcher. |
| `TogetherAIProviderSettings.cs` | `ApiKey` (sensitive) and optional `Endpoint`. |
| `TogetherAIChatCapability.cs` | Chat capability — wraps `OpenAIClient.GetChatClient(modelId).AsIChatClient()`. |
| `TogetherAIEmbeddingCapability.cs` | Embedding capability — wraps `OpenAIClient.GetEmbeddingClient(modelId).AsIEmbeddingGenerator()`. |
| `TogetherAIModelUtilities.cs` | Lives in `Umbraco.AI.Extensions` namespace (matches sibling providers). Strips the `org/` prefix and humanises model ids. |

### Dynamic Model Filtering

Together's `/v1/models` response includes a non-standard `type` field (`chat`, `embedding`, `image`, `moderation`, `rerank`, `audio`, `language`). The OpenAI 2.x SDK's typed `GetModelsAsync()` drops this field, so the provider uses a small `HttpClient`-based fetcher in `FetchModelsAsync` to read the raw JSON and produce `TogetherAIModelInfo(Id, Type, DisplayName)` records.

Each capability filters by `Type == "chat"` or `Type == "embedding"`. **No regex patterns or hard-coded model lists** — when Together adds new models in either category, they appear in the dropdown automatically. Cache TTL is 1 hour, keyed by API key hash + endpoint.

### Why Chat Completions and not the Responses API

`Umbraco.AI.OpenAI` uses `GetResponsesClient(...).AsIChatClient()` (the experimental `OPENAI001` Responses API). Together AI does not implement the Responses API — only Chat Completions — so this provider uses `GetChatClient(...).AsIChatClient()` instead.

## Key Namespaces

- `Umbraco.AI.TogetherAI` - Provider, settings, capabilities
- `Umbraco.AI.Extensions` - `TogetherAIModelUtilities` (matches the convention used by sibling providers)

## Configuration Example

```json
{
    "TogetherAI": {
        "ApiKey": "$TOGETHER_API_KEY"
    }
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$Umbraco:AI:Secrets:TogetherAIApiKey"`). Resolution is default-deny — only keys under `AIOptions.AllowedConfigurationKeyPrefixes` (default `Umbraco:AI:Secrets` / `Umbraco:AI:Variables`) resolve, and secret keys only into `IsSensitive` fields. See the core docs for the rationale.

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Microsoft.Extensions.AI.OpenAI (transitively pulls OpenAI 2.x)

## Provider Discovery

The provider is automatically discovered by Umbraco.AI via the `[AIProvider]` attribute and assembly scanning during Umbraco startup.

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
