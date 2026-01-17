---
description: >-
  Step-by-step guide to creating a custom AI provider.
---

# Creating a Provider

This guide walks through creating a complete custom AI provider from scratch.

## Prerequisites

* A .NET class library project
* Reference to `Umbraco.Ai.Core`
* Understanding of the AI service's API

## Step 1: Create the Project

Create a new class library targeting .NET 10:

{% code title="Terminal" %}
```bash
dotnet new classlib -n MyCompany.Umbraco.Ai.MyProvider -f net10.0
cd MyCompany.Umbraco.Ai.MyProvider
dotnet add package Umbraco.Ai.Core
```
{% endcode %}

## Step 2: Create the Settings Class

Define the configuration your provider needs:

{% code title="MyProviderSettings.cs" %}
```csharp
using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.Settings;

namespace MyCompany.Umbraco.Ai.MyProvider;

public class MyProviderSettings
{
    [AiSetting(
        Label = "API Key",
        Description = "Your MyProvider API key. Use $Config:Key for config reference.",
        SortOrder = 1)]
    [Required]
    public required string ApiKey { get; set; }

    [AiSetting(
        Label = "Base URL",
        Description = "API endpoint (leave empty for default)",
        SortOrder = 2)]
    public string? BaseUrl { get; set; }

    [AiSetting(
        Label = "Organization ID",
        Description = "Optional organization identifier",
        SortOrder = 3)]
    public string? OrganizationId { get; set; }
}
```
{% endcode %}

## Step 3: Create the Chat Capability

Implement the chat capability by extending `AiChatCapabilityBase<TSettings>`:

{% code title="MyProviderChatCapability.cs" %}
```csharp
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

namespace MyCompany.Umbraco.Ai.MyProvider;

public class MyProviderChatCapability : AiChatCapabilityBase<MyProviderSettings>
{
    public MyProviderChatCapability(IAiProvider provider) : base(provider) { }

    protected override IChatClient CreateClient(MyProviderSettings settings, string? modelId)
    {
        // Option 1: Use an existing M.E.AI client
        // return new OpenAIChatClient(settings.ApiKey, modelId ?? "default-model");

        // Option 2: Create your own IChatClient implementation
        return new MyProviderChatClient(
            apiKey: settings.ApiKey,
            baseUrl: settings.BaseUrl,
            modelId: modelId ?? "default-model",
            organizationId: settings.OrganizationId);
    }

    protected override async Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        MyProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Fetch models from your API, or return a static list
        var models = new List<AiModelDescriptor>
        {
            new("model-standard", "Standard Model"),
            new("model-advanced", "Advanced Model"),
            new("model-fast", "Fast Model")
        };

        return models;
    }
}
```
{% endcode %}

## Step 4: Create the Provider Class

The provider class ties everything together:

{% code title="MyProvider.cs" %}
```csharp
using Umbraco.Ai.Core.Providers;

namespace MyCompany.Umbraco.Ai.MyProvider;

[AiProvider("myprovider", "My AI Provider")]
public class MyProvider : AiProviderBase<MyProviderSettings>
{
    public MyProvider(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        // Register capabilities
        WithCapability<MyProviderChatCapability>();

        // Add more capabilities if supported
        // WithCapability<MyProviderEmbeddingCapability>();
    }
}
```
{% endcode %}

## Step 5: Implement IChatClient (If Needed)

If your AI service doesn't have an existing M.E.AI client, implement `IChatClient`:

{% code title="MyProviderChatClient.cs" %}
```csharp
using Microsoft.Extensions.AI;

namespace MyCompany.Umbraco.Ai.MyProvider;

public class MyProviderChatClient : IChatClient
{
    private readonly string _apiKey;
    private readonly string _modelId;
    private readonly HttpClient _httpClient;

    public MyProviderChatClient(
        string apiKey,
        string? baseUrl,
        string modelId,
        string? organizationId)
    {
        _apiKey = apiKey;
        _modelId = modelId;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl ?? "https://api.myprovider.com")
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        if (!string.IsNullOrEmpty(organizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Organization-Id", organizationId);
        }
    }

    public ChatClientMetadata Metadata => new("MyProvider", new Uri("https://myprovider.com"), _modelId);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Implement API call to your provider
        // Convert messages to your API format
        // Send request and parse response
        // Return ChatResponse

        var requestBody = new
        {
            model = _modelId,
            messages = messages.Select(m => new
            {
                role = m.Role.Value,
                content = m.Text
            })
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MyProviderResponse>(
            cancellationToken: cancellationToken);

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, result!.Content))
        {
            FinishReason = ChatFinishReason.Stop,
            Usage = new UsageDetails
            {
                InputTokenCount = result.Usage?.InputTokens,
                OutputTokenCount = result.Usage?.OutputTokens
            }
        };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Implement streaming if supported
        throw new NotSupportedException("Streaming not implemented");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() => _httpClient.Dispose();
}

// Response model for your API
internal class MyProviderResponse
{
    public string? Content { get; set; }
    public UsageInfo? Usage { get; set; }
}

internal class UsageInfo
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
```
{% endcode %}

## Step 6: Package and Install

Build your provider as a NuGet package:

{% code title="MyCompany.Umbraco.Ai.MyProvider.csproj" %}
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>MyCompany.Umbraco.Ai.MyProvider</PackageId>
    <Version>1.0.0</Version>
    <Description>MyProvider integration for Umbraco.Ai</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Umbraco.Ai.Core" Version="*" />
  </ItemGroup>
</Project>
```
{% endcode %}

Install in your Umbraco project:

```bash
dotnet add package MyCompany.Umbraco.Ai.MyProvider
```

The provider will be discovered automatically on startup.

## Verification

After installation:

1. Navigate to **Settings** > **AI** > **Connections**
2. Click **Create Connection**
3. Your provider should appear in the provider dropdown
4. Configure settings and save
5. Create a profile using the connection
6. Test with a chat request

## Next Steps

* [Provider Settings](provider-settings.md) - Learn about `[AiSetting]` options
* [Chat Capability](chat-capability.md) - Detailed chat implementation guide
* [Embedding Capability](embedding-capability.md) - Add embedding support
