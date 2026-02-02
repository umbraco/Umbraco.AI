# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Anthropic provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Anthropic.sln

# Run tests
dotnet test Umbraco.AI.Anthropic.sln
```

## Architecture Overview

Umbraco.AI.Anthropic is a provider plugin for Umbraco.AI that enables integration with Anthropic's Claude models. It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

This provider uses a simplified structure (single project):

| Project | Purpose |
|---------|---------|
| `Umbraco.AI.Anthropic` | Provider implementation, capabilities, and settings |

### Provider Implementation

The provider is implemented using the `AIProviderBase<TSettings>` pattern:

```csharp
[AIProvider("anthropic", "Anthropic")]
public class AnthropicProvider : AIProviderBase<AnthropicSettings>
{
    public AnthropicProvider(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        WithCapability<AnthropicChatCapability>();
    }
}
```

### Capabilities

**Chat Capability** (`AnthropicChatCapability`):
- Extends `AIChatCapabilityBase<AnthropicSettings>`
- Creates `IChatClient` instances using Anthropic SDK
- Supports Claude 3.5 Sonnet, Claude 3 Opus, Claude 3 Sonnet, Claude 3 Haiku
- Handles model configuration with extended context windows

### Settings System

Settings use the `[AIField]` attribute for UI generation:

```csharp
public class AnthropicSettings
{
    [AIField("api-key", "API Key", AiFieldType.Password)]
    public string ApiKey { get; set; } = string.Empty;

    [AIField("base-url", "Base URL", AiFieldType.Text)]
    public string? BaseUrl { get; set; }

    // ... other settings
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$Anthropic:ApiKey"`).

### Supported Models

**Chat Models:**
- `claude-3-5-sonnet-20241022` (Claude 3.5 Sonnet)
- `claude-3-opus-20240229` (Claude 3 Opus)
- `claude-3-sonnet-20240229` (Claude 3 Sonnet)
- `claude-3-haiku-20240307` (Claude 3 Haiku)

**Key Features:**
- Extended context windows (up to 200K tokens)
- Advanced reasoning capabilities
- Vision support (for models that support it)
- System prompt support

## Key Namespaces

- `Umbraco.AI.Anthropic` - Root namespace for provider, capabilities, and settings
- `Umbraco.AI.Anthropic.Chat` - Chat capability implementation

## Configuration Example

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-..."
  }
}
```

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Anthropic SDK

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
- `FakeAiProvider` - Test double for provider testing
- `AIConnectionBuilder` - Fluent builder for test connections
- `AIProfileBuilder` - Fluent builder for test profiles

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and the root [CLAUDE.md](../CLAUDE.md) for coding standards.
