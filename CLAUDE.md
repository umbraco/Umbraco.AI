# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure

This is a monorepo containing Umbraco.Ai and its add-on packages:

| Product | Description | Location |
|---------|-------------|----------|
| **Umbraco.Ai** | Core AI integration layer for Umbraco CMS | `Umbraco.Ai/` |
| **Umbraco.Ai.Prompt** | Prompt template management add-on | `Umbraco.Ai.Prompt/` |
| **Umbraco.Ai.Agent** | AI agent management add-on | `Umbraco.Ai.Agent/` |
| **Umbraco.Ai.OpenAi** | OpenAI provider plugin | `Umbraco.Ai.OpenAi/` |
| **Umbraco.Ai.Anthropic** | Anthropic provider plugin | `Umbraco.Ai.Anthropic/` |

Each product has its own solution file, CLAUDE.md, and can be built independently. For detailed guidance on a specific product, see its CLAUDE.md file.

## Local Development Setup

### Quick Start

```bash
# One-time setup: creates unified solution + demo site
.\Install-DemoSite.ps1

# Open the unified solution
# Umbraco.Ai.local.sln

# Build everything
dotnet build Umbraco.Ai.local.sln
```

### Demo Site

The setup script creates:
- `Umbraco.Ai.local.sln` - Unified solution with all products
- `demo/Umbraco.Ai.DemoSite/` - Umbraco instance with all packages referenced

**Credentials:** admin@example.com / password1234

**Script options:**
- `-SkipTemplateInstall` - Skip reinstalling Umbraco.Templates
- `-Force` - Recreate demo if it already exists

## Build Commands

### .NET

```bash
# Build unified solution (all products + demo)
dotnet build Umbraco.Ai.local.sln

# Build individual product
dotnet build Umbraco.Ai/Umbraco.Ai.sln
dotnet build Umbraco.Ai.OpenAi/Umbraco.Ai.OpenAi.sln
dotnet build Umbraco.Ai.Anthropic/Umbraco.Ai.Anthropic.sln
dotnet build Umbraco.Ai.Prompt/Umbraco.Ai.Prompt.sln
dotnet build Umbraco.Ai.Agent/Umbraco.Ai.Agent.sln

# Run tests for a product
dotnet test Umbraco.Ai/Umbraco.Ai.sln
```

### Frontend (npm)

This monorepo uses **npm workspaces** for efficient dependency management. Add-on packages reference the local core package using `workspace:*`, which automatically resolves to the local workspace during development and to the published version during release.

```bash
# Install all workspace dependencies (run from root)
npm install

# Build all frontends (sequential: core -> prompt -> agent)
npm run build

# Watch all frontends in parallel
npm run watch

# Generate OpenAPI clients (requires running demo site)
npm run generate-client

# Target specific products
npm run build:core
npm run build:prompt
npm run build:agent
npm run watch:core
npm run watch:prompt
npm run watch:agent
```

**Workspace Benefits:**
- Single `npm install` installs all dependencies across all packages
- Add-ons automatically use the local `@umbraco-ai/core` during development
- Common dependencies are hoisted to the root `node_modules`
- When you run `npm pack`, `workspace:*` is automatically replaced with the actual version

### Distribution Build

```bash
# Build all NuGet packages for distribution
.\Build-Distribution.ps1
```

Output goes to `dist/nupkg/`.

## Architecture Overview

### Product Dependencies

```
Umbraco.Ai (Core)
    ├── Umbraco.Ai.OpenAi (Provider - depends on Core)
    ├── Umbraco.Ai.Anthropic (Provider - depends on Core)
    ├── Umbraco.Ai.Prompt (Add-on - depends on Core)
    └── Umbraco.Ai.Agent (Add-on - depends on Core)
```

### Standard Project Structure

**Core and Add-on packages** (Umbraco.Ai, Umbraco.Ai.Agent, Umbraco.Ai.Prompt) follow this structure:

