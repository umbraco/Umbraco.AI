# Umbraco.Ai Testing Strategy

## Overview

This document describes the testing strategy for Umbraco.Ai, focusing on critical paths rather than 100% coverage.

**Key Decisions:**
- **Assertion Library**: Shouldly (fluent assertions)
- **Mocking Framework**: Moq
- **Test Framework**: xUnit
- **Snapshot Testing**: Verify.Xunit (for web tests)
- **Coverage**: Coverlet
- **Real API Tests**: Deferred to provider-specific projects (e.g., Umbraco.Ai.OpenAi will have its own integration tests later)
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
├── Umbraco.Ai.Tests.Unit/          # Unit tests for core services and web controllers
├── Umbraco.Ai.Tests.Integration/   # Integration tests (DI, E2E flows)
├── Umbraco.Ai.Tests.Common/        # Shared test utilities, fakes, builders
```

**Note**: Provider-specific tests (e.g., OpenAI integration tests with real API calls) will live in separate provider projects when those are split out.

---

## Test Projects

| Project | Type | Purpose |
|---------|------|---------|
| `Umbraco.Ai.Tests.Unit` | xUnit | Unit tests for core services, providers, middleware, registry, and Management API controllers |
| `Umbraco.Ai.Tests.Integration` | xUnit | Integration tests for DI container and end-to-end service flows |
| `Umbraco.Ai.Tests.Common` | Class Library | Shared test utilities, builders, and fakes (not executable) |

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

## Shared Test Infrastructure (`Umbraco.Ai.Tests.Common`)

### Builders

Fluent builders for test data construction:

```csharp
var profile = new AiProfileBuilder()
    .WithAlias("chat-1")
    .WithCapability(AiCapability.Chat)
    .WithConnectionId(connectionId)
    .WithModel("openai", "gpt-4")
    .WithTemperature(0.7f)
    .Build();

var connection = new AiConnectionBuilder()
    .WithProviderAlias("openai")
    .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
    .IsActive(true)
    .Build();

var model = new AiModelRefBuilder()
    .WithProviderId("openai")
    .WithModelId("gpt-4")
    .Build();
```

### Fakes

Test doubles for isolated testing:

| Fake | Purpose |
|------|---------|
| `FakeAiProvider` | Configurable provider with fluent API for adding capabilities |
| `FakeChatCapability` | Chat capability implementation without real API calls |
| `FakeChatClient` | M.E.AI `IChatClient` implementation for testing |
| `FakeEmbeddingCapability` | Embedding capability implementation |
| `FakeProviderSettings` | Simple settings class for testing |

Example usage:

```csharp
var fakeChatClient = new FakeChatClient("Test response");
var fakeChatCapability = new FakeChatCapability(fakeChatClient);
var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
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
    var profile = new AiProfileBuilder()
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

Unit tests are organized by category within `Umbraco.Ai.Tests.Unit`:

```
Umbraco.Ai.Tests.Unit/
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
│   └── AiChatClientFactoryTests.cs
├── Middleware/
│   └── MiddlewarePipelineTests.cs
├── Providers/
│   └── AiProviderBaseTests.cs
├── Registry/
│   └── AiRegistryTests.cs
├── Services/
│   ├── AiChatServiceTests.cs
│   ├── AiConnectionServiceTests.cs
│   ├── AiEmbeddingServiceTests.cs
│   └── AiProfileServiceTests.cs
└── Settings/
    └── AiEditableModelResolverTests.cs
```

### Core Service Tests

| Test Class | Covers |
|------------|--------|
| `AiEditableModelResolverTests` | JSON deserialization, `$ConfigKey` resolution, validation |
| `AiChatClientFactoryTests` | Client creation, connection validation, middleware application |
| `AiProfileServiceTests` | Profile CRUD, default profile resolution |
| `AiConnectionServiceTests` | Connection CRUD, validation, test connection |
| `AiChatServiceTests` | Chat completion via profile ID/alias, streaming |
| `AiEmbeddingServiceTests` | Embedding generation via profile |
| `AiRegistryTests` | Provider lookup, capability retrieval |
| `MiddlewarePipelineTests` | Middleware ordering and application |
| `AiProviderBaseTests` | Provider capability registration |

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
        .ReturnsAsync((AiProfile?)null);

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

Integration tests live in `Umbraco.Ai.Tests.Integration` and use real service implementations with fake providers.

### Service Resolution Tests

Verify the DI container can resolve all critical services:

```csharp
[Fact]
public void IAiChatService_CanBeResolved_WithEntireDependencyChain()
{
    var chatService = _serviceProvider.GetService<IAiChatService>();
    chatService.ShouldNotBeNull();
}

[Fact]
public void IAiRegistry_ContainsRegisteredProvider()
{
    var registry = _serviceProvider.GetRequiredService<IAiRegistry>();
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
    var connection = new AiConnectionBuilder()
        .WithId(connectionId)
        .WithProviderId("fake-provider")
        .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
        .Build();
    await _connectionRepository.SaveAsync(connection);

    // Arrange - Create profile
    var profile = new AiProfileBuilder()
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
private static void RegisterAiServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IConfiguration>(configuration);
    services.Configure<AiOptions>(configuration.GetSection("Umbraco:Ai"));

    // Provider infrastructure
    services.AddSingleton<IAiCapabilityFactory, AiCapabilityFactory>();
    services.AddSingleton<IAiProviderInfrastructure, AiProviderInfrastructure>();

    // Register fake provider
    var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
        .WithChatCapability();
    services.AddSingleton<IAiProvider>(fakeProvider);

    // ... remaining registrations
}
```

---

## Running Tests

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

---

## Future - JavaScript Service Tests

When JS services are created (mirroring C# services for Management API consumption):

### Testing Infrastructure
- **Framework**: Vitest (fast, ESM-native, good TypeScript support)
- **Location**: `src/Umbraco.Ai.Web.StaticAssets/Client/src/**/*.test.ts`

### Services to Test (when created)
| Service | Purpose |
|---------|---------|
| `AiProviderService` | Fetch providers from API |
| `AiConnectionService` | CRUD operations for connections |
| `AiProfileService` | CRUD operations for profiles |
| `AiChatService` | Chat completions via API |

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
| `src/Umbraco.Ai.Core/EditableModels/AiEditableModelResolver.cs` | Editable model resolution logic |
| `src/Umbraco.Ai.Core/Factories/AiChatClientFactory.cs` | Client creation and middleware |
| `src/Umbraco.Ai.Core/Profiles/AiProfileService.cs` | Profile CRUD |
| `src/Umbraco.Ai.Core/Connections/AiConnectionService.cs` | Connection CRUD |
| `src/Umbraco.Ai.Core/Services/AiChatService.cs` | High-level chat API |
| `src/Umbraco.Ai.Core/Services/AiEmbeddingService.cs` | High-level embedding API |
| `src/Umbraco.Ai.Core/Registry/AiRegistry.cs` | Provider registry |
| `src/Umbraco.Ai.Web/Api/Management/**/*.cs` | Management API controllers |
