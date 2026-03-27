# CLAUDE.md

## Repository Structure

Monorepo for Umbraco.AI and add-on packages. Each product has its own `.slnx`, `CLAUDE.md`, and builds independently.

| Product | Location | Category |
|---------|----------|----------|
| Umbraco.AI | `Umbraco.AI/` | Core |
| Umbraco.AI.Agent | `Umbraco.AI.Agent/` | Add-on |
| Umbraco.AI.Agent.UI | `Umbraco.AI.Agent.UI/` | Add-on (chat UI library) |
| Umbraco.AI.Agent.Copilot | `Umbraco.AI.Agent.Copilot/` | Add-on (frontend-only) |
| Umbraco.AI.Prompt | `Umbraco.AI.Prompt/` | Add-on |
| Umbraco.AI.Search | `Umbraco.AI.Search/` | Add-on |
| Umbraco.AI.Deploy | `Umbraco.AI.Deploy/` | Deploy |
| Umbraco.AI.Prompt.Deploy | `Umbraco.AI.Prompt.Deploy/` | Deploy |
| Umbraco.AI.Agent.Deploy | `Umbraco.AI.Agent.Deploy/` | Deploy |
| Umbraco.AI.OpenAI | `Umbraco.AI.OpenAI/` | Provider |
| Umbraco.AI.Anthropic | `Umbraco.AI.Anthropic/` | Provider |
| Umbraco.AI.Amazon | `Umbraco.AI.Amazon/` | Provider |
| Umbraco.AI.Google | `Umbraco.AI.Google/` | Provider |
| Umbraco.AI.MicrosoftFoundry | `Umbraco.AI.MicrosoftFoundry/` | Provider |

### Dependency Tree

```
Umbraco.AI (Core)
├── Providers: OpenAI, Anthropic, Amazon, Google, MicrosoftFoundry
├── Umbraco.AI.Prompt → Prompt.Deploy (depends on Prompt + Deploy)
├── Umbraco.AI.Agent → Agent.UI → Agent.Copilot
│                     → Agent.Deploy (depends on Agent + Deploy)
├── Umbraco.AI.Search
└── Umbraco.AI.Deploy
```

### Project Structure Patterns

**Core/Add-on packages** (Umbraco.AI, Agent, Prompt):
```
ProductName/
├── src/
│   ├── ProductName.Core/              # Domain models, services, interfaces
│   ├── ProductName.Web/               # Management API
│   ├── ProductName.Web.StaticAssets/Client/  # TypeScript/Lit frontend
│   ├── ProductName.Persistence/       # EF Core DbContext, repositories
│   ├── ProductName.Persistence.SqlServer/   # SQL Server migrations
│   ├── ProductName.Persistence.Sqlite/      # SQLite migrations
│   ├── ProductName.Startup/           # Umbraco Composer for DI
│   └── ProductName/                   # Meta-package
├── tests/ (Unit, Integration, Common)
├── ProductName.slnx
└── CLAUDE.md
```

**Search package** (Umbraco.AI.Search) uses `Db`, `Db.SqlServer`, `Db.Sqlite` instead of `Persistence.*` — these are `IAIVectorStore` implementations, not domain entity persistence.

**Provider packages**: Single `src/ProviderName/` project + `tests/ProviderName.Tests.Unit/`.

### Core Concepts

- **Providers** - AI service plugins (OpenAI, Anthropic, etc.)
- **Connections** - API keys and provider settings
- **Profiles** - Connection + model settings for use cases
- **Capabilities** - Chat, Embedding, etc.

Built on Microsoft.Extensions.AI (M.E.AI), "thin wrapper" philosophy.

## Development Environment

### Setup

```bash
/repo-setup  # First-time: git hooks, demo site, dependencies, build
```

### Demo Site

**Location:** `demo/Umbraco.AI.DemoSite/` | **Credentials:** admin@example.com / password1234

```bash
/demo-site-management start|stop|open|generate-client|status
/demo-site-automation login|navigate-to-connections|create-connection [provider]
```

- Uses `DemoSite-Claude` profile with dynamic ports (avoids worktree conflicts)
- HTTP over named pipes: `umbraco.demosite.{branch-or-worktree}`
- Site address: query `/site-address` via named pipe to get HTTPS address

### Package Testing Site

Test deployed packages from different feeds (vs demo site which uses project references):

```bash
.\scripts\install-package-test-site.ps1 -Feed nightly|prereleases|release [-SiteName "Name"]
./scripts/install-package-test-site.sh --feed=release --name="Name"  # Linux/Mac
```

Feeds: `nightly` (MyGet nightly), `prereleases` (MyGet pre-release), `release` (NuGet.org). Sites created in `demo/{SiteName}`.

## Project Management (Azure DevOps)

