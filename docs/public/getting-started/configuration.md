---
description: >-
  Configure Umbraco.Ai settings in appsettings.json to set default profiles.
---

# Configuration

Umbraco.Ai uses the standard Umbraco configuration pattern. Settings are stored in `appsettings.json` under the `Umbraco:Ai` section.

## Configuration Options

{% code title="appsettings.json" %}
```json
{
  "Umbraco": {
    "Ai": {
      "DefaultChatProfileAlias": "default-chat",
      "DefaultEmbeddingProfileAlias": "default-embedding"
    }
  }
}
```
{% endcode %}

| Setting | Description |
|---------|-------------|
| `DefaultChatProfileAlias` | The alias of the profile to use when calling chat methods without specifying a profile |
| `DefaultEmbeddingProfileAlias` | The alias of the profile to use when calling embedding methods without specifying a profile |

## How Default Profiles Work

When you call `IAiChatService.GetResponseAsync()` without specifying a profile ID, the service uses the profile identified by `DefaultChatProfileAlias`.

{% code title="Example.cs" %}
```csharp
// Uses the profile with alias "default-chat"
var response = await _chatService.GetResponseAsync(messages);

// Or explicitly specify a profile
var response = await _chatService.GetResponseAsync(profileId, messages);
```
{% endcode %}

{% hint style="warning" %}
If you call a method without specifying a profile and no default is configured, an exception will be thrown. Either configure defaults or always specify profile IDs.
{% endhint %}

## Provider-Specific Configuration

Some providers support reading API keys from configuration. Values prefixed with `$` in connection settings are resolved from configuration.

{% code title="appsettings.json" %}
```json
{
  "OpenAI": {
    "ApiKey": "sk-..."
  }
}
```
{% endcode %}

When creating a connection in the backoffice, you can enter `$OpenAI:ApiKey` as the API key value. This resolves to the actual key from configuration at runtime.

{% hint style="info" %}
Using configuration references keeps sensitive values out of the database and allows different values per environment.
{% endhint %}

## Environment-Specific Configuration

Use standard .NET configuration patterns for environment-specific settings:

* `appsettings.Development.json` - Development settings
* `appsettings.Production.json` - Production settings
* Environment variables
* User secrets (for local development)

## Next Steps

{% content-ref url="first-connection.md" %}
[Your First Connection](first-connection.md)
{% endcontent-ref %}
