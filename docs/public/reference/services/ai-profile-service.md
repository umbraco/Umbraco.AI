---
description: >-
  Service for managing AI profiles.
---

# IAiProfileService

Service for profile CRUD operations and lookups.

## Namespace

```csharp
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Models;
```

## Interface

{% code title="IAiProfileService" %}
```csharp
public interface IAiProfileService
{
    Task<AiProfile?> GetProfileAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AiProfile?> GetProfileByAliasAsync(string alias, CancellationToken cancellationToken = default);

    Task<IEnumerable<AiProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<AiProfile>> GetProfilesAsync(AiCapability capability, CancellationToken cancellationToken = default);

    Task<(IEnumerable<AiProfile> Items, int Total)> GetProfilesPagedAsync(
        string? filter = null,
        AiCapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    Task<AiProfile> GetDefaultProfileAsync(AiCapability capability, CancellationToken cancellationToken = default);

    Task<AiProfile> SaveProfileAsync(AiProfile profile, CancellationToken cancellationToken = default);

    Task<bool> DeleteProfileAsync(Guid id, CancellationToken cancellationToken = default);
}
```
{% endcode %}

## Methods

### GetProfileAsync

Gets a profile by its unique identifier.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `Guid` | The profile ID |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The profile if found, otherwise `null`.

{% code title="Example" %}
```csharp
var profile = await _profileService.GetProfileAsync(profileId);
if (profile != null)
{
    Console.WriteLine($"Profile: {profile.Name}");
    Console.WriteLine($"Model: {profile.Model}");
}
```
{% endcode %}

### GetProfileByAliasAsync

Gets a profile by its alias.

| Parameter | Type | Description |
|-----------|------|-------------|
| `alias` | `string` | The profile alias |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The profile if found, otherwise `null`.

{% code title="Example" %}
```csharp
var profile = await _profileService.GetProfileByAliasAsync("content-assistant");
```
{% endcode %}

### GetAllProfilesAsync

Gets all profiles.

**Returns**: All profiles in the system.

### GetProfilesAsync

Gets profiles for a specific capability.

| Parameter | Type | Description |
|-----------|------|-------------|
| `capability` | `AiCapability` | The capability to filter by |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: Profiles matching the capability.

{% code title="Example" %}
```csharp
var chatProfiles = await _profileService.GetProfilesAsync(AiCapability.Chat);
var embeddingProfiles = await _profileService.GetProfilesAsync(AiCapability.Embedding);
```
{% endcode %}

### GetProfilesPagedAsync

Gets profiles with pagination and optional filtering.

| Parameter | Type | Description |
|-----------|------|-------------|
| `filter` | `string?` | Filter by name (case-insensitive contains) |
| `capability` | `AiCapability?` | Filter by capability |
| `skip` | `int` | Items to skip |
| `take` | `int` | Items to take |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: Tuple of (profiles, total count).

{% code title="Example" %}
```csharp
var (profiles, total) = await _profileService.GetProfilesPagedAsync(
    filter: "assistant",
    capability: AiCapability.Chat,
    skip: 0,
    take: 10);

Console.WriteLine($"Found {total} profiles, showing {profiles.Count()}");
```
{% endcode %}

### GetDefaultProfileAsync

Gets the default profile for a capability.

| Parameter | Type | Description |
|-----------|------|-------------|
| `capability` | `AiCapability` | The capability |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The default profile.

**Throws**: `InvalidOperationException` if no default profile is configured.

{% code title="Example" %}
```csharp
try
{
    var defaultChat = await _profileService.GetDefaultProfileAsync(AiCapability.Chat);
    Console.WriteLine($"Default chat profile: {defaultChat.Alias}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("No default chat profile configured");
}
```
{% endcode %}

### SaveProfileAsync

Creates or updates a profile.

| Parameter | Type | Description |
|-----------|------|-------------|
| `profile` | `AiProfile` | The profile to save |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The saved profile with ID assigned.

{% code title="Example" %}
```csharp
var profile = new AiProfile
{
    Alias = "new-assistant",
    Name = "New Assistant",
    Capability = AiCapability.Chat,
    Model = new AiModelRef("openai", "gpt-4o"),
    ConnectionId = connectionId,
    Settings = new AiChatProfileSettings
    {
        Temperature = 0.7f,
        MaxTokens = 4096
    }
};

var saved = await _profileService.SaveProfileAsync(profile);
Console.WriteLine($"Created profile with ID: {saved.Id}");
```
{% endcode %}

### DeleteProfileAsync

Deletes a profile by ID.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `Guid` | The profile ID |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: `true` if deleted, `false` if not found.

{% code title="Example" %}
```csharp
var deleted = await _profileService.DeleteProfileAsync(profileId);
if (deleted)
{
    Console.WriteLine("Profile deleted");
}
```
{% endcode %}
