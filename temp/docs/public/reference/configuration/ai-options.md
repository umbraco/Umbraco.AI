---
description: >-
  Global configuration options for AI services.
---

# AiOptions

Configuration options for Umbraco.Ai services, bound from `appsettings.json`.

## Namespace

```csharp
using Umbraco.Ai.Core.Models;
```

## Class Definition

{% code title="AiOptions" %}
```csharp
public class AiOptions
{
    public string? DefaultChatProfileAlias { get; set; }
    public string? DefaultEmbeddingProfileAlias { get; set; }
}
```
{% endcode %}

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `DefaultChatProfileAlias` | `string?` | Default profile for chat operations |
| `DefaultEmbeddingProfileAlias` | `string?` | Default profile for embeddings |

## Configuration

{% code title="appsettings.json" %}
```json
{
  "Umbraco": {
    "Ai": {
      "DefaultChatProfileAlias": "content-assistant",
      "DefaultEmbeddingProfileAlias": "document-embeddings"
    }
  }
}
```
{% endcode %}

## Usage

### Setting Defaults

When you call `IAiChatService.GetResponseAsync()` without specifying a profile ID, it uses the profile with the alias specified in `DefaultChatProfileAlias`.

{% code title="Example" %}
```csharp
// Uses the default chat profile ("content-assistant" in this example)
var response = await _chatService.GetResponseAsync(messages);

// Uses a specific profile
var response = await _chatService.GetResponseAsync(specificProfileId, messages);
```
{% endcode %}

### Accessing Options

You can inject `IOptions<AiOptions>` to access configuration:

{% code title="Example" %}
```csharp
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;

public class MyService
{
    private readonly AiOptions _options;

    public MyService(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public void LogDefaults()
    {
        Console.WriteLine($"Default chat: {_options.DefaultChatProfileAlias}");
        Console.WriteLine($"Default embedding: {_options.DefaultEmbeddingProfileAlias}");
    }
}
```
{% endcode %}

## Behavior When Not Set

If default profile aliases are not configured:

1. **Chat**: Throws `InvalidOperationException` when calling methods without a profile ID
2. **Embedding**: Throws `InvalidOperationException` when calling methods without a profile ID

{% hint style="info" %}
Always configure default profiles if your application uses the simplified service methods that don't require explicit profile IDs.
{% endhint %}

## Environment Variables

Override configuration via environment variables:

```bash
export Umbraco__Ai__DefaultChatProfileAlias=production-chat
export Umbraco__Ai__DefaultEmbeddingProfileAlias=production-embedding
```

Note the double underscores (`__`) as section separators.