```
ProductName/
├── src/
│   ├── ProductName.Core/           # Domain models, services, interfaces
│   ├── ProductName.Web/            # Management API (controllers, models)
│   ├── ProductName.Web.StaticAssets/  # TypeScript/Lit frontend
│   │   └── Client/                 # npm project
│   ├── ProductName.Persistence/    # EF Core DbContext, repositories
│   ├── ProductName.Persistence.SqlServer/  # SQL Server migrations
│   ├── ProductName.Persistence.Sqlite/     # SQLite migrations
│   ├── ProductName.Startup/        # Umbraco Composer for DI
│   └── ProductName/                # Meta-package bundling all
├── tests/
│   ├── ProductName.Tests.Unit/
│   ├── ProductName.Tests.Integration/
│   └── ProductName.Tests.Common/
├── ProductName.sln                 # Individual solution
└── CLAUDE.md                       # Product-specific guidance
```

**Provider packages** (Umbraco.Ai.OpenAi, Umbraco.Ai.Anthropic) use a simplified structure:

```
ProviderName/
├── src/
│   └── ProviderName/               # Single project with provider, capabilities, settings
├── tests/
│   └── ProviderName.Tests.Unit/
├── ProviderName.sln
└── CLAUDE.md
```

### Core Concepts (Umbraco.Ai)

- **Providers** - Installable plugins for AI services (e.g., OpenAI)
- **Connections** - Store API keys and provider settings
- **Profiles** - Combine connection with model settings for use cases
- **Capabilities** - Chat, Embedding, etc.

Built on Microsoft.Extensions.AI (M.E.AI) with a "thin wrapper" philosophy.

## Key Files

| File | Purpose |
|------|---------|
| `Install-DemoSite.ps1` | Creates unified local dev environment |
| `Build-Distribution.ps1` | Builds all NuGet packages |
| `Umbraco.Ai.local.sln` | Unified solution (generated) |
| `package.json` | Root npm scripts for frontend builds |

## Frontend Architecture

All products use the same frontend stack:
- **Lit** web components
- **TypeScript**
- **Vite** for building
- **@hey-api/openapi-ts** for API client generation

Frontend projects are in `src/*/Web.StaticAssets/Client/` and compile to `wwwroot/App_Plugins/`.

## Database

- SQL Server and SQLite supported via EF Core
- Each product has its own migrations with prefixes:
  - `UmbracoAi_` - Core
  - `UmbracoAiPrompt_` - Prompt add-on
  - `UmbracoAiAgent_` - Agent add-on

## Target Framework

- .NET 10.0 (`net10.0`)
- Umbraco CMS 17.x
- Central Package Management via `Directory.Packages.props`

## Excluded Folders

- `Ref/` - External reference projects (not part of build)
- `Umbraco.Ai-entity-snapshot-service/` - Legacy/alternate reference

## Coding Standards

These standards apply to all packages in this repository. Sub-project CLAUDE.md files should reference this document for shared conventions.

### Method Naming Conventions

#### Async Methods: `[Action][Entity]Async`

All async service methods MUST follow the pattern `[Action][Entity]Async`:

| Component | Description | Examples |
|-----------|-------------|----------|
| **Action** | Verb describing the operation | `Get`, `Create`, `Update`, `Delete`, `Save`, `Find`, `List`, `Validate`, `Execute`, `Generate` |
| **Entity** | Noun describing what's being operated on | `Profile`, `Connection`, `Prompt`, `Agent`, `Context`, `ChatResponse` |
| **Async** | Required suffix for async methods | Always `Async` |

**Correct Examples:**
```csharp
// GOOD - follows [Action][Entity]Async
Task<AiProfile?> GetProfileAsync(Guid id, CancellationToken ct);
Task<AiConnection> CreateConnectionAsync(AiConnection connection, CancellationToken ct);
Task DeleteAgentAsync(Guid id, CancellationToken ct);
Task<ChatResponse> GetChatResponseAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);
Task<IEnumerable<AiPrompt>> GetPromptsAsync(CancellationToken ct);
Task<PagedResult<AiAgent>> GetAgentsPagedAsync(int skip, int take, CancellationToken ct);
```

