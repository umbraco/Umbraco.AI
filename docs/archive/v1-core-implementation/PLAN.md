# Umbraco.AI v1 Core Implementation Plan

## Overview

Complete the v1 core implementation of Umbraco.AI with:

1. Database persistence (EF Core) replacing in-memory repositories
2. Management API endpoints following Umbraco patterns
3. Full CRUD UI for Connections and Profiles with dynamic settings forms
4. Middleware collection builder using Umbraco's collection builder pattern
5. IAIEmbeddingService implementation (Search deferred to v2)

---

## Phase 0: Provider Collection Builder ✅ COMPLETED

Convert provider registration to use Umbraco's collection builder pattern for consistency with middleware and better extensibility.

### Implementation Status: COMPLETE (2025-11-26)

**Completed Changes:**

- ✅ `IAIProvider.cs` - Added `IDiscoverable` marker interface
- ✅ `AIProviderCollection.cs` - Created with `GetById()` and `GetWithCapability<T>()` helpers
- ✅ `AIProviderCollectionBuilder.cs` - Created extending `LazyCollectionBuilderBase`
- ✅ `UmbracoBuilderExtensions.Providers.cs` - Created with `AIProviders()` extension method
- ✅ `UmbracoBuilderExtensions.cs` - Updated to use TypeLoader auto-discovery, removed old `RegisterProviders()` method
- ✅ `AIRegistry.cs` - Updated to inject `AIProviderCollection` instead of `IEnumerable<IAIProvider>`

**Branch:** `feature/phase-0-provider-collection-builder`

### Changes to IAIProvider

**`src/Umbraco.AI.Core/Providers/IAIProvider.cs`** - Add `IDiscoverable` marker:

```csharp
using Umbraco.Cms.Core.Composing;

public interface IAIProvider : IDiscoverable
{
    // ... existing members
}
```

### New Files

**`src/Umbraco.AI.Core/Providers/AIProviderCollection.cs`**

```csharp
public class AIProviderCollection : BuilderCollectionBase<IAIProvider>
{
    public AIProviderCollection(Func<IEnumerable<IAIProvider>> items) : base(items) { }
}
```

**`src/Umbraco.AI.Core/Providers/AIProviderCollectionBuilder.cs`**

```csharp
public class AIProviderCollectionBuilder
    : LazyCollectionBuilderBase<AIProviderCollectionBuilder, AIProviderCollection, IAIProvider>
{
    protected override AIProviderCollectionBuilder This => this;
}
```

**`src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.Providers.cs`**

```csharp
public static partial class UmbracoBuilderExtensions
{
    public static AIProviderCollectionBuilder AIProviders(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIProviderCollectionBuilder>();
}
```

### Auto-Discovery Registration

In the core `AddUmbracoAI()` extension method, register a producer that uses TypeLoader:

```csharp
// In UmbracoBuilderExtensions.cs
public static IUmbracoBuilder AddUmbracoAI(this IUmbracoBuilder builder)
{
    // Auto-discover providers using TypeLoader
    builder.AIProviders()
        .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIProvider, AIProviderAttribute>(true));

    // ... rest of registration
}
```

### Benefits

- **Consistency**: Same pattern as middleware collection builders
- **TypeLoader caching**: Uses Umbraco's cached, efficient type discovery
- **Extensibility**: Providers can be added/excluded via Composers

### Usage Example

```csharp
// In a Composer - add or exclude providers
builder.AIProviders()
    .Add<CustomProvider>()
    .Exclude<SomeUnwantedProvider>();
```

### Modified Files

- `src/Umbraco.AI.Core/Providers/IAIProvider.cs` - Add `: IDiscoverable`
- `src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` - Initialize provider collection, remove old `RegisterProviders()` method
- `src/Umbraco.AI.Core/Registry/AIRegistry.cs` - Inject `AIProviderCollection` instead of `IEnumerable<IAIProvider>`

---

## Phase 1: Middleware Collection Builder ✅ COMPLETED

Convert middleware registration to use Umbraco's **OrderedCollectionBuilder** pattern (not weighted) to support explicit ordering with `InsertBefore`/`InsertAfter` methods.

### Implementation Status: COMPLETE (2025-11-26)

**Completed Changes:**

