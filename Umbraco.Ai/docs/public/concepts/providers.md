---
description: >-
  Providers are installable plugins that connect Umbraco.Ai to AI services.
---

# Providers

A provider is a NuGet package that enables Umbraco.Ai to communicate with a specific AI service. Providers handle the details of API authentication, request formatting, and response parsing.

## How Providers Work

Providers are discovered automatically when you install their NuGet package. They register themselves using the `[AiProvider]` attribute and implement the `IAiProvider` interface.

```
┌─────────────────────────────────────────────────┐
│                  Umbraco.Ai                     │
│    ┌─────────────────────────────────────────┐  │
│    │        Provider Registry                │  │
│    │  ┌──────────┐  ┌──────────┐            │  │
│    │  │  OpenAI  │  │  Azure   │  ...       │  │
│    │  │ Provider │  │ Provider │            │  │
│    │  └──────────┘  └──────────┘            │  │
│    └─────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

## Available Providers

| Provider | Package | Capabilities |
|----------|---------|--------------|
| OpenAI | `Umbraco.Ai.OpenAi` | Chat, Embedding |

{% hint style="info" %}
Additional providers will be available in future releases. You can also create custom providers.
{% endhint %}

## Provider Discovery

Providers are discovered at application startup through assembly scanning. Any class with the `[AiProvider]` attribute that implements `IAiProvider` is automatically registered.

{% code title="OpenAiProvider.cs" %}
```csharp
[AiProvider("openai", "OpenAI")]
public class OpenAiProvider : AiProviderBase<OpenAiProviderSettings>
{
    public OpenAiProvider(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        WithCapability<OpenAiChatCapability>();
        WithCapability<OpenAiEmbeddingCapability>();
    }
}
```
{% endcode %}

## Provider Settings

Each provider defines its own settings class. Common settings include:

| Setting | Description |
|---------|-------------|
| API Key | Authentication credential |
| Endpoint | API URL (for custom endpoints) |
| Organization | Organization identifier (if applicable) |

Settings are defined using the `[AiSetting]` attribute:

{% code title="OpenAiProviderSettings.cs" %}
```csharp
public class OpenAiProviderSettings
{
    [AiSetting(Label = "API Key", Description = "Your OpenAI API key")]
    public required string ApiKey { get; set; }

    [AiSetting(Label = "Organization", Description = "Optional organization ID")]
    public string? Organization { get; set; }
}
```
{% endcode %}

## Provider Capabilities

A provider can support multiple capabilities:

* **Chat** - Conversational AI and text generation
* **Embedding** - Vector embeddings for semantic search
* **Media** - Image generation (future)
* **Moderation** - Content safety checks (future)

Each capability is implemented as a separate class and registered in the provider constructor.

## Accessing Providers in Code

You rarely need to interact with providers directly. The service layer handles provider resolution based on the profile's connection.

If you need to access provider information:

{% code title="Example.cs" %}
```csharp
public class ProviderInfo
{
    private readonly IAiRegistry _registry;

    public ProviderInfo(IAiRegistry registry)
    {
        _registry = registry;
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return _registry.GetProviders().Select(p => p.Name);
    }
}
```
{% endcode %}

## Creating Custom Providers

You can create providers for AI services not yet supported. See:

{% content-ref url="../extending/providers/README.md" %}
[Custom Providers](../extending/providers/README.md)
{% endcontent-ref %}

## Related

* [Connections](connections.md) - Store credentials for a provider
* [Capabilities](capabilities.md) - The operations a provider supports
