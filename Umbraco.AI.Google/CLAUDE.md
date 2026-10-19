# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Google provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Google.slnx

# Run tests
dotnet test Umbraco.AI.Google.slnx
```

## Architecture Overview

Umbraco.AI.Google is a provider plugin for Umbraco.AI that enables integration with Google's Gemini models. It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

This provider uses a simplified structure (one source project and a unit test project):

| Project                          | Purpose                                             |
| -------------------------------- | --------------------------------------------------- |
| `Umbraco.AI.Google`              | Provider implementation, capabilities, and settings |
| `Umbraco.AI.Google.Tests.Unit`   | Unit tests (xUnit + Shouldly)                       |

### Provider Implementation

The provider is implemented using the `AIProviderBase<TSettings>` pattern:

```csharp
[AIProvider("google", "Google")]
public class GoogleProvider : AIProviderBase<GoogleProviderSettings>
{
    public GoogleProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<GoogleChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`GoogleChatCapability`):

- Extends `AIChatCapabilityBase<GoogleProviderSettings>`
- Creates `IChatClient` instances using Google.GenAI SDK with `AsIChatClient()` extension
- Dynamically discovers available Gemini chat models via API using include/exclude regex patterns
- Resolves default model dynamically (prefers latest stable flash model)
- Uses async `CreateClientAsync` override for model resolution

### Error Classification

`GoogleProvider.ClassifyError` maps Google.GenAI `ClientError`/`ServerError` to `AIProviderErrorCategory` (as `AnthropicProvider` does), falling back to the shared transport mapping for anything else. The SDK keeps the HTTP status in its own `StatusCode`, so the shared mapping alone would report every Google API failure as a network error. Test it through `GoogleProvider.ClassifyError`; the tests use messages captured from real Google responses.

### Settings System

Settings use the `[AIField]` attribute for UI generation:

```csharp
public class GoogleProviderSettings
{
    [AIField]
    [Required]
    public string? ApiKey { get; set; }
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$Umbraco:AI:Secrets:GoogleApiKey"`). Resolution is default-deny — only keys under `AIOptions.AllowedConfigurationKeyPrefixes` (default `Umbraco:AI:Secrets` / `Umbraco:AI:Variables`) resolve, and secret keys only into `IsSensitive` fields. See the core docs for the rationale.

### Supported Models

**Chat Models:** Dynamically discovered from Google API. Filtered using include/exclude regex patterns:
- **Include:** Gemini flash and pro variants (`^gemini-.*\b(flash|pro)\b`)
- **Exclude:** Non-chat variants like image generation, TTS, audio, computer-use (`image|tts|audio|computer-use`)

No hardcoded model list — the provider adapts automatically as Google adds or deprecates models.

**Key Features:**

- Extended context windows (up to 1M tokens for Pro models)
- Multimodal capabilities (for supported models)
- System prompt support

## Error Classification

`GoogleProvider` overrides `ClassifyError` so Google.GenAI failures reach Umbraco.AI as typed
`AIProviderException`s (rate limited, unavailable, invalid key) instead of a generic network error.
`GoogleErrorMapping` reads the status carried by `ClientError`/`ServerError`. Those derive from
`HttpRequestException` but keep the status in their own property, which the shared mapping can't see.
It also tells a daily quota from a per-minute one, and reads the `API_KEY_INVALID` reason, because
Google reports a bad key as HTTP 400. It matches `ClientError`/`ServerError` directly. Their shared
`ApiException` base class is missing from older Google.GenAI versions but exists from the current
floor (1.22.0), so matching on the base is now an option.

## Key Namespaces

- `Umbraco.AI.Google` - Root namespace for provider, capabilities, and settings

## Configuration Example

```json
{
    "Google": {
        "ApiKey": "AIza..."
    }
}
```

## Dependencies

- Umbraco CMS 18.x
- Umbraco.AI 18.x
- Google.GenAI

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Provider Discovery

The provider is automatically discovered by Umbraco.AI through:

1. `[AIProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AIProvidersCollectionBuilder`

## Testing

For testing provider implementations, use the test utilities from `Umbraco.AI.Tests.Common`:

- `FakeAIProvider` - Test double for provider testing
- `AIConnectionBuilder` - Fluent builder for test connections
- `AIProfileBuilder` - Fluent builder for test profiles

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
