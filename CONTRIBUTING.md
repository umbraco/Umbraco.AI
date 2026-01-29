# Contributing to Umbraco.Ai

This guide explains how to contribute to the Umbraco.Ai monorepo, covering branch naming conventions, git workflows, and release processes.

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
- Node.js 20.x
- Git
- SQL Server or SQLite (for database development)
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/umbraco/Umbraco.Ai.git
cd Umbraco.Ai

# Run setup script (creates unified solution + demo site)
.\scripts\install-demo-site.ps1  # Windows
./scripts/install-demo-site.sh   # Linux/Mac

# Configure git hooks (enforces branch naming)
.\scripts\setup-git-hooks.ps1  # Windows
./scripts/setup-git-hooks.sh   # Linux/Mac

# Open unified solution
start Umbraco.Ai.local.sln
```

### Repository Structure

```
Umbraco.Ai/                    # Monorepo root
├── Umbraco.Ai/                # Core AI layer (1.x)
├── Umbraco.Ai.Agent/          # Agent add-on (1.x)
├── Umbraco.Ai.Prompt/         # Prompt add-on (1.x)
├── Umbraco.Ai.OpenAi/         # OpenAI provider (1.x)
├── Umbraco.Ai.Anthropic/      # Anthropic provider (1.x)
├── Umbraco.Ai.Amazon/         # Amazon Bedrock provider (1.x)
├── Umbraco.Ai.Google/         # Google Gemini provider (1.x)
├── Umbraco.Ai.MicrosoftFoundry/ # Microsoft AI Foundry provider (1.x)
├── demo/                      # Demo site (generated)
└── docs/                      # Shared documentation
```

## Branch Naming Convention

**All branches MUST follow these patterns.** This is enforced by git hooks and CI/CD.

### Valid Branch Patterns

| Pattern | Description | Example |
|---------|-------------|---------|
| `main` | Main development branch | `main` |
| `dev` | Integration branch | `dev` |
| `feature/<anything>` | New feature development | `feature/add-embeddings` |
| `release/<anything>` | Release preparation | `release/2026.01` |
| `hotfix/<anything>` | Emergency fixes | `hotfix/2026.01.1` |

### Recommended Naming Conventions

While the pattern allows `<anything>` after the prefix, we recommend these conventions for consistency:

**Release branches:** `release/YYYY.MM`
- `YYYY.MM` = Year and month of the release
- Example: `release/2026.01` for a January 2026 release
- Example: `release/2026.12` for a December 2026 release

**Hotfix branches:** `hotfix/YYYY.MM.N`
- `YYYY.MM` = Year and month
- `.N` = Sequential number (1st, 2nd, 3rd hotfix in that period)
- Example: `hotfix/2026.01.1` for the first hotfix in January 2026
- Example: `hotfix/2026.01.2` for the second hotfix in January 2026

**Benefits of this convention:**
- Calendar-based organization makes it easy to find branches chronologically
- Clear distinction between regular releases (monthly cadence) and hotfixes (emergency patches)
- Sequential hotfix numbering prevents branch name conflicts
- Independent from product version numbers (which follow semantic versioning)

**Note:** This is a recommendation, not a requirement. The validation only enforces the prefix pattern (`release/` or `hotfix/`).

### Examples

**Correct:**
```bash
feature/add-streaming-support
feature/improve-context-handling
feature/add-versioning
release/2026.01              # Recommended: calendar-based
release/v1.1.0               # Valid: version-based
hotfix/2026.01.1             # Recommended: calendar-based with sequence
hotfix/critical-security-fix # Valid: descriptive name
```

**Incorrect:**
```bash
feature-add-streaming        # Wrong delimiter
release-2026.01              # Wrong delimiter
```

### Enforcement

Branch naming is enforced at two levels:

1. **Git Hooks** (`.githooks/pre-push`): Local validation before push
2. **GitHub Actions** (`.github/workflows/validate-branch.yml`): CI/CD validation (cannot be bypassed)

To bypass git hooks temporarily (not recommended):
```bash
git push --no-verify
```

## Development Workflow

### Feature Development (Single Product)

```bash
# 1. Create feature branch from main
git checkout main
git pull origin main
git checkout -b feature/add-embeddings

