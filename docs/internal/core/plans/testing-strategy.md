# Umbraco.AI Testing Strategy

## Overview

This document describes the testing strategy for Umbraco.AI, focusing on critical paths rather than 100% coverage.

**Key Decisions:**
- **Assertion Library**: Shouldly (fluent assertions)
- **Mocking Framework**: Moq
- **Test Framework**: xUnit
- **Snapshot Testing**: Verify.Xunit (for web tests)
- **Coverage**: Coverlet
- **Real API Tests**: Deferred to provider-specific projects (e.g., Umbraco.AI.OpenAI will have its own integration tests later)
- **Frontend Tests**: JS services (when created) will mirror C# services and need Vitest coverage

## Testing Pyramid

```
         ╱╲
        ╱  ╲        E2E Tests (few)
       ╱────╲       - Full backoffice workflows
      ╱      ╲
     ╱────────╲     Integration Tests (some)
    ╱          ╲    - DI container, end-to-end service flows
   ╱────────────╲
  ╱              ╲  Unit Tests (many)
 ╱────────────────╲ - Services, factories, resolvers, controllers
```

---

## Project Structure

```
tests/
├── Umbraco.AI.Tests.Unit/          # Unit tests for core services and web controllers
├── Umbraco.AI.Tests.Integration/   # Integration tests (DI, E2E flows)
├── Umbraco.AI.Tests.Common/        # Shared test utilities, fakes, builders
```

**Note**: Provider-specific tests (e.g., OpenAI integration tests with real API calls) will live in separate provider projects when those are split out.

---

## Test Projects

| Project | Type | Purpose |
|---------|------|---------|
| `Umbraco.AI.Tests.Unit` | xUnit | Unit tests for core services, providers, middleware, registry, and Management API controllers |
| `Umbraco.AI.Tests.Integration` | xUnit | Integration tests for DI container and end-to-end service flows |
| `Umbraco.AI.Tests.Common` | Class Library | Shared test utilities, builders, and fakes (not executable) |

---

## Testing Dependencies (Directory.Packages.props)

```xml
<!-- Test dependencies -->
<ItemGroup>
  <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  <PackageVersion Include="Shouldly" Version="4.2.1" />
  <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="[10.0.0, 10.999.999)" />
  <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
  <PackageVersion Include="Moq" Version="4.20.72" />
  <PackageVersion Include="Verify.Xunit" Version="28.8.0" />
  <PackageVersion Include="xunit" Version="2.9.3" />
  <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
</ItemGroup>
```

---

## Shared Test Infrastructure (`Umbraco.AI.Tests.Common`)

### Builders

Fluent builders for test data construction:

```csharp
var profile = new AIProfileBuilder()
    .WithAlias("chat-1")
    .WithCapability(AICapability.Chat)
    .WithConnectionId(connectionId)
    .WithModel("openai", "gpt-4")
    .WithTemperature(0.7f)
    .Build();

var connection = new AIConnectionBuilder()
    .WithProviderAlias("openai")
    .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
    .IsActive(true)
    .Build();

var model = new AIModelRefBuilder()
    .WithProviderId("openai")
    .WithModelId("gpt-4")
    .Build();
```

### Fakes

Test doubles for isolated testing:

| Fake | Purpose |
|------|---------|
| `FakeAIProvider` | Configurable provider with fluent API for adding capabilities |
| `FakeChatCapability` | Chat capability implementation without real API calls |
| `FakeChatClient` | M.E.AI `IChatClient` implementation for testing |
| `FakeEmbeddingCapability` | Embedding capability implementation |
| `FakeProviderSettings` | Simple settings class for testing |

Example usage:

```csharp
var fakeChatClient = new FakeChatClient("Test response");
var fakeChatCapability = new FakeChatCapability(fakeChatClient);
var fakeProvider = new FakeAIProvider("fake-provider", "Fake Provider")
    .WithChatCapability(fakeChatCapability)
    .WithEmbeddingCapability();
```

---

## Test Patterns

Tests follow Arrange-Act-Assert with Shouldly assertions:

```csharp
[Fact]
public async Task GetProfileAsync_WithExistingId_ReturnsProfile()
{
    // Arrange
    var profileId = Guid.NewGuid();
    var profile = new AIProfileBuilder()
        .WithId(profileId)
        .WithAlias("test-profile")
        .Build();

    _repositoryMock
        .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(profile);

    // Act
    var result = await _service.GetProfileAsync(profileId);

    // Assert
    result.ShouldNotBeNull();
    result!.Id.ShouldBe(profileId);
    result.Alias.ShouldBe("test-profile");
}
```

### Global Usings

Test projects use implicit usings for common namespaces:

```xml
<ItemGroup>
  <Using Include="Xunit" />
  <Using Include="Shouldly" />
  <Using Include="Moq" />
</ItemGroup>
```

---

## Unit Tests

### Test Organization

Unit tests are organized by category within `Umbraco.AI.Tests.Unit`:

```
Umbraco.AI.Tests.Unit/
├── Api/Management/
│   ├── Chat/
│   │   ├── CompleteChatControllerTests.cs
│   │   └── StreamChatControllerTests.cs
│   ├── Connection/
│   │   ├── AllConnectionControllerTests.cs
│   │   ├── ByIdConnectionControllerTests.cs
│   │   ├── CreateConnectionControllerTests.cs
│   │   ├── DeleteConnectionControllerTests.cs
│   │   ├── TestConnectionControllerTests.cs
│   │   └── UpdateConnectionControllerTests.cs
│   ├── Embedding/
│   │   └── GenerateEmbeddingControllerTests.cs
│   ├── Profile/
│   │   ├── AllProfileControllerTests.cs
│   │   ├── ByAliasProfileControllerTests.cs
│   │   ├── ByIdProfileControllerTests.cs
│   │   ├── CreateProfileControllerTests.cs
│   │   ├── DeleteProfileControllerTests.cs
│   │   └── UpdateProfileControllerTests.cs
│   └── Provider/
│       ├── AllProviderControllerTests.cs
│       ├── ByIdProviderControllerTests.cs
│       └── ModelsByProviderControllerTests.cs
├── Factories/
│   └── AIChatClientFactoryTests.cs
├── Middleware/
│   └── MiddlewarePipelineTests.cs
├── Providers/
│   └── AIProviderBaseTests.cs
├── Registry/
│   └── AIRegistryTests.cs
├── Services/
│   ├── AIChatServiceTests.cs
│   ├── AIConnectionServiceTests.cs
│   ├── AIEmbeddingServiceTests.cs
│   └── AIProfileServiceTests.cs
└── Settings/
    └── AIEditableModelResolverTests.cs
```

### Core Service Tests

| Test Class | Covers |
|------------|--------|
| `AIEditableModelResolverTests` | JSON deserialization, `$ConfigKey` resolution, validation |
| `AIChatClientFactoryTests` | Client creation, connection validation, middleware application |
| `AIProfileServiceTests` | Profile CRUD, default profile resolution |
| `AIConnectionServiceTests` | Connection CRUD, validation, test connection |
| `AIChatServiceTests` | Chat completion via profile ID/alias, streaming |
| `AIEmbeddingServiceTests` | Embedding generation via profile |
| `AIRegistryTests` | Provider lookup, capability retrieval |
| `MiddlewarePipelineTests` | Middleware ordering and application |
| `AIProviderBaseTests` | Provider capability registration |

### Management API Controller Tests

Controller tests mock dependencies and verify:
- Correct HTTP status codes (200, 404, etc.)
- Response model mapping via `IUmbracoMapper`
- Repository/service method invocation

Example:

```csharp
[Fact]
public async Task ById_WithNonExistingId_Returns404NotFound()
{
    // Arrange
    var profileId = Guid.NewGuid();
    _profileRepositoryMock
        .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((AIProfile?)null);

    // Act
    var result = await _controller.ById(profileId);

    // Assert
    var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
    var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
    problemDetails.Title.ShouldBe("Profile not found");
}
```

---

## Integration Tests

Integration tests live in `Umbraco.AI.Tests.Integration` and use real service implementations with fake providers.

