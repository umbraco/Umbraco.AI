# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure

This is a monorepo containing Umbraco.AI and its add-on packages:

| Product                         | Description                                | Location                       |
| ------------------------------- | ------------------------------------------ | ------------------------------ |
| **Umbraco.AI**                  | Core AI integration layer for Umbraco CMS  | `Umbraco.AI/`                  |
| **Umbraco.AI.Prompt**           | Prompt template management add-on          | `Umbraco.AI.Prompt/`           |
| **Umbraco.AI.Agent**            | AI agent management add-on                 | `Umbraco.AI.Agent/`            |
| **Umbraco.AI.Agent.UI**         | Reusable chat UI infrastructure (library)  | `Umbraco.AI.Agent.UI/`         |
| **Umbraco.AI.Agent.Copilot**    | Copilot chat UI for agents (frontend-only) | `Umbraco.AI.Agent.Copilot/`    |
| **Umbraco.AI.OpenAI**           | OpenAI provider plugin                     | `Umbraco.AI.OpenAI/`           |
| **Umbraco.AI.Anthropic**        | Anthropic provider plugin                  | `Umbraco.AI.Anthropic/`        |
| **Umbraco.AI.Amazon**           | Amazon Bedrock provider plugin             | `Umbraco.AI.Amazon/`           |
| **Umbraco.AI.Google**           | Google Gemini provider plugin              | `Umbraco.AI.Google/`           |
| **Umbraco.AI.MicrosoftFoundry** | Microsoft AI Foundry provider plugin       | `Umbraco.AI.MicrosoftFoundry/` |

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

# Build all frontends (sequential: core -> prompt -> agent -> agent-ui -> copilot)
npm run build

# Watch all frontends in parallel
npm run watch

# Generate OpenAPI clients (requires running demo site with DemoSite-Claude profile)
npm run generate-client  # Automatically discovers port via named pipe

# Target specific products
npm run build:core
npm run build:prompt
npm run build:agent
npm run build:agent-ui
npm run build:copilot
npm run watch:core
npm run watch:prompt
npm run watch:agent
npm run watch:agent-ui
npm run watch:copilot
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
            ├── Umbraco.AI.Agent.UI (Frontend library - depends on Agent)
            └── Umbraco.AI.Agent.Copilot (Chat UI - depends on Agent + Agent.UI)
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

| File                                    | Purpose                                                                                |
| --------------------------------------- | -------------------------------------------------------------------------------------- |
| `scripts/install-demo-site.ps1`         | Creates unified local dev environment (Windows)                                        |
| `scripts/install-demo-site.sh`          | Creates unified local dev environment (Linux/Mac)                                      |
| `scripts/generate-changelog.ps1`        | Generate changelogs for release (Windows)                                              |
| `scripts/generate-changelog.sh`         | Generate changelogs for release (Linux/Mac)                                            |
| `scripts/generate-changelog.js`         | Node.js changelog generator (main implementation)                                      |
| `scripts/generate-release-manifest.ps1` | Interactive release manifest generator (Windows)                                       |
| `scripts/generate-release-manifest.sh`  | Interactive release manifest generator (Linux/Mac)                                     |
| `Umbraco.AI.local.sln`                  | Unified solution (generated)                                                           |
| `package.json`                          | Root npm scripts for frontend builds and changelog generation                          |
| `commitlint.config.js`                  | Commit message validation with dynamic scope loading                                   |
| `release-manifest.json`                 | Release/hotfix pack list (required on `release/*`, optional on `hotfix/*`)             |
| `pack-manifest` (artifact)              | CI-generated metadata for deployed packages (used by release pipeline for git tagging) |
| `<Product>/version.json`                | Per-product version (updated by `/release-management` skill)                           |
| `<Product>/changelog.config.json`       | Per-product scopes for changelog generation (auto-discovered)                          |
| `<Product>/CHANGELOG.md`                | Per-product changelog (auto-generated from git history)                                |

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

## Release Management Skills

The repository includes Claude Code skills to automate and orchestrate the release process:

### `/release-management` - Release Orchestration

**Use when:** Preparing a complete release from start to finish

**What it does:**
1. **Detects changed products** since their last release tags
2. **Analyzes commits** using conventional commit format to recommend version bumps:
   - BREAKING CHANGE or `!` → Major (1.0.0 → 2.0.0)
   - `feat:` commits → Minor (1.0.0 → 1.1.0)
   - `fix:`, `perf:` → Patch (1.0.0 → 1.0.1)
3. **Confirms versions** with the user (allows adjustments)
4. **Creates release branch** using calendar-based naming (e.g., `release/2026.02.1`) and switches to it
   - Branch name is independent from product versions (follows `release/YYYY.MM.N` convention per CONTRIBUTING.md)
   - N is an incrementing number for each release in that month (1, 2, 3, etc.)
   - A single release like `release/2026.02.1` can contain multiple products at different versions
5. **Updates `version.json`** files for each product
6. **Generates `release-manifest.json`** (calls `/release-manifest-management`)
7. **Generates `CHANGELOG.md`** files (calls `/changelog-management` for each product)
8. **Validates** all files are consistent
9. **Commits all changes** to the release branch