- ✅ `AIChatMiddlewareCollection.cs` - Created extending `BuilderCollectionBase<IAIChatMiddleware>`
- ✅ `AIChatMiddlewareCollectionBuilder.cs` - Created extending `OrderedCollectionBuilderBase`
- ✅ `AIEmbeddingMiddlewareCollection.cs` - Created extending `BuilderCollectionBase<IAIEmbeddingMiddleware>`
- ✅ `AIEmbeddingMiddlewareCollectionBuilder.cs` - Created extending `OrderedCollectionBuilderBase`
- ✅ `UmbracoBuilderExtensions.Collections.cs` - Created with `AIChatMiddleware()` and `AIEmbeddingMiddleware()` extension methods
- ✅ `IAIChatMiddleware.cs` - Removed `Order` property (ordering now managed by collection builder)
- ✅ `IAIEmbeddingMiddleware.cs` - Removed `Order` property (ordering now managed by collection builder)
- ✅ `AIChatClientFactory.cs` - Updated to inject `AIChatMiddlewareCollection`
- ✅ `AIEmbeddingGeneratorFactory.cs` - Updated to inject `AIEmbeddingMiddlewareCollection`
- ✅ `UmbracoBuilderExtensions.cs` - Updated to initialize middleware collections
- ✅ `AIMiddlewareExtensions.cs` - Deleted (replaced by collection builders)
- ✅ `LoggingChatMiddleware.cs` - Updated example to remove `Order` property

**Branch:** `feature/phase-1-middleware-collection-builder`

### New Files

**`src/Umbraco.AI.Core/Middleware/AIChatMiddlewareCollection.cs`**

```csharp
public class AIChatMiddlewareCollection : BuilderCollectionBase<IAIChatMiddleware>
{
    public AIChatMiddlewareCollection(Func<IEnumerable<IAIChatMiddleware>> items) : base(items) { }
}
```

**`src/Umbraco.AI.Core/Middleware/AIChatMiddlewareCollectionBuilder.cs`**

```csharp
public class AIChatMiddlewareCollectionBuilder
    : OrderedCollectionBuilderBase<AIChatMiddlewareCollectionBuilder, AIChatMiddlewareCollection, IAIChatMiddleware>
{
    protected override AIChatMiddlewareCollectionBuilder This => this;
}
```

**`src/Umbraco.AI.Core/Middleware/AIEmbeddingMiddlewareCollection.cs`** - Same pattern

**`src/Umbraco.AI.Core/Middleware/AIEmbeddingMiddlewareCollectionBuilder.cs`** - Same pattern

**`src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.Collections.cs`**

```csharp
public static partial class UmbracoBuilderExtensions
{
    public static AIChatMiddlewareCollectionBuilder AIChatMiddleware(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIChatMiddlewareCollectionBuilder>();

    public static AIEmbeddingMiddlewareCollectionBuilder AIEmbeddingMiddleware(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIEmbeddingMiddlewareCollectionBuilder>();
}
```

### Modified Files

- `src/Umbraco.AI.Core/Factories/AIChatClientFactory.cs` - Inject `AIChatMiddlewareCollection` instead of `IEnumerable<IAIChatMiddleware>`
- `src/Umbraco.AI.Core/Factories/AIEmbeddingGeneratorFactory.cs` - Same pattern
- `src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` - Initialize collections, remove old middleware extension methods
- Delete `src/Umbraco.AI.Core/Middleware/AIMiddlewareExtensions.cs` (replaced by collection builders)
- **Remove `Order` property from `IAIChatMiddleware` and `IAIEmbeddingMiddleware`** - ordering managed by collection builder

### Usage Example

```csharp
// In a Composer - OrderedCollectionBuilder API
builder.AIChatMiddleware()
    .Append<LoggingChatMiddleware>()
    .Append<CachingMiddleware>()
    .InsertBefore<LoggingChatMiddleware, TracingMiddleware>()  // Tracing runs before Logging
    .Remove<SomeUnwantedMiddleware>();
```

---

## Phase 2: IAIEmbeddingService Implementation ✅ COMPLETED

### Implementation Status: COMPLETE (2025-11-26)

**Completed Changes:**

- ✅ `IAIEmbeddingService.cs` - Created interface with 5 methods for single/batch embedding generation and direct generator access
- ✅ `AIEmbeddingService.cs` - Created internal implementation following `AIChatService` pattern
- ✅ `UmbracoBuilderExtensions.cs` - Registered `IAIEmbeddingService` in DI container
- ✅ `FakeEmbeddingCapability.cs` - Enhanced `FakeEmbeddingGenerator` to track received values/options for testing
- ✅ `AIEmbeddingServiceTests.cs` - Created 16 comprehensive unit tests

**Branch:** `feature/phase-2-embedding-service`

### New Files

**`src/Umbraco.AI.Core/Services/IAIEmbeddingService.cs`**

```csharp
public interface IAIEmbeddingService
{
    // Single value - default profile
    Task<Embedding<float>> GenerateEmbeddingAsync(
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    // Single value - specific profile
    Task<Embedding<float>> GenerateEmbeddingAsync(
        Guid profileId,
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    // Multiple values - default profile
    Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    // Multiple values - specific profile
    Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        Guid profileId,
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    // Direct generator access for advanced scenarios
    Task<IEmbeddingGenerator<string, Embedding<float>>> GetEmbeddingGeneratorAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default);
}
```

