---
description: >-
  Global AI settings configure default profiles and system-wide behavior.
---

# Settings

AI Settings provide a central place to configure system-wide defaults for Umbraco.Ai. These settings are stored in the database and managed through the backoffice or programmatically.

## What Settings Store

| Property | Description |
|----------|-------------|
| `Id` | Fixed identifier (always the same GUID) |
| `DefaultChatProfileId` | The profile used when no profile is specified for chat operations |
| `DefaultEmbeddingProfileId` | The profile used when no profile is specified for embedding operations |

{% hint style="info" %}
Settings are a singleton entity - there is only one settings record for the entire application.
{% endhint %}

## Configuring Default Profiles

The recommended way to configure default profiles is through the backoffice:

1. Navigate to **Settings** > **AI** > **Settings**
2. Select your default chat profile from the dropdown
3. Select your default embedding profile (if applicable)
4. Click **Save**

See [Managing Settings](../backoffice/managing-settings.md) for detailed instructions.

## Using Settings in Code

### Getting Current Settings

{% code title="Example.cs" %}
```csharp
public class SettingsExample
{
    private readonly IAiSettingsService _settingsService;

    public SettingsExample(IAiSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<AiSettings> GetCurrentSettings()
    {
        return await _settingsService.GetSettingsAsync();
    }
}
```
{% endcode %}

### Updating Settings

{% code title="Example.cs" %}
```csharp
public async Task UpdateDefaultProfile(Guid chatProfileId)
{
    var settings = await _settingsService.GetSettingsAsync();
    settings.DefaultChatProfileId = chatProfileId;
    await _settingsService.SaveSettingsAsync(settings);
}
```
{% endcode %}

## How Default Profiles Work

When you call an AI service without specifying a profile:

1. The service checks for a default profile in AI Settings (database)
2. If not found, an exception is thrown

{% code title="Example.cs" %}
```csharp
// Uses the default chat profile from Settings
var response = await _chatService.GetChatResponseAsync(messages);

// Explicitly specifies a profile (overrides default)
var response = await _chatService.GetChatResponseAsync(profileId, messages);
```
{% endcode %}

## Configuration File Fallback

For advanced scenarios like CI/CD pipelines or infrastructure-as-code, you can configure defaults via `appsettings.json`:

{% code title="appsettings.json" %}
```json
{
  "Umbraco": {
    "Ai": {
      "DefaultChatProfileAlias": "content-writer",
      "DefaultEmbeddingProfileAlias": "embeddings"
    }
  }
}
```
{% endcode %}

{% hint style="warning" %}
Database settings (configured via backoffice) take precedence over configuration file settings. Use configuration files only when you need environment-specific defaults that can't be managed through the backoffice.
{% endhint %}

## Audit Trail

AI Settings changes are tracked in the audit log. Every modification records:

- When the change was made
- Who made the change
- What was changed

See [Audit Logs](../backoffice/audit-logs.md) for more information.

## Managing Settings

### Via Backoffice

1. Navigate to **Settings** > **AI** > **Settings**
2. Select default profiles from the dropdowns
3. Save changes

### Via Management API

```http
GET /umbraco/management/api/v1/ai/settings
PUT /umbraco/management/api/v1/ai/settings
```

See [Settings API](../management-api/settings/README.md) for details.

## Related

* [Profiles](profiles.md) - The profiles that can be set as defaults
* [Managing Settings](../backoffice/managing-settings.md) - Backoffice guide