**Example:**
```bash
/release-management

# Output:
# Detected changes since last release:
# - Umbraco.AI: 1.0.0 → 1.1.0 (3 feat, 2 fix commits)
# - Umbraco.AI.OpenAI: 1.0.0 → 1.0.1 (1 fix commit)
#
# Confirm versions? [Yes/Adjust/Cancel]
# → Creates release/2026.02.1 branch (calendar-based naming with increment)
# → Switches to release branch
# → Updates all files on the release branch
# → Commits everything with proper message
```

### `/release-manifest-management` - Manifest Generation

**Use when:** You only need to create/update the `release-manifest.json` file

**What it does:**
- Discovers all `Umbraco.AI*` products
- Presents numbered menu for selection
- Generates `release-manifest.json` with selected products

**Note:** Typically called by `/release-management`, but can be used standalone.

### `/changelog-management` - Changelog Generation

**Use when:** Updating a single product's changelog without full release preparation

**What it does:**
- Lists available products
- Generates `CHANGELOG.md` from conventional commit history
- Can generate for specific version or preview unreleased changes

**Example:**
```bash
/changelog-management

# Prompts for:
# - Product selection
# - Version number (or --unreleased)
# - Generates CHANGELOG.md
```

### `/repo-management` - Unified Interface

**Use when:** Unsure which operation to perform

**What it does:**
- Presents interactive menu of all repository management operations
- Delegates to specialized skills (`/release-management`, `/changelog-management`, etc.)
- Includes build, setup, and frontend watch operations

### Version Bump Decision Logic

The `/release-management` skill analyzes conventional commits to determine version bumps:

```
Priority (highest first):
1. BREAKING CHANGE in commit body → Major bump
2. ! after scope (e.g., feat!:) → Major bump
3. feat: or feat(<scope>): → Minor bump
4. fix: or perf: → Patch bump
5. Only docs/chore/refactor → Ask user (suggest patch)
```

### Cross-Product Dependencies

When bumping Umbraco.AI (Core) to a new major version, the skill:
- Checks `Directory.Packages.props` files for dependency ranges
- Warns about add-ons that require the current major version
- Recommends updating dependent products in the same release

## Release and Hotfix Branch Packaging

### Release Manifest

On `release/*` branches, CI **requires** a `release-manifest.json` at repo root:

```json
["Umbraco.AI", "Umbraco.AI.OpenAI"]
```

**Generating the manifest:**

Use the `/release-management` skill for complete release orchestration (recommended), or use `/release-manifest-management` for manifest-only generation:

```bash
# Full release orchestration (recommended)
/release-management

# Manifest-only generation
/release-manifest-management

# Or use scripts directly:
.\scripts\generate-release-manifest.ps1  # Windows
./scripts/generate-release-manifest.sh   # Linux/Mac
```

The manifest generation process:

1. Scans for all `Umbraco.AI*` product folders
2. Presents an interactive multiselect interface
3. Generates `release-manifest.json` at the repository root

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

| Artifact             | Description                                             |
| -------------------- | ------------------------------------------------------- |
| `all-nuget-packages` | All .nupkg files for NuGet deployment                   |
| `all-npm-packages`   | All .tgz files for npm deployment                       |
| `pack-manifest`      | Package metadata (name, version, type) for each package |

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

| Scenario             | Range Format         | Example              | Use Case                                   |
| -------------------- | -------------------- | -------------------- | ------------------------------------------ |
| Minor version series | `[X.Y.0, X.999.999)` | `[1.1.0, 1.999.999)` | Add-on requires min 1.1.0, accepts all 1.x |
| Specific minimum     | `[X.Y.Z, X.999.999)` | `[1.1.5, 1.999.999)` | Add-on requires min 1.1.5, accepts all 1.x |
| Exact version        | `[X.Y.Z]`            | `[1.1.0]`            | **Avoid** - prevents any updates           |

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
    - **Multiple scopes supported** - Use comma-separated scopes when a single change affects multiple areas (e.g., `feat(core,copilot): Add streaming support`)
    - **To list current options**: `npm run commit-options` or `node scripts/list-commit-options.js`
    - **For Claude Code**: Read `commitlint.config.js` at runtime to discover valid types and scopes - never use hardcoded lists

3. **Body lines must not exceed 100 characters** - Wrap long lines in the commit body

4. **Prefer single scope when possible** - Use multiple scopes only when a single logical change affects multiple areas
    - ✅ Use multiple scopes for unified changes: `feat(core,copilot): Add streaming support to chat and UI`
    - ✅ Split logically separate changes: `fix(core): Fix rate limiting` + `fix(agent): Fix validation`
    - ❌ Don't group unrelated changes: `fix(core,agent,prompt): Various fixes`

Commits are validated by commitlint on commit (soft warnings - allows non-conventional commits but warns).

### Choosing the Right Commit Type

**What Appears in the User-Facing Changelog:**

Only these commit types appear in `CHANGELOG.md`:
- ✅ `feat:` - New features (appears in changelog)
- ✅ `fix:` - Bug fixes (appears in changelog)
- ✅ `perf:` - Performance improvements (appears in changelog)
- ✅ `BREAKING CHANGE` - Breaking changes (appears in changelog)