| Purpose | Project | Notes |
|---------|---------|-------|
| Backlog & Work Items | D-Team Tracker | AI Team backlog, tag: `Umbraco AI` |
| CI/CD Pipelines | Umbraco AI | |

**IMPORTANT**: Always scope work item searches to AI Team backlog using `wit_list_backlog_work_items`. The D-Team Tracker project is shared across multiple product teams -- unfiltered searches return cross-product results.

## Build Commands

### .NET

```bash
dotnet build Umbraco.AI.local.slnx          # All products + demo
dotnet build <Product>/<Product>.slnx        # Individual product
dotnet test <Product>/<Product>.slnx         # Run tests
```

### Frontend (npm workspaces)

```bash
npm install                  # All workspace dependencies
npm run build                # All frontends (sequential: core -> prompt -> agent -> agent-ui -> copilot)
npm run watch                # All frontends in parallel
npm run generate-client      # OpenAPI clients (requires running demo site)
npm run build:<target>       # Targets: core, prompt, agent, agent-ui, copilot
npm run watch:<target>       # Same targets as build
```

Add-on packages use `workspace:*` to reference local core during dev; replaced with actual version on `npm pack`.

## Target Framework & Stack

- .NET 10.0 (`net10.0`), Umbraco CMS 17.x, Central Package Management via `Directory.Packages.props`
- Frontend: Lit + TypeScript + Vite + @hey-api/openapi-ts
- Frontend source: `src/*/Web.StaticAssets/Client/` -> `wwwroot/App_Plugins/`
- Database: SQL Server & SQLite via EF Core; migration prefixes: `UmbracoAI_`, `UmbracoAIPrompt_`, `UmbracoAIAgent_`, `UmbracoAISearch_`

## Key Files

| File | Purpose |
|------|---------|
| `scripts/install-demo-site.{ps1,sh}` | Create local dev environment |
| `scripts/install-package-test-site.{ps1,sh}` | Create test site from package feeds |
| `scripts/generate-changelog.{ps1,sh,js}` | Changelog generation |
| `scripts/generate-release-manifest.{ps1,sh}` | Release manifest generator |
| `Umbraco.AI.local.slnx` | Unified solution (generated) |
| `commitlint.config.js` | Commit validation with dynamic scope loading |
| `release-manifest.json` | Release pack list (required on `release/*`, optional on `hotfix/*`) |
| `<Product>/version.json` | Per-product version |
| `<Product>/changelog.config.json` | Per-product scopes for changelog |
| `<Product>/CHANGELOG.md` | Per-product changelog (auto-generated) |

## Release Management

### Skills Overview

| Skill | Purpose |
|-------|---------|
| `/release-management` | Full release orchestration: detect changes, recommend bumps, create branch, update versions/manifests/changelogs, commit |
| `/release-manifest-management` | Generate `release-manifest.json` only |
| `/changelog-management` | Generate single product changelog |
| `/post-release-cleanup` | Merge release->main->dev, bump versions on dev, optionally delete branch |
| `/repo-management` | Interactive menu of all operations |

### Release Flow

1. `/release-management` detects changed products since last release tags
2. Analyzes conventional commits for version bump recommendations
3. Creates `release/YYYY.MM.N` branch (calendar-based, N increments per month)
4. Updates `version.json`, generates `release-manifest.json` and `CHANGELOG.md` files
5. Commits all changes to release branch

### Version Bump Logic

```
BREAKING CHANGE or feat!: -> Major | feat: -> Minor | fix:/perf: -> Patch | docs/chore/refactor only -> Ask user
```

When bumping Core to new major, the skill checks `Directory.Packages.props` for dependent add-ons and warns.

### Release Manifest

On `release/*` branches, CI **requires** `release-manifest.json`:

```json
// Array (legacy): ["Umbraco.AI", "Umbraco.AI.OpenAI"]
// Object (preferred):
{ "include": ["Umbraco.AI"], "exclude": ["Umbraco.AI.Google"] }
```

CI validates every changed product appears in `include` or `exclude`. Unaccounted products fail the build.

On `hotfix/*` branches: manifest optional (falls back to per-product tag-based change detection).

### Hotfix Change Detection

- Compares each product's folder against its most recent release tag (e.g., `Umbraco.AI@1.0.0`)
- Excludes `CHANGELOG.md`, `version.json` from diff
- Falls back to merge-base with main for new products

### Post-Release (`/post-release-cleanup`)

1. Merges release->main (no-ff, push), main->dev (no-ff, push)
2. Bumps `version.json` on dev (patch increment, e.g., `1.5.0` -> `1.5.1` so nightlies are `1.5.1--preview.*`)
3. Optionally deletes release/hotfix branch

### CI Artifacts

| Artifact | Description |
|----------|-------------|
| `all-nuget-packages` | .nupkg files for NuGet |
| `all-npm-packages` | .tgz files for npm |
| `pack-manifest` | Package metadata for git tagging |