**`src/Umbraco.AI.Core/Services/AIEmbeddingService.cs`**

- Inject `IAIEmbeddingGeneratorFactory`, `IAIProfileService`, `IOptionsMonitor<AIOptions>`
- Resolve default profile via `IAIProfileService.GetDefaultProfileAsync(AICapability.Embedding, ...)`
- Delegate to factory for generator creation
- Options merging: caller options override profile defaults

### Modified Files

- `src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` - Register `IAIEmbeddingService`
- `tests/Umbraco.AI.Tests.Common/Fakes/FakeEmbeddingCapability.cs` - Enhanced `FakeEmbeddingGenerator` with `ReceivedValues` and `ReceivedOptions` tracking

### Tests Implemented

**Unit Tests (`tests/Umbraco.AI.Tests.Unit/Services/AIEmbeddingServiceTests.cs`):**

16 tests covering:

- ✅ Default embedding profile resolution
- ✅ Named profile resolution by ID
- ✅ Error handling: profile not found (`InvalidOperationException`)
- ✅ Error handling: profile has wrong capability (Chat instead of Embedding)
- ✅ Options merging when caller provides custom `EmbeddingGenerationOptions`
- ✅ Multiple values returning embeddings for each value
- ✅ Direct generator access via `GetEmbeddingGeneratorAsync`

---

## Phase 3: Management API Endpoints ✅ COMPLETED

### Implementation Status: COMPLETE (2025-11-26)

**Completed Changes:**

- ✅ `UmbracoAIApiRouteAttribute.cs` - Custom route attribute for `/umbraco/ai/management/api/v1`
- ✅ `AIManagementControllerBase.cs` - Base controller with OpenAPI tags
- ✅ Connection controllers (All, ById, Create, Update, Delete, Test)
- ✅ Profile controllers (All, ById, ByAlias, Create, Update, Delete)
- ✅ Provider controllers (All, ById, Models)
- ✅ Request/response models with `UmbracoMapper` integration
- ✅ Unit tests for all controller actions

**Branch:** `feature/management-api`

### File Structure

```
src/Umbraco.AI.Web/Api/Management/
├── Routing/
│   └── UmbracoAIApiRouteAttribute.cs
├── Controllers/
│   └── AIManagementControllerBase.cs
├── Connection/
│   ├── Controllers/
│   │   ├── ConnectionControllerBase.cs
│   │   ├── AllConnectionController.cs
│   │   ├── ByIdConnectionController.cs
│   │   ├── CreateConnectionController.cs
│   │   ├── UpdateConnectionController.cs
│   │   ├── DeleteConnectionController.cs
│   │   └── TestConnectionController.cs
│   └── Models/
│       ├── ConnectionResponseModel.cs
│       ├── ConnectionItemResponseModel.cs
│       ├── CreateConnectionRequestModel.cs
│       ├── UpdateConnectionRequestModel.cs
│       └── ConnectionTestResultModel.cs
├── Profile/
│   ├── Controllers/
│   │   ├── ProfileControllerBase.cs
│   │   ├── AllProfileController.cs
│   │   ├── ByIdProfileController.cs
│   │   ├── ByAliasProfileController.cs
│   │   ├── CreateProfileController.cs
│   │   ├── UpdateProfileController.cs
│   │   └── DeleteProfileController.cs
│   └── Models/
│       ├── ProfileResponseModel.cs
│       ├── CreateProfileRequestModel.cs
│       └── UpdateProfileRequestModel.cs
├── Provider/
│   ├── Controllers/
│   │   ├── ProviderControllerBase.cs
│   │   ├── AllProviderController.cs
│   │   ├── ByIdProviderController.cs
│   │   └── ModelsByProviderController.cs
│   └── Models/
│       ├── ProviderResponseModel.cs
│       ├── ProviderDetailResponseModel.cs
│       ├── SettingDefinitionModel.cs
│       └── ModelDescriptorResponseModel.cs
├── Embedding/
│   ├── Controllers/
│   │   └── GenerateEmbeddingController.cs
│   └── Models/
│       ├── GenerateEmbeddingRequestModel.cs
│       └── EmbeddingResponseModel.cs
├── Chat/
│   ├── Controllers/
│   │   ├── ChatControllerBase.cs
│   │   ├── CompleteChatController.cs
│   │   └── StreamChatController.cs        # SSE streaming endpoint
│   └── Models/
│       ├── ChatRequestModel.cs
│       ├── ChatResponseModel.cs
│       └── ChatStreamChunkModel.cs
└── Common/
    ├── ModelRefModel.cs
    └── OperationStatus/
        ├── ConnectionOperationStatus.cs
        └── ProfileOperationStatus.cs
```

