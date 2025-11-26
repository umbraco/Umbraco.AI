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
dotnet test tests/Umbraco.Ai.Core.Tests/Umbraco.Ai.Core.Tests.csproj

# Run with code coverage
dotnet test Umbraco.Ai.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Test Projects

| Project | Purpose |
|---------|---------|
| `Umbraco.Ai.Core.Tests` | Unit tests for core services, providers, middleware, and registry |
| `Umbraco.Ai.Web.Tests` | Integration tests for Management API endpoints |
| `Umbraco.Ai.Tests.Common` | Shared test utilities, builders, and fakes (not executable) |

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

## Architecture Overview

Umbraco.Ai is a provider-agnostic AI integration layer for Umbraco CMS built on Microsoft.Extensions.AI (M.E.AI). It uses a "thin wrapper" philosophy - exposing M.E.AI types directly (`IChatClient`, `ChatMessage`, `ChatResponse`) rather than creating proprietary abstractions.

### Project Structure

| Project | Purpose |
|---------|---------|
| `Umbraco.Ai.Core` | Core abstractions, services, and models. All interfaces and base classes. |
| `Umbraco.Ai.OpenAi` | Reference provider implementation for OpenAI |
| `Umbraco.Ai.Web` | Management API layer for backoffice integration |
| `Umbraco.Ai.Web.StaticAssets` | TypeScript/Lit frontend components for backoffice UI |
| `Umbraco.Ai.Startup` | Umbraco Composer for auto-discovery and DI registration |
| `Umbraco.Ai` | Meta-package that bundles all components |

### Hierarchical Configuration Model

```
Provider (plugin with capabilities)
    └── Connection (authentication/credentials)
            └── Profile (use-case configuration: model, temperature, system prompt)
                    └── AI Request (the actual call)
```

- **Providers**: Installable NuGet packages supporting specific AI services. Discovered via `[AiProvider]` attribute and assembly scanning.
- **Connections**: Store API keys and provider-specific settings. Currently in-memory, planned for persistent storage.
- **Profiles**: Combine a connection with model settings for specific use cases (e.g., "content-assistant" with GPT-4 and creative settings).

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

## Documentation

For deeper understanding, read these docs files:

- `docs/core-concepts.md` - Providers, Connections, Profiles, and Middleware explained
- `docs/integration-philosophy.md` - Why M.E.AI was chosen and the "thin wrapper" approach
- `docs/capabilities-feature.md` - Chat, Embedding, and planned capabilities (Media, Moderation)
- `docs/core-implementation-details.md` - Comprehensive technical reference with code examples
- `docs/umbraco-ai-agents-design.md` - Future Agents feature design (tools, approval workflow, backoffice integration)