Release pipeline deploys to feeds and tags repo with `[Product]@[Version]`. See [CONTRIBUTING.md](CONTRIBUTING.md#release-process).

## Cross-Product Dependency Management

Dependencies managed via Central Package Management (`Directory.Packages.props`). Always use version ranges for cross-product deps.

**Product-level override** in `<Product>/Directory.Packages.props`:
```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="Umbraco.AI.Core" Version="[1.1.0, 1.999.999)" />
  </ItemGroup>
</Project>
```

Format: `[min, max)` -- inclusive lower, exclusive upper. Use `[X.Y.0, X.999.999)` to accept all X.x versions from X.Y.0+.

| Level | File | Purpose |
|-------|------|---------|
| Root | `Directory.Packages.props` | Default versions for all products |
| Product | `<Product>/Directory.Packages.props` | Override specific ranges |
| Local dev | Project references (`UseProjectReferences=true`) | Bypass NuGet entirely |
| CI/CD | Distribution build (`UseProjectReferences=false`) | Uses NuGet ranges |

**Rules**: Use project refs for local dev. Use `[X.Y.0, X.999.999)` ranges. Avoid exact versions `[X.Y.Z]`. Test with `UseProjectReferences=false` before releasing. When releasing Core with breaking changes, verify dependent products update their minimum.

## Commit Message Format

[Conventional Commits](https://www.conventionalcommits.org/): `<type>(<scope>): <description>`

**Rules (enforced by commitlint):**
- Subject must be **sentence-case** (capitalize first word after scope)
- Scopes: dynamically loaded from `<Product>/changelog.config.json` + meta scopes (`deps`, `ci`, `docs`, `release`). Read `commitlint.config.js` at runtime -- never use hardcoded lists
- Multiple comma-separated scopes allowed for unified cross-area changes
- Body lines max 100 characters
- To list options: `npm run commit-options`

**Types in changelog**: `feat`, `fix`, `perf`, `BREAKING CHANGE`
**Types hidden from changelog**: `refactor`, `chore`, `docs`, `test`, `ci`, `build`

### Commit Type Decision

1. Breaking API/behavior change? -> `feat!:` or `BREAKING CHANGE:` footer
2. User/developer-visible new feature? -> `feat:`
3. User-experienced bug fix? -> `fix:`
4. Noticeable performance gain? -> `perf:`
5. Code restructuring, no behavior change? -> `refactor:`
6. Maintenance (deps, build, tooling)? -> `chore:`

### Batching Guidelines

- Batch commits that represent one logical feature from user perspective
- Batch internal steps (DI setup, service integration, tests) into the feature commit
- Keep separate when changes are logically independent, can be reverted independently, or affect different systems

### Generating Changelogs

```bash
/release-management                    # Generates all changelogs as part of release
/changelog-management                  # Individual product
npm run changelog -- --product=Umbraco.AI --version=1.1.0
npm run changelog -- --product=Umbraco.AI --unreleased
```

CI validates on `release/*` and `hotfix/*`: CHANGELOG.md must exist, be recently updated, and version must match version.json. See [CONTRIBUTING.md](CONTRIBUTING.md#maintaining-changelogs).

## Coding Standards

### Async Methods: `[Action][Entity]Async`

| Component | Description | Examples |
|-----------|-------------|----------|
| Action | Verb | `Get`, `Create`, `Update`, `Delete`, `Save`, `Find`, `List`, `Validate` |
| Entity | Noun | `Profile`, `Connection`, `Prompt`, `Agent`, `Context`, `ChatResponse` |
| Async | Suffix | Always required |

Qualifiers come after entity: `GetProfileByAliasAsync`, `GetAllProfilesAsync`, `GetPromptsPagedAsync`, `GetDefaultProfileAsync`.

Existence checks: `[Entity][Qualifier]ExistsAsync` (e.g., `ProfileAliasExistsAsync`).

**Common mistakes to avoid:**
- `GetAsync` -- missing entity name
- `ProfileExistsAsync` -- entity before action
- `GetProfile` -- missing Async suffix

### Repository Access Pattern

**Repositories are internal to their service.** Only the entity's own service may access its repository. Other services/controllers must go through the service layer.

```
Controller/OtherService -> EntityService -> EntityRepository (internal)
```

- Services encapsulate business logic, validation, caching
- Repositories should be `internal` to persistence assembly

**Repository method names** use shorter forms (entity implicit): `GetByIdAsync`, `GetByAliasAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.

### Extension Methods

All extension methods in `Umbraco.AI.Extensions` namespace (or product-specific: `Umbraco.AI.Prompt.Extensions`).

## Excluded Folders

- `Ref/` - External reference projects
- `Umbraco.AI-entity-snapshot-service/` - Legacy reference

## Lessons Learned

- Never import Lit components by path; export through `index.ts`/`export.ts` for global accessibility
- Avoid god objects