### Endpoint Summary

| Resource   | Method | Route                    | Description                        |
| ---------- | ------ | ------------------------ | ---------------------------------- |
| Connection | GET    | `/connection`            | List all connections               |
| Connection | GET    | `/connection/{id}`       | Get by ID                          |
| Connection | POST   | `/connection`            | Create                             |
| Connection | PUT    | `/connection/{id}`       | Update                             |
| Connection | DELETE | `/connection/{id}`       | Delete                             |
| Connection | POST   | `/connection/{id}/test`  | Test connection                    |
| Profile    | GET    | `/profile`               | List all (filter by capability)    |
| Profile    | GET    | `/profile/{id}`          | Get by ID                          |
| Profile    | GET    | `/profile/alias/{alias}` | Get by alias                       |
| Profile    | POST   | `/profile`               | Create                             |
| Profile    | PUT    | `/profile/{id}`          | Update                             |
| Profile    | DELETE | `/profile/{id}`          | Delete                             |
| Provider   | GET    | `/provider`              | List all providers                 |
| Provider   | GET    | `/provider/{id}`         | Get with settings schema           |
| Provider   | GET    | `/provider/{id}/models`  | Get available models               |
| Embedding  | POST   | `/embedding/generate`    | Generate embeddings                |
| Chat       | POST   | `/chat/complete`         | Chat completion (non-streaming)    |
| Chat       | POST   | `/chat/stream`           | Chat completion with SSE streaming |

### Testing Requirements

**Unit Tests (`tests/Umbraco.AI.Tests.Unit/Api/`):**

For each controller, test the critical request/response mapping and validation:

**Connection Controllers:**

- `AllConnectionController` - Returns paginated list
- `ByIdConnectionController` - Returns 404 when not found
- `CreateConnectionController` - Validates required fields, returns created entity with ID
- `UpdateConnectionController` - Returns 404 when not found
- `DeleteConnectionController` - Returns 404 when not found, handles in-use connections
- `TestConnectionController` - Returns test result with success/failure status

**Profile Controllers:**

- `AllProfileController` - Filters by capability query param
- `ByIdProfileController` - Returns 404 when not found
- `ByAliasProfileController` - Returns 404 when alias not found
- `CreateProfileController` - Validates required fields, alias uniqueness
- `UpdateProfileController` - Returns 404 when not found
- `DeleteProfileController` - Returns 404 when not found

**Provider Controllers:**

- `AllProviderController` - Returns all registered providers
- `ByIdProviderController` - Returns settings schema, 404 when not found
- `ModelsByProviderController` - Returns available models for provider

**Embedding Controller:**

- `GenerateEmbeddingController` - Validates input, returns embeddings array

**Chat Controllers:**

- `CompleteChatController` - Validates messages array, returns response
- `StreamChatController` - Returns SSE stream (integration test preferred)

**Integration Tests (`tests/Umbraco.AI.Tests.Integration/Api/`):**

Create integration tests for full HTTP request/response cycles:

- Connection CRUD workflow (create → read → update → delete)
- Profile CRUD workflow with connection dependency
- Test connection with fake provider
- Generate embeddings with fake provider

---

## Phase 4: EF Core Database Persistence

### Project Structure (Simplified - Using Umbraco's Provider Detection)

Since we're using Umbraco's `UseUmbracoDatabaseProvider()` extension method, we can simplify to a **2-tier structure** with shared core and provider-specific migration assemblies:

```
src/Umbraco.AI.Persistence/           (SHARED CORE)
├── Entities/
│   ├── AIConnectionEntity.cs
│   └── AIProfileEntity.cs
├── Repositories/
│   ├── EFCoreAIConnectionRepository.cs
│   └── EFCoreAIProfileRepository.cs
├── Notifications/
│   └── RunAIMigrationNotificationHandler.cs
├── Extensions/
│   └── UmbracoBuilderExtensions.cs
├── Composers/
│   └── UmbracoAIPersistenceComposer.cs
└── UmbracoAIDbContext.cs

src/Umbraco.AI.Persistence.SqlServer/  (SQL Server migrations only)
├── Migrations/
│   ├── 20251125_InitialCreate.cs
│   └── UmbracoAIDbContextModelSnapshot.cs
└── UmbracoAISqlServerComposer.cs

src/Umbraco.AI.Persistence.Sqlite/     (SQLite migrations only)
├── Migrations/
│   ├── 20251125_InitialCreate.cs
│   └── UmbracoAIDbContextModelSnapshot.cs
└── UmbracoAISqliteComposer.cs
```

### Connection String Strategy

Uses Umbraco's database connection via `UseUmbracoDatabaseProvider()`. AI tables are stored in the same database as Umbraco.

