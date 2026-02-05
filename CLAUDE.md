# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure

This is a monorepo containing Umbraco.AI and its add-on packages:

| Product | Description | Location |
|---------|-------------|----------|
| **Umbraco.AI** | Core AI integration layer for Umbraco CMS | `Umbraco.AI/` |
| **Umbraco.AI.Prompt** | Prompt template management add-on | `Umbraco.AI.Prompt/` |
| **Umbraco.AI.Agent** | AI agent management add-on | `Umbraco.AI.Agent/` |
| **Umbraco.AI.Agent.Copilot** | Copilot chat UI for agents (frontend-only) | `Umbraco.AI.Agent.Copilot/` |
| **Umbraco.AI.OpenAI** | OpenAI provider plugin | `Umbraco.AI.OpenAI/` |
| **Umbraco.AI.Anthropic** | Anthropic provider plugin | `Umbraco.AI.Anthropic/` |
| **Umbraco.AI.Amazon** | Amazon Bedrock provider plugin | `Umbraco.AI.Amazon/` |
| **Umbraco.AI.Google** | Google Gemini provider plugin | `Umbraco.AI.Google/` |
| **Umbraco.AI.MicrosoftFoundry** | Microsoft AI Foundry provider plugin | `Umbraco.AI.MicrosoftFoundry/` |

Each product has its own solution file, CLAUDE.md, and can be built independently. For detailed guidance on a specific product, see its CLAUDE.md file.

## Development Environment

### Initial Setup

Use the setup skill for first-time repository configuration:
```bash
/repo-setup  # Interactive setup: git hooks, demo site, dependencies, build
```

### Demo Site

**Location:** `demo/Umbraco.AI.DemoSite/`
**Credentials:** admin@example.com / password1234

**Infrastructure Operations:**
```bash
/demo-site-management start              # Start with DemoSite-Claude profile (dynamic port)
/demo-site-management stop               # Stop the running demo site
/demo-site-management open               # Open in browser (auto-discovers port)
/demo-site-management generate-client    # Generate OpenAPI clients
/demo-site-management status             # Check running status and port info
```

**Browser Automation (Playwright):**
```bash
/demo-site-automation login                         # Login to Umbraco backoffice
/demo-site-automation navigate-to-connections       # Navigate to AI settings sections
/demo-site-automation create-connection [provider]  # Create/edit AI entities
```

**Architecture Notes:**
- `DemoSite-Claude` profile uses dynamic ports to avoid conflicts between worktrees
- HTTP over named pipes: `umbraco.demosite.{branch-or-worktree}`
- Site address endpoint: `/site-address` (query via named pipe to get HTTPS address)
- Named pipes auto-cleanup on process exit
- Multiple worktrees run simultaneously without port conflicts

## Project Management

**Azure DevOps Configuration:**

This repository uses two Azure DevOps projects:

**Backlog & Work Items:**
- **Project:** D-Team Tracker
- **Backlog:** AI Team
- **Default Tag:** Umbraco AI

**CI/CD Pipelines:**
- **Project:** Umbraco AI

**Default behavior:**
- Unless otherwise specified, all Azure DevOps related tasks (work items, issues, sprints, etc.) should be managed in the D-Team Tracker project under the AI Team backlog
- All backlog items created for this repository should be tagged with `Umbraco AI` unless explicitly stated otherwise
- CI/CD pipelines and build configurations are located in the Umbraco AI project

**Searching for Work Items:**

The Azure DevOps project used for backlog management is shared across multiple Umbraco products. When searching for work items without filters, you will get results from multiple product teams.

**IMPORTANT**: Always scope searches to this repository's team backlog to avoid cross-product results:

1. **Preferred approach**: Filter by team backlog using `wit_list_backlog_work_items` with the project/team specified in the configuration above

2. **Fallback approach**: Fetch team backlog items first, then query over those specific work item IDs

3. **Avoid**: Unfiltered searches that return items across all product teams

## Build Commands

### .NET

```bash
# Build unified solution (all products + demo)
dotnet build Umbraco.AI.local.sln

# Build individual product
dotnet build Umbraco.AI/Umbraco.AI.sln
dotnet build Umbraco.AI.OpenAI/Umbraco.AI.OpenAI.sln
dotnet build Umbraco.AI.Anthropic/Umbraco.AI.Anthropic.sln
dotnet build Umbraco.AI.Amazon/Umbraco.AI.Amazon.sln
dotnet build Umbraco.AI.Google/Umbraco.AI.Google.sln
dotnet build Umbraco.AI.MicrosoftFoundry/Umbraco.AI.MicrosoftFoundry.sln
dotnet build Umbraco.AI.Prompt/Umbraco.AI.Prompt.sln
dotnet build Umbraco.AI.Agent/Umbraco.AI.Agent.sln

# Run tests for a product
dotnet test Umbraco.AI/Umbraco.AI.sln
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

# Generate OpenAPI clients (requires running demo site with DemoSite-Claude profile)
npm run generate-client  # Automatically discovers port via named pipe

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
Umbraco.AI (Core)
    ├── Umbraco.AI.OpenAI (Provider - depends on Core)
    ├── Umbraco.AI.Anthropic (Provider - depends on Core)
    ├── Umbraco.AI.Amazon (Provider - depends on Core)
    ├── Umbraco.AI.Google (Provider - depends on Core)
    ├── Umbraco.AI.MicrosoftFoundry (Provider - depends on Core)
    ├── Umbraco.AI.Prompt (Add-on - depends on Core)
    └── Umbraco.AI.Agent (Add-on - depends on Core)
```

