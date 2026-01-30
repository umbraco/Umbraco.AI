---
description: >-
  Configure Microsoft AI Foundry as an AI provider for chat and embedding capabilities.
---

# Microsoft AI Foundry

Microsoft AI Foundry (formerly Azure AI Studio) provides a unified endpoint for accessing multiple AI models within the Azure ecosystem, with enterprise compliance and security features.

## Installation

{% code title="Package Manager Console" %}
```powershell
Install-Package Umbraco.Ai.MicrosoftFoundry
```
{% endcode %}

Or via .NET CLI:

{% code title="Terminal" %}
```bash
dotnet add package Umbraco.Ai.MicrosoftFoundry
```
{% endcode %}

## Capabilities

| Capability | Supported | Description |
|------------|-----------|-------------|
| Chat | Yes | GPT, Phi, Llama, Mistral models |
| Embedding | Yes | Azure OpenAI embeddings |

## Connection Settings

| Setting | Required | Description |
|---------|----------|-------------|
| Endpoint | Yes | Your AI Foundry endpoint URL |
| API Key | Yes | Your AI Foundry API key |

### Getting Your Credentials

1. Sign in to [Azure AI Studio](https://ai.azure.com)
2. Open your AI project
3. Navigate to **Deployments**
4. Select your deployment
5. Copy the **Target URI** (endpoint) and **Key**

{% hint style="warning" %}
Keep your API key secure. Never commit it to source control or expose it in client-side code.
{% endhint %}

## Available Models

Microsoft AI Foundry provides access to models from multiple providers through a single endpoint:

### Chat Models

| Model Family | Example Models | Notes |
|--------------|---------------|-------|
| Azure OpenAI | GPT-4o, GPT-4, GPT-3.5-turbo | Microsoft-hosted OpenAI |
| Microsoft Phi | Phi-3-medium, Phi-3-mini | Microsoft's small language models |
| Meta Llama | Llama-3.1-405B, Llama-3.1-70B | Open source, hosted on Azure |
| Mistral | Mistral Large, Mistral Small | Open source, Azure-hosted |

### Embedding Models

| Model | Dimensions | Notes |
|-------|-----------|-------|
| text-embedding-ada-002 | 1536 | Azure OpenAI embeddings |
| text-embedding-3-small | 1536 | Latest Azure OpenAI |
| text-embedding-3-large | 3072 | Highest quality |

{% hint style="info" %}
Available models depend on your Azure subscription, region, and deployed models in AI Foundry.
{% endhint %}

## Creating a Connection

### Via Backoffice

1. Navigate to **Settings** > **AI** > **Connections**
2. Click **Create Connection**
3. Select **Microsoft AI Foundry** as the provider
4. Enter your endpoint URL and API key
5. Save the connection

### Via Code

{% code title="Example.cs" %}
```csharp
var connection = new AiConnection
{
    Alias = "ai-foundry-production",
    Name = "AI Foundry Production",
    ProviderId = "microsoft-foundry",
    Settings = new MicrosoftFoundryProviderSettings
    {
        Endpoint = "https://your-project.region.inference.ml.azure.com",
        ApiKey = "..."
    }
};

await _connectionService.SaveConnectionAsync(connection);
```
{% endcode %}

## Creating Profiles

### Chat Profile

{% code title="Example.cs" %}
```csharp
var profile = new AiProfile
{
    Alias = "foundry-assistant",
    Name = "AI Foundry Assistant",
    Capability = AiCapability.Chat,
    ConnectionId = connectionId,
    Model = new AiModelRef("microsoft-foundry", "gpt-4o"),
    Settings = new AiChatProfileSettings
    {
        Temperature = 0.7f,
        MaxTokens = 4096,
        SystemPromptTemplate = "You are a helpful assistant."
    }
};

await _profileService.SaveProfileAsync(profile);
```
{% endcode %}

### Embedding Profile

{% code title="Example.cs" %}
```csharp
var profile = new AiProfile
{
    Alias = "foundry-embeddings",
    Name = "AI Foundry Embeddings",
    Capability = AiCapability.Embedding,
    ConnectionId = connectionId,
    Model = new AiModelRef("microsoft-foundry", "text-embedding-ada-002")
};

await _profileService.SaveProfileAsync(profile);
```
{% endcode %}

## Enterprise Features

### Data Residency

Microsoft AI Foundry provides regional deployments for data residency requirements:

- Deploy models in specific Azure regions
- Data stays within your Azure tenant
- Compliant with regional data protection regulations

### Network Security

{% code title="Example.cs" %}
```csharp
// Use private endpoints for enhanced security
var connection = new AiConnection
{
    // ...
    Settings = new MicrosoftFoundryProviderSettings
    {
        Endpoint = "https://your-private-endpoint.azure.com",
        ApiKey = "..."
    }
};
```
{% endcode %}

### Azure Integration

AI Foundry integrates with Azure services:

- **Azure Active Directory** - Managed identity authentication
- **Azure Key Vault** - Secure key storage
- **Azure Monitor** - Logging and diagnostics
- **Azure Policy** - Governance controls

## Setting Up in Azure

### 1. Create an AI Hub

1. Go to [Azure AI Studio](https://ai.azure.com)
2. Click **+ New hub**
3. Configure region and networking
4. Create the hub

### 2. Create a Project

1. Within your hub, click **+ New project**
2. Name your project
3. Configure settings

### 3. Deploy a Model

1. Go to **Model catalog**
2. Select a model
3. Click **Deploy**
4. Configure deployment settings
5. Copy the endpoint and key

## Pricing Considerations

Pricing depends on the model deployed:

| Model Type | Pricing Model |
|------------|---------------|
| Azure OpenAI | Pay-per-token |
| Phi models | Pay-per-token (lower cost) |
| Llama/Mistral | Pay-per-token or throughput |

{% hint style="info" %}
Check [Azure AI pricing](https://azure.microsoft.com/pricing/details/machine-learning/) for current rates.
{% endhint %}

## Troubleshooting

### Authentication Failed

```
Error: 401 Unauthorized
```

Verify:
- API key is correct
- Key hasn't expired or been rotated
- Endpoint URL matches your deployment

### Model Not Found

```
Error: Model deployment not found
```

Ensure:
- The model name matches your deployment name
- The deployment is active
- You're connecting to the correct project

### Network Errors

```
Error: Connection refused
```

Check:
- Endpoint URL is correct
- Network allows outbound connections
- Private endpoint configuration (if applicable)

## Related

* [Providers Overview](README.md) - Compare all providers
* [Connections](../concepts/connections.md) - Managing credentials
* [Profiles](../concepts/profiles.md) - Configuring models