**Incorrect Examples:**
```csharp
// BAD - missing entity name
Task<AiProfile?> GetAsync(Guid id, CancellationToken ct);           // Should be: GetProfileAsync
Task DeleteAsync(Guid id, CancellationToken ct);                     // Should be: DeleteAgentAsync
Task<ChatResponse> GetResponseAsync(...);                            // Should be: GetChatResponseAsync

// BAD - entity before action
Task<bool> ProfileExistsAsync(string alias, CancellationToken ct);   // Should be: ExistsProfileAsync or ProfileAliasExistsAsync

// BAD - missing Async suffix
Task<AiProfile?> GetProfile(Guid id, CancellationToken ct);          // Should be: GetProfileAsync
```

#### Variations and Qualifiers

Qualifiers like `ByAlias`, `Paged`, `All`, `Default` come after the entity:

```csharp
// Qualified lookups
Task<AiProfile?> GetProfileByAliasAsync(string alias, CancellationToken ct);
Task<AiConnection?> GetConnectionByIdAsync(Guid id, CancellationToken ct);

// Collection operations
Task<IEnumerable<AiProfile>> GetAllProfilesAsync(CancellationToken ct);
Task<PagedResult<AiPrompt>> GetPromptsPagedAsync(int skip, int take, CancellationToken ct);

// Default/specific retrieval
Task<AiProfile?> GetDefaultProfileAsync(AiCapability capability, CancellationToken ct);
```

#### Existence Checks

For checking if something exists, prefer `[Entity][Qualifier]ExistsAsync`:

```csharp
// Acceptable patterns for existence checks
Task<bool> ProfileAliasExistsAsync(string alias, CancellationToken ct);
Task<bool> AgentAliasExistsAsync(string alias, Guid? excludeId, CancellationToken ct);
```

### Repository Access Pattern

**Repositories are internal implementation details of their corresponding service.** Only the entity's service class may access its repository directly.

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Controller    │────▶│    Service      │────▶│   Repository    │
│   Other Service │────▶│  (e.g. Profile) │     │  (e.g. Profile) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        ✓                       ✓                       ✗
   Uses Service            Uses Repository         Direct access
                           internally              forbidden
```

**Correct:**
```csharp
// Other services use the entity service, not the repository
public class AiChatService
{
    private readonly IAiProfileService _profileService;  // ✓ Uses service

    public async Task<IChatClient> GetChatClientAsync(Guid profileId, CancellationToken ct)
    {
        var profile = await _profileService.GetProfileAsync(profileId, ct);  // ✓
        // ...
    }
}
```

**Incorrect:**
```csharp
// BAD: Service directly accessing another entity's repository
public class AiChatService
{
    private readonly IAiProfileRepository _profileRepository;  // ✗ Direct repository access

    public async Task<IChatClient> GetChatClientAsync(Guid profileId, CancellationToken ct)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId, ct);  // ✗
        // ...
    }
}
```

**Why this matters:**
- Services encapsulate business logic, validation, and caching
- Bypassing services leads to inconsistent behavior
- Makes refactoring and testing easier
- Repositories should be `internal` to the persistence assembly

### Repository Naming Conventions

Repository methods follow the same `[Action][Entity]Async` pattern as services, but may use more database-centric verbs:

| Service Method | Repository Method |
|----------------|-------------------|
| `GetProfileAsync` | `GetByIdAsync` (entity is implicit in repository context) |
| `GetProfileByAliasAsync` | `GetByAliasAsync` |
| `GetAllProfilesAsync` | `GetAllAsync` |
| `SaveProfileAsync` | `AddAsync` / `UpdateAsync` or `UpsertAsync` |
| `DeleteProfileAsync` | `DeleteAsync` |

Repository methods can use shorter names since they operate on a single entity type:
```csharp
public interface IAiProfileRepository
{
    Task<AiProfile?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AiProfile?> GetByAliasAsync(string alias, CancellationToken ct);
    Task<IEnumerable<AiProfile>> GetAllAsync(CancellationToken ct);
    Task AddAsync(AiProfile profile, CancellationToken ct);
    Task UpdateAsync(AiProfile profile, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
```

### Extension Methods

All extension methods MUST be placed in the `Umbraco.Ai.Extensions` namespace (or the product-specific equivalent like `Umbraco.Ai.Prompt.Extensions`) for ease of discovery via IntelliSense.
