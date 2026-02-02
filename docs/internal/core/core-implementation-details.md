# Umbraco.AI Core Implementation Details

This document provides comprehensive technical documentation of the Umbraco.AI architecture and implementation. It serves both internal developers working on Umbraco.AI itself and integration developers building AI features in Umbraco sites.

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

Umbraco.AI is an AI integration layer for Umbraco CMS that provides a unified, provider-agnostic interface for AI capabilities. It enables developers to integrate AI features (chat completions, embeddings, and more) into their Umbraco sites without being locked into a specific AI provider.

### Design Philosophy

Umbraco.AI follows a **thin wrapper** philosophy over Microsoft.Extensions.AI (MEAI). Rather than replacing or abstracting away MEAI, it adds Umbraco-specific features while exposing the underlying MEAI types directly:

- **Provider-agnostic**: Support multiple AI providers (OpenAI, Azure, Anthropic, etc.) through a unified interface
- **Capability-based**: Providers expose discrete capabilities (Chat, Embedding, Media, Moderation)
- **Configuration-driven**: Profiles and connections enable flexible, reusable configurations
- **Middleware pipeline**: Extensible cross-cutting concerns (logging, caching, rate limiting)
- **Native Umbraco patterns**: Uses Composers, DI, and familiar configuration patterns

### Solution Structure

```
Umbraco.AI/
├── src/
│   ├── Umbraco.AI.Core/           # Core abstractions and services
│   ├── Umbraco.AI.OpenAI/         # OpenAI provider implementation
│   ├── Umbraco.AI.Web/            # Management API layer
│   ├── Umbraco.AI.Startup/        # Composition and DI setup
│   ├── Umbraco.AI/                # Meta-package for distribution
│   └── Umbraco.AI.Web.StaticAssets/  # Backoffice UI components
├── demo/
│   └── Umbraco.AI.DemoSite/       # Demo Umbraco site
└── docs/                          # Documentation
```

### Project Dependencies

```
Umbraco.AI (meta-package)
    └── Umbraco.AI.Startup
            ├── Umbraco.AI.Core
            │       └── Microsoft.Extensions.AI
            └── Umbraco.AI.Web
                    └── Umbraco.Cms.Api.Management

Umbraco.AI.OpenAI
    ├── Umbraco.AI.Core
    └── Microsoft.Extensions.AI.OpenAI
```

---

## 2. Project Organization

### Umbraco.AI.Core

The core library containing all abstractions, services, and models:

| Namespace | Purpose |
|-----------|---------|
| `Umbraco.AI.Core.Providers` | Provider and capability interfaces/base classes |
| `Umbraco.AI.Core.Services` | High-level services (IAIChatService) |
| `Umbraco.AI.Core.Factories` | Client and generator factories |
| `Umbraco.AI.Core.Models` | Data models (AIConnection, AIProfile, etc.) |
| `Umbraco.AI.Core.Connections` | Connection management |
| `Umbraco.AI.Core.Profiles` | Profile management |
| `Umbraco.AI.Core.Middleware` | Middleware pipeline system |
| `Umbraco.AI.Core.Registry` | Provider registry |
| `Umbraco.AI.Core.Settings` | Settings resolution and validation |
| `Umbraco.AI.Extensions` | DI registration extensions |

### Umbraco.AI.OpenAI

Reference implementation of an AI provider for OpenAI:

- `OpenAIProvider` - Provider class with `[AIProvider]` attribute
- `OpenAIProviderSettings` - Typed settings with `[AIField]` attributes
- `OpenAIChatCapability` - Chat completion capability
- `OpenAIEmbeddingCapability` - Text embedding capability

### Umbraco.AI.Web

Management API layer for backoffice integration:

- API controllers for connections, profiles, and providers
- Swagger/OpenAPI configuration
- Backoffice security integration

### Umbraco.AI.Startup

Umbraco integration via Composer pattern:

- `UmbracoAIComposer` - Implements `IComposer` for auto-discovery
- `UmbracoBuilderExtensions` - Extension methods for `IUmbracoBuilder`

### Umbraco.AI

Meta-package that bundles all components for NuGet distribution.

### Umbraco.AI.Web.StaticAssets

Frontend assets for backoffice UI:

- TypeScript/JavaScript components in `Client/`
- Compiled assets served from `App_Plugins/UmbracoAI`

---

## 3. Core Concepts

### Hierarchical Model

