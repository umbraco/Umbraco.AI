# Umbraco.Ai Testing Strategy Plan

## Overview

This plan establishes a comprehensive testing strategy for Umbraco.Ai, focusing on critical paths rather than 100% coverage. The codebase currently has **no test projects**, presenting an opportunity to build testing infrastructure from scratch.

**Key Decisions:**
- **Mocking Framework**: Moq
- **CI/CD**: GitHub Actions
- **Real API Tests**: Deferred to provider-specific projects (e.g., Umbraco.Ai.OpenAi will have its own integration tests later)
- **Frontend Tests**: JS services (when created) will mirror C# services and need Vitest coverage

## Testing Pyramid

```
         ╱╲
        ╱  ╲        E2E Tests (few)
       ╱────╲       - Full backoffice workflows
      ╱      ╲
     ╱────────╲     Integration Tests (some)
    ╱          ╲    - API endpoints, DI container, provider integration
   ╱────────────╲
  ╱              ╲  Unit Tests (many)
 ╱────────────────╲ - Services, factories, resolvers, validators
```

---

## Project Structure

```
tests/
├── Umbraco.Ai.Core.Tests/           # Unit tests for core logic
├── Umbraco.Ai.Web.Tests/            # Unit tests for API controllers + integration
├── Umbraco.Ai.Tests.Common/         # Shared test utilities, fakes, builders
```

**Note**: Provider-specific tests (e.g., OpenAI integration tests with real API calls) will live in separate provider projects when those are split out.

---

## Phase 1: Test Infrastructure Setup

### 1.1 Create Test Projects

| Project | Type | Dependencies |
|---------|------|--------------|
| `Umbraco.Ai.Tests.Common` | Class Library | Moq, FluentAssertions, test fixtures |
| `Umbraco.Ai.Core.Tests` | xUnit | Core, Tests.Common |
| `Umbraco.Ai.Web.Tests` | xUnit | Web, Tests.Common, WebApplicationFactory |

### 1.2 Testing Dependencies (Directory.Packages.props)

```xml
<PackageVersion Include="xunit" Version="2.9.2" />
<PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
<PackageVersion Include="Moq" Version="4.20.72" />
<PackageVersion Include="FluentAssertions" Version="7.0.0" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
<PackageVersion Include="Verify.Xunit" Version="28.5.0" />
```

### 1.3 Shared Test Infrastructure (`Umbraco.Ai.Tests.Common`)

- **Test Builders**: Fluent builders for `AiProfile`, `AiConnection`, `AiModelRef`
- **Fake Providers**: `FakeAiProvider`, `FakeChatCapability`, `FakeEmbeddingCapability`
- **Fake Clients**: `FakeChatClient` implementing M.E.AI `IChatClient`
- **Mock Factories**: Pre-configured mocks for common interfaces
- **Test Fixtures**: Shared DI containers, configuration builders

---

## Phase 2: Unit Tests (Critical Paths)

### 2.1 Settings Resolution (`AiSettingsResolver`) - CRITICAL

**Why**: Complex logic with JSON parsing, environment variable substitution, validation.

| Test Scenario | Priority |
|--------------|----------|
| Deserialize valid JSON to settings object | High |
| Resolve `$ConfigKey` from IConfiguration | High |
| Resolve nested config paths (`$OpenAI:ApiKey`) | High |
| Validate required fields (DataAnnotations) | High |
| Handle missing config keys gracefully | High |
| Type conversion (string → int, bool, decimal) | Medium |
| Invalid JSON returns meaningful error | Medium |
| Null/empty settings handling | Medium |

### 2.2 Client Factory (`AiChatClientFactory`) - CRITICAL

**Why**: Core orchestration logic - validates connections, resolves providers, applies middleware.

| Test Scenario | Priority |
|--------------|----------|
| Create client with valid profile and connection | High |
| Throw when connection ID is empty | High |
| Throw when connection not found | High |
| Throw when provider doesn't match connection | High |
| Throw when provider lacks chat capability | High |
| Apply middleware in correct order | High |
| Create client without middleware (empty collection) | Medium |
| Settings resolved before creating client | Medium |

