# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI core package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.sln

# Build frontend assets (from Client directory)
cd src/Umbraco.AI.Web.StaticAssets/Client
npm install
npm run build

# Watch frontend during development
npm run watch

# Generate API client from OpenAPI spec (requires running server)
npm run generate-client https://localhost:44331/umbraco/swagger/umbraco-ai/swagger.json

# Set up demo site for local development (run from monorepo root)
cd ..
.\scripts\install-demo-site.ps1  # Windows
./scripts/install-demo-site.sh   # Linux/Mac
# Then open Umbraco.AI.local.sln to work with both package and demo site
```

## Testing

### Test Commands

```bash
# Run all tests
dotnet test Umbraco.AI.sln

# Run tests with detailed output
dotnet test Umbraco.AI.sln --verbosity normal

# Run specific test project
dotnet test tests/Umbraco.AI.Tests.Unit/Umbraco.AI.Tests.Unit.csproj

# Run with code coverage
dotnet test Umbraco.AI.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Test Projects

| Project | Purpose |
|---------|---------|
| `Umbraco.AI.Tests.Unit` | Unit tests for core services, providers, middleware, registry, API controllers, and EF Core repositories |
| `Umbraco.AI.Tests.Integration` | Integration tests for DI resolution and end-to-end service flows |
| `Umbraco.AI.Tests.Common` | Shared test utilities, builders, fakes, and fixtures (not executable) |

### Test Stack

- **Framework**: xUnit
- **Assertions**: Shouldly (fluent assertions)
- **Mocking**: Moq
- **Snapshot Testing**: Verify.Xunit (for web tests)
- **Coverage**: Coverlet

### Test Utilities (Umbraco.AI.Tests.Common)

**Builders** - Fluent test data construction:
```csharp
var profile = new AiProfileBuilder()
    .WithAlias("chat-1")
    .WithCapability(AiCapability.Chat)
    .Build();

var connection = new AiConnectionBuilder()
    .WithProviderAlias("openai")
    .Build();
```

**Fakes** - Test doubles for isolated testing:
- `FakeAiProvider` - Configurable provider for testing
- `FakeChatCapability` / `FakeChatClient` - Chat without real API calls
- `FakeEmbeddingCapability` - Embedding capability implementation
- `FakeProviderSettings` - Provider settings for testing

**Fixtures** - Test infrastructure:
- `EfCoreTestFixture` - In-memory SQLite database for EF Core repository tests

### Test Patterns

Tests follow Arrange-Act-Assert with Shouldly assertions:
```csharp
[Fact]
public async Task GetProfileAsync_WithExistingId_ReturnsProfile()
{
    // Arrange
    var profileId = Guid.NewGuid();
    var profile = new AiProfileBuilder().WithId(profileId).Build();
    _repositoryMock.Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(profile);

    // Act
    var result = await _service.GetProfileAsync(profileId);

    // Assert
    result.ShouldNotBeNull();
    result!.Id.ShouldBe(profileId);
}
```

### Testing Philosophy

We take a pragmatic approach to testing, focusing on value over coverage metrics:

- **Test critical paths** - Prioritize tests for business logic, edge cases, and integration points
- **Question every test** - Before writing a test, ask: What value does this provide? Is it arbitrary?
- **Avoid passthrough tests** - Don't test methods that simply delegate to services already under test
- **Skip trivial code** - Simple property mappings, constructors with no logic, and boilerplate don't need dedicated tests
- **Focus on behavior** - Test what the code does, not how it's implemented

## Architecture Overview

Umbraco.AI is a provider-agnostic AI integration layer for Umbraco CMS built on Microsoft.Extensions.AI (M.E.AI). It uses a "thin wrapper" philosophy - exposing M.E.AI types directly (`IChatClient`, `ChatMessage`, `ChatResponse`) rather than creating proprietary abstractions.

### Project Structure