Umbraco.AI uses a hierarchical configuration model:

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

### IAIProvider Interface

Providers implement `IDiscoverable` for automatic discovery via Umbraco's TypeLoader:

```csharp
public interface IAIProvider : IDiscoverable
{
    // The unique identifier for this provider (e.g., "openai")
    string Id { get; }

    // Display name (e.g., "OpenAI")
    string Name { get; }

    // The type representing provider-specific settings
    Type? SettingsType { get; }

    // Get settings schema for UI rendering
    AIEditableModelSchema? GetSettingsSchema();

    // Get all capabilities this provider supports
    IReadOnlyCollection<IAICapability> GetCapabilities();

    // Get a specific capability by type
    TCapability? GetCapability<TCapability>() where TCapability : class, IAICapability;

    // Check if provider has a specific capability
    bool HasCapability<TCapability>() where TCapability : class, IAICapability;

    // Try to get a capability (returns false if not found)
    bool TryGeCapability<TCapability>(out TCapability? capability)
        where TCapability : class, IAICapability;
}
```

### AIProviderBase<TSettings>

Generic base class for implementing providers with typed settings:

```csharp
[AIProvider("openai", "OpenAI")]
public class OpenAIProvider : AIProviderBase<OpenAIProviderSettings>
{
    public OpenAIProvider(IAIProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        // Register capabilities
        WithCapability<OpenAIChatCapability>();
        WithCapability<OpenAIEmbeddingCapability>();
    }
}
```

### AIProviderAttribute

Marks classes for automatic discovery:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class AIProviderAttribute : Attribute
{
    public string Id { get; }      // Unique provider identifier (e.g., "openai")
    public string Name { get; }    // Display name (e.g., "OpenAI")
}
```

### Provider Collection Builder

Providers are managed via `AIProviderCollectionBuilder`, which extends Umbraco's `LazyCollectionBuilderBase`. This provides:

- **Auto-discovery**: Providers with `[AIProvider]` attribute implementing `IDiscoverable` are automatically found via TypeLoader
- **Extensibility**: Providers can be added or excluded in Composers
- **Caching**: Uses Umbraco's TypeLoader caching for efficient type discovery

```csharp
// In AddUmbracoAICore() - auto-discover providers
builder.AIProviders()
    .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIProvider, AIProviderAttribute>(cache: true));

// In a custom Composer - add or exclude providers
builder.AIProviders()
    .Add<MyCustomProvider>()
    .Exclude<SomeUnwantedProvider>();
```

### AIProviderCollection

The collection provides helper methods for accessing providers:

```csharp
public class AIProviderCollection : BuilderCollectionBase<IAIProvider>
{
    // Get a provider by its unique identifier
    public IAIProvider? GetById(string providerId);

    // Get all providers that support a specific capability
    public IEnumerable<IAIProvider> GetWithCapability<TCapability>()
        where TCapability : class, IAICapability;
}
```

---

## 5. Capability System

Capabilities define what a provider can do. Each capability type has a specific interface.

### IAICapability Interface

```csharp
public interface IAICapability
{
    // The kind of capability (Chat, Embedding, Media, Moderation)
    AICapability Kind { get; }

    // Get available models for this capability
    Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        object? settings = null,
        CancellationToken cancellationToken = default);
}
```

### Capability Types

```csharp
public enum AICapability
{
    Chat = 0,       // Chat completions (conversational AI)
    Embedding = 1,  // Text embeddings (vector representations)
    Media = 2,      // Image/audio generation (planned)
    Moderation = 3  // Content moderation (planned)
}
```

### IAIChatCapability

For chat completion capabilities:

```csharp
public interface IAIChatCapability : IAICapability
{
    // Creates an IChatClient configured with the given settings
    IChatClient CreateClient(object? settings = null);
}
```

### IAIEmbeddingCapability

For text embedding capabilities:

```csharp
public interface IAIEmbeddingCapability : IAICapability
{
    // Creates an embedding generator configured with the given settings
    IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(object? settings);
}
```

### Base Classes

Abstract base classes simplify capability implementation:

| Base Class | Purpose |
|------------|---------|
| `AICapabilityBase` | Base for capabilities without typed settings |
| `AICapabilityBase<TSettings>` | Base for capabilities with typed settings |
| `AIChatCapabilityBase` | Chat capability without typed settings |
| `AIChatCapabilityBase<TSettings>` | Chat capability with typed settings |
| `AIEmbeddingCapabilityBase` | Embedding capability without typed settings |
| `AIEmbeddingCapabilityBase<TSettings>` | Embedding capability with typed settings |

### Example: OpenAI Chat Capability

```csharp
public class OpenAIChatCapability(OpenAIProvider provider)
    : AIChatCapabilityBase<OpenAIProviderSettings>(provider)
{
    protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OpenAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var models = new List<AIModelDescriptor>
        {
            new(new AIModelRef(Provider.Id, "gpt-4o"), "GPT-4o"),
            new(new AIModelRef(Provider.Id, "gpt-4o-mini"), "GPT-4o Mini"),
            // ... more models
        };
        return Task.FromResult<IReadOnlyList<AIModelDescriptor>>(models);
    }

    protected override IChatClient CreateClient(OpenAIProviderSettings settings)
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

