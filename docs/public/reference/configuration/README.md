---
description: >-
  Configuration options for Umbraco.Ai.
---

# Configuration

Umbraco.Ai is configured through the standard `appsettings.json` file under the `Umbraco:Ai` section.

## Configuration Section

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

## Available Options

| Class | Description |
|-------|-------------|
| [AiOptions](ai-options.md) | Global AI service configuration |

## Provider Credentials

Store provider credentials in configuration and reference them in connections:

{% code title="appsettings.json" %}
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here"
  },
  "Azure": {
    "ApiKey": "your-azure-key",
    "Endpoint": "https://your-resource.openai.azure.com"
  }
}
```
{% endcode %}

Reference these in connection settings using the `$` prefix:

- `$OpenAI:ApiKey` - Resolves to the OpenAI API key
- `$Azure:Endpoint` - Resolves to the Azure endpoint

## Environment-Specific Configuration

Use environment-specific files for different settings:

```
appsettings.json              # Base configuration
appsettings.Development.json  # Development overrides
appsettings.Production.json   # Production overrides
```

{% hint style="warning" %}
Never commit API keys to source control. Use environment variables, user secrets, or Azure Key Vault for production.
{% endhint %}

## In This Section

{% content-ref url="ai-options.md" %}
[AiOptions](ai-options.md)
{% endcontent-ref %}