### 2.3 Profile Service (`AiProfileService`) - HIGH

| Test Scenario | Priority |
|--------------|----------|
| Get profile by ID | High |
| Get profile by alias | High |
| Get default profile for capability | High |
| Return null when profile not found | High |
| Default profile alias from configuration | Medium |
| List profiles filtered by capability | Medium |

### 2.4 Connection Service (`AiConnectionService`) - HIGH

| Test Scenario | Priority |
|--------------|----------|
| Create connection with valid provider | High |
| Validate settings on create/update | High |
| Throw when provider not found | High |
| Test connection calls provider's GetModelsAsync | High |
| Update connection preserves ID | Medium |
| Delete connection removes from repository | Medium |

### 2.5 Registry (`AiRegistry`) - HIGH

| Test Scenario | Priority |
|--------------|----------|
| Get provider by alias (case-insensitive) | High |
| Get providers by capability type | High |
| Return null for unknown alias | High |
| List all registered providers | Medium |

### 2.6 Chat Service (`AiChatService`) - HIGH

| Test Scenario | Priority |
|--------------|----------|
| Get response using profile alias | High |
| Get response using profile ID | High |
| Merge caller options with profile defaults | High |
| Use default profile when none specified | High |
| Streaming response returns async enumerable | Medium |
| Pass-through to underlying IChatClient | Medium |

### 2.7 Middleware Pipeline - MEDIUM

| Test Scenario | Priority |
|--------------|----------|
| Middleware applied in order | High |
| Empty middleware returns original client | Medium |
| Each middleware wraps previous client | Medium |

### 2.8 Provider Base Classes - MEDIUM

| Test Scenario | Priority |
|--------------|----------|
| Provider exposes registered capabilities | High |
| GetCapability returns correct type | High |
| GetSettingDefinitions from attributes | Medium |

---

## Phase 3: Integration Tests

### 3.1 DI Container Integration

**Purpose**: Verify services resolve correctly with real DI container.

| Test Scenario | Priority |
|--------------|----------|
| All core services resolve from container | High |
| Provider auto-discovery finds OpenAI provider | High |
| Middleware collection builds correctly | Medium |
| Options bind from configuration | Medium |

### 3.2 End-to-End Service Flow

**Purpose**: Test complete flows with fake providers (no real API calls).

| Test Scenario | Priority |
|--------------|----------|
| Create connection → Create profile → Get chat response | High |
| Settings resolution through full pipeline | High |
| Middleware applied in real factory | Medium |

### 3.3 Management API Integration

**Purpose**: Test HTTP endpoints with `WebApplicationFactory`.

| Test Scenario | Priority |
|--------------|----------|
| GET /providers returns registered providers | High |
| POST /connections creates connection | High |
| GET /connections/{id} returns connection | High |
| POST /connections/{id}/test validates connection | High |
| POST /profiles creates profile | Medium |
| Authentication required for all endpoints | Medium |

---

## Phase 4: Contract/Snapshot Tests (Optional)

### 4.1 API Contract Tests

**Purpose**: Ensure API responses don't change unexpectedly.

- Use `Verify` library for snapshot testing
- Capture OpenAPI spec changes
- Verify response models match expected shape

---

## Phase 5: Future - JavaScript Service Tests

