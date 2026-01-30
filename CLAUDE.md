# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure

This is a monorepo containing Umbraco.Ai and its add-on packages:

| Product | Description | Location |
|---------|-------------|----------|
| **Umbraco.Ai** | Core AI integration layer for Umbraco CMS | `Umbraco.Ai/` |
| **Umbraco.Ai.Prompt** | Prompt template management add-on | `Umbraco.Ai.Prompt/` |
| **Umbraco.Ai.Agent** | AI agent management add-on | `Umbraco.Ai.Agent/` |
| **Umbraco.Ai.Agent.Copilot** | Copilot chat UI for agents (frontend-only) | `Umbraco.Ai.Agent.Copilot/` |
| **Umbraco.Ai.OpenAi** | OpenAI provider plugin | `Umbraco.Ai.OpenAi/` |
| **Umbraco.Ai.Anthropic** | Anthropic provider plugin | `Umbraco.Ai.Anthropic/` |
| **Umbraco.Ai.Amazon** | Amazon Bedrock provider plugin | `Umbraco.Ai.Amazon/` |
| **Umbraco.Ai.Google** | Google Gemini provider plugin | `Umbraco.Ai.Google/` |
| **Umbraco.Ai.MicrosoftFoundry** | Microsoft AI Foundry provider plugin | `Umbraco.Ai.MicrosoftFoundry/` |

Each product has its own solution file, CLAUDE.md, and can be built independently. For detailed guidance on a specific product, see its CLAUDE.md file.

## Local Development Setup

### Quick Start

```bash
# One-time setup: creates unified solution + demo site
.\scripts\install-demo-site.ps1  # Windows
./scripts/install-demo-site.sh   # Linux/Mac

# Configure git hooks (enforces branch naming convention)
.\scripts\setup-git-hooks.ps1  # Windows
./scripts/setup-git-hooks.sh   # Linux/Mac

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

**Script options (PowerShell):**
- `-SkipTemplateInstall` - Skip reinstalling Umbraco.Templates
- `-Force` - Recreate demo if it already exists

**Script options (Bash):**
- `-s, --skip-template-install` - Skip reinstalling Umbraco.Templates
- `-f, --force` - Recreate demo if it already exists

## Build Commands

### .NET

```bash
# Build unified solution (all products + demo)
dotnet build Umbraco.Ai.local.sln

# Build individual product
dotnet build Umbraco.Ai/Umbraco.Ai.sln
dotnet build Umbraco.Ai.OpenAi/Umbraco.Ai.OpenAi.sln
dotnet build Umbraco.Ai.Anthropic/Umbraco.Ai.Anthropic.sln
dotnet build Umbraco.Ai.Amazon/Umbraco.Ai.Amazon.sln
dotnet build Umbraco.Ai.Google/Umbraco.Ai.Google.sln
dotnet build Umbraco.Ai.MicrosoftFoundry/Umbraco.Ai.MicrosoftFoundry.sln
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

## Architecture Overview

### Product Dependencies

