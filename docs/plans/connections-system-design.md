# Connections System Design Plan

## Executive Summary

This document outlines the design and implementation plan for a connections system in Umbraco.Ai. The connections system will manage AI provider credentials and settings, enabling users to configure multiple named connections per provider through a UI, with settings persisted and resolved at runtime for authenticated API calls.

## Background

### Current State

The existing architecture includes:
- **Providers**: AI providers (e.g., OpenAI) implementing `IAiProvider` with capabilities (Chat, Embedding)
- **Profiles**: Named configurations (`AiProfile`) mapping to provider/model combinations with parameters
- **Settings**: Provider-specific settings classes (e.g., `OpenAiProviderSettings` with `ApiKey`)
- **Gap**: No mechanism to persist, manage, or retrieve connection credentials

### Critical Missing Pieces

1. **Line 189 in `AiChatService.cs`**: Comment states "Connection settings will come from the connection system (future)" - currently returns `null`
2. **Line 28 in `AiRegistryExtensions.cs`**: "TODO: Handle settings (Need a connection)"
3. **No storage mechanism**: Settings classes exist but aren't populated from anywhere
4. **No UI integration**: No way to define connections through the management UI

## Key Design Changes (Updated)

Based on feedback, the following design decisions have been made:

1. **Connection IDs are Guids**: `AiConnection.Id` is `Guid` type for stable identity across configuration changes. `Name` remains as human-readable display text.

2. **Single SaveConnectionAsync Method**: `IAiConnectionService` uses `SaveConnectionAsync(AiConnection)` instead of separate Create/Update methods. If `connection.Id == Guid.Empty`, a new Guid is generated. Otherwise, it updates the existing connection.

3. **Capabilities Receive Settings, Not Connections**: All capability methods continue to receive `object? settings` parameter. The service layer (AiChatService, etc.) resolves `Profile.ConnectionId → AiConnection → Settings` before calling capabilities. This maintains separation of concerns and aligns with the existing architecture.

4. **EditorUiAlias Over EditorAlias**: `AiSettingAttribute` uses `EditorUiAlias` property (not `EditorAlias`) to align with Umbraco UI conventions.

## Requirements

Based on user clarifications:

| Requirement | Decision |
|------------|----------|
| Scope | Global connections only (no tenant isolation) |
| Storage | In-memory (fake for now, designed for future DB persistence) |
| Multiplicity | Multiple named connections per provider (e.g., "OpenAI-Dev", "OpenAI-Prod") |
| Profile Relationship | Explicit: `AiProfile` will have a `ConnectionId` property |
| Discovery | Profiles can be looked up by capability |

## Architecture Design

### Key Design Decision: Pass Settings, Not Connections

**Pattern Choice**: Capability methods receive `object? settings`, not `AiConnection`.

**Rationale**:
1. **Separation of Concerns**: Capabilities are infrastructure components that only need configuration (settings) to do their job. They shouldn't know about the connection management system.
2. **Consistent with Existing Architecture**: The codebase already uses `object? settings` throughout the capability interfaces (`IAiCapability.GetModelsAsync(settings)`, etc.).
3. **Testability**: Easier to unit test capabilities by passing mock settings directly without creating full connection objects.
4. **Flexibility**: Supports alternative configuration sources (config files for dev, environment variables) without changing capability code.
5. **Microsoft.Extensions.AI Pattern**: The MEAI library expects configuration at client creation time, not connection context.

**Responsibility Separation**:
- **Service Layer** (AiChatService): Knows about connections, resolves ConnectionId → Settings
- **Capability Layer** (IAiChatCapability): Only knows about settings, uses them to create authenticated clients
- **Connection metadata** (Name, IsActive, etc.) is used by service layer before calling capabilities

### Component Overview