### Standard Project Structure

**Core and Add-on packages** (Umbraco.AI, Umbraco.AI.Agent, Umbraco.AI.Prompt) follow this structure:

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

**Provider packages** (Umbraco.AI.OpenAI, Umbraco.AI.Anthropic, Umbraco.AI.Amazon, Umbraco.AI.Google, Umbraco.AI.MicrosoftFoundry) use a simplified structure:

```
ProviderName/
├── src/
│   └── ProviderName/               # Single project with provider, capabilities, settings
├── tests/
│   └── ProviderName.Tests.Unit/
├── ProviderName.sln
└── CLAUDE.md
```

### Core Concepts (Umbraco.AI)

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
| `Umbraco.AI.local.sln` | Unified solution (generated) |
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
  - `UmbracoAI_` - Core
  - `UmbracoAIPrompt_` - Prompt add-on
  - `UmbracoAIAgent_` - Agent add-on

## Target Framework

- .NET 10.0 (`net10.0`)
- Umbraco CMS 17.x
- Central Package Management via `Directory.Packages.props`

## Release and Hotfix Branch Packaging

### Release Manifest

On `release/*` branches, CI **requires** a `release-manifest.json` at repo root:

```json
[
  "Umbraco.AI",
  "Umbraco.AI.OpenAI"
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
1. Scan for all `Umbraco.AI*` product folders
2. Present an interactive multiselect interface
3. Generate `release-manifest.json` at the repository root

The manifest is treated as the authoritative list of packages to pack and release. CI will fail if any changed product is missing from the list. This ensures intentional releases and prevents accidental package publishing.

On `hotfix/*` branches, the manifest is **optional**:
- If present: Enforced the same way as release branches (explicit pack list)
- If absent: Change detection is used automatically

### Hotfix Change Detection

On `hotfix/*` branches, the CI change detection uses **per-product tag-based comparison** to accurately identify which products have been hotfixed:

**How it works:**
- For each product, finds the most recent release tag (e.g., `Umbraco.AI@1.0.0`)
- Compares changes in that product's folder since its own tag
- Only substantive changes trigger a rebuild (excludes `CHANGELOG.md`, `version.json`)
- Falls back to merge-base with main if no tag exists (new products)

**Example workflow:**
```bash
# Hotfix an old version
git checkout Umbraco.AI@1.0.0
git checkout -b hotfix/fix-critical-bug
# Make fix in Umbraco.AI/
git commit -m "fix(core): Fix critical bug"

# CI automatically detects:
# - Umbraco.AI changed (diff since Umbraco.AI@1.0.0)
# - Other products unchanged (diff since their respective tags)
```

**Multi-product hotfixes:**
- If products were released together, use one hotfix branch
- If products are at different points in history, use separate hotfix branches
- Each product always compares against its own latest release tag

This ensures hotfix builds only include changes since the last release of each product, regardless of unreleased changes on main.

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

**Example tags:** `Umbraco.AI@1.1.0`, `Umbraco.AI.OpenAI@1.2.0`

For detailed release workflows, see [CONTRIBUTING.md](CONTRIBUTING.md#release-process).

## Cross-Product Dependency Management

Add-on packages and providers depend on Umbraco.AI (Core). These dependencies are managed using **Central Package Management** via `Directory.Packages.props`.

### Version Ranges for Add-ons

**Always use version ranges** for cross-product dependencies. This allows add-ons to work with a range of Core versions without requiring simultaneous releases.

When an add-on (e.g., Umbraco.AI.Prompt or Umbraco.AI.Agent) needs to depend on a specific version range of Core, create a `Directory.Packages.props` file within the product folder:

**Example:** `Umbraco.AI.Prompt/Directory.Packages.props`
```xml
<Project>
  <ItemGroup>
    <!-- Minimum version 1.1.0, accepts all 1.x versions -->
    <PackageVersion Include="Umbraco.AI.Core" Version="[1.1.0, 1.999.999)" />
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
   <PackageVersion Include="Umbraco.AI.Core" Version="[1.0.0, 1.999.999)" />
   ```

2. **Root Directory.Packages.props** may have a broader or different range:
   ```xml
   <PackageVersion Include="Umbraco.AI.Core" Version="[1.0.0, 1.999.999)" />
   ```

3. When Agent is ready for Core 1.1.0+, update its `Directory.Packages.props` minimum version:
   ```xml
   <PackageVersion Include="Umbraco.AI.Core" Version="[1.1.0, 1.999.999)" />
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
- `Umbraco.AI-entity-snapshot-service/` - Legacy/alternate reference

## Changelog Generation

Each product maintains its own `CHANGELOG.md` file at the product root, auto-generated from git history using conventional commits.

### Commit Message Format

All commits should follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <description>
```

**Examples:**
```bash
feat(chat): Add streaming support
fix(openai): Handle rate limit errors
docs(core): Update API examples
```

**Formatting Rules (enforced by commitlint):**

1. **Subject must be sentence-case** - Capitalize the first word after the scope
   - ✅ `fix(frontend): Prevent scripts from hanging`
   - ❌ `fix(frontend): prevent scripts from hanging`

2. **Scope must be valid** - Use one of the allowed scopes defined in `commitlint.config.js`
   - **Valid types**: `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `perf`, `ci`, `revert`, `build`
   - **Valid scopes**: Dynamically loaded from all `<Product>/changelog.config.json` files + meta scopes (`deps`, `ci`, `docs`, `release`)
   - **Single scope only** - Multiple scopes are not supported (e.g., `fix(core,agent):` is invalid)
   - **To list current options**: `npm run commit-options` or `node scripts/list-commit-options.js`
   - **For Claude Code**: Read `commitlint.config.js` at runtime to discover valid types and scopes - never use hardcoded lists

3. **Body lines must not exceed 100 characters** - Wrap long lines in the commit body

4. **Split commits when changes affect multiple areas** - If your changes span multiple scopes, create separate commits
   - ✅ Split into: `fix(core): Fix issue A` + `fix(agent): Fix issue B`
   - ❌ Don't use: `fix(core,agent): Fix issues`

Commits are validated by commitlint on commit (soft warnings - allows non-conventional commits but warns).

### Generating Changelogs

Changelogs are generated manually before creating a release:

```bash
# List available products
npm run changelog:list

# Generate changelog for a specific product
npm run changelog -- --product=Umbraco.AI --version=1.1.0

# Generate for unreleased changes
npm run changelog -- --product=Umbraco.AI --unreleased

# PowerShell wrapper
.\scripts\generate-changelog.ps1 -Product Umbraco.AI -Version 1.1.0

# Bash wrapper
./scripts/generate-changelog.sh --product=Umbraco.AI --version=1.1.0
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
Task<AIProfile?> GetProfileAsync(Guid id, CancellationToken ct);
Task<AIConnection> CreateConnectionAsync(AIConnection connection, CancellationToken ct);
Task DeleteAgentAsync(Guid id, CancellationToken ct);
Task<ChatResponse> GetChatResponseAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);
Task<IEnumerable<AIPrompt>> GetPromptsAsync(CancellationToken ct);
Task<PagedResult<AIAgent>> GetAgentsPagedAsync(int skip, int take, CancellationToken ct);
```

**Incorrect Examples:**
```csharp
// BAD - missing entity name
Task<AIProfile?> GetAsync(Guid id, CancellationToken ct);           // Should be: GetProfileAsync
Task DeleteAsync(Guid id, CancellationToken ct);                     // Should be: DeleteAgentAsync
Task<ChatResponse> GetResponseAsync(...);                            // Should be: GetChatResponseAsync

// BAD - entity before action
Task<bool> ProfileExistsAsync(string alias, CancellationToken ct);   // Should be: ExistsProfileAsync or ProfileAliasExistsAsync

// BAD - missing Async suffix
Task<AIProfile?> GetProfile(Guid id, CancellationToken ct);          // Should be: GetProfileAsync
```

#### Variations and Qualifiers

Qualifiers like `ByAlias`, `Paged`, `All`, `Default` come after the entity:

```csharp
// Qualified lookups
Task<AIProfile?> GetProfileByAliasAsync(string alias, CancellationToken ct);
Task<AIConnection?> GetConnectionByIdAsync(Guid id, CancellationToken ct);

// Collection operations
Task<IEnumerable<AIProfile>> GetAllProfilesAsync(CancellationToken ct);
Task<PagedResult<AIPrompt>> GetPromptsPagedAsync(int skip, int take, CancellationToken ct);

// Default/specific retrieval
Task<AIProfile?> GetDefaultProfileAsync(AICapability capability, CancellationToken ct);
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
public class AIChatService
{
    private readonly IAIProfileService _profileService;  // ✓ Uses service

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
public class AIChatService
{
    private readonly IAIProfileRepository _profileRepository;  // ✗ Direct repository access

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
public interface IAIProfileRepository
{
    Task<AIProfile?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AIProfile?> GetByAliasAsync(string alias, CancellationToken ct);
    Task<IEnumerable<AIProfile>> GetAllAsync(CancellationToken ct);
    Task AddAsync(AIProfile profile, CancellationToken ct);
    Task UpdateAsync(AIProfile profile, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
```

### Extension Methods

All extension methods MUST be placed in the `Umbraco.AI.Extensions` namespace (or the product-specific equivalent like `Umbraco.AI.Prompt.Extensions`) for ease of discovery via IntelliSense.

# Lessons Learned