| Project | Purpose |
|---------|---------|
| `Umbraco.AI.Core` | Core abstractions, services, and models. All interfaces and base classes. |
| `Umbraco.AI.Persistence` | EF Core DbContext, entities, and repository implementations |
| `Umbraco.AI.Persistence.SqlServer` | SQL Server migrations for persistence layer |
| `Umbraco.AI.Persistence.Sqlite` | SQLite migrations for persistence layer |
| `Umbraco.AI.Web` | Management API layer for backoffice integration |
| `Umbraco.AI.Web.StaticAssets` | TypeScript/Lit frontend components for backoffice UI |
| `Umbraco.AI.Startup` | Umbraco Composer for auto-discovery and DI registration |
| `Umbraco.AI` | Meta-package that bundles all components |

Provider packages are maintained separately (e.g., `Umbraco.AI.OpenAI`).

### Solution File Organization

When adding new projects to `Umbraco.AI.sln`:

- **Public/deployable projects** (anything shipped with Umbraco.AI NuGet packages) should be added to the **solution root** - not in a solution folder
- **Supplementary projects** like tests belong in solution folders (e.g., `Tests/`)

Do NOT add public projects to a `src/` solution folder - this is incorrect. The solution root is the correct location for all production code projects.

### Hierarchical Configuration Model

```
Provider (plugin with capabilities)
    └── Connection (authentication/credentials)
            └── Profile (use-case configuration: model, temperature, system prompt)
                    └── AI Request (the actual call)
```

- **Providers**: Installable NuGet packages supporting specific AI services. Discovered via `[AiProvider]` attribute and assembly scanning.
- **Connections**: Store API keys and provider-specific settings. Persisted to database via EF Core.
- **Profiles**: Combine a connection with model settings for specific use cases (e.g., "content-assistant" with GPT-4 and creative settings). Persisted to database via EF Core.

### Capability System

Providers expose discrete capabilities:
- `IAiChatCapability` - Chat completions (conversational AI)
- `IAiEmbeddingCapability` - Text embeddings (vector representations)
- Future: Media (image generation), Moderation (content safety)

Each capability creates M.E.AI clients (`IChatClient`, `IEmbeddingGenerator<string, Embedding<float>>`).

### Collection Builder Pattern

Umbraco.AI uses Umbraco's collection builder pattern for extensibility:

**Provider Collection** (`LazyCollectionBuilderBase`):
- Providers are auto-discovered via `[AiProvider]` attribute and `IDiscoverable`
- Use `AiProviders()` extension to add/exclude providers in a Composer

```csharp
// In a Composer - add or exclude providers
builder.AiProviders()
    .Add<CustomProvider>()
    .Exclude<SomeUnwantedProvider>();
```

**Middleware Collections** (`OrderedCollectionBuilderBase`):
- Middleware ordering is explicit via `Append()`, `InsertBefore<T>()`, `InsertAfter<T>()`
- No `Order` property on middleware interfaces - ordering is purely via collection builder

### Key Services

- `IAiChatService` - Primary developer interface for chat completions. Resolves profiles and creates configured clients automatically.
- `IAiProfileService` - Profile CRUD and lookup by alias
- `IAiConnectionService` - Connection CRUD and validation
- `IAiRegistry` - Central registry of all providers and capabilities
- `IAiChatClientFactory` - Creates `IChatClient` instances with middleware applied

### Middleware Pipeline

Middleware wraps M.E.AI clients using the builder pattern. Ordering is controlled via the `OrderedCollectionBuilder` pattern using `Append()`, `InsertBefore<T>()`, and `InsertAfter<T>()` methods.

```csharp
public interface IAiChatMiddleware
{
    IChatClient Apply(IChatClient client);
}

// Register middleware in a Composer:
builder.AiChatMiddleware()
    .Append<LoggingChatMiddleware>()
    .InsertBefore<LoggingChatMiddleware, TracingMiddleware>();
```

### Settings System

Provider settings use `[AiField]` attributes for UI generation. Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$OpenAI:ApiKey"` reads from config).

### Management API

- Root path: `/umbraco/ai/management/api`
- Uses Umbraco backoffice security
- OpenAPI/Swagger documentation auto-generated

### IdOrAlias Pattern