When JS services are created (mirroring C# services for Management API consumption):

### 5.1 Testing Infrastructure
- **Framework**: Vitest (fast, ESM-native, good TypeScript support)
- **Location**: `src/Umbraco.Ai.Web.StaticAssets/Client/src/**/*.test.ts`

### 5.2 Services to Test (when created)
| Service | Purpose |
|---------|---------|
| `AiProviderService` | Fetch providers from API |
| `AiConnectionService` | CRUD operations for connections |
| `AiProfileService` | CRUD operations for profiles |
| `AiChatService` | Chat completions via API |

### 5.3 Test Approach
- Mock `fetch` or use MSW (Mock Service Worker) for API mocking
- Test service methods return correct types
- Test error handling (401, 404, 500 responses)
- Test request payload construction

---

## Implementation Order

### Sprint 1: Foundation
1. Create test project structure (`tests/` folder)
2. Add NuGet dependencies to `Directory.Packages.props`
3. Implement `Umbraco.Ai.Tests.Common` (builders, fakes)
4. Set up GitHub Actions workflow for tests
5. Write first unit tests for `AiSettingsResolver`

### Sprint 2: Core Services
1. Unit tests for `AiChatClientFactory`
2. Unit tests for `AiProfileService`
3. Unit tests for `AiConnectionService`
4. Unit tests for `AiRegistry`
5. Unit tests for `AiChatService`

### Sprint 3: Integration & API
1. DI container integration tests
2. Management API integration tests (WebApplicationFactory)
3. End-to-end flow tests with fake providers

### Sprint 4: Polish & CI
1. Middleware pipeline tests
2. Edge cases and error handling
3. Code coverage reporting in CI
4. PR status checks for test results

---

## GitHub Actions Workflow

```yaml
# .github/workflows/test.yml
name: Tests

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: '**/coverage.cobertura.xml'
```

---

## Success Criteria

| Metric | Target |
|--------|--------|
| Critical path coverage | 90%+ |
| Overall code coverage | 60-70% |
| Build time with tests | < 2 minutes |
| Flaky test rate | < 1% |

---

## Key Files to Modify

### New Files
```
tests/
├── Umbraco.Ai.Tests.Common/
│   ├── Umbraco.Ai.Tests.Common.csproj
│   ├── Builders/
│   │   ├── AiProfileBuilder.cs
│   │   ├── AiConnectionBuilder.cs
│   │   └── AiModelRefBuilder.cs
│   └── Fakes/
│       ├── FakeAiProvider.cs
│       ├── FakeChatCapability.cs
│       ├── FakeChatClient.cs
│       └── FakeEmbeddingCapability.cs
├── Umbraco.Ai.Core.Tests/
│   ├── Umbraco.Ai.Core.Tests.csproj
│   ├── Services/
│   │   ├── AiSettingsResolverTests.cs
│   │   ├── AiChatClientFactoryTests.cs
│   │   ├── AiProfileServiceTests.cs
│   │   ├── AiConnectionServiceTests.cs
│   │   └── AiChatServiceTests.cs
│   └── Registry/
│       └── AiRegistryTests.cs
└── Umbraco.Ai.Web.Tests/
    ├── Umbraco.Ai.Web.Tests.csproj
    ├── Controllers/
    │   ├── ProvidersControllerTests.cs
    │   ├── ConnectionsControllerTests.cs
    │   └── ProfilesControllerTests.cs
    └── Integration/
        └── ManagementApiIntegrationTests.cs

.github/workflows/
└── test.yml
```

### Modified Files
- `Directory.Packages.props` - Add test package versions
- `Umbraco.Ai.sln` - Add test projects

---

## Critical Source Files to Reference

These are the key implementation files that tests will exercise:

| File | Purpose |
|------|---------|
| `src/Umbraco.Ai.Core/Services/AiSettingsResolver.cs` | Settings resolution logic |
| `src/Umbraco.Ai.Core/Services/AiChatClientFactory.cs` | Client creation and middleware |
| `src/Umbraco.Ai.Core/Services/AiProfileService.cs` | Profile CRUD |
| `src/Umbraco.Ai.Core/Services/AiConnectionService.cs` | Connection CRUD |
| `src/Umbraco.Ai.Core/Services/AiChatService.cs` | High-level chat API |
| `src/Umbraco.Ai.Core/Registry/AiRegistry.cs` | Provider registry |
| `src/Umbraco.Ai.Core/Configuration/UmbracoBuilderExtensions.cs` | DI registration |
