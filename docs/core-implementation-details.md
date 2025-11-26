# Umbraco.Ai Core Implementation Details

This document provides comprehensive technical documentation of the Umbraco.Ai architecture and implementation. It serves both internal developers working on Umbraco.Ai itself and integration developers building AI features in Umbraco sites.

---

## Table of Contents

1. [Introduction & Architecture Overview](#1-introduction--architecture-overview)
2. [Project Organization](#2-project-organization)
3. [Core Concepts](#3-core-concepts)
4. [Provider System](#4-provider-system)
5. [Capability System](#5-capability-system)
6. [Connection Management](#6-connection-management)
7. [Profile System](#7-profile-system)
8. [Service Layer](#8-service-layer)
9. [Factory & Middleware](#9-factory--middleware)
10. [Registry](#10-registry)
11. [Configuration & Settings](#11-configuration--settings)
12. [Dependency Injection](#12-dependency-injection)
13. [Management API](#13-management-api)
14. [Request Flow](#14-request-flow)
15. [Future: Agents & Tools (Planned)](#15-future-agents--tools-planned)

---

## 1. Introduction & Architecture Overview

### Purpose

Umbraco.Ai is an AI integration layer for Umbraco CMS that provides a unified, provider-agnostic interface for AI capabilities. It enables developers to integrate AI features (chat completions, embeddings, and more) into their Umbraco sites without being locked into a specific AI provider.

### Design Philosophy

Umbraco.Ai follows a **thin wrapper** philosophy over Microsoft.Extensions.AI (MEAI). Rather than replacing or abstracting away MEAI, it adds Umbraco-specific features while exposing the underlying MEAI types directly:

- **Provider-agnostic**: Support multiple AI providers (OpenAI, Azure, Anthropic, etc.) through a unified interface
- **Capability-based**: Providers expose discrete capabilities (Chat, Embedding, Media, Moderation)
- **Configuration-driven**: Profiles and connections enable flexible, reusable configurations
- **Middleware pipeline**: Extensible cross-cutting concerns (logging, caching, rate limiting)
- **Native Umbraco patterns**: Uses Composers, DI, and familiar configuration patterns

### Solution Structure

```
Umbraco.Ai/
├── src/
│   ├── Umbraco.Ai.Core/           # Core abstractions and services
│   ├── Umbraco.Ai.OpenAi/         # OpenAI provider implementation
│   ├── Umbraco.Ai.Web/            # Management API layer
│   ├── Umbraco.Ai.Startup/        # Composition and DI setup
│   ├── Umbraco.Ai/                # Meta-package for distribution
│   └── Umbraco.Ai.Web.StaticAssets/  # Backoffice UI components
├── demo/
│   └── Umbraco.Ai.DemoSite/       # Demo Umbraco site
└── docs/                          # Documentation
```

### Project Dependencies

```
Umbraco.Ai (meta-package)
    └── Umbraco.Ai.Startup
            ├── Umbraco.Ai.Core
            │       └── Microsoft.Extensions.AI
            └── Umbraco.Ai.Web
                    └── Umbraco.Cms.Api.Management

Umbraco.Ai.OpenAi
    ├── Umbraco.Ai.Core
    └── Microsoft.Extensions.AI.OpenAI
```

---

## 2. Project Organization

### Umbraco.Ai.Core

The core library containing all abstractions, services, and models:

| Namespace | Purpose |
|-----------|---------|
| `Umbraco.Ai.Core.Providers` | Provider and capability interfaces/base classes |
| `Umbraco.Ai.Core.Services` | High-level services (IAiChatService) |
| `Umbraco.Ai.Core.Factories` | Client and generator factories |
| `Umbraco.Ai.Core.Models` | Data models (AiConnection, AiProfile, etc.) |
| `Umbraco.Ai.Core.Connections` | Connection management |
| `Umbraco.Ai.Core.Profiles` | Profile management |
| `Umbraco.Ai.Core.Middleware` | Middleware pipeline system |
| `Umbraco.Ai.Core.Registry` | Provider registry |
| `Umbraco.Ai.Core.Settings` | Settings resolution and validation |
| `Umbraco.Ai.Extensions` | DI registration extensions |

### Umbraco.Ai.OpenAi

Reference implementation of an AI provider for OpenAI:

- `OpenAiProvider` - Provider class with `[AiProvider]` attribute
- `OpenAiProviderSettings` - Typed settings with `[AiSetting]` attributes
- `OpenAiChatCapability` - Chat completion capability
- `OpenAiEmbeddingCapability` - Text embedding capability

### Umbraco.Ai.Web

Management API layer for backoffice integration:

- API controllers for connections, profiles, and providers
- Swagger/OpenAPI configuration
- Backoffice security integration

### Umbraco.Ai.Startup

Umbraco integration via Composer pattern:

- `UmbracoAiComposer` - Implements `IComposer` for auto-discovery
- `UmbracoBuilderExtensions` - Extension methods for `IUmbracoBuilder`

### Umbraco.Ai

Meta-package that bundles all components for NuGet distribution.

### Umbraco.Ai.Web.StaticAssets

Frontend assets for backoffice UI:

- TypeScript/JavaScript components in `Client/`
- Compiled assets served from `App_Plugins/UmbracoAi`

---

## 3. Core Concepts

### Hierarchical Model

Umbraco.Ai uses a hierarchical configuration model:

```
Provider (plugin with capabilities)
    └── Connection (authentication/credentials)
            └── Profile (use-case configuration)
                    └── AI Request (the actual call)
```

**Provider**: A plugin package that implements one or more AI capabilities (e.g., OpenAI provider supports Chat and Embedding)

**Connection**: Stores credentials and provider-specific settings for authenticating with an AI service

**Profile**: A reusable configuration that combines a connection with model settings (temperature, max tokens, system prompt)

**AI Request**: The actual call made using a profile's configuration

### Why This Design?

- **Separation of concerns**: Credentials (connections) are separate from usage settings (profiles)
- **Reusability**: One connection can be used by multiple profiles for different use cases
- **Flexibility**: Profiles can be swapped without changing application code
- **Security**: Credentials are managed centrally, not scattered through code

---

## 4. Provider System

Providers are plugins that expose AI capabilities. They are automatically discovered via assembly scanning.

### IAiProvider Interface

```csharp
public interface IAiProvider : IAiComponent
{
    // The type representing provider-specific settings
    Type? SettingsType { get; }

    // Get setting definitions for UI rendering
    IReadOnlyList<AiSettingDefinition> GetSettingDefinitions();

    // Get all capabilities this provider supports
    IReadOnlyCollection<IAiCapability> GetCapabilities();

    // Get a specific capability by type
    TCapability? GetCapability<TCapability>() where TCapability : class, IAiCapability;

    // Check if provider has a specific capability
    bool HasCapability<TCapability>() where TCapability : class, IAiCapability;

    // Try to get a capability (returns false if not found)
    bool TryGeCapability<TCapability>(out TCapability? capability)
        where TCapability : class, IAiCapability;
}
```

### AiProviderBase<TSettings>

Generic base class for implementing providers with typed settings:

```csharp
[AiProvider("openai", "OpenAI")]
public class OpenAiProvider : AiProviderBase<OpenAiProviderSettings>
{
    public OpenAiProvider(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        // Register capabilities
        WithCapability<OpenAiChatCapability>();
        WithCapability<OpenAiEmbeddingCapability>();
    }
}
```

### AiProviderAttribute

Marks classes for automatic discovery:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class AiProviderAttribute : Attribute
{
    public string Id { get; }      // Unique provider identifier (e.g., "openai")
    public string Name { get; }    // Display name (e.g., "OpenAI")
}
```

### Provider Discovery

During startup, Umbraco.Ai scans all loaded assemblies for types that:
1. Have the `[AiProvider]` attribute
2. Implement `IAiProvider`
3. Are not abstract

These types are registered as singletons with the DI container.

---

## 5. Capability System

Capabilities define what a provider can do. Each capability type has a specific interface.

### IAiCapability Interface

```csharp
public interface IAiCapability
{
    // The kind of capability (Chat, Embedding, Media, Moderation)
    AiCapability Kind { get; }

    // Get available models for this capability
    Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        object? settings = null,
        CancellationToken cancellationToken = default);
}
```

### Capability Types

```csharp
public enum AiCapability
{
    Chat = 0,       // Chat completions (conversational AI)
    Embedding = 1,  // Text embeddings (vector representations)
    Media = 2,      // Image/audio generation (planned)
    Moderation = 3  // Content moderation (planned)
}
```

### IAiChatCapability

For chat completion capabilities:

```csharp
public interface IAiChatCapability : IAiCapability
{
    // Creates an IChatClient configured with the given settings
    IChatClient CreateClient(object? settings = null);
}
```

### IAiEmbeddingCapability

For text embedding capabilities:

```csharp
public interface IAiEmbeddingCapability : IAiCapability
{
    // Creates an embedding generator configured with the given settings
    IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(object? settings);
}
```

### Base Classes

Abstract base classes simplify capability implementation:

| Base Class | Purpose |
|------------|---------|
| `AiCapabilityBase` | Base for capabilities without typed settings |
| `AiCapabilityBase<TSettings>` | Base for capabilities with typed settings |
| `AiChatCapabilityBase` | Chat capability without typed settings |
| `AiChatCapabilityBase<TSettings>` | Chat capability with typed settings |
| `AiEmbeddingCapabilityBase` | Embedding capability without typed settings |
| `AiEmbeddingCapabilityBase<TSettings>` | Embedding capability with typed settings |

### Example: OpenAI Chat Capability

```csharp
public class OpenAiChatCapability(OpenAiProvider provider)
    : AiChatCapabilityBase<OpenAiProviderSettings>(provider)
{
    protected override Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        OpenAiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var models = new List<AiModelDescriptor>
        {
            new(new AiModelRef(Provider.Id, "gpt-4o"), "GPT-4o"),
            new(new AiModelRef(Provider.Id, "gpt-4o-mini"), "GPT-4o Mini"),
            // ... more models
        };
        return Task.FromResult<IReadOnlyList<AiModelDescriptor>>(models);
    }

    protected override IChatClient CreateClient(OpenAiProviderSettings settings)
    {
        return new OpenAI.OpenAIClient(settings.ApiKey)
            .GetChatClient("gpt-4o")
            .AsIChatClient();
    }
}
```

---

## 6. Connection Management

Connections store credentials and provider-specific settings for authenticating with AI services.

### AiConnection Model

```csharp
public class AiConnection
{
    // Unique identifier
    public required Guid Id { get; init; }

    // Display name (shown in UI)
    public required string Name { get; set; }

    // Provider ID this connection is for (e.g., "openai")
    public required string ProviderId { get; set; }

    // Provider-specific settings (credentials, endpoints)
    public object? Settings { get; set; }

    // Whether this connection is currently active
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;
    public DateTime DateModified { get; set; } = DateTime.UtcNow;
}
```

### IAiConnectionService

```csharp
public interface IAiConnectionService
{
    // CRUD operations
    Task<AiConnection?> GetConnectionAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AiConnection>> GetConnectionsAsync(string? providerId = null, CancellationToken ct = default);
    Task<AiConnection> SaveConnectionAsync(AiConnection connection, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);

    // Validation
    Task<ValidationResult> ValidateConnectionAsync(string providerId, object settings, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(Guid connectionId, CancellationToken ct = default);
}
```

### Current Storage

Connections are currently stored in-memory via `InMemoryAiConnectionRepository`. This is a placeholder for future persistent storage implementations.

---

## 7. Profile System

Profiles are reusable configurations that combine a connection with model settings.

### AiProfile Model

```csharp
public sealed class AiProfile
{
    // Unique identifier
    public required Guid Id { get; init; }

    // Unique alias for referencing (e.g., "default-chat")
    public required string Alias { get; init; }

    // Display name
    public required string Name { get; init; }

    // Capability type (Chat, Embedding, etc.)
    public AiCapability Capability { get; init; } = AiCapability.Chat;

    // Model reference (provider ID + model ID)
    public AiModelRef Model { get; init; }

    // Connection to use for authentication
    public required Guid ConnectionId { get; init; }

    // Model parameters (optional, used as defaults)
    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public string? SystemPromptTemplate { get; init; }

    // Categorization tags
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
```

### AiModelRef

Reference to a specific model:

```csharp
public record AiModelRef(string ProviderId, string ModelId);
```

### IAiProfileService

```csharp
public interface IAiProfileService
{
    // Get by ID
    Task<AiProfile?> GetProfileAsync(Guid id, CancellationToken ct = default);

    // Get by alias
    Task<AiProfile?> GetProfileByAliasAsync(string alias, CancellationToken ct = default);

    // Get all profiles, optionally filtered by capability
    Task<IEnumerable<AiProfile>> GetProfilesAsync(AiCapability? capability = null, CancellationToken ct = default);

    // Get default profile for a capability (from AiOptions)
    Task<AiProfile?> GetDefaultProfileAsync(AiCapability capability, CancellationToken ct = default);

    // CRUD
    Task<AiProfile> SaveProfileAsync(AiProfile profile, CancellationToken ct = default);
    Task<bool> DeleteProfileAsync(Guid id, CancellationToken ct = default);
}
```

---

## 8. Service Layer

High-level services provide the primary developer interface for using AI features.

### IAiChatService

The main service for chat completions:

```csharp
public interface IAiChatService
{
    // Get response using default profile
    Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    // Get response using specific profile
    Task<ChatResponse> GetResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    // Streaming response using default profile
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    // Streaming response using specific profile
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    // Get configured client for advanced scenarios
    Task<IChatClient> GetChatClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default);
}
```

### Option Merging

When using `IAiChatService`, options are merged with priority:

1. **Caller options** (passed to method) - highest priority
2. **Profile settings** (Temperature, MaxTokens, etc.) - defaults

This allows profiles to set defaults while callers can override specific settings.

### Usage Example

```csharp
public class MyService
{
    private readonly IAiChatService _chatService;

    public MyService(IAiChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<string> GetSummaryAsync(string content)
    {
        var messages = new[]
        {
            new ChatMessage(ChatRole.User, $"Summarize this: {content}")
        };

        var response = await _chatService.GetResponseAsync(messages);
        return response.Message.Text ?? string.Empty;
    }
}
```

---

## 9. Factory & Middleware

### IAiChatClientFactory

Creates configured `IChatClient` instances with middleware applied:

```csharp
public interface IAiChatClientFactory
{
    Task<IChatClient> CreateClientAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}
```

The factory:
1. Resolves the profile
2. Loads the connection and validates provider match
3. Resolves settings (including environment variables)
4. Gets the capability from the registry
5. Creates the raw client via the capability
6. Applies middleware in order

### IAiChatMiddleware

Middleware can wrap chat clients to add cross-cutting concerns:

```csharp
public interface IAiChatMiddleware
{
    // Wraps the client with middleware behavior
    IChatClient Apply(IChatClient client);
}
```

### Middleware Ordering

Middleware ordering is controlled via the `AiChatMiddlewareCollectionBuilder` using Umbraco's `OrderedCollectionBuilder` pattern. This provides explicit control with `Append()`, `InsertBefore<T>()`, and `InsertAfter<T>()` methods.

```
Caller
  ↓
LoggingMiddleware (added last via Append)
  ↓
CachingMiddleware (inserted before Logging)
  ↓
RateLimitMiddleware (added first via Append)
  ↓
Provider
```

### Registering Middleware

Register middleware in a Composer:

```csharp
public class MyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AiChatMiddleware()
            .Append<RateLimitMiddleware>()
            .Append<CachingMiddleware>()
            .Append<LoggingMiddleware>();

        // Or use InsertBefore/InsertAfter for precise ordering:
        builder.AiChatMiddleware()
            .InsertBefore<LoggingMiddleware, TracingMiddleware>();
    }
}
```

### Example: Logging Middleware

```csharp
public class LoggingChatMiddleware(ILoggerFactory loggerFactory) : IAiChatMiddleware
{
    public IChatClient Apply(IChatClient client)
    {
        return client.AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
    }
}
```

---

## 10. Registry

The registry provides central access to all registered providers.

### IAiRegistry

```csharp
public interface IAiRegistry
{
    // All registered providers
    IEnumerable<IAiProvider> Providers { get; }

    // Get providers that support a specific capability
    IEnumerable<IAiProvider> GetProvidersWithCapability<TCapability>()
        where TCapability : class, IAiCapability;

    // Get provider by ID (case-insensitive)
    IAiProvider? GetProvider(string alias);

    // Get a specific capability from a provider
    TCapability? GetCapability<TCapability>(string providerId)
        where TCapability : class, IAiCapability;
}
```

### Usage Example

```csharp
// Get all providers that support chat
var chatProviders = registry.GetProvidersWithCapability<IAiChatCapability>();

// Get the OpenAI provider
var openai = registry.GetProvider("openai");

// Get chat capability directly
var chatCapability = registry.GetCapability<IAiChatCapability>("openai");
```

---

## 11. Configuration & Settings

### AiOptions

Global configuration from `appsettings.json`:

```csharp
public class AiOptions
{
    public string? DefaultChatProfileAlias { get; set; }
    public string? DefaultEmbeddingProfileAlias { get; set; }
    // Future: DefaultImageProviderAlias, DefaultModerationProviderAlias
}
```

Configuration section: `Umbraco:Ai`

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

### AiSettingAttribute

Decorates provider settings properties for UI generation:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class AiSettingAttribute : Attribute
{
    public string? Label { get; set; }           // Display label
    public string? Description { get; set; }     // Help text
    public string? EditorUiAlias { get; set; }   // Umbraco property editor UI
    public object? DefaultValue { get; set; }    // Default value
    public int SortOrder { get; set; }           // Field order
}
```

### Provider Settings Example

```csharp
public class OpenAiProviderSettings
{
    [AiSetting(
        Label = "API Key",
        Description = "Your OpenAI API key from platform.openai.com",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 1
    )]
    [Required]
    public string? ApiKey { get; set; }

    [AiSetting(
        Label = "Organization ID",
        Description = "Optional: Your OpenAI organization ID",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 2
    )]
    public string? OrganizationId { get; set; }

    [AiSetting(
        Label = "API Endpoint",
        Description = "Custom API endpoint (leave empty for default)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        DefaultValue = "https://api.openai.com/v1",
        SortOrder = 3
    )]
    public string? Endpoint { get; set; }
}
```

### Environment Variable Resolution

The `IAiSettingsResolver` supports resolving values from configuration:

```json
// Connection settings in database
{
  "ApiKey": "$OpenAI:ApiKey"
}

// appsettings.json
{
  "OpenAI": {
    "ApiKey": "sk-actual-key-here"
  }
}
```

Values prefixed with `$` are resolved from `IConfiguration`, allowing secrets to be stored in environment variables or secure configuration providers.

---

## 12. Dependency Injection

### Registration Flow

1. **UmbracoAiComposer** (discovered by Umbraco)
   - Calls `builder.AddUmbracoAi()`

2. **AddUmbracoAi()** (in Umbraco.Ai.Startup)
   - Calls `AddUmbracoAiCore()` for core services
   - Calls `AddUmbracoAiWeb()` for management API

3. **AddUmbracoAiCore()** (in Umbraco.Ai.Core)
   - Binds `AiOptions` from configuration
   - Registers infrastructure services
   - Scans and registers providers
   - Registers repositories, services, and factories

### Registered Services

| Service | Implementation | Lifetime |
|---------|---------------|----------|
| `IAiRegistry` | `AiRegistry` | Singleton |
| `IAiCapabilityFactory` | `AiCapabilityFactory` | Singleton |
| `IAiSettingDefinitionBuilder` | `AiSettingDefinitionBuilder` | Singleton |
| `IAiProviderInfrastructure` | `AiProviderInfrastructure` | Singleton |
| `IAiSettingsResolver` | `AiSettingsResolver` | Singleton |
| `IAiConnectionRepository` | `InMemoryAiConnectionRepository` | Singleton |
| `IAiConnectionService` | `AiConnectionService` | Singleton |
| `IAiProfileRepository` | `InMemoryAiProfileRepository` | Singleton |
| `IAiProfileService` | `AiProfileService` | Singleton |
| `IAiChatClientFactory` | `AiChatClientFactory` | Singleton |
| `IAiEmbeddingGeneratorFactory` | `AiEmbeddingGeneratorFactory` | Singleton |
| `IAiChatService` | `AiChatService` | Singleton |

### Provider Registration

Providers are discovered via reflection:

```csharp
private static void RegisterProviders(IServiceCollection services)
{
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

    foreach (var assembly in assemblies)
    {
        // Skip system assemblies
        if (assembly.FullName?.StartsWith("System") == true ||
            assembly.FullName?.StartsWith("Microsoft") == true)
            continue;

        // Find types with [AiProvider] attribute
        var providerTypes = assembly.GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                type.GetCustomAttribute<AiProviderAttribute>() != null &&
                typeof(IAiProvider).IsAssignableFrom(type));

        // Register as singleton
        foreach (var providerType in providerTypes)
        {
            services.AddSingleton(typeof(IAiProvider), providerType);
        }
    }
}
```

---

## 13. Management API

### API Configuration

- **Root path**: `/umbraco/ai/management/api`
- **API name**: `ai-management`
- **Namespace prefix**: `Umbraco.Ai.Web.Api.Management`

### Security

The management API integrates with Umbraco's backoffice security:

- `UmbracoAiManagementApiBackOfficeSecurityRequirementsOperationFilter` extends `BackOfficeSecurityRequirementsOperationFilterBase`
- Requires backoffice authentication
- Respects Umbraco user permissions

### Swagger Integration

OpenAPI documentation is automatically generated:

- `UmbracoAiManagementApiSchemaIdHandler` - Generates schema IDs
- `UmbracoAiManagementApiOperationIdHandler` - Generates operation IDs

### Planned Controllers

| Controller | Purpose |
|------------|---------|
| Connection | CRUD for AI connections |
| Profile | CRUD for AI profiles |
| Provider | List available providers and capabilities |

---

## 14. Request Flow

### Chat Request Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Application calls IAiChatService.GetResponseAsync()          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Service resolves profile (by ID or default from AiOptions)   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Factory creates client:                                       │
│    a. Load connection by profile.ConnectionId                    │
│    b. Validate provider match (connection.ProviderId == model)   │
│    c. Resolve settings (including environment variables)         │
│    d. Get capability from registry                               │
│    e. Call capability.CreateClient(settings)                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Apply middleware pipeline (ordered by Order property)         │
│    [Logging] → [Caching] → [RateLimit] → [Provider]             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Merge options:                                                │
│    - Start with profile defaults (Temperature, MaxTokens)        │
│    - Override with caller-provided options                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Call chatClient.GetResponseAsync(messages, mergedOptions)     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Return ChatResponse to caller                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Provider Resolution

```
Profile.Model.ProviderId ("openai")
          │
          ▼
    Registry.GetProvider("openai")
          │
          ▼
    Provider.GetCapability<IAiChatCapability>()
          │
          ▼
    Connection.Settings (resolved)
          │
          ▼
    Capability.CreateClient(settings)
          │
          ▼
    IChatClient (configured)
```

---

## 15. Future: Agents & Tools (Planned)

> **Note**: This section describes planned functionality that is not yet implemented.

### Overview

Umbraco.Ai.Agents will extend Umbraco.Ai with autonomous AI capabilities that can perform actions within Umbraco CMS using **tools**.

### Agent Concept

An agent is a configured AI assistant that can:
- Understand natural language requests
- Access tools to gather information or perform actions
- Execute multi-step workflows autonomously
- Request user approval before making changes

### Agent Definition

```csharp
// Planned model
public class AiAgent
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Guid ProfileId { get; }           // References AiProfile
    public string SystemPrompt { get; }       // Agent-specific instructions
    public IEnumerable<string> EnabledTools { get; }  // Tool IDs
    public IEnumerable<string> AllowedUserGroups { get; }  // Permissions
}
```

### Tool System

Tools are discrete actions that agents can invoke:

| Category | Examples |
|----------|----------|
| **Content** | search, get, create, update, publish, delete |
| **Media** | search, upload, organize |
| **Navigation** | tree, children, ancestors, siblings |
| **Search** | fulltext, semantic, similar |
| **Generation** | text, summary, translate |

### Approval Workflow

- **Read-only tools** (search, get): Execute immediately
- **Modifying tools** (create, update, delete): Require user approval

### Planned Architecture

```
Umbraco.Ai.Agents (new project)
├── Models/
│   ├── AiAgent              # Agent definition
│   ├── AgentSession         # Conversation state
│   └── ToolInvocation       # Tool call record
├── Tools/
│   ├── IAiTool              # Tool interface
│   └── AiToolAttribute      # Discovery attribute
├── Services/
│   ├── IAiAgentService      # Agent CRUD
│   ├── IAiAgentExecutor     # Execution engine
│   └── IAgentSessionStore   # Session memory
```

### Backoffice Integration Points

1. **Global AI Assistant** - Sidebar accessible from header
2. **Content Entity Actions** - Right-click menu on content items
3. **Inline Property Actions** - AI buttons in property editors
4. **Agent Management Section** - Configure agents and permissions

---

## Summary

Umbraco.Ai provides a comprehensive, provider-agnostic AI integration layer for Umbraco CMS. Key architectural decisions include:

- **Microsoft.Extensions.AI foundation**: Uses MEAI types directly, not abstracting them away
- **Capability-based providers**: Providers expose discrete capabilities that can be queried
- **Hierarchical configuration**: Provider → Connection → Profile → Request
- **Middleware pipeline**: Extensible cross-cutting concerns
- **Automatic discovery**: Providers are found via assembly scanning and attributes
- **Environment variable support**: Credentials can be stored securely in configuration

This architecture enables flexible, maintainable AI integrations that can grow with Umbraco sites as AI capabilities evolve.
