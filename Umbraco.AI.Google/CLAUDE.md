# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Google provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Google.sln

# Run tests
dotnet test Umbraco.AI.Google.sln
```

## Architecture Overview

Umbraco.AI.Google is a provider plugin for Umbraco.AI that enables integration with Google's Gemini models. It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

This provider uses a simplified structure (single project):

| Project | Purpose |
|---------|---------|
| `Umbraco.AI.Google` | Provider implementation, capabilities, and settings |

### Provider Implementation

The provider is implemented using the `AiProviderBase<TSettings>` pattern:

```csharp
[AiProvider("google", "Google")]
public class GoogleProvider : AiProviderBase<GoogleProviderSettings>
{
    public GoogleProvider(IAiProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        WithCapability<GoogleChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`GoogleChatCapability`):
- Extends `AiChatCapabilityBase<GoogleProviderSettings>`
- Creates `IChatClient` instances using Google.GenAI SDK with `AsIChatClient()` extension
- Supports Gemini 2.0, 1.5 Pro, 1.5 Flash models
- Handles model configuration with extended context windows

### Settings System

Settings use the `[AiField]` attribute for UI generation:

```csharp
public class GoogleProviderSettings
{
    [AiField]
    [Required]
    public string? ApiKey { get; set; }
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$Google:ApiKey"`).

### Supported Models

**Chat Models:**
- `gemini-2.0-flash` (Gemini 2.0 Flash)
- `gemini-2.0-flash-lite` (Gemini 2.0 Flash Lite)
- `gemini-1.5-pro` (Gemini 1.5 Pro)
- `gemini-1.5-flash` (Gemini 1.5 Flash)
- `gemini-1.5-flash-8b` (Gemini 1.5 Flash 8B)

**Key Features:**
- Extended context windows (up to 1M tokens for Pro models)
- Multimodal capabilities (for supported models)
- System prompt support

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

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Google.GenAI

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Provider Discovery

The provider is automatically discovered by Umbraco.AI through:
1. `[AiProvider]` attribute on the provider class
2. Assembly scanning during Umbraco startup
3. Registration in the `AiProvidersCollectionBuilder`

## Testing

For testing provider implementations, use the test utilities from `Umbraco.AI.Tests.Common`:
- `FakeAiProvider` - Test double for provider testing
- `AiConnectionBuilder` - Fluent builder for test connections
- `AiProfileBuilder` - Fluent builder for test profiles

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