### Registration

**`src/Umbraco.AI.Persistence/Extensions/UmbracoBuilderExtensions.cs`:**

```csharp
public static IUmbracoBuilder AddUmbracoAIPersistence(this IUmbracoBuilder builder)
{
    builder.Services.AddUmbracoDbContext<UmbracoAIDbContext>((serviceProvider, options) =>
    {
        options.UseUmbracoDatabaseProvider(serviceProvider);
    });

    // Replace in-memory repositories with EF Core implementations
    builder.Services.AddScoped<IAIConnectionRepository, EFCoreAIConnectionRepository>();
    builder.Services.AddScoped<IAIProfileRepository, EFCoreAIProfileRepository>();

    return builder;
}
```

### Project References

```
Umbraco.AI.Persistence
└── References: Umbraco.AI.Core, Microsoft.EntityFrameworkCore,
                Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Sqlite

Umbraco.AI.Persistence.SqlServer
└── References: Umbraco.AI.Persistence

Umbraco.AI.Persistence.Sqlite
└── References: Umbraco.AI.Persistence
```

**Note**: The shared project references both EF Core providers to use `UseDatabaseProvider()`. The provider-specific projects only contain migrations.

### DbContext Setup (Following Umbraco Patterns)

**UmbracoAIDbContext.cs:**

```csharp
public class UmbracoAIDbContext : DbContext
{
    public required DbSet<AIConnectionEntity> Connections { get; set; }
    public required DbSet<AIProfileEntity> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AIConnectionEntity>(entity =>
        {
            entity.ToTable("umbracoAIConnection");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ProviderId).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.ProviderId);
        });

        modelBuilder.Entity<AIProfileEntity>(entity =>
        {
            entity.ToTable("umbracoAIProfile");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Alias).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Alias).IsUnique();
            entity.HasOne<AIConnectionEntity>()
                  .WithMany()
                  .HasForeignKey(e => e.ConnectionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### Auto-Migration on Startup

**RunAIMigrationNotificationHandler.cs:**

```csharp
public class RunAIMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly UmbracoAIDbContext _dbContext;

    public RunAIMigrationNotificationHandler(UmbracoAIDbContext dbContext)
        => _dbContext = dbContext;

    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
```

### Repository Pattern with IEFCoreScopeProvider

**EFCoreAIConnectionRepository.cs:**

```csharp
public class EFCoreAIConnectionRepository : IAIConnectionRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EFCoreAIConnectionRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    public async Task<AIConnection?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var scope = _scopeProvider.CreateScope();
        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.FirstOrDefaultAsync(c => c.Id == id, ct));
        scope.Complete();
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<AIConnection> SaveAsync(AIConnection connection, CancellationToken ct = default)
    {
        using var scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Connections.FindAsync([connection.Id], ct);
            if (existing is null)
            {
                db.Connections.Add(MapToEntity(connection));
            }
            else
            {
                UpdateEntity(existing, connection);
            }
            await db.SaveChangesAsync(ct);
        });
        scope.Complete();
        return connection;
    }

    // ... other methods
}
```

### Entity Definitions

**AIConnectionEntity:**

- `Id` (Guid, PK)
- `Name` (string, required, max 255)
- `ProviderId` (string, required, max 100, indexed)
- `SettingsJson` (string, nullable) - JSON serialized settings
- `IsActive` (bool)
- `DateCreated` (DateTime)
- `DateModified` (DateTime)

**AIProfileEntity:**

- `Id` (Guid, PK)
- `Alias` (string, required, max 100, unique index)
- `Name` (string, required, max 255)
- `Capability` (int) - enum stored as int
- `ProviderId` (string, required)
- `ModelId` (string, required)
- `ConnectionId` (Guid, FK to Connection)
- `Temperature` (float?)
- `MaxTokens` (int?)
- `SystemPromptTemplate` (string?)
- `TagsJson` (string?) - JSON array

### Migrations

Generate migrations per database provider (context lives in shared project, migrations in provider project):

```bash
# SQL Server
dotnet ef migrations add InitialCreate \
  --context UmbracoAIDbContext \
  --project src/Umbraco.AI.Persistence.SqlServer \
  --startup-project src/Umbraco.AI.DemoSite

# SQLite
dotnet ef migrations add InitialCreate \
  --context UmbracoAIDbContext \
  --project src/Umbraco.AI.Persistence.Sqlite \
  --startup-project src/Umbraco.AI.DemoSite
