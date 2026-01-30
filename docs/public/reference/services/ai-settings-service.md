---
description: >-
  Service for managing global AI settings.
---

# IAiSettingsService

Service for reading and updating global AI settings such as default profiles.

## Namespace

```csharp
using Umbraco.Ai.Core.Settings;
```

## Interface

{% code title="IAiSettingsService" %}
```csharp
public interface IAiSettingsService
{
    Task<AiSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<AiSettings> SaveSettingsAsync(AiSettings settings, CancellationToken cancellationToken = default);
}
```
{% endcode %}

## Methods

### GetSettingsAsync

Gets the current global AI settings.

| Parameter | Type | Description |
|-----------|------|-------------|
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The current settings. Creates default settings if none exist.

{% code title="Example" %}
```csharp
var settings = await _settingsService.GetSettingsAsync();

if (settings.DefaultChatProfileId.HasValue)
{
    Console.WriteLine($"Default chat profile: {settings.DefaultChatProfileId}");
}
else
{
    Console.WriteLine("No default chat profile configured");
}
```
{% endcode %}

### SaveSettingsAsync

Updates the global AI settings.

| Parameter | Type | Description |
|-----------|------|-------------|
| `settings` | `AiSettings` | The settings to save |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The saved settings with audit properties updated.

{% code title="Example" %}
```csharp
var settings = await _settingsService.GetSettingsAsync();
settings.DefaultChatProfileId = chatProfileId;
settings.DefaultEmbeddingProfileId = embeddingProfileId;

var saved = await _settingsService.SaveSettingsAsync(settings);
Console.WriteLine($"Settings updated at {saved.DateModified}");
```
{% endcode %}

## Usage Example

{% code title="SettingsManager.cs" %}
```csharp
public class SettingsManager
{
    private readonly IAiSettingsService _settingsService;
    private readonly IAiProfileService _profileService;

    public SettingsManager(
        IAiSettingsService settingsService,
        IAiProfileService profileService)
    {
        _settingsService = settingsService;
        _profileService = profileService;
    }

    public async Task SetDefaultChatProfileAsync(string profileAlias)
    {
        var profile = await _profileService.GetProfileByAliasAsync(profileAlias);
        if (profile == null)
        {
            throw new ArgumentException($"Profile '{profileAlias}' not found");
        }

        if (profile.Capability != AiCapability.Chat)
        {
            throw new ArgumentException($"Profile '{profileAlias}' is not a chat profile");
        }

        var settings = await _settingsService.GetSettingsAsync();
        settings.DefaultChatProfileId = profile.Id;
        await _settingsService.SaveSettingsAsync(settings);
    }

    public async Task ClearDefaultsAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        settings.DefaultChatProfileId = null;
        settings.DefaultEmbeddingProfileId = null;
        await _settingsService.SaveSettingsAsync(settings);
    }
}
```
{% endcode %}

## Notes

- Settings use a singleton pattern - there is always exactly one settings record
- Changes to settings are tracked in the audit log
- Settings are cached for performance; cache is invalidated on save

## Related

* [AiSettings](../models/ai-settings.md) - The settings model
* [Settings Concept](../../concepts/settings.md) - Settings concepts
