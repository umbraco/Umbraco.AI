# Umbraco.AI.MicrosoftFoundry

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.MicrosoftFoundry.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.MicrosoftFoundry/)

Microsoft AI Foundry provider plugin for Umbraco.AI, enabling integration with models hosted through Microsoft AI Foundry (Azure AI).

## Features

- **Chat Completion**: Support for GPT-4o, Mistral, Llama, Cohere, Phi, and other chat models
- **Embeddings**: Support for text-embedding-3-small, text-embedding-3-large, and other embedding models
- **Multi-Model Access**: One endpoint provides access to all deployed models
- **API Key Authentication**: Simple authentication using Microsoft AI Foundry API keys
- **Entra ID Authentication**: Azure AD authentication via service principal or managed identity
- **Deployed Model Listing**: When using Entra ID, the model dropdown shows only your deployed models (not the full catalog)

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Prerequisites

1. An Azure subscription
2. A Microsoft AI Foundry resource (Azure AI hub) with deployed models
3. The endpoint URL and either an API key or Entra ID credentials

> **Important:** Models must be deployed in Microsoft AI Foundry before they can be used. When using API key authentication, the model dropdown shows all available models in the catalog. When using Entra ID authentication, only deployed models are shown.

## Azure Account Setup

If you don't already have an Azure account and AI Foundry resource set up, follow these steps:

### Step 1: Create an Azure Account

