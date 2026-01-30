---
description: >-
  Profile configuration for AI model usage.
---

# AiProfile

Represents a profile that combines a connection, model, and settings for a specific use case.

## Namespace

```csharp
using Umbraco.Ai.Core.Profiles;
```

## Class Definition

{% code title="AiProfile" %}
```csharp
public sealed class AiProfile
{
    public Guid Id { get; internal set; }
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public AiCapability Capability { get; init; } = AiCapability.Chat;
    public AiModelRef Model { get; set; }
    public required Guid ConnectionId { get; set; }
    public IAiProfileSettings? Settings { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
}
```
{% endcode %}

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier (assigned on save) |
| `Alias` | `string` | Unique alias for code references |
| `Name` | `string` | Display name |
| `Capability` | `AiCapability` | Type of AI capability (Chat, Embedding) |
| `Model` | `AiModelRef` | Reference to provider and model |
| `ConnectionId` | `Guid` | ID of the connection to use |
| `Settings` | `IAiProfileSettings?` | Capability-specific settings |
| `Tags` | `IReadOnlyList<string>` | Optional categorization tags |

## Settings Types

Settings are polymorphic based on capability.

### AiChatProfileSettings

{% code title="Chat Settings" %}
```csharp
public class AiChatProfileSettings : IAiProfileSettings
{
    public float? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string? SystemPromptTemplate { get; set; }
}
```
{% endcode %}

| Property | Type | Description |
|----------|------|-------------|
| `Temperature` | `float?` | Randomness (0.0-1.0, default varies by model) |
| `MaxTokens` | `int?` | Maximum response tokens |
| `SystemPromptTemplate` | `string?` | Default system prompt |

### AiEmbeddingProfileSettings

{% code title="Embedding Settings" %}
```csharp
public class AiEmbeddingProfileSettings : IAiProfileSettings
{
    // Currently no additional settings
}
```
{% endcode %}

## Creating a Profile

{% code title="Example" %}
```csharp
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Models;

var profile = new AiProfile
{
    Alias = "content-assistant",
    Name = "Content Assistant",
    Capability = AiCapability.Chat,
    Model = new AiModelRef("openai", "gpt-4o"),
    ConnectionId = connectionId,
    Settings = new AiChatProfileSettings
    {
        Temperature = 0.7f,
        MaxTokens = 4096,
        SystemPromptTemplate = "You are a helpful content assistant."
    },
    Tags = new[] { "content", "assistant" }
};

var saved = await profileService.SaveProfileAsync(profile);
```
{% endcode %}

## Notes

- `Id` is assigned automatically when saving a new profile
- `Capability` is immutable after creation (`init` setter)
- `Alias` must be unique across all profiles
- `ConnectionId` must reference a valid connection with a matching provider
