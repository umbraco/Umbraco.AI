# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.MicrosoftFoundry provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.MicrosoftFoundry.slnx

# Run tests
dotnet test Umbraco.AI.MicrosoftFoundry.slnx
```

## Architecture Overview

Umbraco.AI.MicrosoftFoundry is a provider plugin for Umbraco.AI that enables integration with Microsoft AI Foundry (Azure AI). It follows the provider plugin architecture defined by Umbraco.AI.Core.

### Project Structure

| Project                                  | Purpose                                             |
| ---------------------------------------- | --------------------------------------------------- |
| `Umbraco.AI.MicrosoftFoundry`            | Provider implementation, capabilities, and settings |
| `Umbraco.AI.MicrosoftFoundry.Tests.Unit` | Unit tests                                          |

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

### Per-Model Setting Support

Foundry is a **gateway**: it fronts other vendors' models and inherits their per-model restrictions without
owning any of them. Both capabilities override `GetSettingsSupport`, which the core capability bases both
project into the model list (so the profile editor hides what does not apply) and enforce per request (so a
stale profile cannot send it). `MicrosoftFoundryModelUtilities` holds the predicates behind it.

The gateway shape makes these predicates deliberately **different from the first-party provider packages**,
and the difference is easy to get wrong:

- **Unknown models are treated as supported**, the opposite of the allow-lists in `Umbraco.AI.OpenAI` and
  `Umbraco.AI.Anthropic`. Those packages each own a closed set of models with a known restriction, so an
  allow-list means an unrecognised model degrades rather than fails. Foundry's catalogue is mostly Mistral,
  Llama, Cohere, Phi and Nova, almost none of which restrict anything, so the same allow-list would stop
  sending a temperature that most of the catalogue honours today.
- **Within a vendor known to restrict, allow-list semantics still apply.** A name that reads as OpenAI's or
  Anthropic's but is unrecognised (a future `gpt-6`, a `claude-opus-4-9`) is treated as restricted, so the
  fail-safe behaviour is kept exactly where it is warranted.
- **A reported publisher can only rule a restriction out, never in** — a Llama deployment named `o3-llama`
  must not inherit o3's restriction, but knowing only that a vendor is OpenAI is not enough to infer that a
  given deployment is restricted.
- **Azure spells GPT-3.5 without the dot** (`gpt-35-turbo`), because an Azure model name cannot contain one.
  OpenAI's own `^gpt-3\.5` pattern never has to match that, so the undotted form is listed here separately.

The family patterns duplicate a small part of `OpenAIModelUtilities` and `AnthropicModelUtilities`. That is
intentional — sharing them would couple independently released packages, and lifting them into core would put
vendor knowledge where it does not belong — so **a change to a vendor's restrictions needs the same edit in
every package naming that vendor's families**.

### Deployment Metadata

The deployments API reports `modelName`, `modelVersion` and `modelPublisher` alongside the deployment name.
This matters because **a deployment name is user-chosen and can say nothing about the model behind it**: a
deployment called `prod-chat` may front `o3`. The provider carries all three onto `MicrosoftFoundryModelInfo`
and caches each entry under its own key, so `TryGetModelInfo` can read them synchronously at request time.
Both capabilities prefetch the (cached) model list when creating a client so that lookup is warm.

They are `null` on the models API path, which reports only an ID, so every consumer has to cope with not
knowing — that case falls back to reasoning from the ID, which keeps today's behaviour rather than guessing.

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

Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$Umbraco:AI:Secrets:MicrosoftFoundryApiKey"`). Resolution is default-deny — only keys under `AIOptions.AllowedConfigurationKeyPrefixes` (default `Umbraco:AI:Secrets` / `Umbraco:AI:Variables`) resolve, and secret keys only into `IsSensitive` fields. See the core docs for the rationale.

### Model Listing Strategy

- **API Key auth**: Calls `GET {endpoint}/openai/models?api-version=2024-10-21` — returns all models available in the catalog.
- **Entra ID auth (with ProjectName)**: Calls `GET {endpoint}/api/projects/{ProjectName}/deployments?api-version=v1` using `https://ai.azure.com/.default` scope — returns only deployed models, and the only path that reports what each deployment fronts (see Deployment Metadata). Falls back to the models API if the deployments call fails. Requires `Azure AI Developer` RBAC role.
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