API endpoints that reference profiles accept an `IdOrAlias` type, allowing identification by either GUID or string alias:

```csharp
// In API request models
public IdOrAlias? ProfileIdOrAlias { get; set; }

// Usage - accepts either format
new IdOrAlias(Guid.Parse("..."))  // By ID
new IdOrAlias("my-chat-profile")  // By alias
```

The `IdOrAlias` class:
- Implements `IParsable<IdOrAlias>` for model binding
- Has a custom JSON converter for serialization
- Provides `IsId` and `IsAlias` properties to check which format was provided
- Located in `Umbraco.AI.Web.Api.Common.Models`

Resolution is handled via extension methods on `IAiProfileService`:
```csharp
// Returns Guid? - null if not found
await profileService.TryGetProfileIdAsync(idOrAlias, cancellationToken);

// Returns Guid - throws if not found
await profileService.GetProfileIdAsync(idOrAlias, cancellationToken);
```

## Project Organization (Feature-Sliced Architecture)

### Core Principles

1. **Feature folders are flat** - All files for a feature live at the folder root
   - NO `Services/`, `Repositories/`, `Factories/` subfolders within features
   - Interfaces and implementations live side-by-side

2. **Only create subfolders for conceptually different content**
   - `Examples/` for sample code is acceptable
   - NOT for grouping by implementation type

3. **Shared code lives at the project root level**
   - `Models/` - Shared domain models used across features
   - `Providers/` - Provider SDK (base classes, interfaces, collections)
   - `Registry/` - Provider discovery
   - `EditableModels/` - Editable model infrastructure (schemas, field definitions, resolution)
   - `Extensions/` - Utility extensions
   - `Configuration/` - DI registration

4. **Feature folders contain everything for that feature**
   - Domain models specific to the feature
   - Service interfaces and implementations
   - Repository interfaces and implementations
   - Factory interfaces and implementations
   - Middleware interfaces, collections, and builders

### When to Create a New Folder

| Scenario | Action |
|----------|--------|
| New capability (e.g., Media generation) | Create new feature folder: `Media/` |
| New shared infrastructure | Create root-level folder |
| Sample/example code within a feature | Create `Examples/` subfolder |
| More files of same type in a feature | Keep flat - do NOT create subfolders |

### Naming Conventions

- Interfaces: `I{Name}.cs` (e.g., `IAiChatService.cs`)
- Implementations: `{Name}.cs` (e.g., `AiChatService.cs`)
- Collections: `{Name}Collection.cs` and `{Name}CollectionBuilder.cs`

### Exception: API Projects (Umbraco.AI.Web)

Web follows Umbraco CMS Management API conventions with subfolders:
- `Controllers/` - API endpoints
- `Models/` - Request/response DTOs
- `Mapping/` - UmbracoMapper definitions

This is acceptable because:
- Higher file counts per feature
- Matches CMS patterns developers expect
- Not a direct code extension point

### Exception: Test Projects

Test projects use **layer-based organization**, not feature-sliced:
- `Services/` - Service tests (AiChatServiceTests, AiProfileServiceTests)
- `Repositories/` - EF Core repository tests
- `Factories/` - Factory tests
- `Providers/` - Provider base class tests
- `Api/Management/{Feature}/` - API controller tests (grouped by feature within Api)

This is intentional because:
- Tests are located by *what they test* (class type), not by domain feature
- When builds fail, developers look for "ServiceTests" not "ChatTests"
- Direct mapping: `Services/AiChatServiceTests.cs` tests `AiChatService`
- API tests are the exception - they mirror the Web project's feature structure

## Frontend Architecture

Located in `src/Umbraco.AI.Web.StaticAssets/Client/`:
- Uses Lit web components with `@umbraco-cms/backoffice` package
- Compiled to `wwwroot/` and served from `App_Plugins/UmbracoAi`
- API client generated from OpenAPI spec using `@hey-api/openapi-ts`

## Creating a New Provider

