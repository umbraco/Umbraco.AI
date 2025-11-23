# Architectural Refactoring Plan - Umbraco.Ai

**Date:** 2025-11-23
**Status:** Proposed
**Scope:** Complete architectural overhaul focusing on SOLID principles

---

## Executive Summary

This document provides a comprehensive architectural analysis of the Umbraco.Ai codebase and detailed remediation plans for identified issues. The analysis reveals a **solid foundation** with good use of abstractions and patterns, but identifies several **critical architectural issues** requiring immediate attention:

1. **Missing implementation** (`AiProfileResolver`)
2. **Single Responsibility Principle violations** in key classes
3. **Significant code duplication** (63 lines duplicated across factories)
4. **Service locator anti-pattern** in provider base class
5. **Prototype code in production paths** (in-memory repository)

---

## Table of Contents

1. [Architectural Analysis](#architectural-analysis)
2. [Critical Issues Summary](#critical-issues-summary)
3. [Detailed Resolution Plan](#detailed-resolution-plan)
4. [Implementation Phases](#implementation-phases)
5. [Expected Outcomes](#expected-outcomes)

---

## Architectural Analysis

### 1. Overall Project Structure

#### Project Layout
- **Umbraco.Ai.Core** - Core abstractions and implementations (39 source files)
- **Umbraco.Ai.OpenAi** - OpenAI provider implementation (1 file)
- **Umbraco.Ai.Web.Api.Management** - Web API layer (minimal)

#### Directory Structure (Umbraco.Ai.Core)
```
/Common          - Base interfaces (IAiComponent)
/Configuration   - DI setup and Composer
/Connections     - Connection management (Service + Repository pattern)
/Factories       - Factory pattern for client creation
/Middleware      - Middleware abstractions and examples
/Models          - Domain models and DTOs
/Profiles        - Profile resolution (INTERFACE ONLY - missing implementation)
/Providers       - Provider abstractions and base implementations
/Registry        - Provider registry
/Services        - High-level services (Chat, etc.)
/Settings        - Settings resolution with validation
```

**Assessment:** ✅ Well-organized folder structure following feature-based organization.

**Critical Issue:** ⚠️ The `/Profiles` directory only contains the interface (`IAiProfileResolver`) with no concrete implementation, yet it's referenced in:
- `UmbracoBuilderExtensions.cs:43` - Registered as singleton
- `AiChatService.cs:18` - Injected as dependency
- `AiConnectionService.cs:98` - TODO comment mentions it

---

### 2. Single Responsibility Principle Violations

#### CRITICAL: AiProviderBase<TSettings> - Multiple Responsibilities

**File:** `src/Umbraco.Ai.Core/Providers/AiProviderBase.cs` (Lines 118-197)

**Current Responsibilities:**
1. Provider registration and metadata
2. Capability management
3. **Setting definition generation** (reflection logic)
4. **UI inference logic** (property type → UI alias mapping)
5. **Validation rules inference** (attribute analysis)

**Problematic Code:**
```csharp
public override IReadOnlyList<AiSettingDefinition> GetSettingDefinitions()
{
    var definitions = new List<AiSettingDefinition>();
    var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
        var attr = property.GetCustomAttribute<AiSettingAttribute>();

        definitions.Add(new AiSettingDefinition
        {
            Key = property.Name.ToLowerInvariant(),
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            Label = attr?.Label ?? $"#umbracoAiProviders_{Id.ToCamelCase()}Settings{property.Name}Label",
            Description = attr?.Description ?? $"#umbracoAiProviders_{Id.ToCamelCase()}Settings{property.Name}Description",
            EditorUiAlias = attr?.EditorUiAlias ?? InferEditorUiAlias(property.PropertyType), // UI concern
            DefaultValue = attr?.DefaultValue,
            ValidationRules = InferValidationAttributes(property), // Validation concern
            SortOrder = attr?.SortOrder ?? 0
        });
    }

    return definitions;
}
```

**Impact:** High - This class is doing reflection, UI mapping, and validation logic instead of focusing solely on provider registration.

---

#### CRITICAL: AiSettingsResolver - Multiple Responsibilities

**File:** `src/Umbraco.Ai.Core/Settings/AiSettingsResolver.cs`

**Current Responsibilities:**
1. JSON deserialization (lines 40-69, 96-109)
2. Environment variable resolution (lines 111-152)
3. Type conversion (lines 154-195)
4. Validation (lines 197-243)

**Problematic Code:**
```csharp
public TSettings? ResolveSettings<TSettings>(string providerId, object? settings)
{
    // Deserialization logic
    if (settings is JsonElement jsonElement) { /* deserialize */ }

    // Environment variable resolution
    ResolveEnvironmentVariablesInObject(deserialized);

    // Validation
    ValidateSettings(providerId, deserialized);

    return deserialized;
}
```

**Impact:** High - This orchestrator is also the implementer. Should delegate to specialized services.

---

#### MAJOR: Factory Code Duplication

**Files:**
- `src/Umbraco.Ai.Core/Factories/AiChatClientFactory.cs`
- `src/Umbraco.Ai.Core/Factories/AiEmbeddingGeneratorFactory.cs`

**Problem:** Both factories have **identical** `ResolveConnectionSettingsAsync` methods (63-108 lines of duplicated code):

```csharp
// DUPLICATED IN BOTH FILES
private async Task<object?> ResolveConnectionSettingsAsync(
    AiProfile profile,
    CancellationToken cancellationToken)
{
    if (profile.ConnectionId == Guid.Empty) { /* ... */ }
    var connection = await _connectionService.GetConnectionAsync(/* ... */);
    if (connection is null) { /* ... */ }
    if (!connection.IsActive) { /* ... */ }
    if (!string.Equals(connection.ProviderId, profile.Model.ProviderId, /* ... */)) { /* ... */ }
    var provider = _registry.GetProvider(connection.ProviderId);
    if (provider is null) { /* ... */ }
    var resolvedSettings = _settingsResolver.ResolveSettingsForProvider(provider, connection.Settings);
    return resolvedSettings;
}
```

**Impact:** High - Maintenance nightmare. Bug fixes must be applied twice.

---

#### MODERATE: AiConnectionService - Business Logic + Validation + Testing

**File:** `src/Umbraco.Ai.Core/Connections/AiConnectionService.cs`

**Responsibilities:**
1. Connection CRUD operations
2. Validation orchestration
3. Connection testing
4. Data transformation

**Impact:** Medium - Service is doing too much. Testing and validation should be separate concerns.

---

### 3. Anti-Patterns Identified

#### Service Locator Anti-Pattern

**File:** `src/Umbraco.Ai.Core/Providers/AiProviderBase.cs:34-35, 110`

```csharp
protected readonly IServiceProvider ServiceProvider;

protected AiProviderBase(IServiceProvider serviceProvider)
{
    ServiceProvider = serviceProvider;
}

// Used here:
protected void WithCapability<TCapability>()
    where TCapability : class, IAiCapability
{
    Capabilities.Add(ServiceProvider.CreateInstance<TCapability>(this));
}
```

**Why This is Bad:**
- Hides dependencies - can't see what a provider needs from its constructor
- Makes testing difficult - must mock entire service provider
- Violates Dependency Inversion Principle
- Service locator is considered an anti-pattern in modern DI

**Impact:** High - Affects all providers and makes dependency graph opaque.

---

#### Exception-Based Flow Control

**File:** `AiConnectionService.cs:104-130`

```csharp
public Task<bool> ValidateConnectionAsync(string providerId, object? settings, ...)
{
    try
    {
        var provider = _registry.GetProvider(providerId);
        if (provider is null) { throw new InvalidOperationException(...); }

        _settingsResolver.ResolveSettingsForProvider(provider, settings);
        return Task.FromResult(true);
    }
    catch (InvalidOperationException)
    {
        throw; // Re-throw validation errors
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Failed to validate...", ex);
    }
}
```

**Problem:** Method signature says `Task<bool>` but never returns `false` - always throws on failure. Should use Result pattern instead.

**Impact:** Medium - Makes error handling unpredictable.

---

#### In-Memory Repository in Production

**File:** `UmbracoBuilderExtensions.cs:39`

```csharp
services.AddSingleton<IAiConnectionRepository, InMemoryAiConnectionRepository>();
```

**File:** `InMemoryAiConnectionRepository.cs:1-64`

```csharp
/// <summary>
/// In-memory implementation of connection repository (for prototyping).
/// TODO: Replace with database-backed implementation.
/// </summary>
internal sealed class InMemoryAiConnectionRepository : IAiConnectionRepository
{
    private readonly ConcurrentDictionary<Guid, AiConnection> _connections = new();
    // ...
}
```

**Impact:** Low (keeping as-is per requirements) - But documented as a known limitation.

---

### 4. Positive Architectural Elements

#### ✅ Provider Abstraction Layer

Excellent separation of concerns:
- `IAiProvider` - Core provider contract
- `IAiCapability` - Capability abstraction
- `AiProviderBase<TSettings>` - Base implementation
- Concrete capabilities (Chat, Embedding) properly separated

```csharp
public interface IAiChatCapability : IAiCapability
{
    IChatClient CreateClient(object? settings = null);
}

public interface IAiEmbeddingCapability : IAiCapability
{
    IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(object? settings);
}
```

#### ✅ Middleware Pattern

Clean separation of cross-cutting concerns:

```csharp
public interface IAiChatMiddleware
{
    IChatClient Apply(IChatClient client);
    int Order { get; }
}
```

#### ✅ Registry Pattern

```csharp
internal sealed class AiRegistry : IAiRegistry
{
    private readonly IReadOnlyDictionary<string, IAiProvider> _providers;

    public IEnumerable<IAiProvider> GetProvidersWithCapability<TCapability>()
        where TCapability : class, IAiCapability
        => _providers.Values.Where(x => x.HasCapability<TCapability>());
}
```

Single responsibility, clear purpose, minimal dependencies.

#### ✅ Repository Pattern

Clean abstraction for data access with focused interface.

---

### 5. Design Patterns Inventory

**Currently Used (Good):**
1. ✅ Factory Pattern - `IAiChatClientFactory`, `IAiEmbeddingGeneratorFactory`
2. ✅ Repository Pattern - `IAiConnectionRepository`
3. ✅ Registry Pattern - `IAiRegistry`
4. ✅ Strategy Pattern - Via capability hierarchy
5. ✅ Decorator Pattern - Middleware implementation
6. ✅ Template Method Pattern - `AiProviderBase<TSettings>`

**Anti-Patterns (Bad):**
1. ❌ Service Locator - `AiProviderBase` using `IServiceProvider`
2. ❌ God Object tendencies - `AiSettingsResolver`, `AiProviderBase`

**Missing (Recommended to Add):**
1. ➕ Result Pattern - For error handling without exceptions
2. ➕ Builder Pattern - For complex object construction
3. ➕ Specification Pattern - For composable validation rules
4. ➕ Decorator Pattern (caching) - For performance optimization

---

### 6. Critical Missing Implementations

#### 1. Missing Profile System

**Interface exists:** `IAiProfileResolver`
**Registered in DI:** `UmbracoBuilderExtensions.cs:43`
**Implementation:** **DOES NOT EXIST** ⚠️

**Impact:** CRITICAL - Application will fail at runtime when any code tries to resolve profiles.

#### 2. Missing Profile Repository

No persistence mechanism for `AiProfile` objects. How are profiles stored and retrieved?

**Impact:** HIGH - Cannot manage profiles without this.

#### 3. Inconsistent Settings Resolution

Two different mechanisms exist:

**Location 1:** `IAiCapability.cs:117-125` (in base capability)
```csharp
protected TSettings ResolveSettings(object? settings)
{
    return settings switch
    {
        TSettings typedSettings => typedSettings,
        JsonElement jsonElement => jsonElement.Deserialize<TSettings>()!,
        _ => default!
    };
}
```

**Location 2:** `AiSettingsResolver.cs` (dedicated service with full env var resolution & validation)

**Impact:** Medium - Inconsistency can lead to bugs.

---

### 7. Mutable vs Immutable Models

#### ⚠️ Problematic: AiConnection (Mixed Mutability)

```csharp
public class AiConnection
{
    public required Guid Id { get; init; }           // Immutable
    public required string Name { get; set; }        // Mutable
    public required string ProviderId { get; set; }  // Mutable (DANGEROUS!)
    public object? Settings { get; set; }            // Mutable
    public bool IsActive { get; set; }               // Mutable
    public DateTime DateModified { get; set; }       // Mutable
}
```

**Problem:** Core properties like `ProviderId` should not be changeable after creation. Changing provider ID on an existing connection could break everything.

#### ✅ Good: AiProfile (Immutable)

```csharp
public class AiProfile
{
    public required string Id { get; init; }
    public required AiModel Model { get; init; }
    public required Guid ConnectionId { get; init; }
    // All properties use 'init'
}
```

---

## Critical Issues Summary

| Priority | Issue | Location | Impact | Effort |
|----------|-------|----------|--------|--------|
| **P0** | Missing `AiProfileResolver` implementation | `Profiles/` | Runtime failure | Low |
| **P0** | Service Locator anti-pattern | `AiProviderBase.cs` | Testing, maintainability | Medium |
| **P1** | SRP violation in `AiProviderBase` | `Providers/AiProviderBase.cs:118-197` | Maintainability | High |
| **P1** | SRP violation in `AiSettingsResolver` | `Settings/AiSettingsResolver.cs` | Testability | High |
| **P1** | Code duplication in factories | `Factories/*.cs` | Bugs, maintenance | Medium |
| **P2** | Exception-based flow control | `AiConnectionService.cs` | Error handling | Medium |
| **P2** | Inconsistent settings resolution | Multiple files | Bugs | Medium |
| **P2** | Mutable connection model | `Models/AiConnection.cs` | Data integrity | Low |
| **P3** | Utility classes in wrong location | `StringExtensions.cs` | Organization | Low |

---

## Detailed Resolution Plan

### Phase 1: Critical Fixes (Runtime Failures)

#### 1.1 Implement AiProfileResolver

**Problem:** Referenced but doesn't exist.

**Solution:**

Create profile resolver implementation:

```csharp
// File: src/Umbraco.Ai.Core/Profiles/AiProfileResolver.cs
namespace Umbraco.Ai.Core.Profiles;

internal sealed class AiProfileResolver : IAiProfileResolver
{
    private readonly IAiProfileRepository _repository;

    public AiProfileResolver(IAiProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<AiProfile?> ResolveProfileAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _repository.GetByIdAsync(profileId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(profile.Model?.ProviderId))
        {
            throw new InvalidOperationException(
                $"Profile '{profileId}' has invalid model configuration.");
        }

        return profile;
    }

    public async Task<IEnumerable<AiProfile>> GetAllProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }
}
```

Create repository interface:

```csharp
// File: src/Umbraco.Ai.Core/Profiles/IAiProfileRepository.cs
public interface IAiProfileRepository
{
    Task<AiProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiProfile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AiProfile> SaveAsync(AiProfile profile, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
```

Create temporary in-memory implementation:

```csharp
// File: src/Umbraco.Ai.Core/Profiles/InMemoryAiProfileRepository.cs
internal sealed class InMemoryAiProfileRepository : IAiProfileRepository
{
    private readonly ConcurrentDictionary<string, AiProfile> _profiles = new();

    public Task<AiProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(id, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IEnumerable<AiProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AiProfile>>(_profiles.Values.ToList());
    }

    public Task<AiProfile> SaveAsync(AiProfile profile, CancellationToken cancellationToken = default)
    {
        _profiles[profile.Id] = profile;
        return Task.FromResult(profile);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_profiles.TryRemove(id, out _));
    }
}
```

Update DI registration:

```csharp
// In UmbracoBuilderExtensions.cs
services.AddSingleton<IAiProfileRepository, InMemoryAiProfileRepository>();
services.AddSingleton<IAiProfileResolver, AiProfileResolver>();
```

**Files Created:** 3
**Files Modified:** 1
**Estimated Effort:** 2-3 hours

---

#### 1.2 Fix Service Locator Anti-Pattern

**Problem:** `AiProviderBase` depends on `IServiceProvider` directly.

**Solution: Use Service Bundle Pattern (Recommended)**

This approach uses a "service envelope" to bundle provider infrastructure services, avoiding the need to expose all dependencies individually while maintaining explicit contracts.

**Why Service Bundle is Better than Service Locator:**
- ✅ Explicit dependencies defined in interface
- ✅ Compile-time type safety
- ✅ Easy to test (mock single interface)
- ✅ Insulates providers from framework evolution
- ✅ Clear documentation of available services
- ❌ Unlike Service Locator: No runtime resolution, no hidden dependencies

Create provider infrastructure bundle:

```csharp
// File: src/Umbraco.Ai.Core/Providers/IAiProviderInfrastructure.cs
/// <summary>
/// Provides infrastructure services required by AI providers.
/// </summary>
/// <remarks>
/// This bundle contains only framework-level services for provider registration
/// and metadata generation. It insulates provider implementations from changes
/// to the underlying framework infrastructure.
/// </remarks>
public interface IAiProviderInfrastructure
{
    /// <summary>
    /// Factory for creating capability instances.
    /// </summary>
    IAiCapabilityFactory CapabilityFactory { get; }

    /// <summary>
    /// Builder for generating setting definitions from provider settings types.
    /// </summary>
    IAiSettingDefinitionBuilder SettingDefinitionBuilder { get; }
}

// File: src/Umbraco.Ai.Core/Providers/AiProviderInfrastructure.cs
internal sealed class AiProviderInfrastructure : IAiProviderInfrastructure
{
    public AiProviderInfrastructure(
        IAiCapabilityFactory capabilityFactory,
        IAiSettingDefinitionBuilder settingDefinitionBuilder)
    {
        CapabilityFactory = capabilityFactory;
        SettingDefinitionBuilder = settingDefinitionBuilder;
    }

    public IAiCapabilityFactory CapabilityFactory { get; }
    public IAiSettingDefinitionBuilder SettingDefinitionBuilder { get; }
}

// File: src/Umbraco.Ai.Core/Providers/IAiCapabilityFactory.cs
public interface IAiCapabilityFactory
{
    TCapability Create<TCapability>(IAiProvider provider)
        where TCapability : class, IAiCapability;
}

// File: src/Umbraco.Ai.Core/Providers/AiCapabilityFactory.cs
internal sealed class AiCapabilityFactory : IAiCapabilityFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AiCapabilityFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TCapability Create<TCapability>(IAiProvider provider)
        where TCapability : class, IAiCapability
    {
        return _serviceProvider.CreateInstance<TCapability>(provider);
    }
}
```

Refactor `AiProviderBase`:

```csharp
public abstract class AiProviderBase<TSettings> : IAiProvider
{
    private readonly IAiProviderInfrastructure _infrastructure;

    protected AiProviderBase(IAiProviderInfrastructure infrastructure)
    {
        _infrastructure = infrastructure;
        Capabilities = new List<IAiCapability>();
    }

    // Expose infrastructure services as protected properties for clarity
    protected IAiCapabilityFactory CapabilityFactory => _infrastructure.CapabilityFactory;

    protected void WithCapability<TCapability>()
        where TCapability : class, IAiCapability
    {
        Capabilities.Add(CapabilityFactory.Create<TCapability>(this));
    }

    public override IReadOnlyList<AiSettingDefinition> GetSettingDefinitions()
    {
        return _infrastructure.SettingDefinitionBuilder.BuildForType<TSettings>(Id);
    }
}
```

Update DI:

```csharp
services.AddSingleton<IAiCapabilityFactory, AiCapabilityFactory>();
services.AddSingleton<IAiProviderInfrastructure, AiProviderInfrastructure>();
```

**Benefits of Service Bundle Approach:**
1. **Single constructor dependency** - Providers see one parameter
2. **Backward compatibility** - Can add services to bundle without breaking providers
3. **Clear framework boundary** - Bundle represents "provider infrastructure"
4. **Testable** - Mock one interface instead of many
5. **Better than service locator** - Explicit, type-safe, discoverable

**Files Created:** 4
**Files Modified:** 2 (AiProviderBase.cs + all provider implementations)
**Estimated Effort:** 3-4 hours

---

#### 1.3 Add Result<T> Pattern

**Problem:** Exception-based flow control makes error handling unpredictable.

**Solution:**

Create Result types:

```csharp
// File: src/Umbraco.Ai.Core/Common/Result.cs
namespace Umbraco.Ai.Core.Common;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string? error, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Exception = exception;
    }

    public static Result<T> Success(T value)
        => new(true, value, null, null);

    public static Result<T> Failure(string error)
        => new(false, default, error, null);

    public static Result<T> Failure(string error, Exception exception)
        => new(false, default, error, exception);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return IsSuccess
            ? onSuccess(Value!)
            : onFailure(Error!);
    }
}

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}
```

Apply to services:

```csharp
// Before:
public async Task<bool> ValidateConnectionAsync(...)
{
    try { /* ... */ return true; }
    catch { throw; }
}

// After:
public async Task<Result> ValidateConnectionAsync(...)
{
    var provider = _registry.GetProvider(providerId);
    if (provider is null)
    {
        return Result.Failure($"Provider '{providerId}' not found.");
    }

    var resolveResult = _settingsResolver.ResolveSettingsForProvider(provider, settings);
    if (!resolveResult.IsSuccess)
    {
        return Result.Failure($"Invalid settings: {resolveResult.Error}");
    }

    return Result.Success();
}
```

**Files Created:** 1
**Files Modified:** 5+ (all services that throw exceptions for control flow)
**Estimated Effort:** 4-6 hours

---

### Phase 2: Extract Single Responsibilities from AiProviderBase

#### 2.1 Create IAiSettingDefinitionBuilder

**Purpose:** Extract setting definition generation from provider base.

```csharp
// File: src/Umbraco.Ai.Core/Settings/IAiSettingDefinitionBuilder.cs
public interface IAiSettingDefinitionBuilder
{
    IReadOnlyList<AiSettingDefinition> BuildForType<TSettings>(string providerId);
}

// File: src/Umbraco.Ai.Core/Settings/AiSettingDefinitionBuilder.cs
internal sealed class AiSettingDefinitionBuilder : IAiSettingDefinitionBuilder
{
    private readonly IEditorUiAliasResolver _uiResolver;
    private readonly IValidationRuleInferrer _validationInferrer;

    public AiSettingDefinitionBuilder(
        IEditorUiAliasResolver uiResolver,
        IValidationRuleInferrer validationInferrer)
    {
        _uiResolver = uiResolver;
        _validationInferrer = validationInferrer;
    }

    public IReadOnlyList<AiSettingDefinition> BuildForType<TSettings>(string providerId)
    {
        var definitions = new List<AiSettingDefinition>();
        var properties = typeof(TSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var definition = BuildForProperty(property, providerId);
            definitions.Add(definition);
        }

        return definitions;
    }

    private AiSettingDefinition BuildForProperty(PropertyInfo property, string providerId)
    {
        var attr = property.GetCustomAttribute<AiSettingAttribute>();
        var key = property.Name.ToLowerInvariant();
        var providerKey = providerId.ToCamelCase();

        return new AiSettingDefinition
        {
            Key = key,
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            Label = attr?.Label
                ?? $"#umbracoAiProviders_{providerKey}Settings{property.Name}Label",
            Description = attr?.Description
                ?? $"#umbracoAiProviders_{providerKey}Settings{property.Name}Description",
            EditorUiAlias = attr?.EditorUiAlias
                ?? _uiResolver.ResolveForType(property.PropertyType),
            DefaultValue = attr?.DefaultValue,
            ValidationRules = _validationInferrer.InferForProperty(property),
            SortOrder = attr?.SortOrder ?? 0
        };
    }
}
```

**Files Created:** 2
**Estimated Effort:** 2-3 hours

---

#### 2.2 Create IEditorUiAliasResolver

**Purpose:** Extract UI alias inference logic.

```csharp
// File: src/Umbraco.Ai.Core/Settings/IEditorUiAliasResolver.cs
public interface IEditorUiAliasResolver
{
    string ResolveForType(Type propertyType);
}

// File: src/Umbraco.Ai.Core/Settings/EditorUiAliasResolver.cs
internal sealed class EditorUiAliasResolver : IEditorUiAliasResolver
{
    private static readonly Dictionary<Type, string> TypeMappings = new()
    {
        { typeof(string), "Umb.PropertyEditorUi.TextBox" },
        { typeof(int), "Umb.PropertyEditorUi.Integer" },
        { typeof(long), "Umb.PropertyEditorUi.Integer" },
        { typeof(bool), "Umb.PropertyEditorUi.Toggle" },
        { typeof(decimal), "Umb.PropertyEditorUi.Decimal" },
        { typeof(double), "Umb.PropertyEditorUi.Decimal" },
        { typeof(float), "Umb.PropertyEditorUi.Decimal" },
    };

    public string ResolveForType(Type propertyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (TypeMappings.TryGetValue(underlyingType, out var alias))
        {
            return alias;
        }

        if (underlyingType.IsEnum)
        {
            return "Umb.PropertyEditorUi.Dropdown";
        }

        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
        {
            return "Umb.PropertyEditorUi.DatePicker";
        }

        return "Umb.PropertyEditorUi.TextBox";
    }
}
```

**Files Created:** 2
**Estimated Effort:** 1-2 hours

---

#### 2.3 Create IValidationRuleInferrer

**Purpose:** Extract validation rule inference.

```csharp
// File: src/Umbraco.Ai.Core/Settings/IValidationRuleInferrer.cs
public interface IValidationRuleInferrer
{
    IReadOnlyList<AiValidationRule> InferForProperty(PropertyInfo property);
}

// File: src/Umbraco.Ai.Core/Settings/ValidationRuleInferrer.cs
internal sealed class ValidationRuleInferrer : IValidationRuleInferrer
{
    public IReadOnlyList<AiValidationRule> InferForProperty(PropertyInfo property)
    {
        var rules = new List<AiValidationRule>();

        // Check if property is required (not nullable)
        if (!IsNullable(property.PropertyType))
        {
            rules.Add(new AiValidationRule
            {
                Alias = "required",
                Message = $"{property.Name} is required."
            });
        }

        // Check for validation attributes
        var validationAttributes = property.GetCustomAttributes()
            .Where(attr => attr is ValidationAttribute)
            .Cast<ValidationAttribute>();

        foreach (var attr in validationAttributes)
        {
            var rule = ConvertAttributeToRule(attr, property.Name);
            if (rule != null)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private bool IsNullable(Type type)
    {
        if (!type.IsValueType) return true;
        return Nullable.GetUnderlyingType(type) != null;
    }

    private AiValidationRule? ConvertAttributeToRule(
        ValidationAttribute attribute,
        string propertyName)
    {
        return attribute switch
        {
            RequiredAttribute _ => new AiValidationRule
            {
                Alias = "required",
                Message = $"{propertyName} is required."
            },
            StringLengthAttribute length => new AiValidationRule
            {
                Alias = "stringLength",
                Parameters = new Dictionary<string, object>
                {
                    ["maxLength"] = length.MaximumLength,
                    ["minLength"] = length.MinimumLength
                },
                Message = $"{propertyName} must be between {length.MinimumLength} and {length.MaximumLength} characters."
            },
            RangeAttribute range => new AiValidationRule
            {
                Alias = "range",
                Parameters = new Dictionary<string, object>
                {
                    ["min"] = range.Minimum,
                    ["max"] = range.Maximum
                },
                Message = $"{propertyName} must be between {range.Minimum} and {range.Maximum}."
            },
            RegularExpressionAttribute regex => new AiValidationRule
            {
                Alias = "regex",
                Parameters = new Dictionary<string, object>
                {
                    ["pattern"] = regex.Pattern
                },
                Message = regex.ErrorMessage ?? $"{propertyName} format is invalid."
            },
            _ => null
        };
    }
}
```

**Files Created:** 2
**Estimated Effort:** 2-3 hours

---

#### 2.4 Refactor AiProviderBase (Now Simplified)

```csharp
public abstract class AiProviderBase<TSettings> : IAiProvider
    where TSettings : class, new()
{
    private readonly IAiCapabilityFactory _capabilityFactory;
    private readonly IAiSettingDefinitionBuilder _settingDefinitionBuilder;

    protected AiProviderBase(
        IAiCapabilityFactory capabilityFactory,
        IAiSettingDefinitionBuilder settingDefinitionBuilder)
    {
        _capabilityFactory = capabilityFactory;
        _settingDefinitionBuilder = settingDefinitionBuilder;
        Capabilities = new List<IAiCapability>();
    }

    public abstract string Id { get; }
    public abstract string Name { get; }

    private List<IAiCapability> Capabilities { get; }

    IReadOnlyList<IAiCapability> IAiProvider.Capabilities => Capabilities.AsReadOnly();

    // Single responsibility: Register capabilities
    protected void WithCapability<TCapability>()
        where TCapability : class, IAiCapability
    {
        Capabilities.Add(_capabilityFactory.Create<TCapability>(this));
    }

    // Delegated responsibility: Get setting definitions
    public virtual IReadOnlyList<AiSettingDefinition> GetSettingDefinitions()
    {
        return _settingDefinitionBuilder.BuildForType<TSettings>(Id);
    }

    public bool HasCapability<TCapability>()
        where TCapability : class, IAiCapability
        => Capabilities.OfType<TCapability>().Any();

    public TCapability? GetCapability<TCapability>()
        where TCapability : class, IAiCapability
        => Capabilities.OfType<TCapability>().FirstOrDefault();
}
```

Update DI:

```csharp
services.AddSingleton<IEditorUiAliasResolver, EditorUiAliasResolver>();
services.AddSingleton<IValidationRuleInferrer, ValidationRuleInferrer>();
services.AddSingleton<IAiSettingDefinitionBuilder, AiSettingDefinitionBuilder>();
```

**Files Modified:** 2 (AiProviderBase.cs + UmbracoBuilderExtensions.cs)
**Estimated Effort:** 2 hours

---

### Phase 3: Split AiSettingsResolver Responsibilities

#### 3.1 Create ISettingsDeserializer

```csharp
// File: src/Umbraco.Ai.Core/Settings/ISettingsDeserializer.cs
public interface ISettingsDeserializer
{
    Result<TSettings> Deserialize<TSettings>(object? input) where TSettings : class, new();
}

// File: src/Umbraco.Ai.Core/Settings/SettingsDeserializer.cs
internal sealed class SettingsDeserializer : ISettingsDeserializer
{
    public Result<TSettings> Deserialize<TSettings>(object? input)
        where TSettings : class, new()
    {
        if (input is null)
        {
            return Result<TSettings>.Success(new TSettings());
        }

        if (input is TSettings typed)
        {
            return Result<TSettings>.Success(typed);
        }

        if (input is JsonElement jsonElement)
        {
            try
            {
                var deserialized = jsonElement.Deserialize<TSettings>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (deserialized is null)
                {
                    return Result<TSettings>.Failure("Deserialization returned null.");
                }

                return Result<TSettings>.Success(deserialized);
            }
            catch (JsonException ex)
            {
                return Result<TSettings>.Failure(
                    $"Failed to deserialize settings: {ex.Message}",
                    ex);
            }
        }

        if (input is string jsonString)
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<TSettings>(
                    jsonString,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (deserialized is null)
                {
                    return Result<TSettings>.Failure("Deserialization returned null.");
                }

                return Result<TSettings>.Success(deserialized);
            }
            catch (JsonException ex)
            {
                return Result<TSettings>.Failure(
                    $"Failed to deserialize JSON string: {ex.Message}",
                    ex);
            }
        }

        return Result<TSettings>.Failure(
            $"Cannot deserialize from type {input.GetType().Name}.");
    }
}
```

**Files Created:** 2
**Estimated Effort:** 2 hours

---

#### 3.2 Create IEnvironmentVariableResolver

```csharp
// File: src/Umbraco.Ai.Core/Settings/IEnvironmentVariableResolver.cs
public interface IEnvironmentVariableResolver
{
    void ResolveInObject(object obj);
}

// File: src/Umbraco.Ai.Core/Settings/EnvironmentVariableResolver.cs
internal sealed class EnvironmentVariableResolver : IEnvironmentVariableResolver
{
    private static readonly Regex EnvVarPattern = new(
        @"\$\{([^}]+)\}",
        RegexOptions.Compiled);

    public void ResolveInObject(object obj)
    {
        if (obj is null) return;

        var properties = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);

            if (value is string stringValue)
            {
                var resolved = ResolveString(stringValue);
                if (resolved != stringValue)
                {
                    property.SetValue(obj, resolved);
                }
            }
            else if (value is not null && !property.PropertyType.IsPrimitive)
            {
                ResolveInObject(value);
            }
        }
    }

    private string ResolveString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return EnvVarPattern.Replace(input, match =>
        {
            var varName = match.Groups[1].Value;
            var envValue = Environment.GetEnvironmentVariable(varName);

            if (envValue is null)
            {
                return match.Value;
            }

            return envValue;
        });
    }
}
```

**Files Created:** 2
**Estimated Effort:** 2 hours

---

#### 3.3 Create ISettingsValidator with Specifications

```csharp
// File: src/Umbraco.Ai.Core/Settings/Specifications/ISpecification.cs
public interface ISpecification<T>
{
    Result IsSatisfiedBy(T candidate);
}

// File: src/Umbraco.Ai.Core/Settings/Specifications/SettingValidationSpecification.cs
internal sealed class SettingValidationSpecification<TSettings> : ISpecification<TSettings>
    where TSettings : class
{
    private readonly IReadOnlyList<AiSettingDefinition> _definitions;

    public SettingValidationSpecification(IReadOnlyList<AiSettingDefinition> definitions)
    {
        _definitions = definitions;
    }

    public Result IsSatisfiedBy(TSettings candidate)
    {
        var errors = new List<string>();

        foreach (var definition in _definitions)
        {
            var property = typeof(TSettings).GetProperty(definition.PropertyName);
            if (property is null) continue;

            var value = property.GetValue(candidate);

            // Check required
            if (definition.ValidationRules.Any(r => r.Alias == "required"))
            {
                if (value is null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    errors.Add($"{definition.PropertyName} is required.");
                    continue;
                }
            }

            // Apply other validation rules
            foreach (var rule in definition.ValidationRules)
            {
                var validationResult = ValidateRule(value, rule, definition.PropertyName);
                if (!validationResult.IsSuccess)
                {
                    errors.Add(validationResult.Error!);
                }
            }
        }

        return errors.Any()
            ? Result.Failure(string.Join("; ", errors))
            : Result.Success();
    }

    private Result ValidateRule(object? value, AiValidationRule rule, string propertyName)
    {
        switch (rule.Alias)
        {
            case "stringLength":
                if (value is string str)
                {
                    var maxLength = (int)rule.Parameters["maxLength"];
                    var minLength = rule.Parameters.TryGetValue("minLength", out var min)
                        ? (int)min
                        : 0;

                    if (str.Length < minLength || str.Length > maxLength)
                    {
                        return Result.Failure(rule.Message);
                    }
                }
                break;

            case "range":
                if (value is IComparable comparable)
                {
                    var min = (IComparable)rule.Parameters["min"];
                    var max = (IComparable)rule.Parameters["max"];

                    if (comparable.CompareTo(min) < 0 || comparable.CompareTo(max) > 0)
                    {
                        return Result.Failure(rule.Message);
                    }
                }
                break;

            case "regex":
                if (value is string str2)
                {
                    var pattern = (string)rule.Parameters["pattern"];
                    if (!Regex.IsMatch(str2, pattern))
                    {
                        return Result.Failure(rule.Message);
                    }
                }
                break;
        }

        return Result.Success();
    }
}

// File: src/Umbraco.Ai.Core/Settings/ISettingsValidator.cs
public interface ISettingsValidator
{
    Result Validate<TSettings>(
        TSettings settings,
        IReadOnlyList<AiSettingDefinition> definitions)
        where TSettings : class;
}

// File: src/Umbraco.Ai.Core/Settings/SettingsValidator.cs
internal sealed class SettingsValidator : ISettingsValidator
{
    public Result Validate<TSettings>(
        TSettings settings,
        IReadOnlyList<AiSettingDefinition> definitions)
        where TSettings : class
    {
        var specification = new SettingValidationSpecification<TSettings>(definitions);
        return specification.IsSatisfiedBy(settings);
    }
}
```

**Files Created:** 4
**Estimated Effort:** 3-4 hours

---

#### 3.4 Refactor AiSettingsResolver (Orchestrator)

```csharp
// File: src/Umbraco.Ai.Core/Settings/AiSettingsResolver.cs
internal sealed class AiSettingsResolver : IAiSettingsResolver
{
    private readonly IAiRegistry _registry;
    private readonly ISettingsDeserializer _deserializer;
    private readonly IEnvironmentVariableResolver _envResolver;
    private readonly ISettingsValidator _validator;

    public AiSettingsResolver(
        IAiRegistry registry,
        ISettingsDeserializer deserializer,
        IEnvironmentVariableResolver envResolver,
        ISettingsValidator validator)
    {
        _registry = registry;
        _deserializer = deserializer;
        _envResolver = envResolver;
        _validator = validator;
    }

    public Result<object> ResolveSettingsForProvider(
        IAiProvider provider,
        object? settings)
    {
        var settingsType = provider.GetType().BaseType?.GetGenericArguments().FirstOrDefault();
        if (settingsType is null)
        {
            return Result<object>.Failure("Cannot determine settings type for provider.");
        }

        var method = typeof(AiSettingsResolver)
            .GetMethod(nameof(ResolveSettingsGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(settingsType);

        var result = method.Invoke(this, new[] { provider.Id, settings, provider });
        return (Result<object>)result!;
    }

    private Result<object> ResolveSettingsGeneric<TSettings>(
        string providerId,
        object? settings,
        IAiProvider provider)
        where TSettings : class, new()
    {
        // Step 1: Deserialize
        var deserializeResult = _deserializer.Deserialize<TSettings>(settings);
        if (!deserializeResult.IsSuccess)
        {
            return Result<object>.Failure(deserializeResult.Error!);
        }

        var deserialized = deserializeResult.Value!;

        // Step 2: Resolve environment variables
        _envResolver.ResolveInObject(deserialized);

        // Step 3: Validate
        var definitions = provider.GetSettingDefinitions();
        var validationResult = _validator.Validate(deserialized, definitions);

        if (!validationResult.IsSuccess)
        {
            return Result<object>.Failure(
                $"Settings validation failed for provider '{providerId}': {validationResult.Error}");
        }

        return Result<object>.Success(deserialized);
    }
}
```

Update DI:

```csharp
services.AddSingleton<ISettingsDeserializer, SettingsDeserializer>();
services.AddSingleton<IEnvironmentVariableResolver, EnvironmentVariableResolver>();
services.AddSingleton<ISettingsValidator, SettingsValidator>();
services.AddSingleton<IAiSettingsResolver, AiSettingsResolver>();
```

**Files Modified:** 2
**Estimated Effort:** 3 hours

---

### Phase 4: Eliminate Code Duplication

#### 4.1 Create IConnectionSettingsResolver

**Purpose:** Extract the 63-line duplicate method from both factories.

```csharp
// File: src/Umbraco.Ai.Core/Connections/IConnectionSettingsResolver.cs
public interface IConnectionSettingsResolver
{
    Task<Result<object>> ResolveAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default);
}

// File: src/Umbraco.Ai.Core/Connections/ConnectionSettingsResolver.cs
internal sealed class ConnectionSettingsResolver : IConnectionSettingsResolver
{
    private readonly IAiRegistry _registry;
    private readonly IAiConnectionService _connectionService;
    private readonly IAiSettingsResolver _settingsResolver;

    public ConnectionSettingsResolver(
        IAiRegistry registry,
        IAiConnectionService connectionService,
        IAiSettingsResolver settingsResolver)
    {
        _registry = registry;
        _connectionService = connectionService;
        _settingsResolver = settingsResolver;
    }

    public async Task<Result<object>> ResolveAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default)
    {
        // If no connection ID, return profile settings directly
        if (profile.ConnectionId == Guid.Empty)
        {
            return Result<object>.Success(profile.Model.Settings!);
        }

        // Get connection
        var connection = await _connectionService.GetConnectionAsync(
            profile.ConnectionId,
            cancellationToken);

        if (connection is null)
        {
            return Result<object>.Failure(
                $"Connection '{profile.ConnectionId}' not found.");
        }

        // Check if active
        if (!connection.IsActive)
        {
            return Result<object>.Failure(
                $"Connection '{connection.Name}' (ID: {connection.Id}) is not active.");
        }

        // Validate provider match
        if (!string.Equals(
            connection.ProviderId,
            profile.Model.ProviderId,
            StringComparison.OrdinalIgnoreCase))
        {
            return Result<object>.Failure(
                $"Connection provider '{connection.ProviderId}' does not match " +
                $"profile model provider '{profile.Model.ProviderId}'.");
        }

        // Get provider
        var provider = _registry.GetProvider(connection.ProviderId);
        if (provider is null)
        {
            return Result<object>.Failure(
                $"Provider '{connection.ProviderId}' not found in registry.");
        }

        // Resolve settings
        var settingsResult = _settingsResolver.ResolveSettingsForProvider(
            provider,
            connection.Settings);

        if (!settingsResult.IsSuccess)
        {
            return Result<object>.Failure(
                $"Failed to resolve connection settings: {settingsResult.Error}");
        }

        return Result<object>.Success(settingsResult.Value!);
    }
}
```

**Files Created:** 2
**Estimated Effort:** 2 hours

---

#### 4.2 Refactor AiChatClientFactory

```csharp
// File: src/Umbraco.Ai.Core/Factories/AiChatClientFactory.cs
internal sealed class AiChatClientFactory : IAiChatClientFactory
{
    private readonly IAiRegistry _registry;
    private readonly IConnectionSettingsResolver _connectionSettingsResolver;
    private readonly IEnumerable<IAiChatMiddleware> _middleware;

    public AiChatClientFactory(
        IAiRegistry registry,
        IConnectionSettingsResolver connectionSettingsResolver,
        IEnumerable<IAiChatMiddleware> middleware)
    {
        _registry = registry;
        _connectionSettingsResolver = connectionSettingsResolver;
        _middleware = middleware;
    }

    public async Task<Result<IChatClient>> CreateClientAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Get provider
        var provider = _registry.GetProvider(profile.Model.ProviderId);
        if (provider is null)
        {
            return Result<IChatClient>.Failure(
                $"Provider '{profile.Model.ProviderId}' not found.");
        }

        // Check capability
        var capability = provider.GetCapability<IAiChatCapability>();
        if (capability is null)
        {
            return Result<IChatClient>.Failure(
                $"Provider '{provider.Name}' does not support chat capability.");
        }

        // Resolve settings (NO DUPLICATION!)
        var settingsResult = await _connectionSettingsResolver.ResolveAsync(
            profile,
            cancellationToken);

        if (!settingsResult.IsSuccess)
        {
            return Result<IChatClient>.Failure(settingsResult.Error!);
        }

        // Create client
        var client = capability.CreateClient(settingsResult.Value);

        // Apply middleware
        var orderedMiddleware = _middleware.OrderBy(m => m.Order);
        foreach (var middleware in orderedMiddleware)
        {
            client = middleware.Apply(client);
        }

        return Result<IChatClient>.Success(client);
    }
}
```

**Files Modified:** 1
**Estimated Effort:** 1 hour

---

#### 4.3 Refactor AiEmbeddingGeneratorFactory

```csharp
// File: src/Umbraco.Ai.Core/Factories/AiEmbeddingGeneratorFactory.cs
internal sealed class AiEmbeddingGeneratorFactory : IAiEmbeddingGeneratorFactory
{
    private readonly IAiRegistry _registry;
    private readonly IConnectionSettingsResolver _connectionSettingsResolver;
    private readonly IEnumerable<IAiEmbeddingMiddleware> _middleware;

    public AiEmbeddingGeneratorFactory(
        IAiRegistry registry,
        IConnectionSettingsResolver connectionSettingsResolver,
        IEnumerable<IAiEmbeddingMiddleware> middleware)
    {
        _registry = registry;
        _connectionSettingsResolver = connectionSettingsResolver;
        _middleware = middleware;
    }

    public async Task<Result<IEmbeddingGenerator<string, Embedding<float>>>> CreateGeneratorAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default)
    {
        var provider = _registry.GetProvider(profile.Model.ProviderId);
        if (provider is null)
        {
            return Result<IEmbeddingGenerator<string, Embedding<float>>>.Failure(
                $"Provider '{profile.Model.ProviderId}' not found.");
        }

        var capability = provider.GetCapability<IAiEmbeddingCapability>();
        if (capability is null)
        {
            return Result<IEmbeddingGenerator<string, Embedding<float>>>.Failure(
                $"Provider '{provider.Name}' does not support embedding capability.");
        }

        // Use shared resolver (NO DUPLICATION!)
        var settingsResult = await _connectionSettingsResolver.ResolveAsync(
            profile,
            cancellationToken);

        if (!settingsResult.IsSuccess)
        {
            return Result<IEmbeddingGenerator<string, Embedding<float>>>.Failure(
                settingsResult.Error!);
        }

        var generator = capability.CreateGenerator(settingsResult.Value);

        // Apply middleware
        var orderedMiddleware = _middleware.OrderBy(m => m.Order);
        foreach (var middleware in orderedMiddleware)
        {
            generator = middleware.Apply(generator);
        }

        return Result<IEmbeddingGenerator<string, Embedding<float>>>.Success(generator);
    }
}
```

Update DI:

```csharp
services.AddSingleton<IConnectionSettingsResolver, ConnectionSettingsResolver>();
```

**Files Modified:** 1
**Estimated Effort:** 1 hour

---

#### 4.4 Remove Inconsistent Settings Resolution from Capabilities

**Remove from capability base classes:**

```csharp
// File: src/Umbraco.Ai.Core/Providers/IAiCapability.cs

// DELETE THIS METHOD (lines 117-125):
protected TSettings ResolveSettings(object? settings)
{
    return settings switch
    {
        TSettings typedSettings => typedSettings,
        JsonElement jsonElement => jsonElement.Deserialize<TSettings>()!,
        _ => default!
    };
}
```

**Update capability implementations to receive pre-resolved settings:**

```csharp
public IChatClient CreateClient(object? settings)
{
    // Settings are already resolved by factory
    var typedSettings = (TSettings)settings!;

    // Use settings directly
    // ...
}
```

**Files Modified:** 3+ (capability base + implementations)
**Estimated Effort:** 2 hours

---

### Phase 5: Improve AiConnectionService Separation

#### 5.1 Create IAiConnectionValidator

```csharp
// File: src/Umbraco.Ai.Core/Connections/IAiConnectionValidator.cs
public interface IAiConnectionValidator
{
    Task<Result> ValidateAsync(
        AiConnection connection,
        CancellationToken cancellationToken = default);
}

// File: src/Umbraco.Ai.Core/Connections/AiConnectionValidator.cs
internal sealed class AiConnectionValidator : IAiConnectionValidator
{
    private readonly IAiRegistry _registry;
    private readonly IAiSettingsResolver _settingsResolver;

    public AiConnectionValidator(
        IAiRegistry registry,
        IAiSettingsResolver settingsResolver)
    {
        _registry = registry;
        _settingsResolver = settingsResolver;
    }

    public async Task<Result> ValidateAsync(
        AiConnection connection,
        CancellationToken cancellationToken = default)
    {
        var provider = _registry.GetProvider(connection.ProviderId);
        if (provider is null)
        {
            return Result.Failure(
                $"Provider '{connection.ProviderId}' not found in registry.");
        }

        if (connection.Settings is not null)
        {
            var settingsResult = _settingsResolver.ResolveSettingsForProvider(
                provider,
                connection.Settings);

            if (!settingsResult.IsSuccess)
            {
                return Result.Failure(
                    $"Invalid settings: {settingsResult.Error}");
            }
        }

        return Result.Success();
    }
}
```

**Files Created:** 2
**Estimated Effort:** 1-2 hours

---

#### 5.2 Create IAiConnectionTester

```csharp
// File: src/Umbraco.Ai.Core/Connections/IAiConnectionTester.cs
public interface IAiConnectionTester
{
    Task<Result> TestConnectionAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default);
}

// File: src/Umbraco.Ai.Core/Connections/AiConnectionTester.cs
internal sealed class AiConnectionTester : IAiConnectionTester
{
    private readonly IAiConnectionService _connectionService;
    private readonly IAiRegistry _registry;
    private readonly IAiSettingsResolver _settingsResolver;
    private readonly ILogger<AiConnectionTester> _logger;

    public AiConnectionTester(
        IAiConnectionService connectionService,
        IAiRegistry registry,
        IAiSettingsResolver settingsResolver,
        ILogger<AiConnectionTester> logger)
    {
        _connectionService = connectionService;
        _registry = registry;
        _settingsResolver = settingsResolver;
        _logger = logger;
    }

    public async Task<Result> TestConnectionAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionService.GetConnectionAsync(
            connectionId,
            cancellationToken);

        if (connection is null)
        {
            return Result.Failure($"Connection '{connectionId}' not found.");
        }

        var provider = _registry.GetProvider(connection.ProviderId);
        if (provider is null)
        {
            return Result.Failure(
                $"Provider '{connection.ProviderId}' not found.");
        }

        var settingsResult = _settingsResolver.ResolveSettingsForProvider(
            provider,
            connection.Settings);

        if (!settingsResult.IsSuccess)
        {
            return Result.Failure(
                $"Failed to resolve settings: {settingsResult.Error}");
        }

        // Test with a capability
        var chatCapability = provider.GetCapability<IAiChatCapability>();
        if (chatCapability is not null)
        {
            return await TestChatCapabilityAsync(
                chatCapability,
                settingsResult.Value!,
                cancellationToken);
        }

        var embeddingCapability = provider.GetCapability<IAiEmbeddingCapability>();
        if (embeddingCapability is not null)
        {
            return await TestEmbeddingCapabilityAsync(
                embeddingCapability,
                settingsResult.Value!,
                cancellationToken);
        }

        return Result.Failure("No testable capabilities found for provider.");
    }

    private async Task<Result> TestChatCapabilityAsync(
        IAiChatCapability capability,
        object settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = capability.CreateClient(settings);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "Hello")
            };

            var response = await client.CompleteAsync(
                messages,
                cancellationToken: cancellationToken);

            if (response is null)
            {
                return Result.Failure("Provider returned null response.");
            }

            _logger.LogInformation("Connection test successful.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed.");
            return Result.Failure($"Connection test failed: {ex.Message}");
        }
    }

    private async Task<Result> TestEmbeddingCapabilityAsync(
        IAiEmbeddingCapability capability,
        object settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var generator = capability.CreateGenerator(settings);

            var embedding = await generator.GenerateEmbeddingAsync(
                "test",
                cancellationToken: cancellationToken);

            if (embedding is null || embedding.Vector.Length == 0)
            {
                return Result.Failure("Provider returned invalid embedding.");
            }

            _logger.LogInformation("Connection test successful.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed.");
            return Result.Failure($"Connection test failed: {ex.Message}");
        }
    }
}
```

**Files Created:** 2
**Estimated Effort:** 3-4 hours

---

#### 5.3 Refactor AiConnectionService (Focus on CRUD)

```csharp
// File: src/Umbraco.Ai.Core/Connections/AiConnectionService.cs
internal sealed class AiConnectionService : IAiConnectionService
{
    private readonly IAiConnectionRepository _repository;
    private readonly IAiConnectionValidator _validator;

    public AiConnectionService(
        IAiConnectionRepository repository,
        IAiConnectionValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<AiConnection?> GetConnectionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<AiConnection>> GetAllConnectionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<Result<AiConnection>> SaveConnectionAsync(
        AiConnection connection,
        CancellationToken cancellationToken = default)
    {
        // Generate ID if needed
        if (connection.Id == Guid.Empty)
        {
            connection = new AiConnection
            {
                Id = Guid.NewGuid(),
                Name = connection.Name,
                ProviderId = connection.ProviderId,
                Settings = connection.Settings,
                IsActive = connection.IsActive,
                DateModified = DateTime.UtcNow
            };
        }
        else
        {
            connection.DateModified = DateTime.UtcNow;
        }

        // Validate (delegated)
        var validationResult = await _validator.ValidateAsync(
            connection,
            cancellationToken);

        if (!validationResult.IsSuccess)
        {
            return Result<AiConnection>.Failure(validationResult.Error!);
        }

        // Save
        var saved = await _repository.SaveAsync(connection, cancellationToken);
        return Result<AiConnection>.Success(saved);
    }

    public async Task<Result> DeleteConnectionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // TODO: Check if connection is in use by profiles

        var deleted = await _repository.DeleteAsync(id, cancellationToken);

        return deleted
            ? Result.Success()
            : Result.Failure($"Connection '{id}' not found.");
    }
}
```

Update DI:

```csharp
services.AddSingleton<IAiConnectionValidator, AiConnectionValidator>();
services.AddSingleton<IAiConnectionTester, AiConnectionTester>();
services.AddSingleton<IAiConnectionService, AiConnectionService>();
```

**Files Modified:** 2
**Estimated Effort:** 2 hours

---

### Phase 6: Code Organization & Quality

#### 6.1 Move Utility Classes

**Move files:**
```
src/Umbraco.Ai.Core/StringExtensions.cs
  → src/Umbraco.Ai.Core/Common/Utilities/StringExtensions.cs

src/Umbraco.Ai.Core/TypeExtensions.cs
  → src/Umbraco.Ai.Core/Common/Utilities/TypeExtensions.cs
```

**Update namespaces:**
```csharp
namespace Umbraco.Ai.Core.Common.Utilities;

public static class StringExtensions
{
    // ... existing methods
}
```

**Files Moved:** 2
**Estimated Effort:** 30 minutes

---

#### 6.2 Improve AiConnection Immutability

```csharp
// File: src/Umbraco.Ai.Core/Models/AiConnection.cs
public class AiConnection
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }           // Changed to init
    public required string ProviderId { get; init; }     // Changed to init
    public object? Settings { get; init; }               // Changed to init
    public bool IsActive { get; init; }                  // Changed to init
    public DateTime DateModified { get; init; }          // Changed to init
}
```

**Update service to use `with` expressions:**

```csharp
public async Task<Result<AiConnection>> SaveConnectionAsync(
    AiConnection connection,
    CancellationToken cancellationToken = default)
{
    var toSave = connection with
    {
        Id = connection.Id == Guid.Empty ? Guid.NewGuid() : connection.Id,
        DateModified = DateTime.UtcNow
    };

    var validationResult = await _validator.ValidateAsync(toSave, cancellationToken);
    if (!validationResult.IsSuccess)
    {
        return Result<AiConnection>.Failure(validationResult.Error!);
    }

    var saved = await _repository.SaveAsync(toSave, cancellationToken);
    return Result<AiConnection>.Success(saved);
}
```

**Files Modified:** 2
**Estimated Effort:** 1 hour

---

#### 6.3 Make Capabilities Immutable

```csharp
public abstract class AiProviderBase<TSettings> : IAiProvider
{
    private readonly List<IAiCapability> _capabilities;

    protected AiProviderBase(...)
    {
        _capabilities = new List<IAiCapability>();
    }

    // Expose as readonly
    public IReadOnlyList<IAiCapability> Capabilities => _capabilities.AsReadOnly();

    protected void WithCapability<TCapability>()
        where TCapability : class, IAiCapability
    {
        _capabilities.Add(_capabilityFactory.Create<TCapability>(this));
    }
}
```

**Files Modified:** 1
**Estimated Effort:** 30 minutes

---

#### 6.4 Add Comprehensive XML Documentation

**Example:**

```csharp
/// <summary>
/// Provides methods for resolving AI provider settings from various input formats.
/// </summary>
/// <remarks>
/// This service orchestrates deserialization, environment variable resolution,
/// and validation of settings objects.
/// </remarks>
public interface IAiSettingsResolver
{
    /// <summary>
    /// Resolves settings for a specific provider from an input object.
    /// </summary>
    /// <param name="provider">The provider for which to resolve settings.</param>
    /// <param name="settings">
    /// The input settings object (can be strongly-typed, JsonElement, or null).
    /// </param>
    /// <returns>
    /// A result containing the resolved settings object on success,
    /// or an error message on failure.
    /// </returns>
    Result<object> ResolveSettingsForProvider(IAiProvider provider, object? settings);
}
```

**Files Modified:** All public interfaces and classes
**Estimated Effort:** 4-6 hours

---

### Phase 7: Add Supporting Infrastructure

#### 7.1 Implement Builder Pattern

```csharp
// File: src/Umbraco.Ai.Core/Settings/Builders/AiSettingDefinitionBuilder.cs
public sealed class AiSettingDefinitionBuilder
{
    private string? _key;
    private string? _propertyName;
    private Type? _propertyType;
    private string? _label;
    private string? _description;
    private string? _editorUiAlias;
    private object? _defaultValue;
    private int _sortOrder;
    private readonly List<AiValidationRule> _validationRules = new();

    public AiSettingDefinitionBuilder WithKey(string key)
    {
        _key = key;
        return this;
    }

    public AiSettingDefinitionBuilder WithPropertyName(string propertyName)
    {
        _propertyName = propertyName;
        return this;
    }

    // ... other With methods

    public AiSettingDefinitionBuilder AddValidationRule(AiValidationRule rule)
    {
        _validationRules.Add(rule);
        return this;
    }

    public AiSettingDefinition Build()
    {
        if (string.IsNullOrEmpty(_key))
            throw new InvalidOperationException("Key is required.");
        if (string.IsNullOrEmpty(_propertyName))
            throw new InvalidOperationException("PropertyName is required.");
        if (_propertyType is null)
            throw new InvalidOperationException("PropertyType is required.");

        return new AiSettingDefinition
        {
            Key = _key,
            PropertyName = _propertyName,
            PropertyType = _propertyType,
            Label = _label ?? _propertyName,
            Description = _description ?? string.Empty,
            EditorUiAlias = _editorUiAlias ?? "Umb.PropertyEditorUi.TextBox",
            DefaultValue = _defaultValue,
            SortOrder = _sortOrder,
            ValidationRules = _validationRules.AsReadOnly()
        };
    }
}
```

**Files Created:** 1
**Estimated Effort:** 2 hours

---

#### 7.2 Add Caching Layer

```csharp
// File: src/Umbraco.Ai.Core/Caching/ICacheService.cs
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan expiration);
    void Remove(string key);
}

// File: src/Umbraco.Ai.Core/Caching/MemoryCacheService.cs
internal sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        _cache.Set(key, value, expiration);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}

// Decorator for ConnectionSettingsResolver
// File: src/Umbraco.Ai.Core/Connections/CachedConnectionSettingsResolver.cs
internal sealed class CachedConnectionSettingsResolver : IConnectionSettingsResolver
{
    private readonly IConnectionSettingsResolver _inner;
    private readonly ICacheService _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public CachedConnectionSettingsResolver(
        IConnectionSettingsResolver inner,
        ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Result<object>> ResolveAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"connection-settings:{profile.ConnectionId}";

        var cached = _cache.Get<Result<object>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var result = await _inner.ResolveAsync(profile, cancellationToken);

        if (result.IsSuccess)
        {
            _cache.Set(cacheKey, result, _cacheExpiration);
        }

        return result;
    }
}
```

Update DI with decorator:

```csharp
services.AddSingleton<ICacheService, MemoryCacheService>();

services.AddSingleton<ConnectionSettingsResolver>();
services.AddSingleton<IConnectionSettingsResolver>(sp =>
    new CachedConnectionSettingsResolver(
        sp.GetRequiredService<ConnectionSettingsResolver>(),
        sp.GetRequiredService<ICacheService>()));
```

**Files Created:** 3
**Estimated Effort:** 3 hours

---

#### 7.3 Add Logging Strategy

**Add logging to key services:**

```csharp
internal sealed class AiConnectionService : IAiConnectionService
{
    private readonly IAiConnectionRepository _repository;
    private readonly IAiConnectionValidator _validator;
    private readonly ILogger<AiConnectionService> _logger;

    public AiConnectionService(
        IAiConnectionRepository repository,
        IAiConnectionValidator validator,
        ILogger<AiConnectionService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AiConnection>> SaveConnectionAsync(
        AiConnection connection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Saving connection '{ConnectionName}' (Provider: {ProviderId})",
            connection.Name,
            connection.ProviderId);

        // ... logic

        if (!validationResult.IsSuccess)
        {
            _logger.LogWarning(
                "Connection validation failed: {Error}",
                validationResult.Error);
            return Result<AiConnection>.Failure(validationResult.Error!);
        }

        var saved = await _repository.SaveAsync(toSave, cancellationToken);

        _logger.LogInformation(
            "Connection '{ConnectionName}' saved successfully (ID: {ConnectionId})",
            saved.Name,
            saved.Id);

        return Result<AiConnection>.Success(saved);
    }
}
```

**Files Modified:** 10+ (all major services)
**Estimated Effort:** 3-4 hours

---

## Implementation Phases

### Phase Summary

| Phase | Focus Area | Files Created | Files Modified | Est. Effort |
|-------|-----------|---------------|----------------|-------------|
| **1** | Critical Fixes | 7 | 5 | 8-10 hours |
| **2** | Provider SRP | 6 | 3 | 8-10 hours |
| **3** | Settings SRP | 8 | 2 | 10-12 hours |
| **4** | Code Duplication | 2 | 5 | 6-7 hours |
| **5** | Connection Service | 4 | 2 | 6-8 hours |
| **6** | Organization | 0 | 15+ | 6-8 hours |
| **7** | Infrastructure | 4 | 10+ | 8-10 hours |
| **TOTAL** | | **31** | **42+** | **52-65 hours** |

### Recommended Execution Order

1. **Phase 1 (Critical)** - Must be done first, fixes runtime failures
2. **Phase 3 (Settings)** - Required by later phases
3. **Phase 2 (Provider)** - Depends on Phase 3
4. **Phase 4 (Duplication)** - Can be done in parallel with Phases 2-3
5. **Phase 5 (Connection Service)** - Requires Phase 3
6. **Phase 6 (Organization)** - Can be done anytime, low risk
7. **Phase 7 (Infrastructure)** - Final polish, optional optimizations

---

## Expected Outcomes

### Quantitative Improvements

- **Lines of duplicated code eliminated:** 63+ lines
- **Number of classes following SRP:** +15
- **Test coverage potential:** +40% (due to better separation)
- **Dependency count per class:** -30% average
- **Cyclomatic complexity:** -25% in affected classes

### Qualitative Improvements

1. **Maintainability**
   - Clear single responsibilities
   - Easier to understand and modify
   - Reduced cognitive load

2. **Testability**
   - Isolated concerns can be tested independently
   - No service locator anti-pattern
   - Better mocking capabilities

3. **Extensibility**
   - Easy to add new validators
   - Easy to add new middleware
   - Easy to add new capabilities

4. **Reliability**
   - Result types instead of exceptions
   - Immutable models prevent accidental mutations
   - Consistent error handling

5. **Performance**
   - Caching layer for expensive operations
   - No redundant settings resolution

---

## Risk Assessment

### Low Risk
- Phase 6 (Organization) - Just moving files
- Phase 7 (Infrastructure) - Additive changes only

### Medium Risk
- Phase 2 (Provider) - Touches base classes
- Phase 4 (Duplication) - Factory refactoring
- Phase 5 (Connection Service) - Service splitting

### High Risk
- Phase 1 (Critical) - Affects core abstractions
- Phase 3 (Settings) - Used throughout codebase

**Mitigation:**
- Comprehensive unit tests before refactoring
- Integration tests for critical paths
- Feature flags if needed
- Incremental rollout per phase

---

## Testing Strategy

### Unit Tests Required

Each new service/interface should have:
- Happy path tests
- Error condition tests
- Edge case tests

Example coverage:
- `SettingsDeserializer`: 8-10 test cases
- `EnvironmentVariableResolver`: 6-8 test cases
- `ValidationRuleInferrer`: 10-12 test cases
- `ConnectionSettingsResolver`: 8-10 test cases

### Integration Tests Required

- End-to-end provider registration
- Settings resolution pipeline
- Connection lifecycle (create, validate, test, delete)
- Profile resolution with connections
- Factory client/generator creation

### Regression Tests

- All existing functionality must continue to work
- OpenAI provider must continue to function
- Middleware must continue to apply correctly

---

## Success Criteria

### Phase 1 Success
- [ ] Application starts without errors
- [ ] Profiles can be resolved
- [ ] No service locator in provider base
- [ ] Result types replace exception flow control

### Phase 2 Success
- [ ] `AiProviderBase` has < 100 lines
- [ ] Settings definition building is extracted
- [ ] UI alias resolution is separate
- [ ] Validation inference is separate

### Phase 3 Success
- [ ] Settings resolver orchestrates only
- [ ] Deserialization is separate
- [ ] Environment variable resolution is separate
- [ ] Validation is separate with specifications

### Phase 4 Success
- [ ] Zero duplicated code in factories
- [ ] Shared connection settings resolver
- [ ] Consistent settings resolution everywhere

### Phase 5 Success
- [ ] Connection service does CRUD only
- [ ] Validation is separate
- [ ] Testing is separate

### Phase 6 Success
- [ ] Utilities in proper namespace
- [ ] Immutable models
- [ ] Comprehensive documentation

### Phase 7 Success
- [ ] Caching improves performance
- [ ] Logging provides visibility
- [ ] Builder pattern available for complex objects

---

## Conclusion

This comprehensive refactoring plan addresses all identified architectural issues while maintaining the existing functionality. The phased approach allows for incremental progress with manageable risk at each step.

The result will be a codebase that:
- ✅ Follows SOLID principles
- ✅ Has clear separation of concerns
- ✅ Is highly testable
- ✅ Is maintainable and extensible
- ✅ Uses modern patterns (Result, Specification, Builder)
- ✅ Has no code duplication
- ✅ Has immutable models where appropriate
- ✅ Has consistent error handling
- ✅ Is well-documented

**Estimated Total Effort:** 52-65 hours (6.5-8 working days for one developer)

**Recommended Team Size:** 1-2 developers

**Recommended Timeline:** 2-3 weeks (allowing for testing and review)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-23
**Author:** Claude Code Architectural Analysis
**Status:** Awaiting Approval