### Service Resolution Tests

Verify the DI container can resolve all critical services:

```csharp
[Fact]
public void IAIChatService_CanBeResolved_WithEntireDependencyChain()
{
    var chatService = _serviceProvider.GetService<IAIChatService>();
    chatService.ShouldNotBeNull();
}

[Fact]
public void IAIRegistry_ContainsRegisteredProvider()
{
    var registry = _serviceProvider.GetRequiredService<IAIRegistry>();
    var provider = registry.GetProvider("fake-provider");

    provider.ShouldNotBeNull();
    provider!.Id.ShouldBe("fake-provider");
}
```

### End-to-End Service Flow Tests

Test complete flows using real services with fake providers:

```csharp
[Fact]
public async Task FullFlow_CreateConnectionAndProfile_GetChatResponse_Succeeds()
{
    // Arrange - Create connection
    var connection = new AIConnectionBuilder()
        .WithId(connectionId)
        .WithProviderId("fake-provider")
        .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
        .Build();
    await _connectionRepository.SaveAsync(connection);

    // Arrange - Create profile
    var profile = new AIProfileBuilder()
        .WithAlias("default-chat")
        .WithConnectionId(connectionId)
        .Build();
    await _profileRepository.SaveAsync(profile);

    // Act
    var response = await _chatService.GetResponseAsync(messages);

    // Assert
    response.ShouldNotBeNull();
    response.Text.ShouldBe("Test response from fake provider");
}
```

### Integration Test Setup

Integration tests bypass Umbraco's collection builder by registering services directly:

```csharp
private static void RegisterAIServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IConfiguration>(configuration);
    services.Configure<AIOptions>(configuration.GetSection("Umbraco:AI"));

    // Provider infrastructure
    services.AddSingleton<IAICapabilityFactory, AICapabilityFactory>();
    services.AddSingleton<IAIProviderInfrastructure, AIProviderInfrastructure>();

    // Register fake provider
    var fakeProvider = new FakeAIProvider("fake-provider", "Fake Provider")
        .WithChatCapability();
    services.AddSingleton<IAIProvider>(fakeProvider);

    // ... remaining registrations
}
```

---

## Running Tests

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

---

## Future - JavaScript Service Tests

When JS services are created (mirroring C# services for Management API consumption):

### Testing Infrastructure
- **Framework**: Vitest (fast, ESM-native, good TypeScript support)
- **Location**: `src/Umbraco.AI.Web.StaticAssets/Client/src/**/*.test.ts`

### Services to Test (when created)
| Service | Purpose |
|---------|---------|
| `AIProviderService` | Fetch providers from API |
| `AIConnectionService` | CRUD operations for connections |
| `AIProfileService` | CRUD operations for profiles |
| `AIChatService` | Chat completions via API |

### Test Approach
- Mock `fetch` or use MSW (Mock Service Worker) for API mocking
- Test service methods return correct types
- Test error handling (401, 404, 500 responses)
- Test request payload construction

---

## Success Criteria

| Metric | Target |
|--------|--------|
| Critical path coverage | 90%+ |
| Overall code coverage | 60-70% |
| Build time with tests | < 2 minutes |
| Flaky test rate | < 1% |

---

## Critical Source Files Under Test

| File | Purpose |
|------|---------|
| `src/Umbraco.AI.Core/EditableModels/AIEditableModelResolver.cs` | Editable model resolution logic |
| `src/Umbraco.AI.Core/Factories/AIChatClientFactory.cs` | Client creation and middleware |
| `src/Umbraco.AI.Core/Profiles/AIProfileService.cs` | Profile CRUD |
| `src/Umbraco.AI.Core/Connections/AIConnectionService.cs` | Connection CRUD |
| `src/Umbraco.AI.Core/Services/AIChatService.cs` | High-level chat API |
| `src/Umbraco.AI.Core/Services/AIEmbeddingService.cs` | High-level embedding API |
| `src/Umbraco.AI.Core/Registry/AIRegistry.cs` | Provider registry |
| `src/Umbraco.AI.Web/Api/Management/**/*.cs` | Management API controllers |
