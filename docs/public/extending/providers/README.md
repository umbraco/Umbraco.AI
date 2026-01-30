---
description: >-
  Create custom AI providers to support additional AI services.
---

# Custom Providers

Providers are plugins that connect Umbraco.Ai to AI services. Create a custom provider to add support for AI services not included out of the box.

## Provider Architecture

A provider consists of:

1. **Provider class** - Main class with `[AiProvider]` attribute
2. **Settings class** - Configuration properties with `[AiSetting]` attributes
3. **Capability classes** - Implementations for Chat, Embedding, and so on

```
MyProvider/
├── MyProvider.cs              # Main provider class
├── MyProviderSettings.cs      # Settings with [AiSetting] attributes
├── MyChatCapability.cs        # Chat capability implementation
└── MyEmbeddingCapability.cs   # Embedding capability (optional)
```

## Quick Start

### 1. Create a Settings Class

{% code title="MyProviderSettings.cs" %}
```csharp
using Umbraco.Ai.Core.Settings;

public class MyProviderSettings
{
    [AiSetting(Label = "API Key", Description = "Your API key")]
    public required string ApiKey { get; set; }

    [AiSetting(Label = "Endpoint", Description = "API endpoint URL")]
    public string? Endpoint { get; set; }
}
```
{% endcode %}

### 2. Create a Chat Capability

{% code title="MyChatCapability.cs" %}
```csharp
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

public class MyChatCapability : AiChatCapabilityBase<MyProviderSettings>
{
    public MyChatCapability(IAiProvider provider) : base(provider) { }

    protected override IChatClient CreateClient(MyProviderSettings settings, string? modelId)
    {
        // Create and return your IChatClient implementation
        return new MyChatClient(settings.ApiKey, settings.Endpoint, modelId);
    }

    protected override Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        MyProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Return available models
        var models = new List<AiModelDescriptor>
        {
            new("my-model-1", "My Model 1"),
            new("my-model-2", "My Model 2")
        };
        return Task.FromResult<IReadOnlyList<AiModelDescriptor>>(models);
    }
}
```
{% endcode %}

### 3. Create the Provider Class

{% code title="MyProvider.cs" %}
```csharp
using Umbraco.Ai.Core.Providers;

[AiProvider("myprovider", "My AI Provider")]
public class MyProvider : AiProviderBase<MyProviderSettings>
{
    public MyProvider(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        WithCapability<MyChatCapability>();
    }
}
```
{% endcode %}

## How It Works

1. Providers are discovered automatically via assembly scanning
2. The `[AiProvider]` attribute registers the provider with an ID and name
3. Settings are rendered in the backoffice using `[AiSetting]` metadata
4. Capabilities are registered in the constructor with `WithCapability<T>()`

## In This Section

{% content-ref url="creating-a-provider.md" %}
[Creating a Provider](creating-a-provider.md)
{% endcontent-ref %}

{% content-ref url="provider-settings.md" %}
[Provider Settings](provider-settings.md)
{% endcontent-ref %}

{% content-ref url="chat-capability.md" %}
[Chat Capability](chat-capability.md)
{% endcontent-ref %}

{% content-ref url="embedding-capability.md" %}
[Embedding Capability](embedding-capability.md)
{% endcontent-ref %}
