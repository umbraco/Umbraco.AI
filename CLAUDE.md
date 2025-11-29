# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.Ai.sln

# Build frontend assets (from Client directory)
cd src/Umbraco.Ai.Web.StaticAssets/Client
npm install
npm run build

# Watch frontend during development
npm run watch

# Generate API client from OpenAPI spec (requires running server)
npm run generate-client https://localhost:44331/umbraco/swagger/umbraco-ai/swagger.json

# Set up demo site for local development
.\scripts\Install-DemoSite.ps1
# Then open Umbraco.Ai.local.sln to work with both package and demo site
```

## Testing

### Test Commands

```bash
# Run all tests
dotnet test Umbraco.Ai.sln

# Run tests with detailed output
dotnet test Umbraco.Ai.sln --verbosity normal

# Run specific test project
dotnet test tests/Umbraco.Ai.Tests.Unit/Umbraco.Ai.Tests.Unit.csproj

# Run with code coverage
dotnet test Umbraco.Ai.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Test Projects

| Project | Purpose |
|---------|---------|
| `Umbraco.Ai.Tests.Unit` | Unit tests for core services, providers, middleware, registry, API controllers, and EF Core repositories |
| `Umbraco.Ai.Tests.Integration` | Integration tests for DI resolution and end-to-end service flows |
| `Umbraco.Ai.Tests.Common` | Shared test utilities, builders, fakes, and fixtures (not executable) |

### Test Stack

- **Framework**: xUnit
- **Assertions**: Shouldly (fluent assertions)
- **Mocking**: Moq
- **Snapshot Testing**: Verify.Xunit (for web tests)
- **Coverage**: Coverlet

### Test Utilities (Umbraco.Ai.Tests.Common)

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

Umbraco.Ai is a provider-agnostic AI integration layer for Umbraco CMS built on Microsoft.Extensions.AI (M.E.AI). It uses a "thin wrapper" philosophy - exposing M.E.AI types directly (`IChatClient`, `ChatMessage`, `ChatResponse`) rather than creating proprietary abstractions.

### Project Structure

| Project | Purpose |
|---------|---------|
| `Umbraco.Ai.Core` | Core abstractions, services, and models. All interfaces and base classes. |
| `Umbraco.Ai.Persistence` | EF Core DbContext, entities, and repository implementations |
| `Umbraco.Ai.Persistence.SqlServer` | SQL Server migrations for persistence layer |
| `Umbraco.Ai.Persistence.Sqlite` | SQLite migrations for persistence layer |
| `Umbraco.Ai.OpenAi` | Reference provider implementation for OpenAI |
| `Umbraco.Ai.Web` | Management API layer for backoffice integration |
| `Umbraco.Ai.Web.StaticAssets` | TypeScript/Lit frontend components for backoffice UI |
| `Umbraco.Ai.Startup` | Umbraco Composer for auto-discovery and DI registration |
| `Umbraco.Ai` | Meta-package that bundles all components |

### Solution File Organization

When adding new projects to `Umbraco.Ai.sln`:

- **Public/deployable projects** (anything shipped with Umbraco.Ai NuGet packages) should be added to the **solution root** - not in a solution folder
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

Umbraco.Ai uses Umbraco's collection builder pattern for extensibility:

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

Provider settings use `[AiSetting]` attributes for UI generation. Values prefixed with `$` are resolved from `IConfiguration` (e.g., `"$OpenAI:ApiKey"` reads from config).

### Management API

- Root path: `/umbraco/ai/management/api`
- Uses Umbraco backoffice security
- OpenAPI/Swagger documentation auto-generated

## Frontend Architecture

Located in `src/Umbraco.Ai.Web.StaticAssets/Client/`:
- Uses Lit web components with `@umbraco-cms/backoffice` package
- Compiled to `wwwroot/` and served from `App_Plugins/UmbracoAi`
- API client generated from OpenAPI spec using `@hey-api/openapi-ts`

## Creating a New Provider

1. Create a new project referencing `Umbraco.Ai.Core`
2. Create settings class with `[AiSetting]` attributes
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

- `Umbraco.Ai.Core.Providers` - Provider and capability interfaces/base classes
- `Umbraco.Ai.Core.Services` - High-level services (`IAiChatService`)
- `Umbraco.Ai.Core.Models` - Data models (`AiConnection`, `AiProfile`, `AiModelRef`)
- `Umbraco.Ai.Core.Middleware` - Middleware pipeline system
- `Umbraco.Ai.Core.Registry` - Provider registry
- `Umbraco.Ai.Core.Repositories` - Repository interfaces (`IAiConnectionRepository`, `IAiProfileRepository`)
- `Umbraco.Ai.Persistence` - EF Core DbContext and repository implementations
- `Umbraco.Ai.Persistence.Entities` - Database entities (`AiConnectionEntity`, `AiProfileEntity`)

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

## Coding Standard

- All extensions live in Umbraci.Ai.Extensions namespace for ease of discovery

## Database Migrations

Umbraco.Ai uses EF Core with provider-specific migrations. All migrations MUST use the `UmbracoAi_` prefix to clearly identify them as belonging to Umbraco.Ai (e.g., `UmbracoAi_InitialCreate`, `UmbracoAi_AddNewEntity`).

To create new migrations after modifying entities:

```bash
# SQL Server
dotnet ef migrations add UmbracoAi_<MigrationName> -p src/Umbraco.Ai.Persistence.SqlServer -c UmbracoAiDbContext --output-dir Migrations

# SQLite
dotnet ef migrations add UmbracoAi_<MigrationName> -p src/Umbraco.Ai.Persistence.Sqlite -c UmbracoAiDbContext --output-dir Migrations
```

See `docs/ef-core-migrations.md` for complete documentation.

## Documentation

For deeper understanding, read these docs files:

**Core Documentation:**
- `docs/core-concepts.md` - Providers, Connections, Profiles, and Middleware explained
- `docs/integration-philosophy.md` - Why M.E.AI was chosen and the "thin wrapper" approach
- `docs/capabilities-feature.md` - Chat, Embedding, and planned capabilities (Media, Moderation)
- `docs/core-implementation-details.md` - Comprehensive technical reference with code examples
- `docs/ef-core-migrations.md` - How to create and manage EF Core database migrations
- `docs/umbraco-ai-agents-design.md` - Future Agents feature design (tools, approval workflow, backoffice integration)

**Planning Documents:**
- `docs/plans/v1-core-implementation-plan.md` - V1 implementation roadmap
- `docs/plans/testing-strategy.md` - Testing approach and strategy
- `docs/plans/tools-and-agents-architecture.md` - Tools and agents system design

**Ideas (Future Exploration):**
- `docs/ideas/` - Exploratory design documents for future features (toolsets, MCP integration, workflows, etc.)
