# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Perplexity provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Perplexity.slnx
```

## Architecture Overview

Umbraco.AI.Perplexity is a provider plugin for Umbraco.AI that integrates Perplexity's Sonar search-augmented chat models. Perplexity's API is OpenAI-compatible, so the provider reuses the `OpenAI` .NET SDK (via `Microsoft.Extensions.AI.OpenAI`) pointed at `https://api.perplexity.ai`.

### Project Structure

| Project                 | Purpose                                             |
| ----------------------- | --------------------------------------------------- |
| `Umbraco.AI.Perplexity` | Provider implementation, capabilities, and settings |

### Provider Implementation

```csharp
[AIProvider("perplexity", "Perplexity")]
public class PerplexityProvider : AIProviderBase<PerplexityProviderSettings>
{
    public PerplexityProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<PerplexityChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`PerplexityChatCapability`):

- Extends `AIChatCapabilityBase<PerplexityProviderSettings>`
- Creates `IChatClient` instances via the OpenAI SDK pointed at Perplexity's endpoint, wrapped in `PerplexityChatClient` (a `DelegatingChatClient`) to apply Perplexity-specific adaptations
- Dynamic model discovery — `GET /v1/models` filtered to `owned_by == "perplexity"`, with the `perplexity/` prefix stripped before use with chat completions
- API-key validation via a 1-token chat-completions probe (`/v1/models` is unauthenticated on Perplexity, so it can't validate keys on its own)

**`PerplexityChatClient` adaptations**:

- **Strips `Tools` and `ToolMode` from outgoing requests.** Perplexity's Sonar `/chat/completions` API has no `tools` parameter and rejects requests with one. Tool-calling is a deliberate non-feature on Sonar (search is the built-in capability). See "Limitations" below.
- **Reorders messages so system messages come first.** Perplexity requires the last message to have role `user` or `tool`, but Umbraco.AI middleware (e.g., runtime context injection) can append a system message after the user prompt. We move all system messages to the front, preserving the order of everything else.
- **Surfaces 4xx response bodies in exceptions** so configuration mistakes show up as readable errors instead of "Bad Request".

### Settings

```csharp
public class PerplexityProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://api.perplexity.ai";
}
```

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Microsoft.Extensions.AI.OpenAI (Perplexity API is OpenAI-compatible)

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Provider Discovery

The provider is automatically discovered by Umbraco.AI through:

1. `[AIProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AIProvidersCollectionBuilder`

## Limitations

**Tool / function calling is not supported on Sonar.** Confirmed against Perplexity's official OpenAPI schema (no `tools` field in the request, no `tool_calls` in the response) and against multiple OSS integrations: LiteLLM closed the bug "as not planned"; Perplexity's own marketing copy is misleading. This is a Perplexity product decision — search is the model's built-in capability instead of generic function calling.

If Perplexity later ships tool calling on Sonar (which the marketing copy already implies), drop `StripUnsupportedOptions` in `PerplexityChatClient` and tools will pass through. Until then, surface this clearly in user-facing docs and recommend OpenAI/Anthropic for tool-using flows.

Tool calling *does* work on Perplexity's separate **Agent API** (`/v1/agent`), but that endpoint routes to third-party models (`openai/gpt-5.4`, `anthropic/claude-…`) — a different integration shape and a different product. Not in scope for this provider.

## Notes

- `GET /v1/models` on Perplexity is unauthenticated — it returns 200 with no auth header. We can't use it to validate API keys; that's why `GetAvailableModelIdsAsync` does a 1-token chat-completions probe first.
- `/v1/models` returns entries like `perplexity/sonar`, `openai/gpt-5.4`, etc. (tied to the Agent API namespace). We filter to `owned_by == "perplexity"` and strip the `perplexity/` prefix because `/chat/completions` expects the bare model ID (`sonar`, not `perplexity/sonar`).
- Perplexity returns `citations` and `search_results` as top-level fields alongside `choices`. The OpenAI SDK drops these silently — surfacing them via `ChatResponse.AdditionalProperties` is a future enhancement.

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