1. Create a new project referencing `Umbraco.AI.Core`
2. Create settings class with `[AiField]` attributes
3. Create provider class with `[AiProvider]` attribute extending `AiProviderBase<TSettings>`
4. Register capabilities using `WithCapability<T>()` in constructor
5. Implement capability classes extending `AiChatCapabilityBase<TSettings>` or `AiEmbeddingCapabilityBase<TSettings>`

Example:
```csharp
[AiProvider("myprovider", "My Provider")]
public class MyProvider : AiProviderBase<MyProviderSettings>
{
    public MyProvider(IAiProviderInfrastructure infrastructure) : base(infrastructure)
    {
        WithCapability<MyChatCapability>();
    }
}
```

## Key Namespaces

**Feature namespaces (Umbraco.AI.Core):**
- `Umbraco.AI.Core.Chat` - Chat service, factory, middleware (`IAiChatService`, `IAiChatClientFactory`)
- `Umbraco.AI.Core.Embeddings` - Embedding service, factory, middleware (`IAiEmbeddingService`)
- `Umbraco.AI.Core.Connections` - Connection model, service, repository (`AiConnection`, `IAiConnectionService`)
- `Umbraco.AI.Core.Profiles` - Profile model, service, repository (`AiProfile`, `IAiProfileService`)

**Shared namespaces (Umbraco.AI.Core):**
- `Umbraco.AI.Core.Providers` - Provider SDK (base classes, capabilities, collections)
- `Umbraco.AI.Core.Models` - Shared domain models (`AiCapability`, `AiModelRef`, `AiOptions`)
- `Umbraco.AI.Core.Registry` - Provider registry (`IAiRegistry`)
- `Umbraco.AI.Core.EditableModels` - Editable model infrastructure (`AiFieldAttribute`, `AiEditableModelSchema`, `IAiEditableModelResolver`)

**Persistence namespaces:**
- `Umbraco.AI.Persistence.Connections` - EF Core connection repository and entity
- `Umbraco.AI.Persistence.Profiles` - EF Core profile repository and entity

## Configuration

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

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Database Migrations

Umbraco.AI uses EF Core with provider-specific migrations. All migrations MUST use the `UmbracoAi_` prefix to clearly identify them as belonging to Umbraco.AI (e.g., `UmbracoAi_InitialCreate`, `UmbracoAi_AddNewEntity`).

To create new migrations after modifying entities:

```bash
# SQL Server
dotnet ef migrations add UmbracoAi_<MigrationName> -p src/Umbraco.AI.Persistence.SqlServer -c UmbracoAiDbContext --output-dir Migrations

# SQLite
dotnet ef migrations add UmbracoAi_<MigrationName> -p src/Umbraco.AI.Persistence.Sqlite -c UmbracoAiDbContext --output-dir Migrations
```

See `docs/internal/ef-core-migrations.md` for complete documentation.

## Documentation

Documentation is organized into two categories:

- `docs/public/` - User-facing documentation (guides, tutorials, API reference)
- `docs/internal/` - Maintainer documentation, architecture decisions, and AI context

### Internal Documentation

For deeper understanding, read these docs files:

**Core Documentation:**
- `docs/internal/core-concepts.md` - Providers, Connections, Profiles, and Middleware explained
- `docs/internal/integration-philosophy.md` - Why M.E.AI was chosen and the "thin wrapper" approach
- `docs/internal/capabilities-feature.md` - Chat, Embedding, and planned capabilities (Media, Moderation)
- `docs/internal/core-implementation-details.md` - Comprehensive technical reference with code examples
- `docs/internal/ef-core-migrations.md` - How to create and manage EF Core database migrations
- `docs/internal/umbraco-ai-agents-design.md` - Future Agents feature design (tools, approval workflow, backoffice integration)

**Planning Documents:**
- `docs/internal/plans/v1-core-implementation-plan.md` - V1 implementation roadmap
- `docs/internal/plans/testing-strategy.md` - Testing approach and strategy
- `docs/internal/plans/tools-and-agents-architecture.md` - Tools and agents system design

**Ideas (Future Exploration):**
- `docs/internal/ideas/` - Exploratory design documents for future features (toolsets, MCP integration, workflows, etc.)