```
┌─────────────────┐
│   UI Layer      │  (Future: Management API endpoints)
│  (Not in scope) │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│              Connection Service Layer                    │
│  ┌─────────────────────┐    ┌──────────────────────┐   │
│  │ IAiConnectionService│───▶│ IAiConnectionRepo     │   │
│  │  - Validation       │    │  - CRUD operations    │   │
│  │  - Schema checks    │    │  - In-memory storage  │   │
│  └─────────────────────┘    └──────────────────────┘   │
└─────────────────────────────────────────────────────────┘
         ▲
         │
         │ (Connection resolution)
         │
┌────────┴────────────────────────────────────────────────┐
│                Service Layer                             │
│  AiChatService, AiEmbeddingService (future)             │
│  - Profile → ConnectionId → Settings resolution         │
└─────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│              Provider/Capability Layer                   │
│  IAiProvider → IAiCapability → CreateClient(settings)   │
│  - Receives resolved settings object                     │
│  - Creates authenticated MEAI clients                    │
└─────────────────────────────────────────────────────────┘
```

### Data Flow

```
User Request
    │
    ▼
AiChatService.SendMessageAsync(request)
    │
    ├─→ Resolve Profile (by name or use default)
    │       │
    │       ▼
    │   AiProfile { ConnectionId: "openai-prod", ModelRef: {...} }
    │
    ├─→ Get Connection by ConnectionId
    │       │
    │       ▼
    │   AiConnection { Id: "openai-prod", ProviderId: "openai", Settings: {...} }
    │
    ├─→ Get Provider from Registry
    │       │
    │       ▼
    │   IAiProvider (OpenAiProvider)
    │
    ├─→ Get Capability
    │       │
    │       ▼
    │   IAiChatCapability
    │
    └─→ Create Client with Settings
            │
            ▼
        IChatClient (authenticated with API key from settings)
            │
            ▼
        External API Call
```

## Detailed Component Specifications

### 1. Connection Models

#### `AiConnection` (Core Model)

**Location**: `src/Umbraco.Ai.Core/Models/AiConnection.cs`

```csharp
/// <summary>
/// Represents a connection to an AI provider with credentials and settings.
/// </summary>
public class AiConnection
{
    /// <summary>
    /// Unique identifier for the connection (Guid string).
    /// Ensures stable identity even if volatile definitions change.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Display name for the connection (shown in UI).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The ID of the provider this connection is for (e.g., "openai", "azure").
    /// Must match a registered provider's ID.
    /// </summary>
    public required string ProviderId { get; set; }

    /// <summary>
    /// Provider-specific settings (credentials, endpoints, etc.).
    /// Type depends on provider (e.g., OpenAiProviderSettings).
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// Whether this connection is currently active/enabled.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the connection was created.
    /// </summary>
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the connection was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;
}
```