```

The `MigrationsAssembly()` call in each provider's setup ensures EF Core looks for migrations in the correct provider-specific assembly.

Migrations auto-apply on startup via `RunAIMigrationNotificationHandler`.

### Testing Requirements

**Unit Tests (`tests/Umbraco.AI.Tests.Unit/Repositories/`):**

Test repository methods with in-memory SQLite:

**EFCoreAIConnectionRepository:**

- `GetAsync` - Returns null when not found
- `GetAllAsync` - Returns empty list when no data
- `SaveAsync` - Creates new entity (insert)
- `SaveAsync` - Updates existing entity (upsert)
- `DeleteAsync` - Removes entity, returns true
- `DeleteAsync` - Returns false when not found
- Settings JSON serialization/deserialization

**EFCoreAIProfileRepository:**

- `GetByIdAsync` - Returns null when not found
- `GetByAliasAsync` - Case-insensitive alias lookup
- `GetAllAsync` - Filters by capability
- `SaveAsync` - Enforces unique alias constraint
- `DeleteAsync` - Handles foreign key constraint (connection reference)
- Tags JSON serialization/deserialization

**Migration Tests:**

- Verify migrations apply cleanly to empty database
- Verify schema matches entity configuration

**Test Fixture (`tests/Umbraco.AI.Tests.Common/Fixtures/EFCoreTestFixture.cs`):**

```csharp
public class EFCoreTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public UmbracoAIDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UmbracoAIDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new UmbracoAIDbContext(options);
    }

    public EFCoreTestFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();
}
```

Usage in tests:

```csharp
public class EFCoreAIConnectionRepositoryTests : IClassFixture<EFCoreTestFixture>
{
    private readonly EFCoreTestFixture _fixture;

    public EFCoreAIConnectionRepositoryTests(EFCoreTestFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task GetAsync_WhenNotFound_ReturnsNull()
    {
        await using var context = _fixture.CreateContext();
        var scopeProvider = CreateScopeProvider(context);
        var repository = new EFCoreAIConnectionRepository(scopeProvider);

        var result = await repository.GetAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }
}
```

---

## Phase 5: Frontend UI Implementation

### Location in Backoffice

UI lives in the **Settings section** with a new **AI group**:

- Settings → AI → Connections (collection view → workspace)
- Settings → AI → Profiles (collection view → workspace)

Each section shows a table collection view. Clicking a row opens the entity workspace editor. "Create" button in collection header and menu item context actions.

### Directory Structure (Following Umbraco Commerce Patterns)

```
Client/src/
├── api/                              # Regenerated from OpenAPI
├── connections/
│   ├── collection/
│   │   ├── action/
│   │   │   └── manifests.ts          # Create button action
│   │   ├── views/
│   │   │   ├── table/
│   │   │   │   └── connection-table-collection-view.element.ts
│   │   │   └── manifests.ts
│   │   └── manifests.ts
│   ├── entity-action/
│   │   └── manifests.ts              # Delete action
│   ├── menu/
│   │   └── manifests.ts              # Sidebar menu item
│   ├── repository/
│   │   ├── connection.repository.ts
│   │   ├── connection.server.data-source.ts
│   │   ├── connection.store.ts
│   │   └── manifests.ts
│   ├── workspace/
│   │   ├── collection/
│   │   │   ├── connections-workspace.context.ts
│   │   │   ├── connections-workspace-collection.element.ts
│   │   │   └── manifests.ts
│   │   ├── entity/
│   │   │   ├── connection-workspace.context.ts
│   │   │   ├── connection-workspace-editor.element.ts
│   │   │   ├── views/
│   │   │   │   └── connection-details-workspace-view.element.ts
│   │   │   └── manifests.ts
│   │   └── manifests.ts
│   ├── constants.ts
│   ├── types.ts
│   ├── type-mapper.ts
│   └── manifests.ts                  # Root aggregator
├── profiles/                         # Same structure as connections
│   ├── collection/
│   ├── entity-action/
│   ├── menu/
│   ├── repository/
│   ├── workspace/
│   ├── constants.ts
│   ├── types.ts
│   ├── type-mapper.ts
│   └── manifests.ts
├── providers/
│   ├── repository/
│   │   ├── provider.repository.ts
│   │   ├── provider.server.data-source.ts
│   │   └── manifests.ts
│   ├── constants.ts
│   ├── types.ts
│   └── manifests.ts
├── shared/
│   └── components/
│       ├── ai-dynamic-settings-form/
│       │   └── ai-dynamic-settings-form.element.ts
│       ├── ai-provider-picker/
│       ├── ai-connection-picker/
│       └── ai-model-picker/
└── manifests.ts                      # Root manifest aggregator
```

### Key Patterns (from Umbraco Commerce)

**Constants Pattern (`constants.ts`):**

```typescript
export const AIConnectionConstants = {
    EntityType: {
        Collection: "ai:connections",
        Entity: "ai:connection",
    },
    Icon: {
        Collection: "icon-plug",
        Entity: "icon-plug",
    },
    Workspace: {
        Collection: "UmbracoAI.Workspace.Connections",
        Entity: "UmbracoAI.Workspace.Connection",
    },
    Store: "UmbracoAI.Store.Connection",
    Repository: "UmbracoAI.Repository.Connection",
    Collection: "UmbracoAI.Collection.Connection",
};
```

**Three-Layer Repository Pattern:**

1. **ServerDataSource** - HTTP client wrapper using generated API client
2. **Store** - In-memory state with `UmbArrayState`
3. **Repository** - Orchestrates store + data source with debouncing, caching, observables

**Type Mapper Pattern (`type-mapper.ts`):**

```typescript
export class AIConnectionTypeMapper {
    static responseToViewModel(dto: ConnectionResponseModel): AIConnectionModel { ... }
    static viewToCollectionModel(dto: ConnectionResponseModel): AIConnectionCollectionModel { ... }
    static viewToEditModel(dto: ConnectionResponseModel): AIConnectionEditModel { ... }
    static editToCreateRequest(model: AIConnectionEditModel): CreateConnectionRequestModel { ... }
    static editToUpdateRequest(model: AIConnectionEditModel): UpdateConnectionRequestModel { ... }
}
```

**Manifest Aggregation Pattern:**

```typescript
// connections/manifests.ts
import { manifests as collectionManifests } from "./collection/manifests.js";
import { manifests as menuManifests } from "./menu/manifests.js";
import { manifests as repositoryManifests } from "./repository/manifests.js";
import { manifests as workspaceManifests } from "./workspace/manifests.js";
import { manifests as entityActionManifests } from "./entity-action/manifests.js";

export const manifests: UmbExtensionManifest[] = [
    ...collectionManifests,
    ...menuManifests,
    ...repositoryManifests,
    ...workspaceManifests,
    ...entityActionManifests,
];
```

### Workspace Context Pattern

**Entity Workspace Context** (for create/edit):

```typescript
export class AIConnectionWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<AIConnectionEditModel>
    implements UmbSubmittableWorkspaceContext, UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    constructor(host: UmbControllerHost) {
        super(host, AIConnectionConstants.Workspace.Entity);

        this.routes.setRoutes([
            {
                path: "create",
                component: AIConnectionWorkspaceEditorElement,
                setup: async () => {
                    await this.scaffold();
                },
            },
            {
                path: ":unique",
                component: AIConnectionWorkspaceEditorElement,
                setup: async (_component, info) => {
                    await this.load(info.match.params.unique);
                },
            },
        ]);
    }

