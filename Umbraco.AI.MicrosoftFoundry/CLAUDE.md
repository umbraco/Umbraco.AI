# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.MicrosoftFoundry provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.MicrosoftFoundry.sln

# Run tests
dotnet test Umbraco.AI.MicrosoftFoundry.sln
```

## Architecture Overview

Umbraco.AI.MicrosoftFoundry is a provider plugin for Umbraco.AI that enables integration with Microsoft AI Foundry (Azure AI). It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

This provider uses a simplified structure (single project):

| Project                       | Purpose                                             |
| ----------------------------- | --------------------------------------------------- |
| `Umbraco.AI.MicrosoftFoundry` | Provider implementation, capabilities, and settings |

### Provider Implementation

The provider is implemented using the `AIProviderBase<TSettings>` pattern:

```csharp
[AIProvider("microsoft-foundry", "Microsoft AI Foundry")]
public class MicrosoftFoundryProvider : AIProviderBase<MicrosoftFoundryProviderSettings>
{
    public MicrosoftFoundryProvider(
        IAIProviderInfrastructure infrastructure,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<MicrosoftFoundryProvider> logger)
        : base(infrastructure)
    {
        WithCapability<MicrosoftFoundryChatCapability>();
        WithCapability<MicrosoftFoundryEmbeddingCapability>();
    }
}
```

### Authentication

The provider supports two authentication methods:

- **API Key**: Simple authentication using an API key. Model listing uses the OpenAI models API (shows all available models in the catalog).
- **Entra ID**: Azure AD authentication via service principal (`ClientSecretCredential`) or managed identity (`DefaultAzureCredential`). Model listing uses the deployments API (shows only deployed models).

Authentication is determined at runtime based on which settings fields are populated. If Entra ID fields are present, Entra ID is used; otherwise API key is used.

### Capabilities

**Chat Capability** (`MicrosoftFoundryChatCapability`):

- Extends `AIChatCapabilityBase<MicrosoftFoundryProviderSettings>`
- Default: Creates `IChatClient` using `AzureOpenAIClient.GetChatClient().AsIChatClient()` (Chat Completions API)
- Opt-in: When `UseResponsesApi` is enabled, uses `OpenAIClient.GetResponsesClient().AsIChatClient()` (Responses API)
- Lists chat models from the models/deployments API
- Default model: `gpt-4o`

**Embedding Capability** (`MicrosoftFoundryEmbeddingCapability`):

- Extends `AIEmbeddingCapabilityBase<MicrosoftFoundryProviderSettings>`
- Creates `IEmbeddingGenerator` instances using `AzureOpenAIClient.GetEmbeddingClient().AsIEmbeddingGenerator()`
- Lists embedding models from the models/deployments API
- Default model: `text-embedding-3-small`

### Settings System

Settings use the `[AIField]` attribute with groups for UI organization:

```csharp
public class MicrosoftFoundryProviderSettings
{
    [AIField]
    [Required]
    public string? Endpoint { get; set; }

    [AIField(Group = "Advanced")]
    public bool UseResponsesApi { get; set; }

    [AIField(Group = "EntraId")]
    public string? ProjectName { get; set; }

    [AIField(Group = "EntraId")]
    public string? TenantId { get; set; }

    [AIField(Group = "EntraId")]
    public string? ClientId { get; set; }

    [AIField(IsSensitive = true, Group = "EntraId")]
    public string? ClientSecret { get; set; }

    [AIField(IsSensitive = true, Group = "ApiKey")]
    public string? ApiKey { get; set; }
}
```

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$MicrosoftFoundry:ApiKey"`).

### Model Listing Strategy

- **API Key auth**: Calls `GET {endpoint}/openai/models?api-version=2024-10-21` — returns all models available in the catalog.
- **Entra ID auth (with ProjectName)**: Calls `GET {endpoint}/api/projects/{ProjectName}/deployments?api-version=v1` using `https://ai.azure.com/.default` scope — returns only deployed models. Falls back to the models API if the deployments call fails. Requires `Azure AI Developer` RBAC role.
- **Entra ID auth (without ProjectName)**: Falls back to the models API using `https://cognitiveservices.azure.com/.default` scope.

## Key Namespaces

- `Umbraco.AI.MicrosoftFoundry` - Root namespace for provider, capabilities, and settings

## Configuration Examples

### API Key Authentication

```json
{
    "MicrosoftFoundry": {
        "Endpoint": "https://your-resource.services.ai.azure.com/",
        "ApiKey": "..."
    }
}
```

### Entra ID Authentication (Service Principal)

```json
{
    "MicrosoftFoundry": {
        "Endpoint": "https://your-resource.services.ai.azure.com/",
        "ProjectName": "your-project-name",
        "TenantId": "your-tenant-id",
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret"
    }
}
```

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Azure.AI.OpenAI
- Azure.Identity
- Microsoft.Extensions.AI.OpenAI

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