# 2. Make changes in the product directory
# Edit: Umbraco.Ai/src/Umbraco.Ai.Core/...

# 3. Build and test
dotnet build Umbraco.Ai/Umbraco.Ai.sln
dotnet test Umbraco.Ai/Umbraco.Ai.sln

# 4. Test in demo site
cd demo/Umbraco.Ai.DemoSite
dotnet run

# 5. Commit changes
git add .
git commit -m "feat(core): add embedding support

Implements IChatClient.EmbeddAsync using M.E.AI abstractions"

# 6. Push and create PR
git push -u origin feature/add-embeddings
```

### Feature Development (Cross-Product)

When a feature spans multiple products (e.g., Core + Agent):

```bash
# 1. Create feature branch
git checkout -b feature/shared-context

# 2. Make changes to both products
# Edit: Umbraco.Ai/src/Umbraco.Ai.Core/...
# Edit: Umbraco.Ai.Agent/src/Umbraco.Ai.Agent.Core/...

# 3. Build unified solution (tests everything together)
dotnet build Umbraco.Ai.local.sln

# 4. Test in demo site
cd demo/Umbraco.Ai.DemoSite
dotnet run

# 5. Commit atomic changes
git add .
git commit -m "feat(core,agent): add shared context handling

- Core: Add IContextProvider interface
- Agent: Implement context sharing between agents"

# 6. Push and create PR
git push -u origin feature/shared-context
```

**Note:** Use a descriptive branch name that reflects the scope of the work.

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
<ProjectReference Include="..\..\..\Umbraco.Ai\src\Umbraco.Ai.Core\Umbraco.Ai.Core.csproj"
                  Condition="'$(UseProjectReferences)' == 'true'" />
```

This means:
- **Local builds**: Agent/Prompt/Providers automatically use your local Core changes
- **Distribution builds**: CI/CD builds with `UseProjectReferences=false` for package references

### Running Tests

```bash
# Run tests for specific product
dotnet test Umbraco.Ai/Umbraco.Ai.sln
dotnet test Umbraco.Ai.Agent/Umbraco.Ai.Agent.sln

# Run all tests
dotnet test Umbraco.Ai.local.sln
```

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