    async scaffold() {
        /* Create empty model */
    }
    async load(id: string) {
        /* Fetch from repository */
    }
    async submit() {
        /* Create or update via repository */
    }
}
```

### Key Components

**connection-details-workspace-view.element.ts:**

- Provider picker (dropdown) - loads settings schema on change
- Dynamic settings form using `<umb-property>` components
- Active toggle
- Validation feedback

**profile-details-workspace-view.element.ts:**

- Name/Alias fields (alias auto-generated from name)
- Capability dropdown (Chat/Embedding)
- Connection picker
- Model picker (filtered by connection's provider and capability)
- Temperature slider, MaxTokens input, SystemPrompt textarea
- Tags input

**ai-dynamic-settings-form.element.ts:**

- Receives `settingDefinitions` array from provider settings schema
- Dynamically renders `<umb-property>` for each setting based on `EditorUiAlias`
- Emits change events with updated values

### Testing Requirements

**No automated tests required for v1.**

Rationale: Frontend tests (Playwright/WebdriverIO) add significant complexity and maintenance overhead. For v1, rely on manual QA testing of the UI. Consider adding E2E tests in v2 once the UI patterns stabilize.

**Manual Test Checklist:**

- Connections collection view loads and displays data
- Connection create/edit workspace saves correctly
- Connection delete shows confirmation and removes entity
- Profiles collection view loads with capability filter
- Profile create/edit workspace validates required fields
- Profile delete handles connection dependency warning
- Dynamic settings form renders based on provider schema
- Model picker filters by provider and capability

---

## Implementation Order

### Week 1: Core Backend

1. **Middleware Collection Builder** (Phase 1)
    - Create collection and builder classes
    - Update factories
    - Update DI registration

2. **IAIEmbeddingService** (Phase 2)
    - Interface and implementation
    - Register in DI

3. **Management API - Providers** (Phase 3 partial)
    - Provider controllers (read-only)
    - Settings schema endpoint

### Week 2: Full API

4. **Management API - Connections** (Phase 3)
    - All connection endpoints
    - Test connection functionality

5. **Management API - Profiles** (Phase 3)
    - All profile endpoints
    - Alias lookup

6. **Management API - Embeddings** (Phase 3)
    - Generate embeddings endpoint

### Week 3: Persistence

7. **EF Core Persistence** (Phase 4)
    - Create new project
    - Entities and DbContext
    - Repository implementations
    - Migrations for SqlServer/Sqlite

### Week 4: Frontend

8. **Frontend Foundation** (Phase 5)
    - Regenerate API client
    - Shared components (dynamic settings form, pickers)
    - Repository/store pattern

9. **Connections UI** (Phase 5)
    - Collection view
    - Workspace (create/edit)
    - Delete modal

10. **Profiles UI** (Phase 5)
    - Collection view
    - Workspace (create/edit)
    - Delete modal

11. **Dashboard & Navigation** (Phase 5)
    - AI Dashboard
    - Settings section integration

---

## Critical Files to Read Before Implementation

### Umbraco.AI (Current)

- `src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` - Central DI registration
- `src/Umbraco.AI.Core/Middleware/IAIChatMiddleware.cs` - Current middleware interface
- `src/Umbraco.AI.Core/Connections/IAIConnectionRepository.cs` - Repository interface
- `src/Umbraco.AI.Core/Profiles/IAIProfileRepository.cs` - Repository interface
- `src/Umbraco.AI.Core/Services/AIChatService.cs` - Pattern for embedding service
- `src/Umbraco.AI.Core/Factories/AIChatClientFactory.cs` - Middleware application pattern

### Umbraco CMS (Reference)

- `src/Umbraco.Core/Composing/WeightedCollectionBuilderBase.cs` - Collection builder pattern
- `src/Umbraco.Core/Composing/OrderedCollectionBuilderBase.cs` - Ordered collection builder
- `src/Umbraco.Core/Composing/BuilderCollectionBase.cs` - Collection base class
- `src/Umbraco.Core/Composing/TypeLoader.cs` - Type discovery with caching
- `src/Umbraco.Core/Composing/ITypeFinder.cs` - Type finder interface
- `src/Umbraco.Core/Composing/IDiscoverable.cs` - Marker interface for discoverable types
- `src/Umbraco.Cms.Api.Management/Controllers/` - Controller patterns
- `src/Umbraco.Web.UI.Client/src/packages/core/property/` - Dynamic property rendering

### Umbraco CMS EF Core (Persistence Reference)

- `src/Umbraco.Cms.Persistence.EFCore/UmbracoDbContext.cs` - Single DbContext for all providers
- `src/Umbraco.Cms.Persistence.EFCore/Migrations/IMigrationProviderSetup.cs` - Provider setup interface
- `src/Umbraco.Cms.Persistence.EFCore/Scoping/EFCoreScopeProvider.cs` - Scope provider pattern
- `src/Umbraco.Cms.Persistence.EFCore.SqlServer/SqlServerMigrationProviderSetup.cs` - SQL Server implementation
- `src/Umbraco.Cms.Persistence.EFCore.Sqlite/SqliteMigrationProviderSetup.cs` - SQLite implementation
- `src/Umbraco.Cms.Persistence.EFCore.SqlServer/Migrations/` - SQL Server-specific migrations
- `src/Umbraco.Cms.Persistence.EFCore.Sqlite/Migrations/` - SQLite-specific migrations

### Umbraco Commerce (Frontend Reference)

- `src/Umbraco.Commerce.Cms.Web.StaticAssets/Client/src/location/` - Complete example of:
    - Collection views with table display
    - Entity workspace with create/edit routes
    - Three-layer repository pattern (ServerDataSource → Store → Repository)
    - Constants, types, type-mapper organization
    - Manifest aggregation pattern
    - Menu item registration in Settings section

---

## Open Questions Resolved

| Question              | Decision                                                                                                                             |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| Database access       | EF Core                                                                                                                              |
| API style             | Umbraco Management API patterns                                                                                                      |
| Search service        | Deferred to v2                                                                                                                       |
| UI scope              | Full CRUD UI                                                                                                                         |
| Persistence packaging | 2-tier: shared `Umbraco.AI.Persistence` (entities, DbContext, repos) + `Umbraco.AI.Persistence.SqlServer`/`Sqlite` (migrations only) |
| Connection string     | Use `UseUmbracoDatabaseProvider()` to reuse Umbraco's database                                                                       |
| UI location           | Settings section with AI group (Connections, Profiles menu items)                                                                    |
| Middleware ordering   | OrderedCollectionBuilder with `InsertBefore`/`InsertAfter` (remove `Order` property from interface)                                  |
| Provider discovery    | `LazyCollectionBuilderBase` with auto-discovery via `FindClassesOfType<IAIProvider>()`                                               |
| Chat streaming        | SSE (Server-Sent Events) for streaming responses                                                                                     |