These commit types are excluded from the changelog:
- ❌ `refactor:` - Internal code improvements (hidden)
- ❌ `chore:` - Maintenance tasks (hidden)
- ❌ `docs:` - Documentation changes (hidden)
- ❌ `test:` - Test changes (hidden)
- ❌ `ci:` - CI/CD changes (hidden)
- ❌ `build:` - Build system changes (hidden)

**Decision Tree: Which Commit Type to Use?**

Ask yourself these questions in order:

1. **Does this change the public API or break existing code?**
   → Use `feat:` with `BREAKING CHANGE:` footer or `feat!:`

2. **Is this visible to end users or developers using the library?**
   - New UI feature, new API method, new capability
   → Use `feat:`

3. **Does this fix a bug that users experienced?**
   → Use `fix:`

4. **Is this improving performance in a noticeable way?**
   → Use `perf:`

5. **Is this restructuring code without changing behavior?**
   - Extracting methods, renaming variables, reorganizing files
   → Use `refactor:`

6. **Is this maintenance work?**
   - Updating dependencies, build scripts, tooling
   → Use `chore:`

**Examples of User-Facing vs Internal Changes:**

| Change Description | Commit Type | Rationale |
|--------------------|-------------|-----------|
| Add "Select All" checkbox to permissions UI | `feat(core):` | User sees new UI feature |
| Add lifecycle notifications API | `feat(core):` | Developer-facing public API |
| Add welcome dashboard | `feat(core):` | User sees new dashboard |
| Fix timezone handling in logs | `fix(core):` | User-visible bug fix |
| **Register notification publisher in DI** | `chore(core):` | **Internal DI setup, not user-facing** |
| **Integrate notifications into service** | `refactor(core):` | **Internal plumbing, no behavior change** |
| **Extract helper method** | `refactor(core):` | **Code organization, not user-facing** |
| **Add unit tests for service** | `test(core):` | **Test infrastructure** |
| **Update EF Core dependency** | `chore(deps):` | **Maintenance** |

**When to Batch Related Work:**

Instead of many small commits:
```bash
feat(core): Add base notification classes
feat(core): Add profile notifications
feat(core): Add connection notifications
feat(core): Integrate notifications into services
```

Consider batching into one user-facing commit:
```bash
feat(core): Add lifecycle notifications for AI entities

Adds created, updated, deleted, and rollback notifications for:
- AIProfile (saving, saved, deleted, rolledback)
- AIConnection (saving, saved, deleted, rolledback)
- AIContext (saving, saved, deleted, rolledback)
```

**Guidelines for Batching:**
- ✅ Batch when commits represent one logical feature from a user's perspective
- ✅ Batch internal implementation steps (DI setup, service integration, tests)
- ❌ Don't batch unrelated changes just to reduce changelog size
- ❌ Don't batch changes to different features

**When to Keep Commits Separate:**

Separate commits are appropriate when:
- Changes are logically independent (different features)
- Changes can be released/reverted independently
- Changes affect different systems/areas
- Changes have different risk profiles

Example of proper separation:
```bash
feat(core): Add tool scope API endpoint
feat(core): Add tool scope UI components
feat(agent): Add tool filtering for agents
```
These are related but distinct features that could ship independently.

### Generating Changelogs

Changelogs are generated before creating a release. Use the `/release-management` skill for automatic generation of all changelogs, or `/changelog-management` for individual products:

```bash
# Full release orchestration (generates all changelogs automatically)
/release-management

# Individual product changelog
/changelog-management

# Or use scripts directly:
npm run changelog:list  # List available products
npm run changelog -- --product=Umbraco.AI --version=1.1.0
npm run changelog -- --product=Umbraco.AI --unreleased

# Platform-specific wrappers
.\scripts\generate-changelog.ps1 -Product Umbraco.AI -Version 1.1.0  # Windows
./scripts/generate-changelog.sh --product=Umbraco.AI --version=1.1.0  # Linux/Mac
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

| Component  | Description                              | Examples                                                                                       |
| ---------- | ---------------------------------------- | ---------------------------------------------------------------------------------------------- |
| **Action** | Verb describing the operation            | `Get`, `Create`, `Update`, `Delete`, `Save`, `Find`, `List`, `Validate`, `Execute`, `Generate` |
| **Entity** | Noun describing what's being operated on | `Profile`, `Connection`, `Prompt`, `Agent`, `Context`, `ChatResponse`                          |
| **Async**  | Required suffix for async methods        | Always `Async`                                                                                 |

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

| Service Method           | Repository Method                                         |
| ------------------------ | --------------------------------------------------------- |
| `GetProfileAsync`        | `GetByIdAsync` (entity is implicit in repository context) |
| `GetProfileByAliasAsync` | `GetByAliasAsync`                                         |
| `GetAllProfilesAsync`    | `GetAllAsync`                                             |
| `SaveProfileAsync`       | `AddAsync` / `UpdateAsync` or `UpsertAsync`               |
| `DeleteProfileAsync`     | `DeleteAsync`                                             |

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