1. Go to [azure.microsoft.com](https://azure.microsoft.com)
2. Click **Start free** or **Sign in** if you already have an account
3. New accounts get $200 free credit for 30 days, plus 12 months of free services

### Step 2: Create a Microsoft AI Foundry Hub

1. Go to [ai.azure.com](https://ai.azure.com) (Microsoft AI Foundry portal)
2. Sign in with your Azure account
3. Click **+ New project**
4. Enter a project name and select or create a **hub**:
    - If creating a new hub, select your subscription and resource group
    - Choose a region (note: model availability varies by region)
5. Click **Create** and wait for provisioning to complete

### Step 3: Deploy Models

Before you can use models, you must deploy them to your project:

1. In your AI Foundry project, go to **Model catalog** (left sidebar)
2. Browse or search for models you want to use:
    - For chat: `gpt-4o`, `gpt-4o-mini`, `mistral-large`, `llama-3-70b`
    - For embeddings: `text-embedding-3-small`, `text-embedding-3-large`
3. Click on a model, then click **Deploy**
4. Choose a deployment name (or use the default)
5. Select **Serverless API** deployment type for pay-as-you-go pricing
6. Click **Deploy** and wait for the deployment to complete
7. Repeat for each model you want to use

> **Tip:** Start with `gpt-4o-mini` for chat and `text-embedding-3-small` for embeddings - they offer good performance at lower cost.

### Step 4: Get Your Endpoint and API Key

1. In your AI Foundry project, go to **Project settings** (bottom of left sidebar)
2. Under **Connected resources**, find your Azure AI Services connection
3. Click on the connection to view details
4. Copy the **Endpoint URL** (e.g., `https://your-resource.services.ai.azure.com/`)
5. Copy one of the **API keys**

Alternatively, from the Azure Portal:

1. Go to [portal.azure.com](https://portal.azure.com)
2. Navigate to your Azure AI Services resource
3. Go to **Keys and Endpoint** in the left menu
4. Copy the endpoint and one of the keys

### Region Availability

Not all models are available in all Azure regions. Popular regions with good model availability include:

- **East US** / **East US 2**
- **West US** / **West US 3**
- **Sweden Central** (EU data residency)
- **UK South**

Check [Microsoft AI Foundry model availability](https://learn.microsoft.com/en-us/azure/ai-studio/how-to/model-catalog-overview) for the latest regional availability.

## Installation

```bash
dotnet add package Umbraco.AI.MicrosoftFoundry
```

## Configuration

### 1. Create a Microsoft AI Foundry Resource

1. Go to the [Azure Portal](https://portal.azure.com)
2. Create a new Azure AI Foundry resource (or Azure AI hub)
3. Once created, note the **Endpoint** (e.g., `https://your-resource.services.ai.azure.com/`)
4. Go to **Keys and Endpoint** and copy one of the API keys

### 2. Deploy Models

In your Microsoft AI Foundry resource:

1. Go to the Model catalog
2. Deploy the models you need (e.g., `gpt-4o`, `text-embedding-3-small`, `mistral-large`)
3. Note the model names for use in Umbraco profiles

### 3. Configure in Umbraco

In the Umbraco backoffice:

1. Navigate to **Settings** > **AI** > **Connections**
2. Create a new connection and select **Microsoft AI Foundry**
3. Enter your endpoint and choose an authentication method:

**Option A: API Key (simplest)**
- Enter your **API Key** in the API Key Authentication section

**Option B: Entra ID / Service Principal (recommended for production)**
- Enter your **Tenant ID**, **Client ID**, and **Client Secret** in the Entra ID Authentication section
- This enables the model dropdown to show only your deployed models

**Option C: Managed Identity / DefaultAzureCredential**
- Enter only your **Tenant ID** (leave Client ID and Client Secret empty)
- Uses DefaultAzureCredential, which works with managed identity, Azure CLI, Visual Studio, etc.

### 4. Create AI Profiles

1. Navigate to **Settings** > **AI** > **Profiles**
2. Create a new profile using your Microsoft AI Foundry connection
3. Select a model from the dropdown (Entra ID) or type the model name (API key)

## Entra ID Authentication Setup

Entra ID authentication allows the provider to list only your deployed models and provides more secure, token-based access.

### Create a Service Principal

1. Go to the [Azure Portal](https://portal.azure.com) > **Microsoft Entra ID** > **App registrations**
2. Click **New registration**, enter a name, and click **Register**
3. Note the **Application (client) ID** and **Directory (tenant) ID**
4. Go to **Certificates & secrets** > **New client secret**
5. Copy the secret value

### Assign RBAC Roles

The service principal needs the following role on your Azure AI Services resource:

1. Go to your Azure AI Services resource in the Azure Portal
2. Go to **Access control (IAM)** > **Add role assignment**
3. Assign **Cognitive Services OpenAI Contributor** to your service principal

This role grants access to both the deployments API (for listing models) and the inference API (for chat/embeddings).

## Settings

| Setting      | Group    | Description                                    | Required                         |
| ------------ | -------- | ---------------------------------------------- | -------------------------------- |
| Endpoint     | General  | Microsoft AI Foundry endpoint URL              | Yes                              |
| ApiKey       | API Key  | API key for authentication                     | Required if no Entra ID          |
| TenantId     | Entra ID | Azure AD tenant ID                             | Required for Entra ID            |
| ClientId     | Entra ID | Application (client) ID of service principal   | Required for service principal   |
| ClientSecret | Entra ID | Client secret for service principal            | Required for service principal   |

## Supported Models

Microsoft AI Foundry provides access to multiple model providers through a single endpoint (subject to your Azure resource's model deployments):

### Chat Models

- **OpenAI**: GPT-4o, GPT-4, GPT-3.5-turbo
- **Mistral**: mistral-large, mistral-small
- **Meta Llama**: llama-3-70b, llama-3-8b
- **Cohere**: command-r, command-r-plus
- **Microsoft Phi**: phi-3-medium, phi-3-small

### Embedding Models

- **OpenAI**: text-embedding-3-large, text-embedding-3-small, text-embedding-ada-002
- **Cohere**: embed-v3

## Configuration from appsettings.json

You can store credentials in `appsettings.json` and reference them using the `$` prefix:

### API Key Authentication

```json
{
    "MicrosoftFoundry": {
        "Endpoint": "https://your-resource.services.ai.azure.com/",
        "ApiKey": "your-api-key-here"
    }
}
```

Then in your connection settings, use:

- Endpoint: `$MicrosoftFoundry:Endpoint`
- API Key: `$MicrosoftFoundry:ApiKey`

### Entra ID Authentication

```json
{
    "MicrosoftFoundry": {
        "Endpoint": "https://your-resource.services.ai.azure.com/",
        "TenantId": "your-tenant-id",
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret"
    }
}
```

Then in your connection settings, use:

- Endpoint: `$MicrosoftFoundry:Endpoint`
- Tenant ID: `$MicrosoftFoundry:TenantId`
- Client ID: `$MicrosoftFoundry:ClientId`
- Client Secret: `$MicrosoftFoundry:ClientSecret`

## Troubleshooting

### Common Issues

**"Resource not found" error**

- Ensure your endpoint URL is correct
- Verify the model name matches exactly (case-sensitive)
- Check that the model is deployed in your Microsoft AI Foundry resource

**"Access denied" error**

- Check your API key is correct
- For Entra ID: verify the service principal has the **Cognitive Services OpenAI Contributor** role
- Verify your Microsoft AI Foundry resource allows access from your IP/network

**"Unavailable model" or "Model not available" error**

- **This is the most common issue** - the model is not deployed in your Microsoft AI Foundry resource
- Go to [ai.azure.com](https://ai.azure.com), open your project, and deploy the model you want to use
- Once deployed, the model will work through your connection

**Model dropdown is empty (Entra ID)**

- Ensure the service principal has the **Cognitive Services OpenAI Contributor** role
- Check that models are deployed and their provisioning state is "Succeeded"
- Check the Umbraco logs for warnings from the deployments API

**Model dropdown shows all models instead of deployed ones**

- This happens with API key authentication, which cannot access the deployments API
- Switch to Entra ID authentication to see only deployed models

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