### AIConnection Model

```csharp
public class AIConnection
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

### IAIConnectionService

```csharp
public interface IAIConnectionService
{
    // CRUD operations
    Task<AIConnection?> GetConnectionAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AIConnection>> GetConnectionsAsync(string? providerId = null, CancellationToken ct = default);
    Task<AIConnection> SaveConnectionAsync(AIConnection connection, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);

    // Validation
    Task<ValidationResult> ValidateConnectionAsync(string providerId, object settings, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(Guid connectionId, CancellationToken ct = default);
}
```

### Current Storage

Connections are currently stored in-memory via `InMemoryAIConnectionRepository`. This is a placeholder for future persistent storage implementations.

---

## 7. Profile System

Profiles are reusable configurations that combine a connection with model settings.

### AIProfile Model

```csharp
public sealed class AIProfile
{
    // Unique identifier
    public required Guid Id { get; init; }

    // Unique alias for referencing (e.g., "default-chat")
    public required string Alias { get; init; }

    // Display name
    public required string Name { get; init; }

    // Capability type (Chat, Embedding, etc.)
    public AICapability Capability { get; init; } = AICapability.Chat;

    // Model reference (provider ID + model ID)
    public AIModelRef Model { get; init; }

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

### AIModelRef

Reference to a specific model:

```csharp
public record AIModelRef(string ProviderId, string ModelId);
```

### IAIProfileService

```csharp
public interface IAIProfileService
{
    // Get by ID
    Task<AIProfile?> GetProfileAsync(Guid id, CancellationToken ct = default);

    // Get by alias
    Task<AIProfile?> GetProfileByAliasAsync(string alias, CancellationToken ct = default);

    // Get all profiles, optionally filtered by capability
    Task<IEnumerable<AIProfile>> GetProfilesAsync(AICapability? capability = null, CancellationToken ct = default);

    // Get default profile for a capability (from AIOptions)
    Task<AIProfile?> GetDefaultProfileAsync(AICapability capability, CancellationToken ct = default);

    // CRUD
    Task<AIProfile> SaveProfileAsync(AIProfile profile, CancellationToken ct = default);
    Task<bool> DeleteProfileAsync(Guid id, CancellationToken ct = default);
}
```

---

## 8. Service Layer

High-level services provide the primary developer interface for using AI features.

### IAIChatService

The main service for chat completions:

```csharp
public interface IAIChatService
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

When using `IAIChatService`, options are merged with priority:

1. **Caller options** (passed to method) - highest priority
2. **Profile settings** (Temperature, MaxTokens, etc.) - defaults

This allows profiles to set defaults while callers can override specific settings.

### Usage Example

```csharp
public class MyService
{
    private readonly IAIChatService _chatService;