**Key Design Points**:
- `Id` is a Guid for stable identity (won't change even if name/config changes)
- `Name` is the human-readable display name shown in UI
- `ProviderId` links to `IAiProvider.Id` (enforced by validation)
- `Settings` is `object?` (nullable) to match existing pattern (will be cast to provider-specific type)
- `IsActive` allows soft-disabling connections without deletion
- Timestamps for auditing

#### `AiConnectionRef` (Lightweight Reference)

**Location**: `src/Umbraco.Ai.Core/Models/AiConnectionRef.cs`

```csharp
/// <summary>
/// Lightweight reference to a connection (for use in profiles, lists, etc.).
/// </summary>
public record AiConnectionRef(Guid Id, string Name);
```

**Purpose**: Used when you need to reference a connection without loading full settings (e.g., dropdown lists in UI).

### 2. Connection Repository

#### `IAiConnectionRepository` (Interface)

**Location**: `src/Umbraco.Ai.Core/Connections/IAiConnectionRepository.cs`

```csharp
/// <summary>
/// Repository for managing AI provider connections.
/// </summary>
public interface IAiConnectionRepository
{
    /// <summary>
    /// Get a connection by its ID.
    /// </summary>
    Task<AiConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections.
    /// </summary>
    Task<IEnumerable<AiConnection>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections for a specific provider.
    /// </summary>
    Task<IEnumerable<AiConnection>> GetByProviderAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a connection (insert if new, update if exists).
    /// </summary>
    Task<AiConnection> SaveAsync(AiConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection by ID.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a connection exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
```

#### `InMemoryAiConnectionRepository` (Implementation)

**Location**: `src/Umbraco.Ai.Core/Connections/InMemoryAiConnectionRepository.cs`

```csharp
/// <summary>
/// In-memory implementation of connection repository (for prototyping).
/// TODO: Replace with database-backed implementation.
/// </summary>
internal sealed class InMemoryAiConnectionRepository : IAiConnectionRepository
{
    private readonly ConcurrentDictionary<Guid, AiConnection> _connections = new();

    // Implementation details:
    // - Thread-safe using ConcurrentDictionary
    // - Guid keys for stable identity
    // - All methods return completed tasks (synchronous operations)
    // - SaveAsync checks if Id exists (update) or not (insert), updates DateModified on update
    // - Delete returns true if found and removed, false otherwise
}
```

**Key Design Points**:
- Thread-safe for concurrent access
- Guid IDs ensure stable identity across name changes
- Simple replacement point: swap this for DB repository later
- No persistence across restarts (acceptable for prototype phase)
- SaveAsync handles both insert and update logic

### 3. Connection Service

#### `IAiConnectionService` (Interface)

**Location**: `src/Umbraco.Ai.Core/Connections/IAiConnectionService.cs`

```csharp
/// <summary>
/// Service for managing AI provider connections with validation and business logic.
/// </summary>
public interface IAiConnectionService
{
    /// <summary>
    /// Get a connection by ID.
    /// </summary>
    Task<AiConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections, optionally filtered by provider.
    /// </summary>
    Task<IEnumerable<AiConnection>> GetConnectionsAsync(string? providerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connection references for a provider (lightweight list for UI).
    /// </summary>
    Task<IEnumerable<AiConnectionRef>> GetConnectionReferencesAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a connection (insert if new, update if exists) with validation.
    /// If connection.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    Task<AiConnection> SaveConnectionAsync(AiConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection (with usage checks).
    /// </summary>
    Task DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate connection settings against provider schema.
    /// </summary>
    Task<ValidationResult> ValidateConnectionAsync(string providerId, object? settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test a connection by attempting to fetch models (if supported).
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default);
}
```

**Validation Logic**:
- Check provider exists in registry
- Validate settings type matches provider's expected type
- Check required fields based on `AiSettingDefinition`
- (Future) Check if connection is in use by profiles before deletion

#### `AiConnectionService` (Implementation)

**Location**: `src/Umbraco.Ai.Core/Connections/AiConnectionService.cs`

**Dependencies**:
- `IAiConnectionRepository` - for storage operations
- `IAiRegistry` - for provider validation and lookup
- (Future) `IAiProfileResolver` - for usage checks

**Key Methods**:

1. **SaveConnectionAsync**:
   - If `connection.Id == Guid.Empty`, generate new Guid
   - Validate provider exists in registry
   - Validate settings type matches provider's expected type
   - Update `DateModified` timestamp
   - Call `repository.SaveAsync(connection)`

2. **ValidateConnectionAsync**:
   - Get provider from registry
   - Get setting definitions
   - Validate required fields are present
   - Validate types match schema
   - Return validation result with errors (if any)

3. **TestConnectionAsync**:
   - Get connection from repository
   - Get provider capability from registry
   - Call `capability.GetModelsAsync(settings)` to verify authentication
   - Return success/failure with error details

### 4. Profile Updates

#### Modify `AiProfile`

**Location**: `src/Umbraco.Ai.Core/Models/AiProfile.cs`

**Change**: Add `ConnectionId` property

```csharp
public class AiProfile
{
    public required string Name { get; init; }
    public required AiCapability Capability { get; init; }
    public required AiModelRef Model { get; init; }

    // NEW: Explicit connection reference
    /// <summary>
    /// The ID of the connection to use for this profile.
    /// Must reference a valid AiConnection.Id that matches the provider in Model.ProviderId.
    /// </summary>
    public required Guid ConnectionId { get; init; }

    // Existing properties...
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public string? SystemPromptTemplate { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
}
```

**Validation**: Profile's `ConnectionId` must reference a connection whose `ProviderId` matches `Model.ProviderId`.

**Example**:
```csharp
// Assume we have a connection with Id = someGuid and ProviderId = "openai"
var connectionId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

new AiProfile
{
    Name = "content-writer",
    Capability = AiCapability.Chat,
    Model = new AiModelRef("openai", "gpt-4"),
    ConnectionId = connectionId, // Connection must have ProviderId = "openai"
    Temperature = 0.7,
    SystemPromptTemplate = "You are a content writer..."
}
```

### 5. Provider Setting Definitions

#### Modify `IAiProvider`

**Location**: `src/Umbraco.Ai.Core/Providers/IAiProvider.cs`

**Change**: Add method to expose setting schema

```csharp
public interface IAiProvider : IAiComponent
{
    // Existing methods...
    IReadOnlyList<IAiCapability> GetCapabilities();
    bool TryGetCapability<T>(out T? capability) where T : class, IAiCapability;
    T GetCapability<T>() where T : class, IAiCapability;
    bool HasCapability<T>() where T : class, IAiCapability;

    // NEW: Get setting definitions for UI rendering
    /// <summary>
    /// Get the setting definitions that describe the configuration schema for this provider.
    /// Used by the UI to render connection configuration forms.
    /// </summary>
    IReadOnlyList<AiSettingDefinition> GetSettingDefinitions();
}
```

#### Modify `AiProviderBase<TSettings>`

**Location**: `src/Umbraco.Ai.Core/Providers/AiProviderBase.cs`

**Change**: Implement `GetSettingDefinitions()` using reflection

```csharp
public abstract class AiProviderBase<TSettings> : AiProviderBase
    where TSettings : class
{
    // Existing code...

    /// <summary>
    /// Build setting definitions from TSettings using reflection and attributes.
    /// </summary>
    public override IReadOnlyList<AiSettingDefinition> GetSettingDefinitions()
    {
        var definitions = new List<AiSettingDefinition>();
        var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Get custom attributes for metadata
            var attr = property.GetCustomAttribute<AiSettingAttribute>();

            definitions.Add(new AiSettingDefinition
            {
                Key = property.Name.ToLowerInvariant(),
                PropertyName = property.Name,
                PropertyType = property.PropertyType,
                Label = attr?.Label ?? property.Name,
                Description = attr?.Description,
                EditorUiAlias = attr?.EditorUiAlias ?? InferEditorUiAlias(property.PropertyType),
                DefaultValue = attr?.DefaultValue,
                IsRequired = !IsNullable(property.PropertyType),
                SortOrder = attr?.SortOrder ?? 0,
                // ... other mappings
            });
        }

        return definitions;
    }

    private static bool IsNullable(Type type)
    {
        if (!type.IsValueType) return true; // Reference types are nullable
        return Nullable.GetUnderlyingType(type) != null;
    }

    private static string InferEditorUiAlias(Type type)
    {
        if (type == typeof(string)) return "Umb.PropertyEditorUi.TextBox";
        if (type == typeof(int)) return "Umb.PropertyEditorUi.Integer";
        if (type == typeof(bool)) return "Umb.PropertyEditorUi.Toggle";
        // ... more type mappings
        return "Umb.PropertyEditorUi.TextBox"; // fallback
    }
}
```

#### Create `AiSettingAttribute`

**Location**: `src/Umbraco.Ai.Core/Models/AiSettingAttribute.cs`

```csharp
/// <summary>
/// Attribute to decorate provider setting properties with metadata for UI rendering.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AiSettingAttribute : Attribute
{
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? EditorUiAlias { get; set; }
    public object? DefaultValue { get; set; }
    public int SortOrder { get; set; }
    // ... other UI metadata
}
```

#### Update OpenAI Provider

**Location**: `src/Umbraco.Ai.OpenAi/OpenAiProvider.cs`

**Change**: Annotate `OpenAiProviderSettings`

```csharp
public class OpenAiProviderSettings
{
    [AiSetting(
        Label = "API Key",
        Description = "Your OpenAI API key from platform.openai.com",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox", // Could be password input
        SortOrder = 1
    )]
    [Required]
    public string? ApiKey { get; set; }

    [AiSetting(
        Label = "Organization ID",
        Description = "Optional: Your OpenAI organization ID",
        SortOrder = 2
    )]
    public string? OrganizationId { get; set; }

    [AiSetting(
        Label = "API Endpoint",
        Description = "Custom API endpoint (leave empty for default)",
        DefaultValue = "https://api.openai.com/v1",
        SortOrder = 3
    )]
    public string? Endpoint { get; set; }
}
```

### 6. Service Layer Integration

#### Modify `AiChatService`

**Location**: `src/Umbraco.Ai.Core/Services/AiChatService.cs`

**Changes**:

1. **Add dependency**: `IAiConnectionService` in constructor
2. **Replace line 189**: Implement connection resolution

**Before** (line ~189):
```csharp
// Connection settings will come from the connection system (future)
object? settings = null;
```

**After**:
```csharp
// Resolve connection settings from profile
object? settings = await ResolveConnectionSettingsAsync(profile, cancellationToken);
```

**New method**:
```csharp
private async Task<object?> ResolveConnectionSettingsAsync(
    AiProfile profile,
    CancellationToken cancellationToken)
{
    if (profile.ConnectionId == Guid.Empty)
    {
        // No connection specified - this is an error state
        throw new InvalidOperationException(
            $"Profile '{profile.Name}' does not specify a valid ConnectionId.");
    }

    var connection = await _connectionService.GetConnectionAsync(
        profile.ConnectionId,
        cancellationToken);

    if (connection is null)
    {
        throw new InvalidOperationException(
            $"Connection with ID '{profile.ConnectionId}' not found for profile '{profile.Name}'.");
    }

    if (!connection.IsActive)
    {
        throw new InvalidOperationException(
            $"Connection '{connection.Name}' (ID: {profile.ConnectionId}) is not active.");
    }

    // Validate connection provider matches profile's model provider
    if (!string.Equals(connection.ProviderId, profile.Model.ProviderId, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Connection '{connection.Name}' is for provider '{connection.ProviderId}' " +
            $"but profile '{profile.Name}' requires provider '{profile.Model.ProviderId}'.");
    }

    return connection.Settings;
}
```

**Impact**: This change completes the flow from profile → connection → authenticated API call.

### 7. Registry Extensions Update

#### Modify `AiRegistryExtensions`

**Location**: `src/Umbraco.Ai.Core/Registry/AiRegistryExtensions.cs`

**Change**: Remove TODO and add connection parameter

**Before** (line ~28):
```csharp
// TODO: Handle settings (Need a connection)
var models = await capability.GetModelsAsync(null, cancellationToken);
```

**After**:
```csharp
/// <summary>
/// Get available models for a capability, optionally with connection settings.
/// </summary>
public static async Task<IEnumerable<AiModelDescriptor>> GetModelsByCapabilityAsync(
    this IAiRegistry registry,
    string providerId,
    AiCapability capability,
    object? settings = null, // NEW: Allow passing settings
    CancellationToken cancellationToken = default)
{
    var provider = registry.GetProvider(providerId);
    var capabilityInstance = provider.GetCapabilities()
        .FirstOrDefault(c => c.Capability == capability);

    if (capabilityInstance is null)
    {
        return [];
    }

    var models = await capabilityInstance.GetModelsAsync(settings, cancellationToken);
    return models;
}
```

**Note**: Settings now flow through properly when available.

### 8. Dependency Injection Registration

#### Modify `UmbracoBuilderExtensions`

**Location**: `src/Umbraco.Ai.Core/Configuration/UmbracoBuilderExtensions.cs`

**Change**: Register connection services

```csharp
public static IUmbracoBuilder AddUmbracoAi(this IUmbracoBuilder builder)
{
    builder.Services.AddOptions<AiOptions>()
        .Bind(builder.Config.GetSection("Umbraco:Ai"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Register providers (existing)
    RegisterProviders(builder.Services);

    // Register connection system (NEW)
    builder.Services.AddSingleton<IAiConnectionRepository, InMemoryAiConnectionRepository>();
    builder.Services.AddSingleton<IAiConnectionService, AiConnectionService>();

    // Register registry (existing)
    builder.Services.AddSingleton<IAiRegistry, AiRegistry>();

    // Register services (existing - will be uncommented when connection system is ready)
    // builder.Services.AddSingleton<IAiChatService, AiChatService>();

    return builder;
}
```

## Implementation Sequence

To minimize breaking changes and enable incremental testing, implement in this order:

### Phase 1: Foundation (Models & Repository)
1. Create `AiConnection.cs` model
2. Create `AiConnectionRef.cs` model
3. Create `IAiConnectionRepository.cs` interface
4. Create `InMemoryAiConnectionRepository.cs` implementation
5. **Test**: Repository CRUD operations work correctly

### Phase 2: Service Layer
6. Create `IAiConnectionService.cs` interface
7. Create `AiConnectionService.cs` implementation
8. Create validation logic and exceptions
9. Register services in `UmbracoBuilderExtensions.cs`
10. **Test**: Service validation and business logic

### Phase 3: Provider Integration
11. Add `GetSettingDefinitions()` to `IAiProvider`
12. Create `AiSettingAttribute.cs`
13. Implement `GetSettingDefinitions()` in `AiProviderBase<TSettings>`
14. Annotate `OpenAiProviderSettings` with attributes
15. **Test**: Provider exposes correct setting definitions

### Phase 4: Profile Integration
16. Add `ConnectionId` property to `AiProfile.cs`
17. Update profile resolver (when implemented) to validate ConnectionId
18. **Test**: Profiles correctly reference connections

### Phase 5: Service Integration
19. Add `IAiConnectionService` dependency to `AiChatService`
20. Implement `ResolveConnectionSettingsAsync()` method
21. Replace `null` settings with connection resolution
22. Update `AiRegistryExtensions.GetModelsByCapabilityAsync()` signature
23. **Test**: End-to-end flow from request → profile → connection → API call

### Phase 6: OpenAI Implementation
24. Implement `GetModelsAsync()` in `OpenAiChatCapability`
25. Implement `CreateClient()` in `OpenAiChatCapability`
26. Use settings to configure authenticated client
27. **Test**: OpenAI provider works with real credentials

## Testing Strategy

### Unit Tests

1. **Repository Tests** (`InMemoryAiConnectionRepositoryTests`)
   - SaveAsync (insert new, update existing)
   - GetAsync by Guid
   - GetByProviderAsync filtering
   - DeleteAsync operations
   - Concurrent access safety
   - Non-existent connection handling

2. **Service Tests** (`AiConnectionServiceTests`)
   - Validation logic (provider exists, settings match)
   - Business rules (can't delete in-use connection)
   - Test connection functionality

3. **Provider Tests** (`ProviderSettingDefinitionTests`)
   - Setting definitions generated correctly
   - Attributes parsed properly
   - Type inference works

### Integration Tests

4. **End-to-End Flow** (`ConnectionIntegrationTests`)
   - Create connection → Create profile → Send chat request
   - Settings flow through to provider capability
   - Error handling (missing connection, inactive connection, etc.)

### Manual Testing

5. **OpenAI Integration**
   - Create OpenAI connection with real API key
   - Create profile using that connection
   - Send chat request
   - Verify authenticated API call succeeds

## Example Usage

### Creating a Connection (Code)

```csharp
// Inject IAiConnectionService
var connectionService = serviceProvider.GetRequiredService<IAiConnectionService>();

// Create OpenAI connection using SaveConnectionAsync
var connection = await connectionService.SaveConnectionAsync(
    new AiConnection
    {
        Id = Guid.Empty, // Guid.Empty triggers generation of new ID
        Name = "OpenAI Production",
        ProviderId = "openai",
        Settings = new OpenAiProviderSettings
        {
            ApiKey = "sk-..."
        }
    },
    cancellationToken: default);

// connection.Id now contains the generated Guid
var connectionId = connection.Id;
```

### Creating a Profile

```csharp
var profile = new AiProfile
{
    Name = "content-writer",
    Capability = AiCapability.Chat,
    Model = new AiModelRef("openai", "gpt-4"),
    ConnectionId = connectionId, // References the connection's Guid
    Temperature = 0.7,
    SystemPromptTemplate = "You are a helpful content writer."
};
```

### Using the Service

```csharp
// Inject IAiChatService
var chatService = serviceProvider.GetRequiredService<IAiChatService>();

// Send message - connection is resolved automatically from profile
var response = await chatService.SendMessageAsync(new AiChatRequest
{
    ProfileName = "content-writer", // Resolves profile -> connection -> settings
    Messages = [new AiChatMessage(AiChatRole.User, "Write a blog post about AI")]
});
```

## Future Enhancements (Out of Scope)

These are explicitly **not** part of this implementation but are design considerations:

1. **Database Persistence**: Replace `InMemoryAiConnectionRepository` with EF Core/NPoco implementation
2. **Management API**: REST endpoints for CRUD operations on connections
3. **UI Components**: Backoffice dashboard for connection management
4. **Secret Management**: Integration with Azure Key Vault, AWS Secrets Manager, etc.
5. **Multi-tenancy**: Tenant-scoped connections with fallback to global
6. **Connection Pooling**: Reuse authenticated clients across requests
7. **Health Checks**: Periodic connection testing and monitoring
8. **Audit Logging**: Track who created/modified connections and when
9. **Import/Export**: Backup/restore connection configurations
10. **Connection Templates**: Pre-configured templates for common providers

## Risk Analysis

| Risk | Mitigation |
|------|-----------|
| Breaking existing code | Incremental implementation; existing null settings already handled |
| Security (credentials in memory) | Document as prototype phase; design allows drop-in replacement with secure storage |
| Profile validation complexity | Service layer enforces provider matching; clear error messages |
| Concurrent access issues | Use thread-safe ConcurrentDictionary; test concurrency |
| Settings type mismatches | Validation in service layer; runtime checks in capabilities |

## Success Criteria

The implementation will be considered successful when:

1. ✅ Connections can be created programmatically with in-memory persistence
2. ✅ Profiles explicitly reference connections via `ConnectionId`
3. ✅ `AiChatService` resolves settings from connections and passes to providers
4. ✅ OpenAI provider successfully authenticates using connection settings
5. ✅ Validation prevents invalid configurations (mismatched providers, missing connections)
6. ✅ All unit and integration tests pass
7. ✅ No breaking changes to existing provider/capability interfaces (additive only)

## Conclusion

This design provides a clean, extensible connection system that:
- Fills the critical gap in credential management
- Maintains consistency with existing architecture patterns
- Supports multiple connections per provider
- Allows explicit profile→connection mapping
- Uses in-memory storage for rapid prototyping
- Designed for easy migration to persistent storage

The implementation will proceed in phases to minimize risk and enable incremental testing at each stage.
