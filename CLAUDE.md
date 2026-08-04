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
| Umbraco.AI.Automate | `Umbraco.AI.Automate/` | Automate |
| Umbraco.AI.OpenAI | `Umbraco.AI.OpenAI/` | Provider |
| Umbraco.AI.Anthropic | `Umbraco.AI.Anthropic/` | Provider |
| Umbraco.AI.Amazon | `Umbraco.AI.Amazon/` | Provider |
| Umbraco.AI.Google | `Umbraco.AI.Google/` | Provider |
| Umbraco.AI.MicrosoftFoundry | `Umbraco.AI.MicrosoftFoundry/` | Provider |
| Umbraco.AI.Mistral | `Umbraco.AI.Mistral/` | Provider |
| Umbraco.AI.DeepSeek | `Umbraco.AI.DeepSeek/` | Provider |
| Umbraco.AI.HuggingFace | `Umbraco.AI.HuggingFace/` | Provider |
| Umbraco.AI.FireworksAI | `Umbraco.AI.FireworksAI/` | Provider |
| Umbraco.AI.TogetherAI | `Umbraco.AI.TogetherAI/` | Provider |

### Dependency Tree

```
Umbraco.AI (Core)
├── Providers: OpenAI, Anthropic, Amazon, Google, MicrosoftFoundry,
│              Mistral, DeepSeek, HuggingFace, FireworksAI, TogetherAI
├── Umbraco.AI.Prompt → Prompt.Deploy (depends on Prompt + Deploy)
├── Umbraco.AI.Agent → Agent.UI → Agent.Copilot
│                     → Agent.Deploy (depends on Agent + Deploy)
│                     → Automate (depends on Agent + Automate.Core)
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

**Location:** `demos/vN/Umbraco.AI.DemoSite/` (N = major from `Umbraco.Cms.Core` lower bound in `Directory.Packages.props`) | **Credentials:** admin@example.com / password1234

**If `demos/vN/` doesn't exist**, generate it with `scripts/install-demo-site.{sh,ps1}` (auto-detects the version from your branch) or run `/repo-setup`. Do not create demo files by hand.

```bash
/demo-site-management start|stop|open|generate-client|status
/demo-site-automation login|navigate-to-connections|create-connection [provider]
```

- **Path convention:** demo sites live under `demos/vN/` — one directory per CMS major version line (e.g. `demos/v18/`, `demos/v17/`). Never the old top-level `demo/`. The whole `demos/` tree is gitignored and generated per-developer.
- Uses `DemoSite-Claude` profile with dynamic ports (avoids worktree conflicts)
- HTTP over named pipes: `umbraco.demosite.{branch-or-worktree}`
- Site address: query `/site-address` via named pipe to get HTTPS address

### Package Testing Site

Test deployed packages from different feeds (vs demo site which uses project references):

```bash
.\scripts\install-package-test-site.ps1 -Feed nightly|prereleases|release [-SiteName "Name"]
./scripts/install-package-test-site.sh --feed=release --name="Name"  # Linux/Mac
```

Feeds: `nightly` (MyGet nightly), `prereleases` (MyGet pre-release), `release` (NuGet.org). Sites created in `demos/vN/{SiteName}` (N = major from `Umbraco.Cms.Core` lower bound in `Directory.Packages.props`).

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

**Build core first.** `@umbraco-ai/core` exposes its types via a rollup (`types/umbraco-ai-public-types.d.ts`) generated by its `build:api` (api-extractor) step. Until it exists, dependent packages fail to typecheck with `TS2307: Cannot find module '@umbraco-ai/core'` (cascading into `{}`-inferred modal types, e.g. `TS2339: Property 'selection' does not exist`). Always run `npm run build:core` (or full `npm run build`) before building/typechecking a dependent package in isolation — these errors mean a stale/missing core rollup, not a code defect.

**Restore from the monorepo root, not a nested `Client/`.** Frontend builds resolve deps through npm workspaces. If `tsc` fails with `@umbraco-cms/backoffice` type-drift errors (e.g. `'keywords' does not exist in type 'MetaPropertyEditorUi'`) across many manifest files, suspect stale nested `node_modules` — run `npm install` from the repo root to reconcile the workspace before concluding the build is broken. Do not `npm install` inside an individual `src/*/Client/` directory.

## Target Framework & Stack

- .NET 10.0 (`net10.0`), Umbraco CMS 18.x, Central Package Management via `Directory.Packages.props`
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
| `release-manifest.json` | Release pack list (required on `vN/release/*`, optional on `vN/hotfix/*`) |
| `<Product>/version.json` | Per-product version |
| `<Product>/changelog.config.json` | Per-product scopes for changelog |
| `<Product>/CHANGELOG.md` | Per-product changelog (auto-generated) |

## Multi-Version Support

Umbraco.AI major versions track Umbraco CMS major versions. Multiple versions may be in active support simultaneously, following the [Umbraco CMS LTS/EOL policy](https://umbraco.com/products/knowledge-center/long-term-support-and-end-of-life/).

### Branch Model

All branches are version-prefixed. The `claude/` prefix is exempt (auto-created by Claude Code).

| Branch pattern | Role |
|----------------|------|
| `vN/dev` | Active development for version N |
| `vN/main` | Last released state for version N |
| `vN/feature/<name>` | Feature or fix branch targeting version N |
| `vN/release/<date>` | Release preparation branch for version N |
| `vN/hotfix/<name>` | Hotfix branch for version N |

**Major version cutover:** when a new CMS major ships, rename `vN/dev` → `vN/main` (archive), then create fresh `v(N+1)/dev` and `v(N+1)/main` for the new line.

### Support Policy

| CMS Phase | What to apply |
|-----------|---------------|
| Active support | Features + bug fixes |
| Security phase | Security patches only |
| EOL | No updates unless explicitly requested |

### Current Versions

Check [Umbraco CMS LTS/EOL](https://umbraco.com/products/knowledge-center/long-term-support-and-end-of-life/) for the latest status.

| CMS Version | Type | Active Support Until | AI Branches | Current Policy |
|-------------|------|----------------------|-------------|----------------|
| v18 | STS | Mar 2027 | `v18/dev` / `v18/main` | Features + bug fixes |
| v17 | LTS | Nov 2027 | `v17/dev` / `v17/main` | Features + bug fixes |

### Keep Active Versions in Sync

A bug fix or feature is developed against one version line but usually applies to every version still in an active phase (see the table above and [Umbraco CMS LTS/EOL](https://umbraco.com/products/knowledge-center/long-term-support-and-end-of-life/)). **Before treating any fix or feature as done, ask whether it should be ported to the other active version(s) and confirm with the user.** This applies in both directions — e.g. a fix on `v18/dev` may need backporting to `v17/dev`, and a fix on `v17/dev` may need porting up to `v18/dev`. Respect each version's phase: security phase → security patches only; EOL → skip unless explicitly requested. Lines are maintained independently (no forward-merge), so port each one via the Backport Workflow below.

### Backport Workflow

When a fix or feature applies to an older supported version:

1. Branch from `vN/dev` (e.g. `v17/dev`), naming it `vN/feature/<name>`
2. Apply and commit the change on the feature/fix branch
3. Merge back into `vN/dev`
4. Release via the normal release flow on that branch

Do **not** forward-merge `vN/dev` into a newer version's `dev` — each version line is maintained independently.

When backporting **worktree/tooling config**, note that some files hardcode the version line and must
be rewritten to the target `vN`: `.humanlayer/workspace.json` (`branchTemplate: "v18/feature/..."`) and
`.worktreeinclude` (demo path `demos/v18/`). Leaving `v18` in place makes worktrees use the wrong branch
prefix and PRs target the wrong `vN/dev` base.

## Release Management

### Skills Overview

| Skill | Purpose |
|-------|---------|
| `/release-management` | Full release orchestration: detect changes, recommend bumps, create branch, update versions/manifests/changelogs, commit |
| `/release-manifest-management` | Generate `release-manifest.json` only |
| `/changelog-management` | Generate single product changelog |
| `/post-release-cleanup` | Merge `vN/release`->`vN/main`->`vN/dev`, bump versions on `vN/dev`, optionally delete branch |
| `/repo-management` | Interactive menu of all operations |

### Release Flow

1. `/release-management` detects changed products since last release tags
2. Analyzes conventional commits for version bump recommendations
3. Creates `vN/release/YYYY.MM.N` branch (calendar-based, N increments per month)
4. Updates `version.json`, generates `release-manifest.json` and `CHANGELOG.md` files
5. Commits all changes to release branch

### Version Bump Logic

```
BREAKING CHANGE or feat!: -> Major | feat: -> Minor | fix:/perf: -> Patch | docs/chore/refactor only -> Ask user
```

**Major version alignment:** Package major versions track the Umbraco CMS major version. All packages ship as `17.x.x` for CMS 17, `18.x.x` for CMS 18, etc. When preparing a release that spans a CMS major boundary, bump all products to the new major simultaneously — a prep commit on `vN/dev` sets all `version.json` files and inter-product ranges in `Directory.Packages.props` before running `/release-management`.

When bumping Core to new major, the skill checks `Directory.Packages.props` for dependent add-ons and warns.

#### Prerelease versioning — always use dotted `-{stage}.N`

Prerelease identifiers **must** be dot-separated with a numeric segment: `-alpha.1`, `-beta.1`, `-rc.1` (→ `-beta.2`, `-beta.10`, …).

**Never use the non-dotted form** (`-beta1`, `-alpha2`). NuGet/SemVer treats `beta10` as a single alphanumeric identifier and compares it as a *string*, so it sorts **below** `beta9` (`'1' < '9'`). The result: a published `1.0.0-beta10` is lower-precedence than `1.0.0-beta9`, so `--prerelease` installs and range resolution silently pick the *older* build. Dotted `-beta.10` compares the `10` numerically and sorts correctly.

Note you cannot retrofit a broken line: `-beta.11` (dotted) sorts *below* an existing non-dotted `-beta9` (because identifier `beta` < `beta9`). So a line that already shipped non-dotted betas can only be escaped by advancing the stage (`-rc.1`) or the base version, not by dotifying. Apply the dotted rule to every prerelease line. See [[project_release_tag_sort_prerelease_bug]].

### Release Manifest

On `vN/release/*` branches, CI **requires** `release-manifest.json`:

```json
// Array (legacy): ["Umbraco.AI", "Umbraco.AI.OpenAI"]
// Object (preferred):
{ "include": ["Umbraco.AI"], "exclude": ["Umbraco.AI.Google"] }
```

CI validates every changed product appears in `include` or `exclude`. Unaccounted products fail the build.

On `vN/hotfix/*` branches: manifest optional (falls back to per-product tag-based change detection).

### Hotfix Change Detection

- Compares each product's folder against its most recent release tag (e.g., `Umbraco.AI@1.0.0`)
- Excludes `CHANGELOG.md`, `version.json` from diff
- Falls back to merge-base with `vN/main` for new products

### Post-Release (`/post-release-cleanup`)

1. Merges `vN/release/*` -> `vN/main` (no-ff, push), `vN/main` -> `vN/dev` (no-ff, push)
2. Bumps `version.json` on `vN/dev` (patch increment, e.g., `1.5.0` -> `1.5.1` so nightlies are `1.5.1--preview.*`)
3. Optionally deletes the release/hotfix branch

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
    <PackageVersion Include="Umbraco.AI.Core" Version="[17.0.0, 17.999.999)" />
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

**Pack recompiles against ranges (do not re-add `--no-build` to ranged packs).** The Build stage compiles the whole solution with project references (sibling *source*). For ranged packs (release/hotfix/main, or `packWithNuGetRanges`), `pack-product.yml` deliberately drops `--no-build` and recompiles so the shipped binary is validated against the *same* dependency versions the `.nuspec` declares — resolved from the LocalCI feed when the dependency was packed in the same run (co-release) or from nuget.org when it was not (solo release). This is what makes a solo release that needs an unpublished dependency API fail the pack instead of silently shipping a binary compiled against a higher version than its declared floor. Project-reference packs (dev previews) keep `--no-build` since the Build stage already produced those exact binaries.

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

### Public API Backwards Compatibility

Never break a public API. When a new method replaces an old one, keep the old signature and have it
**proxy to the new method**, resolving any new parameters via service locator (e.g.
`StaticServiceProvider.Instance.GetRequiredService<T>()`). Mark the old method `[Obsolete]` with the
message `"Will be removed in vX"`, where **X = current major version + 2** (currently v18, so
`"Will be removed in v20"`). This gives consumers two major versions to migrate.

### Backoffice UI: mirror an existing editor, don't hand-roll

For any create/edit/list/detail surface, use the standard backoffice extension stack — **workspaces**
(`<umb-workspace-editor>`, workspace views, `UmbSubmittableWorkspaceContextBase`, `UmbSubmitWorkspaceAction`),
**collections**, **entity actions** (`UmbEntityActionBase`), and `umb-property-layout` for fields — never
hand-rolled editor markup + state. **Before writing new editor/list UI, find the nearest existing
implementation and mirror it** (e.g. the Context/Connection/Profile editors under
`Umbraco.AI/src/*/Web.StaticAssets/Client/src/**/workspace/`). Custom elements are only for genuinely
non-standard surfaces (e.g. a chat view) — and even then keep the standard workspace chrome and make only
the inner view custom.

## Excluded Folders

- `Ref/` - External reference projects
- `Umbraco.AI-entity-snapshot-service/` - Legacy reference

## Lessons Learned

- Never import Lit components by path; export through `index.ts`/`export.ts` for global accessibility
- Avoid god objects
