---
description: >-
  Enumeration of AI capability types.
---

# AiCapability

Enumeration defining the types of AI capabilities available.

## Namespace

```csharp
using Umbraco.Ai.Core.Models;
```

## Definition

{% code title="AiCapability" %}
```csharp
public enum AiCapability
{
    Chat = 0,
    Embedding = 1,
    Media = 2,
    Moderation = 3
}
```
{% endcode %}

## Values

| Value | Int | Description | Status |
|-------|-----|-------------|--------|
| `Chat` | 0 | Conversational AI / chat completions | Available |
| `Embedding` | 1 | Text to vector embeddings | Available |
| `Media` | 2 | Image/media generation | Planned |
| `Moderation` | 3 | Content safety/moderation | Planned |

## Usage

### Filtering Profiles

{% code title="Example" %}
```csharp
// Get all chat profiles
var chatProfiles = await profileService.GetProfilesAsync(AiCapability.Chat);

// Get all embedding profiles
var embeddingProfiles = await profileService.GetProfilesAsync(AiCapability.Embedding);
```
{% endcode %}

### Getting Available Capabilities

{% code title="Example" %}
```csharp
// Get capabilities available from configured connections
var available = await connectionService.GetAvailableCapabilitiesAsync();

foreach (var capability in available)
{
    Console.WriteLine($"Available: {capability}");
}
```
{% endcode %}

### Creating Profiles

{% code title="Example" %}
```csharp
// Chat profile
var chatProfile = new AiProfile
{
    Alias = "chat-assistant",
    Name = "Chat Assistant",
    Capability = AiCapability.Chat,
    // ...
};

// Embedding profile
var embeddingProfile = new AiProfile
{
    Alias = "document-embeddings",
    Name = "Document Embeddings",
    Capability = AiCapability.Embedding,
    // ...
};
```
{% endcode %}

### Checking Provider Support

{% code title="Example" %}
```csharp
// Get connections that support chat
var chatConnections = await connectionService.GetConnectionsByCapabilityAsync(
    AiCapability.Chat);

// Get connections that support embeddings
var embeddingConnections = await connectionService.GetConnectionsByCapabilityAsync(
    AiCapability.Embedding);
```
{% endcode %}

## Notes

- `Chat` and `Embedding` are currently implemented
- `Media` and `Moderation` are reserved for future use
- Capability is set on profile creation and cannot be changed
