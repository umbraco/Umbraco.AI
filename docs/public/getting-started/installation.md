---
description: >-
  Install Umbraco.Ai and a provider package to add AI capabilities to your Umbraco site.
---

# Installation

Umbraco.Ai is distributed as NuGet packages. You need to install the core package and at least one provider package.

## Install the Core Package

Add the Umbraco.Ai package to your Umbraco project:

{% code title="Package Manager Console" %}
```powershell
Install-Package Umbraco.Ai
```
{% endcode %}

Or using the .NET CLI:

{% code title=".NET CLI" %}
```bash
dotnet add package Umbraco.Ai
```
{% endcode %}

## Install a Provider Package

Umbraco.Ai requires at least one provider to connect to AI services. Install the provider for your preferred AI service.

### OpenAI

{% code title=".NET CLI" %}
```bash
dotnet add package Umbraco.Ai.OpenAi
```
{% endcode %}

{% hint style="info" %}
Additional providers will be available in future releases. You can also create custom providers for other AI services.
{% endhint %}

## Package Contents

The packages install the following components:

| Package | Contents |
|---------|----------|
| `Umbraco.Ai` | Core services, backoffice UI, Management API, database migrations |
| `Umbraco.Ai.OpenAi` | OpenAI provider with chat and embedding capabilities |

## Verify Installation

After installation, build your project:

{% code title=".NET CLI" %}
```bash
dotnet build
```
{% endcode %}

When you run your Umbraco site, the AI section will appear in the backoffice Settings area.

## Next Steps

{% content-ref url="configuration.md" %}
[Configuration](configuration.md)
{% endcontent-ref %}