- [ ] Branch name follows convention (`feature/<anything>`)
- [ ] Code follows [coding standards](CLAUDE.md#coding-standards)
- [ ] All tests pass
- [ ] Frontend builds (if frontend changes)
- [ ] Documentation updated (if needed)
- [ ] CLAUDE.md updated (if architecture changes)
- [ ] No console errors or warnings

### Review Process

1. **Automated Checks**: GitHub Actions runs branch validation + unit tests
2. **Code Review**: At least one team member must approve
3. **CI/CD**: Azure DevOps builds affected products
4. **Merge**: Squash merge to main (keeps history clean)

## Release Process

Each product is versioned and released independently using Nerdbank.GitVersioning (NBGV).

### Version Numbers

| Product | Version Scheme | Current |
|---------|----------------|---------|
| Umbraco.Ai (Core) | 1.x (independent) | 1.0.0 |
| Umbraco.Ai.Agent | 1.x (independent) | 1.0.0 |
| Umbraco.Ai.Prompt | 1.x (independent) | 1.0.0 |
| Umbraco.Ai.OpenAi | 1.x (independent) | 1.0.0 |
| Umbraco.Ai.Anthropic | 1.x (independent) | 1.0.0 |
| Umbraco.Ai.Amazon | 1.x (independent) | 1.0.0 |
| Umbraco.Ai.Google | 1.x (independent) | 1.0.0 |
| Umbraco.Ai.MicrosoftFoundry | 1.x (independent) | 1.0.0 |

### Release Workflow

#### 1. Create Release Branch

```bash
git checkout main
git pull origin main
git checkout -b release/2026.01
```

#### 2. Define Release Manifest

Create `release-manifest.json` at repo root:

```json
[
  "Umbraco.Ai",
  "Umbraco.Ai.OpenAi"
]
```

#### 3. Update Versions

Edit each product's `version.json` in the manifest:

```json
{
  "version": "1.1.0",
  "assemblyVersion": {
    "precision": "build"
  },
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/heads/release/",
    "^refs/heads/hotfix/"
  ]
}
```

```bash
git add release-manifest.json Umbraco.Ai/version.json
git commit -m "chore(release): prepare 2026.01"
git push -u origin release/2026.01
```

#### 4. CI/CD Build Pipeline

Azure DevOps detects the `release/*` branch pattern:
- Enforces `release-manifest.json` (CI fails if any changed product is missing from the list)
- Builds and packs only the listed products
- Publishes two artifacts:
  - `all-nuget-packages` - Contains all NuGet packages (.nupkg)
  - `all-npm-packages` - Contains all npm packages (.tgz)
- Publishes `pack-manifest` artifact - Contains metadata for each package (name, version, type)

#### 5. Release Pipeline Deployment

The Azure DevOps **release pipeline** automatically triggers after the build completes:

1. **Download Artifacts**
   - Downloads `all-nuget-packages` artifact (contains all .nupkg files)
   - Downloads `all-npm-packages` artifact (contains all .tgz files)
   - Downloads `pack-manifest` artifact (contains package metadata)

2. **Deploy Packages**
   - Deploys NuGet packages to **MyGet** (pre-release feed)
   - Deploys npm packages to **npm registry** with `@next` tag

3. **Tag Git Repository**
   - Reads `pack-manifest` artifact to get each package name and version
   - Creates git tag for each deployed package: `[Product_Name]@[Version]`
   - Examples: `Umbraco.Ai@1.1.0`, `Umbraco.Ai.OpenAi@1.2.0`
   - Tags are pushed to the repository

**MyGet URL:** `https://www.myget.org/F/umbraco-ai/api/v3/index.json`

#### 6. Test Pre-Release

```bash
# Add MyGet feed
dotnet nuget add source https://www.myget.org/F/umbraco-ai/api/v3/index.json -n UmbracoAi

# Install pre-release package
dotnet add package Umbraco.Ai.Core --version 1.1.0-*

# Install pre-release npm package
npm install @umbraco-ai/core@next
```

Test the packages in a real Umbraco site.

#### 7. Production Release Pipeline

Once testing passes, trigger the production release from Azure DevOps. The release pipeline:

1. **Download Artifacts**
   - Downloads `all-nuget-packages` artifact from the build
   - Downloads `all-npm-packages` artifact from the build
   - Downloads `pack-manifest` artifact

2. **Deploy to Production**
   - Deploys NuGet packages to **NuGet.org**
   - Deploys npm packages to **npm registry** with `@latest` tag

3. **Tag Git Repository**
   - Reads `pack-manifest` to get each package name and version
   - Creates git tag for each deployed package: `[Product_Name]@[Version]`
   - Examples: `Umbraco.Ai@1.1.0`, `Umbraco.Ai.OpenAi@1.2.0`
   - Tags are pushed to the repository

**NuGet URL:** `https://www.nuget.org/packages/Umbraco.Ai.Core`
**npm URL:** `https://www.npmjs.com/package/@umbraco-ai/core`

#### 8. Merge to Main

```bash
# Create PR: release/2026.01 → main
# After approval and merge, delete release branch
git checkout main
git pull origin main
git branch -d release/2026.01
git push origin --delete release/2026.01
```

**Note on Git Tags:** The release pipeline automatically creates git tags during deployment:
- Product-specific tags (e.g., `Umbraco.Ai@1.1.0`) track each deployed package version
- These tags reference the exact commit that was built and released
- Use these tags as base points for hotfix branches or to trace which code version is in production

### Hotfix Workflow

For emergency fixes to production:

```bash
# 1. Create hotfix branch from the production tag
# Find the specific product version that needs fixing
git tag --list | grep "Umbraco.Ai@"
# Example output: Umbraco.Ai@1.1.0, Umbraco.Ai.OpenAi@1.2.0

# Branch from the specific product tag
git checkout -b hotfix/2026.01.1 Umbraco.Ai@1.1.0

# If multiple products need hotfixes, branch from main instead
# git checkout -b hotfix/2026.01.1 main

# 2. Fix the issue
# Edit: Umbraco.Ai/src/...

# 3. Update version.json for affected products
# Change: "version": "1.1.1"
# Edit: Umbraco.Ai/version.json

# 4. Generate changelog for the hotfix
npm run changelog -- --product=Umbraco.Ai --version=1.1.1
# Review and edit the changelog entry

# 5. (Optional) Add release-manifest.json if you want an explicit pack list
# On hotfix/* branches, the manifest is optional:
#   - If present: Only listed products are packed (enforced)
#   - If absent: Change detection is used (automatic)
echo '["Umbraco.Ai"]' > release-manifest.json

# 6. Commit and push
git add Umbraco.Ai/CHANGELOG.md release-manifest.json Umbraco.Ai/version.json
git commit -m "fix(core): resolve critical security issue"
git push -u origin hotfix/2026.01.1

# 7. Build pipeline runs
# - Changelog validation runs (same as release branches)
# - Packs affected products (per manifest or change detection)
# - Publishes artifacts: all-nuget-packages, all-npm-packages, pack-manifest

# 8. Release pipeline deploys to MyGet and creates pre-release tags
# Tags example: Umbraco.Ai@1.1.1-preview

# 9. Test hotfix packages
dotnet add package Umbraco.Ai.Core --version 1.1.1-*

# 10. Trigger production release from Azure DevOps
# - Release pipeline deploys to NuGet.org and npm registry
# - Automatically creates production tags: Umbraco.Ai@1.1.1

# 11. Merge hotfix to main
# Create PR: hotfix/2026.01.1 → main
# After approval and merge, delete hotfix branch
```

### Releasing Multiple Products

To release multiple products in a single release:

1. **Create `release-manifest.json`** at repo root with all products to release:

```json
[
  "Umbraco.Ai",
  "Umbraco.Ai.OpenAi",
  "Umbraco.Ai.Anthropic"
]
```

2. **Update `version.json`** for each listed product

3. **Push release branch** - CI enforces that all listed products are packed

4. **Release pipeline creates tags** for each product:
   - `Umbraco.Ai@1.1.0`
   - `Umbraco.Ai.OpenAi@1.2.0`
   - `Umbraco.Ai.Anthropic@1.2.0`

**Important:** On `release/*` branches, `release-manifest.json` is **required**. CI will fail if any changed product is missing from the list. This ensures intentional releases and prevents accidental package publishing.

On `hotfix/*` branches, the manifest is **optional**. If present, it is enforced the same way; if absent, change detection is used automatically.

### Cross-Product Dependency Management

Add-on packages and providers depend on Umbraco.Ai (Core). When releasing products with dependencies, follow these guidelines:

#### Version Ranges (Required)

**Always use version ranges** for cross-product dependencies. This allows add-ons to work with a range of Core versions without requiring simultaneous releases.

**Example:** If Umbraco.Ai.Prompt 1.1.0 is compatible with Core 1.1.x and later within the 1.x series:

```xml
<!-- Umbraco.Ai.Prompt/Directory.Packages.props -->
<Project>
  <ItemGroup>
    <!-- Use a range: minimum version 1.1.0, up to (but not including) 1.999.999 -->
    <PackageVersion Include="Umbraco.Ai.Core" Version="[1.1.0, 1.999.999)" />
  </ItemGroup>
</Project>
```

The range format `[minimum, maximum)` means:
- `[` = inclusive lower bound (>= 1.1.0)
- `)` = exclusive upper bound (< 1.999.999)
- Result: accepts any 1.x version from 1.1.0 onwards

#### How It Works

1. **Root level** (`Directory.Packages.props` at repo root): Defines default package versions for all products
2. **Product level** (`ProductFolder/Directory.Packages.props`): Overrides specific package versions for that product only
3. **During local development**: Project references (`UseProjectReferences=true`) bypass NuGet versions
4. **During CI/CD build**: Distribution builds (`UseProjectReferences=false`) use the specified NuGet version ranges

#### Release Coordination

When releasing Core with breaking changes:

1. **Bump Core minor version**: `1.1.0` → `1.2.0`
2. **Update dependent products**: Update their `Directory.Packages.props` to the new minimum version:
   ```xml
   <PackageVersion Include="Umbraco.Ai.Core" Version="[1.2.0, 1.999.999)" />
   ```
3. **Include in release manifest**: All dependent products must be included in the same release:
   ```json
   [
     "Umbraco.Ai",
     "Umbraco.Ai.Prompt",
     "Umbraco.Ai.Agent"
   ]
   ```

#### Version Range Guidelines

| Scenario | Range Format | Example | Description |
|----------|--------------|---------|-------------|
| Minor version series | `[X.Y.0, X.999.999)` | `[1.1.0, 1.999.999)` | Min 1.1.0, accepts all 1.x |
| Specific minimum | `[X.Y.Z, X.999.999)` | `[1.1.5, 1.999.999)` | Min 1.1.5, accepts all 1.x |
| Exact version | `[X.Y.Z]` | `[1.1.0]` | **Avoid** - prevents any updates |

**Best Practice:** Use `[X.Y.0, X.999.999)` format where X.Y.0 is the minimum supported Core version. This allows all future patch and minor releases within the major version.

#### Testing Dependencies

Before releasing, verify dependencies resolve correctly:

```bash
# Build with NuGet references (not project references)
dotnet build Umbraco.Ai.local.sln /p:UseProjectReferences=false

# Verify the correct Core version is resolved
dotnet list Umbraco.Ai.Prompt/src/Umbraco.Ai.Prompt.Core package --include-transitive | grep Umbraco.Ai.Core
```

## Maintaining Changelogs

Each product maintains its own `CHANGELOG.md` file at the product root, auto-generated from git history using conventional commits. Changelogs follow the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format.

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
- `revert`: Reverts a previous commit

**Scope** - The product or feature area affected (see table below)

**Description** - Brief summary in present tense (e.g., "add streaming support")

### Commit Scopes

Scopes are automatically discovered from product `changelog.config.json` files:

| Product | Scopes |
|---------|--------|
| **Umbraco.Ai** | `core`, `profile`, `chat`, `embedding`, `connection`, `middleware`, `registry`, `settings`, `providers`, `ui`, `frontend`, `api` |
| **Umbraco.Ai.Agent** | `agent` |
| **Umbraco.Ai.Agent.Copilot** | `copilot`, `tools`, `approval` |
| **Umbraco.Ai.Prompt** | `prompt` |
| **Umbraco.Ai.OpenAi** | `openai` |
| **Umbraco.Ai.Anthropic** | `anthropic` |
| **Umbraco.Ai.Amazon** | `amazon` |
| **Umbraco.Ai.Google** | `google` |
| **Umbraco.Ai.MicrosoftFoundry** | `microsoft-foundry` |
| **Meta scopes** | `deps`, `ci`, `docs`, `release` |

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
feat(core): redesign profile API

BREAKING CHANGE: Profile.GetByName() removed, use GetByAlias() instead
```

### Generating Changelogs

Changelogs are generated manually before creating a release in Azure DevOps.

**List available products:**
```bash
npm run changelog:list
# Or: node scripts/generate-changelog.js --list
```

**Generate changelog for a specific product:**
```bash
# Using npm script
npm run changelog -- --product=Umbraco.Ai --version=1.1.0

# Using Node.js directly
node scripts/generate-changelog.js --product=Umbraco.Ai --version=1.1.0

# Using PowerShell wrapper
.\scripts\generate-changelog.ps1 -Product Umbraco.Ai -Version 1.1.0

# Using Bash wrapper
./scripts/generate-changelog.sh --product=Umbraco.Ai --version=1.1.0
```

**Generate unreleased changes:**
```bash
npm run changelog -- --product=Umbraco.Ai --unreleased
```

**Generate all changelogs at once:**
```bash
npm run changelog:all
```

### Updated Release Workflow with Changelogs

When creating a release, follow these steps:

1. **Create release branch:**
   ```bash
   git checkout -b release/2026.01
   ```

2. **Create release manifest** (`release-manifest.json`):
   ```json
   ["Umbraco.Ai", "Umbraco.Ai.OpenAi"]
   ```

3. **Update version.json** for each product in the manifest

4. **Generate changelogs** for each product:
   ```bash
   npm run changelog -- --product=Umbraco.Ai --version=1.1.0
   npm run changelog -- --product=Umbraco.Ai.OpenAi --version=1.2.0
   ```

5. **Review and edit** generated changelogs (if needed):
   - Check that entries are accurate
   - Add context to commit messages if needed
   - Group related changes
   - Highlight important changes

6. **Commit changelogs:**
   ```bash
   git add Umbraco.Ai/CHANGELOG.md Umbraco.Ai.OpenAi/CHANGELOG.md
   git commit -m "docs(core,openai): update CHANGELOGs for release 2026.01"
   ```

7. **Commit version updates:**
   ```bash
   git add release-manifest.json Umbraco.Ai/version.json Umbraco.Ai.OpenAi/version.json
   git commit -m "chore(release): prepare 2026.01"
   ```

8. **Push release branch:**
   ```bash
   git push -u origin release/2026.01
   ```

9. **Azure DevOps validates and builds:**
   - **Changelog validation** runs automatically (release and hotfix branches)
     - Verifies CHANGELOG.md exists for each product in manifest
     - Checks CHANGELOG.md was updated in recent commits
     - Validates version in CHANGELOG.md matches version.json
     - **Build fails if validation fails** - fix issues and push again
   - Builds and publishes to MyGet (pre-release)

10. **Test packages** from MyGet

11. **Trigger production release** from Azure DevOps
    - Release pipeline deploys to NuGet.org and npm
    - Automatically creates git tags: `Umbraco.Ai@1.1.0`, `Umbraco.Ai.OpenAi@1.2.0`
    - Tags include the changelog commits

12. **Merge release branch to main**

### Adding a New Product

To add changelog support for a new product:

1. **Create product directory:** `Umbraco.Ai.NewProduct/`

2. **Create changelog config:**
   ```json
   // Umbraco.Ai.NewProduct/changelog.config.json
   {
     "scopes": ["new-product"]
   }
   ```

3. **Verify discovery:**
   ```bash
   npm run changelog:list
   # Should show your new product automatically!
   ```

4. **Generate initial changelog:**
   ```bash
   npm run changelog -- --product=Umbraco.Ai.NewProduct --unreleased
   ```

No script changes needed - products are discovered automatically by convention!

### Commit Message Validation

The repository uses `commitlint` to validate commit messages. Invalid commits will show warnings but are still allowed.

**Setup validation hooks:**
```bash
.\scripts\setup-git-hooks.ps1    # Windows
./scripts/setup-git-hooks.sh     # Linux/Mac
```

### Troubleshooting Changelog Validation

If the Azure DevOps build fails with changelog validation errors on a release branch:

**Error: "CHANGELOG.md not found"**
```bash
# Generate the missing changelog
npm run changelog -- --product=<ProductName> --version=<Version>
git add <Product>/CHANGELOG.md
git commit -m "docs(<scope>): add CHANGELOG for v<Version>"
git push
```

**Error: "Version mismatch"**
```bash
# The version in CHANGELOG.md doesn't match version.json
# Either update the changelog version manually, or regenerate it:
npm run changelog -- --product=<ProductName> --version=<Version>
git add <Product>/CHANGELOG.md
git commit -m "docs(<scope>): update CHANGELOG version to v<Version>"
git push
```

**Warning: "CHANGELOG.md does not appear to have been updated"**
- This is a warning, not an error - build will still pass
- Indicates the CHANGELOG.md exists but wasn't modified in recent commits
- Usually means you forgot to regenerate the changelog for this release
- Regenerate and commit to resolve

This enables:
- **commit-msg hook**: Validates commit messages using commitlint
- **pre-push hook**: Validates branch naming conventions

**Testing your commit message:**
```bash
# Test a commit message
echo "feat(chat): add streaming" | npx commitlint

# Check recent commits
npx commitlint --from HEAD~5 --to HEAD
```

## CI/CD Pipeline

### Overview

The CI/CD pipeline consists of two main stages:

1. **Build Pipeline** - Triggered by commits to `release/*`, `hotfix/*`, or other branches
   - Builds and tests products
   - Creates NuGet and npm packages
   - Publishes artifacts for deployment

2. **Release Pipeline** - Triggered by build completion or git tags
   - Downloads artifacts from build pipeline
   - Deploys packages to package feeds
   - Tags git repository with package versions

### Build Artifacts

Each build produces the following artifacts:

| Artifact Name | Contents | Used By |
|---------------|----------|---------|
| `all-nuget-packages` | All .nupkg files from the build | Release pipeline (NuGet deployment) |
| `all-npm-packages` | All .tgz files from the build | Release pipeline (npm deployment) |
| `pack-manifest` | JSON metadata for each package (name, version, type) | Release pipeline (git tagging) |

**Example `pack-manifest` content:**
```json
[
  {
    "name": "Umbraco.Ai",
    "version": "1.1.0",
    "type": "nuget"
  },
  {
    "name": "@umbraco-ai/core",
    "version": "1.1.0",
    "type": "npm"
  }
]
```

### Git Tagging Strategy

The release pipeline automatically creates git tags for traceability:

| Tag Format | Example | Purpose | Created When |
|------------|---------|---------|--------------|
| `<Product>@<Version>` | `Umbraco.Ai@1.1.0` | Tracks deployed package version | Automated (by release pipeline) |
| `<Product>@<Version>` | `Umbraco.Ai.OpenAi@1.2.0` | Tracks deployed package version | Automated (by release pipeline) |

**How it works:**
1. Release pipeline reads `pack-manifest` artifact
2. For each package in the manifest, creates a git tag: `[Product_Name]@[Version]`
3. Tags are pushed to the repository pointing to the commit that was built and deployed

**Benefits:**
- Trace which exact commit was deployed for each package
- Navigate to source code for any production version
- Use tags as base points for hotfix branches
- Compare versions across products (e.g., `git log Umbraco.Ai@1.0.0..Umbraco.Ai@1.1.0`)

### Change Detection

The Azure DevOps pipeline uses smart change detection to build only affected products:

**Branch Builds:**
```powershell
# Analyze git diff
$changedFiles = git diff --name-only HEAD~1 HEAD

# Determine changed products
if ($file.StartsWith("Umbraco.Ai/")) {
    $changedProducts["core"] = $true
}

# No dependency propagation (only products with direct changes pack)
```

**Release Branches:**
- `release/*` branches require `release-manifest.json` and pack only the listed products.
- `hotfix/*` branches honor the manifest if present; otherwise, change detection is used.

### Pipeline Stages

```mermaid
graph TB
    A[DetectChanges] --> B[Build]
    B --> C[Test]
    B --> D[PublishArtifacts]
    D --> E[ReleasePipeline]
    E --> F{Deploy Type}
    F -->|release/* branch| G[MyGet/npm@next]
    F -->|release-* tag| H[NuGet/npm@latest]
    G --> I[TagRepository]
    H --> I[TagRepository]
```

#### Build Pipeline Stages

**1. DetectChanges**
- Analyzes git changes or reads `release-manifest.json`
- Sets variables: `CoreChanged`, `AgentChanged`, etc.
- Enforces manifest requirements on `release/*` branches

**2. Build (Parallel)**
- Builds only changed products (or manifest-listed products)
- Uses `UseProjectReferences=false` for distribution builds
- Generates NuGet packages (.nupkg) and npm packages (.tgz)
- Creates `pack-manifest` with metadata

**3. Test (Parallel)**
- Runs unit tests for changed products
- Runs integration tests where applicable
- Publishes code coverage reports

**4. PublishArtifacts**
- Publishes `all-nuget-packages` artifact
- Publishes `all-npm-packages` artifact
- Publishes `pack-manifest` artifact

#### Release Pipeline Stages

**5. ReleasePipeline** (triggered on build completion or git tag)
- Downloads artifacts from build pipeline
- Validates package integrity
- Determines deployment target (pre-release vs production)

**6. DeployMyGet** (on `release/*` or `hotfix/*` branches)
- Deploys NuGet packages to MyGet feed
- Deploys npm packages with `@next` tag
- URL: `https://www.myget.org/F/umbraco-ai/api/v3/index.json`

**7. DeployProduction** (on `release-*` or `hotfix-*` tags)
- Deploys NuGet packages to NuGet.org
- Deploys npm packages with `@latest` tag
- URLs: `https://www.nuget.org/`, `https://www.npmjs.com/`

**8. TagRepository** (all deployments)
- Reads `pack-manifest` artifact
- Creates git tag for each package: `<Product>@<Version>`
- Pushes tags to repository (e.g., `Umbraco.Ai@1.1.0`)
- Tags point to the commit that was built

## Coding Standards

All contributions must follow the [coding standards in CLAUDE.md](CLAUDE.md#coding-standards).

### Key Conventions

**Method Naming:**
```csharp
// Async methods: [Action][Entity]Async
Task<AiProfile?> GetProfileAsync(Guid id, CancellationToken ct);
Task<IEnumerable<AiAgent>> GetAllAgentsAsync(CancellationToken ct);
```

**Repository Access:**
```csharp
// Only access your own repository
public class AiProfileService
{
    private readonly IAiProfileRepository _profileRepository;  // ✓ Own repo
    private readonly IAiConnectionRepository _connectionRepository; // ✗ Other repo
}
```

**Extension Methods:**
```csharp
// Must be in .Extensions namespace
namespace Umbraco.Ai.Extensions
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

| Type | Location |
|------|----------|
| Product-specific guidance | `<Product>/CLAUDE.md` |
| Shared coding standards | `CLAUDE.md` |
| Contributing guide | `CONTRIBUTING.md` (this file) |
| Monorepo structure | `docs/migration-guide.md` |
| User guides | `docs/<topic>.md` |
| API documentation | XML comments in code |

## Questions and Support

### Getting Help

1. **Search existing issues**: [GitHub Issues](https://github.com/umbraco/Umbraco.Ai/issues)
2. **Ask on Discord**: [Umbraco Discord Server](https://discord.umbraco.com)
3. **Create new issue**: Provide minimal reproduction

### Reporting Bugs

Include:
- Product and version (e.g., Umbraco.Ai.Core 1.0.0)
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

By contributing, you agree that your contributions will be licensed under the same license as the Umbraco.Ai project.

---

Thank you for contributing to Umbraco.Ai! 🚀
