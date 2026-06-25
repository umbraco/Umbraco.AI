# Contributing to Umbraco.AI

This guide explains how to contribute to the Umbraco.AI monorepo, covering branch naming conventions, git workflows, and release processes.

## Table of Contents

- [Getting Started](#getting-started)
- [Branch Naming Convention](#branch-naming-convention)
- [Development Workflow](#development-workflow)
- [Pull Request Process](#pull-request-process)
- [Release Process](#release-process)
- [CI/CD Pipeline](#cicd-pipeline)
- [Coding Standards](#coding-standards)

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Node.js 24.x
- Git
- SQL Server or SQLite (for database development)
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/umbraco/Umbraco.AI.git
cd Umbraco.AI

# Run setup script (creates unified solution + demo site)
.\scripts\install-demo-site.ps1  # Windows
./scripts/install-demo-site.sh   # Linux/Mac

# Configure git hooks (enforces branch naming and commit message syntax)
.\scripts\setup-git-hooks.ps1  # Windows
./scripts/setup-git-hooks.sh   # Linux/Mac

# Open unified solution
start Umbraco.AI.local.slnx
```

### Repository Structure

```
Umbraco.AI/                    # Monorepo root
├── Umbraco.AI/                # Core AI layer
├── Umbraco.AI.Agent/          # Agent add-on
├── Umbraco.AI.Agent.UI/       # Agent UI library
├── Umbraco.AI.Agent.Copilot/  # Agent copilot UI
├── Umbraco.AI.Prompt/         # Prompt add-on
├── Umbraco.AI.OpenAI/         # OpenAI provider
├── Umbraco.AI.Anthropic/      # Anthropic provider
├── Umbraco.AI.Amazon/         # Amazon Bedrock provider
├── Umbraco.AI.Google/         # Google Gemini provider
├── Umbraco.AI.MicrosoftFoundry/ # Microsoft AI Foundry provider
├── demo/                      # Demo site (generated)
└── docs/                      # Shared documentation
```

## Branch Naming Convention

**All branches MUST follow these patterns.** This is enforced by git hooks and CI/CD.

Branch names are version-prefixed so that each major version (which tracks the Umbraco CMS major) has its own isolated set of branches. The `claude/` prefix is exempt — it is used by Claude Code automation and cannot be versioned.

### Valid Branch Patterns

| Pattern                     | Description                          | Example                             |
| --------------------------- | ------------------------------------ | ----------------------------------- |
| `vN/dev`                    | Active development for version N     | `v18/dev`                           |
| `vN/main`                   | Last released state for version N    | `v18/main`                          |
| `vN/feature/<anything>`     | Feature or fix branch for version N  | `v18/feature/add-embeddings`        |
| `vN/release/<anything>`     | Release preparation for version N    | `v18/release/2026.06.1`             |
| `vN/hotfix/<anything>`      | Hotfix branch for version N          | `v17/hotfix/2026.06.1`              |
| `claude/<anything>`         | Claude Code automation branches      | `claude/add-streaming-abc123`       |

### Currently Active Version Lines

| CMS Version | AI Branches               | Policy                |
| ----------- | ------------------------- | --------------------- |
| v18 (STS)   | `v18/dev` / `v18/main`   | Features + bug fixes  |
| v17 (LTS)   | `v17/dev` / `v17/main`   | Features + bug fixes  |

The GitHub default branch always points to the latest version's `dev` (currently `v18/dev`).

### Recommended Naming Conventions

**Release branches:** `vN/release/YYYY.MM.N`

- `N` is the CMS major version
- `YYYY.MM` = Year and month of the release
- Trailing `.N` = Incrementing release number within that month
- Example: `v18/release/2026.01.1` for the first January 2026 release on v18

**Hotfix branches:** `vN/hotfix/YYYY.MM.N`

- Example: `v17/hotfix/2026.01.1` for the first hotfix in January 2026 on v17

**Feature branches:** `vN/feature/<descriptive-name>`

- Example: `v18/feature/add-streaming-support`
- Example: `v17/feature/backport-split-view-fix`

### Examples

**Correct:**

```bash
v18/feature/add-streaming-support
v18/feature/improve-context-handling
v17/feature/backport-split-view-fix
v18/release/2026.01.1      # Calendar-based (recommended)
v17/hotfix/2026.01.1       # Calendar-based with sequence
```

**Incorrect:**

```bash
feature/add-streaming       # Missing version prefix
release/2026.01             # Missing version prefix
dev                         # Bare dev (no longer valid)
main                        # Bare main (no longer valid)
support/17.x                # Old maintenance convention
```

### Enforcement

Branch naming is enforced at two levels:

1. **Git Hooks** (`.githooks/pre-push`): Local validation before push — blocks pushes from branches that don't match the `vN/` convention
2. **Azure DevOps CI/CD**: Trigger patterns only run on `v*/dev`, `v*/main`, `v*/release/*`, `v*/hotfix/*`, `v*/feature/*`

### Git Hooks for Release Manifest Protection

The repository includes several git hooks to manage `release-manifest.json` lifecycle:

**Protection on release/hotfix branches:**

- **pre-merge-commit hook**: Automatically restores `release-manifest.json` if it gets deleted during a merge to a `vN/release/*` or `vN/hotfix/*` branch (e.g., when merging from `vN/dev`)
- **merge driver**: Preserves `release-manifest.json` when there are content conflicts (defense-in-depth)

**Cleanup on long-term branches:**

- **post-merge hook**: Automatically removes `release-manifest.json` after merging to `vN/main` or `vN/dev` (these branches should never have the manifest file)

This ensures:

- ✅ Release branches always keep their manifest during merges
- ✅ Long-term branches never accumulate manifest files
- ✅ No manual intervention needed

## Development Workflow

### Feature Development (Single Product)

```bash
# 1. Create feature branch from the appropriate vN/dev
git checkout v18/dev
git pull origin v18/dev
git checkout -b v18/feature/add-embeddings

# 2. Make changes in the product directory
# Edit: Umbraco.AI/src/Umbraco.AI.Core/...

# 3. Build and test
dotnet build Umbraco.AI/Umbraco.AI.slnx
dotnet test Umbraco.AI/Umbraco.AI.slnx

# 4. Test in demo site
cd demo/Umbraco.AI.DemoSite
dotnet run

# 5. Commit changes
git add .
git commit -m "feat(core): add embedding support

Implements IChatClient.EmbeddAsync using M.E.AI abstractions"

# 6. Push and create PR targeting vN/dev
git push -u origin v18/feature/add-embeddings
```

### Feature Development (Cross-Product)

When a feature spans multiple products (e.g., Core + Agent):

```bash
# 1. Create feature branch from the appropriate vN/dev
git checkout v18/dev
git checkout -b v18/feature/shared-context

# 2. Make changes to both products
# Edit: Umbraco.AI/src/Umbraco.AI.Core/...
# Edit: Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/...

# 3. Build unified solution (tests everything together)
dotnet build Umbraco.AI.local.slnx

# 4. Test in demo site
cd demo/Umbraco.AI.DemoSite
dotnet run

# 5. Commit atomic changes
git add .
git commit -m "feat(core,agent): add shared context handling

- Core: Add IContextProvider interface
- Agent: Implement context sharing between agents"

# 6. Push and create PR targeting vN/dev
git push -u origin v18/feature/shared-context
```

### Backport Workflow

When a fix or feature also applies to an older supported version:

```bash
# 1. Branch from the older version's dev — NOT from v18/dev
git checkout v17/dev
git pull origin v17/dev
git checkout -b v17/feature/backport-split-view-fix

# 2. Apply the fix (cherry-pick or re-implement)
git cherry-pick <commit-sha>

# 3. Push and create PR targeting v17/dev
git push -u origin v17/feature/backport-split-view-fix
```

Do **not** forward-merge `v17/dev` into `v18/dev` — each version line is maintained independently.

### Frontend Development

```bash
# Watch all frontends in parallel (hot reload)
npm run watch

# Or watch specific product
npm run watch:core
npm run watch:agent

# Generate OpenAPI clients (demo site must be running)
npm run generate-client
```

### Working with Project References

By default, all products use **project references** to Core (changes visible immediately):

```xml
<!-- Agent.Core.csproj -->
<ProjectReference Include="..\..\..\Umbraco.AI\src\Umbraco.AI.Core\Umbraco.AI.Core.csproj"
                  Condition="'$(UseProjectReferences)' == 'true'" />
```

This means:

- **Local builds**: Agent/Prompt/Providers automatically use your local Core changes
- **Distribution builds**: CI/CD builds with `UseProjectReferences=false` for package references

### Running Tests

```bash
# Run tests for specific product
dotnet test Umbraco.AI/Umbraco.AI.slnx
dotnet test Umbraco.AI.Agent/Umbraco.AI.Agent.slnx

# Run all tests
dotnet test Umbraco.AI.local.slnx
```

### Local Packing

Products with a `Web.StaticAssets` project ship a Bellissima frontend bundle alongside their .NET assemblies. When packing locally, the frontend **must be built first** — `dotnet pack` does not invoke the npm build, and there is no error if the frontend output is missing.

The required order is:

```bash
# 1. Install npm workspace dependencies (once, or after lockfile changes)
npm install

# 2. Build the target product's frontend (and any frontend dependencies)
#    Targets: core, prompt, agent, agent-ui, copilot
npm run build:agent

# 3. Pack the .NET solution
dotnet pack Umbraco.AI.Agent/Umbraco.AI.Agent.slnx
```

**Why this matters:** if you skip step 2, the resulting `*.Web.StaticAssets.nupkg` will contain only `lib/net10.0/*.dll` — its `staticwebassets/` folder will be empty. The backoffice composers will register at runtime but no UI will render, and there is no build-time signal that anything is wrong.

**Frontend dependency order:** `npm run build` runs the workspace targets sequentially (`core → prompt → agent → agent-ui → copilot`). When packing an add-on locally, build `core` first if you haven't already, since add-on frontends consume the core bundle.

## Pull Request Process

### PR Title Format

Use conventional commits format:

```
<type>(<scope>): <description>

Types: feat, fix, docs, chore, refactor, test, perf
Scopes: core, agent, prompt, openai, anthropic
```

**Examples:**

```
feat(core): add streaming chat support
fix(agent): resolve context memory leak
docs(prompt): update README with examples
chore(core,agent): update dependencies
```

### PR Description Template

```markdown
## Summary

Brief description of what this PR does.

## Changes

- List of key changes
- Another change

## Testing

- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Tested in demo site
- [ ] Frontend builds successfully (if applicable)

## Breaking Changes

None / List any breaking changes

## Related Issues

Closes #123
```

### PR Checklist

Before submitting a PR:

- [ ] Branch name follows convention (`vN/feature/<anything>`)
- [ ] PR targets the correct `vN/dev` base branch
- [ ] Code follows [coding standards](CLAUDE.md#coding-standards)
- [ ] All tests pass
- [ ] Frontend builds (if frontend changes)
- [ ] Documentation updated (if needed)
- [ ] CLAUDE.md updated (if architecture changes)
- [ ] No console errors or warnings

### Review Process

1. **Automated Checks**: CI runs branch validation + unit tests
2. **Code Review**: At least one team member must approve
3. **CI/CD**: Azure DevOps builds affected products
4. **Merge**: Merge into `vN/dev` (no-ff to preserve history)

## Release Process

Each product is versioned and released independently using Nerdbank.GitVersioning (NBGV). Package major versions track the Umbraco CMS major version — all v18 packages ship as `18.x.x`, all v17 packages as `17.x.x`.

### Release Workflow

#### 1. Create Release Branch

Branch from the appropriate `vN/dev`:

```bash
git checkout v18/dev
git pull origin v18/dev
git checkout -b v18/release/2026.06.1
```

Or use the `/release-management` skill which handles this automatically.

#### 2. Define Release Manifest

Use the interactive script to generate `release-manifest.json`:

```bash
# Windows
.\scripts\generate-release-manifest.ps1

# Linux/Mac
./scripts/generate-release-manifest.sh
```

Or create manually:

```json
{ "include": ["Umbraco.AI", "Umbraco.AI.OpenAI"] }
```

#### 3. Update Versions

Edit each product's `version.json` in the manifest:

```json
{
    "version": "18.1.0",
    "assemblyVersion": {
        "precision": "build"
    },
    "publicReleaseRefSpec": [
        "^refs/heads/v\\d+/main$",
        "^refs/heads/v\\d+/release/",
        "^refs/heads/v\\d+/hotfix/"
    ]
}
```

```bash
git add release-manifest.json Umbraco.AI/version.json
git commit -m "chore(release): prepare 2026.06.1"
git push -u origin v18/release/2026.06.1
```

#### 4. CI/CD Build Pipeline

Azure DevOps detects the `vN/release/*` branch pattern:

- Enforces `release-manifest.json` (CI fails if any changed product is missing from the list)
- Builds and packs only the listed products
- Publishes two artifacts:
    - `all-nuget-packages` - Contains all NuGet packages (.nupkg)
    - `all-npm-packages` - Contains all npm packages (.tgz)
- Publishes `pack-manifest` artifact - Contains metadata for each package (name, version, type)

#### 5. Release Pipeline Deployment

The Azure DevOps **release pipeline** automatically triggers after the build completes:

1. **Download Artifacts**
    - Downloads `all-nuget-packages`, `all-npm-packages`, and `pack-manifest` artifacts

2. **Deploy Packages**
    - Deploys NuGet packages to **MyGet** (pre-release feed)
    - Deploys npm packages to **npm registry** with `@next` tag

3. **Tag Git Repository**
    - Reads `pack-manifest` to get each package name and version
    - Creates git tag for each deployed package: `[Product_Name]@[Version]`
    - Examples: `Umbraco.AI@18.1.0`, `Umbraco.AI.OpenAI@18.0.1`

**MyGet URL:** `https://www.myget.org/F/umbraco-ai/api/v3/index.json`

#### 6. Test Pre-Release

Before production deployment, validate the pre-release packages work correctly:

**Option A: Automated Test Site (Recommended)**

```bash
# Windows
.\scripts\install-package-test-site.ps1 -Feed prereleases -Force

# Linux/Mac
./scripts/install-package-test-site.sh --feed=prereleases --force
```

**Option B: Manual Testing**

```bash
# Add MyGet feed
dotnet nuget add source https://www.myget.org/F/umbraco-ai/api/v3/index.json -n UmbracoAI

# Install pre-release package
dotnet add package Umbraco.AI.Core --version 18.1.0-*
```

#### 7. Production Release Pipeline

Once testing passes, trigger the production release from Azure DevOps. The release pipeline:

1. Deploys NuGet packages to **NuGet.org**
2. Deploys npm packages with `@latest` tag
3. Creates git tags: `Umbraco.AI@18.1.0`, `Umbraco.AI.OpenAI@18.0.1`

#### 8. Merge Back and Bump Dev Versions

Use the `/post-release-cleanup` skill to automate this step:

```bash
/post-release-cleanup
```

This will:
1. Detect released products from git tags on the release branch
2. Merge `vN/release/*` → `vN/main` (no-ff)
3. Merge `vN/main` → `vN/dev` (no-ff)
4. Bump `version.json` on `vN/dev` for each released product (patch increment)
5. If this was a new major version, create the next version's `v(N+1)/dev` and `v(N+1)/main` branches and update the GitHub default branch
6. Optionally delete the release branch

**Manual alternative:**

```bash
# Merge to vN/main
git checkout v18/main
git pull origin v18/main
git merge v18/release/2026.06.1 --no-ff
git push origin v18/main

# Merge vN/main to vN/dev and bump versions
git checkout v18/dev
git pull origin v18/dev
git merge v18/main --no-ff
# Bump version.json for each released product (patch increment)
git push origin v18/dev

# Clean up release branch
git branch -d v18/release/2026.06.1
git push origin --delete v18/release/2026.06.1
```

**Automatic Cleanup:** When you merge to `vN/main` or `vN/dev`, the `post-merge` git hook automatically removes `release-manifest.json` and commits the cleanup.

### Hotfix Workflow

For emergency fixes to a released version:

```bash
# 1. Branch from the appropriate vN/dev (or from a product tag if targeting a specific release)
git checkout v17/dev
git pull origin v17/dev
git checkout -b v17/hotfix/2026.06.1

# 2. Fix the issue
# Edit: Umbraco.AI/src/...

# 3. Update version.json for affected products
# Change: "version": "17.0.1"

# 4. Generate changelog for the hotfix
npm run changelog -- --product=Umbraco.AI --version=17.0.1

# 5. (Optional) Add release-manifest.json
# On vN/hotfix/* branches, the manifest is optional:
#   - If present: Only listed products are packed (enforced)
#   - If absent: Change detection is used (automatic)

.\scripts\generate-release-manifest.ps1   # Windows
./scripts/generate-release-manifest.sh    # Linux/Mac

# 6. Commit and push
git add Umbraco.AI/CHANGELOG.md release-manifest.json Umbraco.AI/version.json
git commit -m "fix(core): resolve critical issue"
git push -u origin v17/hotfix/2026.06.1

# 7. After CI builds and release pipeline deploys, run post-release cleanup
/post-release-cleanup
```

### Releasing Multiple Products

To release multiple products in a single release, include them all in `release-manifest.json`:

```json
{ "include": ["Umbraco.AI", "Umbraco.AI.OpenAI", "Umbraco.AI.Anthropic"] }
```

**Important:** On `vN/release/*` branches, `release-manifest.json` is **required**. CI will fail if any changed product is missing from the list.

On `vN/hotfix/*` branches, the manifest is **optional**. If present, it is enforced the same way; if absent, change detection is used automatically.

### Cross-Product Dependency Management

Add-on packages and providers depend on Umbraco.AI (Core). When releasing products with dependencies, follow these guidelines:

#### Version Ranges (Required)

**Always use version ranges** for cross-product dependencies:

```xml
<!-- Umbraco.AI.Prompt/Directory.Packages.props -->
<Project>
  <ItemGroup>
    <!-- Use a range: minimum version 18.1.0, up to (but not including) 18.999.999 -->
    <PackageVersion Include="Umbraco.AI.Core" Version="[18.1.0, 18.999.999)" />
  </ItemGroup>
</Project>
```

The range format `[minimum, maximum)` means:

- `[` = inclusive lower bound (>= 18.1.0)
- `)` = exclusive upper bound (< 18.999.999)
- Result: accepts any 18.x version from 18.1.0 onwards

#### Version Range Guidelines

| Scenario             | Range Format          | Example               | Description                      |
| -------------------- | --------------------- | --------------------- | -------------------------------- |
| Minor version series | `[X.Y.0, X.999.999)` | `[18.1.0, 18.999.999)` | Min 18.1.0, accepts all 18.x    |
| Specific minimum     | `[X.Y.Z, X.999.999)` | `[18.1.5, 18.999.999)` | Min 18.1.5, accepts all 18.x    |
| Exact version        | `[X.Y.Z]`            | `[18.1.0]`             | **Avoid** — prevents any updates |

## Maintaining Changelogs

Each product maintains its own `CHANGELOG.md` file at the product root, auto-generated from git history using conventional commits.

### Commit Message Format

All commits should follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Type** - The kind of change:

- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code refactoring
- `perf`: Performance improvement
- `docs`: Documentation only
- `test`: Tests only
- `chore`: Maintenance
- `ci`: CI/CD changes
- `build`: Build system changes

**Scope** - The product or feature area affected (see table below)

### Commit Scopes

Scopes are automatically discovered from product `changelog.config.json` files:

| Product                         | Scopes                                                                                                                           |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| **Umbraco.AI**                  | `core`, `profile`, `chat`, `embedding`, `connection`, `middleware`, `registry`, `settings`, `providers`, `ui`, `frontend`, `api` |
| **Umbraco.AI.Agent**            | `agent`                                                                                                                          |
| **Umbraco.AI.Agent.UI**         | `agent-ui`                                                                                                                       |
| **Umbraco.AI.Agent.Copilot**    | `copilot`, `tools`, `approval`                                                                                                   |
| **Umbraco.AI.Prompt**           | `prompt`                                                                                                                         |
| **Umbraco.AI.OpenAI**           | `openai`                                                                                                                         |
| **Umbraco.AI.Anthropic**        | `anthropic`                                                                                                                      |
| **Umbraco.AI.Amazon**           | `amazon`                                                                                                                         |
| **Umbraco.AI.Google**           | `google`                                                                                                                         |
| **Umbraco.AI.MicrosoftFoundry** | `microsoft-foundry`                                                                                                              |
| **Meta scopes**                 | `deps`, `ci`, `docs`, `release`                                                                                                  |

**Examples:**

```bash
# Single product
feat(chat): add streaming support
fix(openai): handle rate limit errors correctly
docs(prompt): update template examples

# Multiple products
feat(core,agent): add shared context API
fix(openai,anthropic): standardize error handling

# Breaking changes
feat(core)!: redesign profile API

BREAKING CHANGE: Profile.GetByName() removed, use GetByAlias() instead
```

### Generating Changelogs

**Generate changelog for a specific product:**

```bash
npm run changelog -- --product=Umbraco.AI --version=18.1.0

# Using PowerShell wrapper
.\scripts\generate-changelog.ps1 -Product Umbraco.AI -Version 18.1.0

# Using Bash wrapper
./scripts/generate-changelog.sh --product=Umbraco.AI --version=18.1.0
```

**Generate unreleased changes:**

```bash
npm run changelog -- --product=Umbraco.AI --unreleased
```

### Release Workflow with Changelogs

1. **Create release branch:**

    ```bash
    git checkout -b v18/release/2026.06.1
    ```

2. **Create release manifest:**

    ```bash
    .\scripts\generate-release-manifest.ps1   # Windows
    ./scripts/generate-release-manifest.sh    # Linux/Mac
    ```

3. **Update version.json** for each product in the manifest

4. **Generate changelogs** for each product:

    ```bash
    npm run changelog -- --product=Umbraco.AI --version=18.1.0
    npm run changelog -- --product=Umbraco.AI.OpenAI --version=18.0.1
    ```

5. **Review and edit** generated changelogs if needed

6. **Commit changelogs:**

    ```bash
    git add Umbraco.AI/CHANGELOG.md Umbraco.AI.OpenAI/CHANGELOG.md
    git commit -m "docs(core,openai): update CHANGELOGs for release 2026.06.1"
    ```

7. **Commit version updates:**

    ```bash
    git add release-manifest.json Umbraco.AI/version.json Umbraco.AI.OpenAI/version.json
    git commit -m "chore(release): prepare 2026.06.1"
    ```

8. **Push release branch:**

    ```bash
    git push -u origin v18/release/2026.06.1
    ```

9. **Azure DevOps validates and builds** — changelog validation runs automatically on `vN/release/*` and `vN/hotfix/*` branches.

10. **Test packages** from MyGet, then trigger production release from Azure DevOps.

### Commit Message Validation

The repository uses `commitlint` to validate commit messages.

**Setup validation hooks:**

```bash
.\scripts\setup-git-hooks.ps1    # Windows
./scripts/setup-git-hooks.sh     # Linux/Mac
```

This enables:

- **commit-msg hook**: Validates commit messages using commitlint (warnings only)
- **pre-push hook**: Validates branch naming conventions (blocks invalid names)
- **post-merge hook**: Cleans up `release-manifest.json` after merge to `vN/main` or `vN/dev`
- **pre-merge-commit hook**: Restores `release-manifest.json` on `vN/release/*` or `vN/hotfix/*` branches if deleted during merge
- **merge driver**: Preserves `release-manifest.json` on release/hotfix branches (content conflicts only)

### Troubleshooting Changelog Validation

If the Azure DevOps build fails with changelog validation errors on a release branch:

**Error: "CHANGELOG.md not found"**

```bash
npm run changelog -- --product=<ProductName> --version=<Version>
git add <Product>/CHANGELOG.md
git commit -m "docs(<scope>): add CHANGELOG for v<Version>"
git push
```

**Error: "Version mismatch"**

```bash
npm run changelog -- --product=<ProductName> --version=<Version>
git add <Product>/CHANGELOG.md
git commit -m "docs(<scope>): update CHANGELOG version to v<Version>"
git push
```

### Troubleshooting Release Manifest Issues

If `release-manifest.json` gets deleted when merging `vN/dev` into a release branch:

```bash
# 1. Verify git hooks are configured
git config --get core.hooksPath
# Should output: .githooks

# 2. If not configured, run setup script
.\scripts\setup-git-hooks.ps1    # Windows
./scripts/setup-git-hooks.sh     # Linux/Mac

# 3. If manifest was deleted, restore it manually
git show HEAD:release-manifest.json > release-manifest.json
git add release-manifest.json
git commit -m "fix(ci): restore release-manifest.json"
git push
```

## CI/CD Pipeline

### Overview

The CI/CD pipeline consists of two main stages:

1. **Build Pipeline** — Triggered by commits to `vN/release/*`, `vN/hotfix/*`, `vN/dev`, `vN/main`, and `vN/feature/*` branches
    - Builds and tests products
    - Creates NuGet and npm packages
    - Publishes artifacts for deployment

2. **Release Pipeline** — Triggered by build completion
    - Downloads artifacts from build pipeline
    - Deploys packages to package feeds
    - Tags git repository with package versions

### Build Artifacts

| Artifact Name        | Contents                                             | Used By                             |
| -------------------- | ---------------------------------------------------- | ----------------------------------- |
| `all-nuget-packages` | All .nupkg files from the build                      | Release pipeline (NuGet deployment) |
| `all-npm-packages`   | All .tgz files from the build                        | Release pipeline (npm deployment)   |
| `pack-manifest`      | JSON metadata for each package (name, version, type) | Release pipeline (git tagging)      |

### Change Detection

The Azure DevOps pipeline uses smart change detection to build only affected products:

- **`vN/dev` and `vN/main` pushes**: Compare against previous completed build on the same branch
- **`vN/release/*` branches**: Require `release-manifest.json`; pack only the listed products
- **`vN/hotfix/*` branches**: Honor manifest if present; otherwise use change detection
- **`vN/feature/*` branches**: Compare against merge-base with `vN/dev` (where N is extracted from the branch name)

### Git Tagging Strategy

The release pipeline automatically creates git tags for traceability:

| Tag Format            | Example                    | Created When                    |
| --------------------- | -------------------------- | ------------------------------- |
| `<Product>@<Version>` | `Umbraco.AI@18.1.0`        | Automated (by release pipeline) |
| `<Product>@<Version>` | `Umbraco.AI.OpenAI@18.0.1` | Automated (by release pipeline) |

Use these tags as base points for hotfix branches or to compare versions:

```bash
git log Umbraco.AI@18.0.0..Umbraco.AI@18.1.0
```

## Coding Standards

All contributions must follow the [coding standards in CLAUDE.md](CLAUDE.md#coding-standards).

### Key Conventions

**Method Naming:**

```csharp
// Async methods: [Action][Entity]Async
Task<AIProfile?> GetProfileAsync(Guid id, CancellationToken ct);
Task<IEnumerable<AIAgent>> GetAllAgentsAsync(CancellationToken ct);
```

**Repository Access:**

```csharp
// Only access your own repository
public class AIProfileService
{
    private readonly IAIProfileRepository _profileRepository;  // ✓ Own repo
    // ✗ Never inject another product's repository directly
}
```

**Extension Methods:**

```csharp
// Must be in .Extensions namespace
namespace Umbraco.AI.Extensions
{
    public static class ChatClientExtensions { }
}
```

### Code Review Guidelines

Reviewers should check:

- [ ] Follows method naming conventions
- [ ] No cross-repository access (services use services, not repositories)
- [ ] Extension methods in correct namespace
- [ ] Async methods have CancellationToken
- [ ] No hardcoded strings (use constants or resources)
- [ ] Tests included for new functionality
- [ ] No breaking changes without discussion

## Documentation

### When to Update Documentation

Update documentation when:

- Adding new features or public APIs
- Changing build/deployment process
- Modifying architecture or patterns
- Adding new dependencies

### Documentation Locations

| Type                      | Location                      |
| ------------------------- | ----------------------------- |
| Product-specific guidance | `<Product>/CLAUDE.md`         |
| Shared coding standards   | `CLAUDE.md`                   |
| Contributing guide        | `CONTRIBUTING.md` (this file) |
| User guides               | `docs/<topic>.md`             |
| API documentation         | XML comments in code          |

## Questions and Support

### Getting Help

1. **Search existing issues**: [GitHub Issues](https://github.com/umbraco/Umbraco.AI/issues)
2. **Ask on Discord**: [Umbraco Discord Server](https://discord.umbraco.com)
3. **Create new issue**: Provide minimal reproduction

### Reporting Bugs

Include:

- Product and version (e.g., Umbraco.AI.Core 18.1.0)
- Umbraco CMS version
- .NET version
- Steps to reproduce
- Expected vs actual behavior
- Stack trace (if applicable)

### Suggesting Features

Include:

- Which product(s) would be affected
- Use case / problem to solve
- Proposed API or interface
- Breaking change considerations

## License

By contributing, you agree that your contributions will be licensed under the same license as the Umbraco.AI project.

---

Thank you for contributing to Umbraco.AI! 🚀