    public MyService(IAIChatService chatService)
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

### IAIChatClientFactory

Creates configured `IChatClient` instances with middleware applied:

```csharp
public interface IAIChatClientFactory
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

### IAIChatMiddleware

Middleware can wrap chat clients to add cross-cutting concerns. Note that the interface has no `Order` property - ordering is managed entirely via the collection builder.

```csharp
public interface IAIChatMiddleware
{
    // Wraps the client with middleware behavior
    IChatClient Apply(IChatClient client);
}
```

### IAIEmbeddingMiddleware

Similarly for embedding middleware:

```csharp
public interface IAIEmbeddingMiddleware
{
    // Wraps the generator with middleware behavior
    IEmbeddingGenerator<string, Embedding<float>> Apply(
        IEmbeddingGenerator<string, Embedding<float>> generator);
}
```

### Middleware Collection Builders

Both chat and embedding middleware use `OrderedCollectionBuilderBase` for explicit ordering:

- `AIChatMiddlewareCollectionBuilder` - manages `IAIChatMiddleware` instances
- `AIEmbeddingMiddlewareCollectionBuilder` - manages `IAIEmbeddingMiddleware` instances

### Middleware Ordering

Middleware ordering is controlled via the `AIChatMiddlewareCollectionBuilder` using Umbraco's `OrderedCollectionBuilder` pattern. This provides explicit control with `Append()`, `InsertBefore<T>()`, and `InsertAfter<T>()` methods.

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
        builder.AIChatMiddleware()
            .Append<RateLimitMiddleware>()
            .Append<CachingMiddleware>()
            .Append<LoggingMiddleware>();

        // Or use InsertBefore/InsertAfter for precise ordering:
        builder.AIChatMiddleware()
            .InsertBefore<LoggingMiddleware, TracingMiddleware>();
    }
}
```

### Example: Logging Middleware

```csharp
public class LoggingChatMiddleware(ILoggerFactory loggerFactory) : IAIChatMiddleware
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

### IAIRegistry

```csharp
public interface IAIRegistry
{
    // All registered providers
    IEnumerable<IAIProvider> Providers { get; }

    // Get providers that support a specific capability
    IEnumerable<IAIProvider> GetProvidersWithCapability<TCapability>()
        where TCapability : class, IAICapability;

    // Get provider by ID (case-insensitive)
    IAIProvider? GetProvider(string alias);