```
Umbraco.Ai (Core)
    ├── Umbraco.Ai.OpenAi (Provider - depends on Core)
    ├── Umbraco.Ai.Anthropic (Provider - depends on Core)
    ├── Umbraco.Ai.Amazon (Provider - depends on Core)
    ├── Umbraco.Ai.Google (Provider - depends on Core)
    ├── Umbraco.Ai.MicrosoftFoundry (Provider - depends on Core)
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

**Provider packages** (Umbraco.Ai.OpenAi, Umbraco.Ai.Anthropic, Umbraco.Ai.Amazon, Umbraco.Ai.Google, Umbraco.Ai.MicrosoftFoundry) use a simplified structure:

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
| `scripts/install-demo-site.ps1` | Creates unified local dev environment (Windows) |
| `scripts/install-demo-site.sh` | Creates unified local dev environment (Linux/Mac) |
| `scripts/generate-changelog.ps1` | Generate changelogs for release (Windows) |
| `scripts/generate-changelog.sh` | Generate changelogs for release (Linux/Mac) |
| `scripts/generate-changelog.js` | Node.js changelog generator (main implementation) |
| `scripts/generate-release-manifest.ps1` | Interactive release manifest generator (Windows) |
| `scripts/generate-release-manifest.sh` | Interactive release manifest generator (Linux/Mac) |
| `Umbraco.Ai.local.sln` | Unified solution (generated) |
| `package.json` | Root npm scripts for frontend builds and changelog generation |
| `commitlint.config.js` | Commit message validation with dynamic scope loading |
| `release-manifest.json` | Release/hotfix pack list (required on `release/*`, optional on `hotfix/*`) |
| `pack-manifest` (artifact) | CI-generated metadata for deployed packages (used by release pipeline for git tagging) |
| `<Product>/changelog.config.json` | Per-product scopes for changelog generation (auto-discovered) |
| `<Product>/CHANGELOG.md` | Per-product changelog (auto-generated from git history) |

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

## Release and Hotfix Branch Packaging

### Release Manifest

On `release/*` branches, CI **requires** a `release-manifest.json` at repo root:

```json
[
  "Umbraco.Ai",
  "Umbraco.Ai.OpenAi"
]
```

**Generating the manifest:**

Use the interactive script to select which products to include:

```bash
# Windows
.\scripts\generate-release-manifest.ps1

# Linux/Mac
./scripts/generate-release-manifest.sh
```

The script will:
1. Scan for all `Umbraco.Ai*` product folders
2. Present an interactive multiselect interface
3. Generate `release-manifest.json` at the repository root

The manifest is treated as the authoritative list of packages to pack and release. CI will fail if any changed product is missing from the list. This ensures intentional releases and prevents accidental package publishing.

On `hotfix/*` branches, the manifest is **optional**:
- If present: Enforced the same way as release branches (explicit pack list)
- If absent: Change detection is used automatically

### Release Pipeline Artifacts

The CI build produces the following artifacts for deployment:

| Artifact | Description |
|----------|-------------|
| `all-nuget-packages` | All .nupkg files for NuGet deployment |
| `all-npm-packages` | All .tgz files for npm deployment |
| `pack-manifest` | Package metadata (name, version, type) for each package |

The Azure DevOps release pipeline:
1. Downloads these artifacts
2. Deploys packages to package feeds (MyGet for pre-release, NuGet.org/npm for production)
3. Tags the git repository with `[Product_Name]@[Version]` for each deployed package

**Example tags:** `Umbraco.Ai@1.1.0`, `Umbraco.Ai.OpenAi@1.2.0`

For detailed release workflows, see [CONTRIBUTING.md](CONTRIBUTING.md#release-process).

## Cross-Product Dependency Management

Add-on packages and providers depend on Umbraco.Ai (Core). These dependencies are managed using **Central Package Management** via `Directory.Packages.props`.

### Version Ranges for Add-ons

**Always use version ranges** for cross-product dependencies. This allows add-ons to work with a range of Core versions without requiring simultaneous releases.

When an add-on (e.g., Umbraco.Ai.Prompt or Umbraco.Ai.Agent) needs to depend on a specific version range of Core, create a `Directory.Packages.props` file within the product folder:

**Example:** `Umbraco.Ai.Prompt/Directory.Packages.props`
```xml
<Project>
  <ItemGroup>
    <!-- Minimum version 1.1.0, accepts all 1.x versions -->
    <PackageVersion Include="Umbraco.Ai.Core" Version="[1.1.0, 1.999.999)" />
  </ItemGroup>
</Project>
```

The range format `[minimum, maximum)` means:
- `[` = inclusive lower bound (>= 1.1.0)
- `)` = exclusive upper bound (< 1.999.999)
- Result: accepts any 1.x version from 1.1.0 onwards

**How it works:**
- **Root level** (`Directory.Packages.props` at repo root): Defines default package versions and ranges for all products
- **Product level** (`ProductFolder/Directory.Packages.props`): Overrides specific package version ranges for that product only
- **During local development**: Project references (`UseProjectReferences=true`) bypass NuGet versions entirely
- **During CI/CD build**: Distribution builds (`UseProjectReferences=false`) use the specified NuGet version ranges

### Example Scenario

If you release Core 1.1.0 with breaking changes, but Agent 1.0.0 isn't ready for the upgrade:

1. **Agent's Directory.Packages.props** specifies minimum Core 1.0.0:
   ```xml
   <PackageVersion Include="Umbraco.Ai.Core" Version="[1.0.0, 1.999.999)" />
   ```

2. **Root Directory.Packages.props** may have a broader or different range:
   ```xml
   <PackageVersion Include="Umbraco.Ai.Core" Version="[1.0.0, 1.999.999)" />
   ```

3. When Agent is ready for Core 1.1.0+, update its `Directory.Packages.props` minimum version:
   ```xml
   <PackageVersion Include="Umbraco.Ai.Core" Version="[1.1.0, 1.999.999)" />
   ```

### Version Range Guidelines

| Scenario | Range Format | Example | Use Case |
|----------|--------------|---------|----------|
| Minor version series | `[X.Y.0, X.999.999)` | `[1.1.0, 1.999.999)` | Add-on requires min 1.1.0, accepts all 1.x |
| Specific minimum | `[X.Y.Z, X.999.999)` | `[1.1.5, 1.999.999)` | Add-on requires min 1.1.5, accepts all 1.x |
| Exact version | `[X.Y.Z]` | `[1.1.0]` | **Avoid** - prevents any updates |

### Best Practices

- **Local development**: Always use project references (default). Changes to Core are immediately visible to add-ons.
- **Release coordination**: When releasing Core with breaking changes, verify all dependent products in the release manifest are updated to the new minimum version.
- **Version ranges**: Always use `[X.Y.0, X.999.999)` format where X.Y.0 is the minimum supported Core version.
- **Testing**: Always test with `UseProjectReferences=false` before releasing to ensure NuGet dependencies resolve correctly.

## Excluded Folders

- `Ref/` - External reference projects (not part of build)
- `Umbraco.Ai-entity-snapshot-service/` - Legacy/alternate reference

## Changelog Generation

Each product maintains its own `CHANGELOG.md` file at the product root, auto-generated from git history using conventional commits.

### Commit Message Format

All commits should follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <description>
```

**Examples:**
```bash
feat(chat): add streaming support
fix(openai): handle rate limit errors
docs(core): update API examples
```

Commits are validated by commitlint on commit (soft warnings - allows non-conventional commits but warns).

### Generating Changelogs

Changelogs are generated manually before creating a release:

```bash
# List available products
npm run changelog:list

# Generate changelog for a specific product
npm run changelog -- --product=Umbraco.Ai --version=1.1.0

# Generate for unreleased changes
npm run changelog -- --product=Umbraco.Ai --unreleased

# PowerShell wrapper
.\scripts\generate-changelog.ps1 -Product Umbraco.Ai -Version 1.1.0

# Bash wrapper
./scripts/generate-changelog.sh --product=Umbraco.Ai --version=1.1.0
```

Each product has a `changelog.config.json` file defining its scopes. The generation script automatically discovers all products by scanning for these config files - no hardcoded product lists.

### Automated Validation

On `release/*` and `hotfix/*` branches, Azure DevOps automatically validates changelogs:
- ✅ CHANGELOG.md exists for each product in release-manifest.json
- ✅ CHANGELOG.md was updated in recent commits
- ✅ Version in CHANGELOG.md matches version.json
- ❌ **Build fails if validation fails**

This prevents releasing without proper changelog documentation for both regular releases and emergency hotfixes.

**For full details on commit scopes, changelog workflow, and release process, see [CONTRIBUTING.md](CONTRIBUTING.md#maintaining-changelogs).**

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