    // Get a specific capability from a provider
    TCapability? GetCapability<TCapability>(string providerId)
        where TCapability : class, IAICapability;
}
```

### Usage Example

```csharp
// Get all providers that support chat
var chatProviders = registry.GetProvidersWithCapability<IAIChatCapability>();

// Get the OpenAI provider
var openai = registry.GetProvider("openai");

// Get chat capability directly
var chatCapability = registry.GetCapability<IAIChatCapability>("openai");
```

---

## 11. Configuration & Settings

### AIOptions

Global configuration from `appsettings.json`:

```csharp
public class AIOptions
{
    public string? DefaultChatProfileAlias { get; set; }
    public string? DefaultEmbeddingProfileAlias { get; set; }
    // Future: DefaultImageProviderAlias, DefaultModerationProviderAlias
}
```

Configuration section: `Umbraco:AI`

```json
{
  "Umbraco": {
    "AI": {
      "DefaultChatProfileAlias": "default-chat",
      "DefaultEmbeddingProfileAlias": "default-embedding"
    }
  }
}
```

### AIFieldAttribute

Decorates properties for UI generation (used for both provider settings and data models):

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class AIFieldAttribute : Attribute
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
public class OpenAIProviderSettings
{
    [AIField(
        Label = "API Key",
        Description = "Your OpenAI API key from platform.openai.com",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 1
    )]
    [Required]
    public string? ApiKey { get; set; }

    [AIField(
        Label = "Organization ID",
        Description = "Optional: Your OpenAI organization ID",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 2
    )]
    public string? OrganizationId { get; set; }

    [AIField(
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

The `IAIEditableModelResolver` supports resolving values from configuration:

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

1. **UmbracoAIComposer** (discovered by Umbraco)
   - Calls `builder.AddUmbracoAI()`

2. **AddUmbracoAI()** (in Umbraco.AI.Startup)
   - Calls `AddUmbracoAICore()` for core services
   - Calls `AddUmbracoAIWeb()` for management API

3. **AddUmbracoAICore()** (in Umbraco.AI.Core)
   - Binds `AIOptions` from configuration
   - Registers infrastructure services
   - Scans and registers providers
   - Registers repositories, services, and factories

### Registered Services

| Service | Implementation | Lifetime |
|---------|---------------|----------|
| `IAIRegistry` | `AIRegistry` | Singleton |
| `IAICapabilityFactory` | `AICapabilityFactory` | Singleton |
| `IAIEditableModelSchemaBuilder` | `AIEditableModelSchemaBuilder` | Singleton |
| `IAIProviderInfrastructure` | `AIProviderInfrastructure` | Singleton |
| `IAIEditableModelResolver` | `AIEditableModelResolver` | Singleton |
| `IAIConnectionRepository` | `InMemoryAIConnectionRepository` | Singleton |
| `IAIConnectionService` | `AIConnectionService` | Singleton |
| `IAIProfileRepository` | `InMemoryAIProfileRepository` | Singleton |
| `IAIProfileService` | `AIProfileService` | Singleton |
| `IAIChatClientFactory` | `AIChatClientFactory` | Singleton |
| `IAIEmbeddingGeneratorFactory` | `AIEmbeddingGeneratorFactory` | Singleton |
| `IAIChatService` | `AIChatService` | Singleton |

### Provider Registration

Providers are discovered via Umbraco's TypeLoader and registered using the collection builder pattern:

```csharp
// In AddUmbracoAICore()
builder.AIProviders()
    .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIProvider, AIProviderAttribute>(cache: true));
```

This approach:
- Uses Umbraco's cached, efficient type discovery via `TypeLoader`
- Allows providers to be added or excluded in Composers via `AIProviders().Add<T>()` / `AIProviders().Exclude<T>()`
- Follows the standard Umbraco collection builder pattern

### Middleware Registration

Middleware collections are initialized (empty by default) and can be populated in Composers:

```csharp
// In AddUmbracoAICore() - initialize empty collections
_ = builder.AIChatMiddleware();
_ = builder.AIEmbeddingMiddleware();

// In a custom Composer - add middleware
builder.AIChatMiddleware()
    .Append<LoggingChatMiddleware>()
    .InsertBefore<LoggingChatMiddleware, TracingMiddleware>();
```

---

## 13. Management API

### API Configuration

- **Root path**: `/umbraco/ai/management/api`
- **API name**: `ai-management`
- **Namespace prefix**: `Umbraco.AI.Web.Api.Management`

### Security

The management API integrates with Umbraco's backoffice security:

- `UmbracoAIManagementApiBackOfficeSecurityRequirementsOperationFilter` extends `BackOfficeSecurityRequirementsOperationFilterBase`
- Requires backoffice authentication
- Respects Umbraco user permissions

### Swagger Integration

OpenAPI documentation is automatically generated:

- `UmbracoAIManagementApiSchemaIdHandler` - Generates schema IDs
- `UmbracoAIManagementApiOperationIdHandler` - Generates operation IDs

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
│ 1. Application calls IAIChatService.GetResponseAsync()          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Service resolves profile (by ID or default from AIOptions)   │
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
│ 4. Apply middleware pipeline (ordered by collection builder)     │
│    [Middleware1] → [Middleware2] → ... → [Provider]             │
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
    Provider.GetCapability<IAIChatCapability>()
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

Umbraco.AI.Agents will extend Umbraco.AI with autonomous AI capabilities that can perform actions within Umbraco CMS using **tools**.

### Agent Concept

An agent is a configured AI assistant that can:
- Understand natural language requests
- Access tools to gather information or perform actions
- Execute multi-step workflows autonomously
- Request user approval before making changes

### Agent Definition

```csharp
// Planned model
public class AIAgent
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Guid ProfileId { get; }           // References AIProfile
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
Umbraco.AI.Agents (new project)
├── Models/
│   ├── AIAgent              # Agent definition
│   ├── AgentSession         # Conversation state
│   └── ToolInvocation       # Tool call record
├── Tools/
│   ├── IAITool              # Tool interface
│   └── AIToolAttribute      # Discovery attribute
├── Services/
│   ├── IAIAgentService      # Agent CRUD
│   ├── IAIAgentExecutor     # Execution engine
│   └── IAgentSessionStore   # Session memory
```

### Backoffice Integration Points

1. **Global AI Assistant** - Sidebar accessible from header
2. **Content Entity Actions** - Right-click menu on content items
3. **Inline Property Actions** - AI buttons in property editors
4. **Agent Management Section** - Configure agents and permissions

---

## Summary

Umbraco.AI provides a comprehensive, provider-agnostic AI integration layer for Umbraco CMS. Key architectural decisions include:

- **Microsoft.Extensions.AI foundation**: Uses MEAI types directly, not abstracting them away
- **Capability-based providers**: Providers expose discrete capabilities that can be queried
- **Hierarchical configuration**: Provider → Connection → Profile → Request
- **Middleware pipeline**: Extensible cross-cutting concerns
- **Automatic discovery**: Providers are found via assembly scanning and attributes
- **Environment variable support**: Credentials can be stored securely in configuration

This architecture enables flexible, maintainable AI integrations that can grow with Umbraco sites as AI capabilities evolve.
