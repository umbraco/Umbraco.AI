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
├── Umbraco.Ai/                # Core AI layer (17.x)
├── Umbraco.Ai.Agent/          # Agent add-on (17.x)
├── Umbraco.Ai.Prompt/         # Prompt add-on (17.x)
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

### Examples

**Correct:**
```bash
feature/add-streaming-support
feature/improve-context-handling
feature/add-versioning
release/2026.01
hotfix/2026.01.1
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
| Umbraco.Ai (Core) | 17.x (matches Umbraco CMS) | 17.0.0 |
| Umbraco.Ai.Agent | 17.x (matches Umbraco CMS) | 17.0.0 |
| Umbraco.Ai.Prompt | 17.x (matches Umbraco CMS) | 17.0.0 |
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
  "version": "17.1.0",
  "assemblyVersion": {
    "precision": "build"
  },
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/heads/release/",
    "^refs/heads/hotfix/",
    "^refs/tags/release-",
    "^refs/tags/hotfix-"
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
   - Examples: `Umbraco.Ai@17.1.0`, `Umbraco.Ai.OpenAi@1.2.0`
   - Tags are pushed to the source branch

**MyGet URL:** `https://www.myget.org/F/umbraco-ai/api/v3/index.json`

#### 6. Test Pre-Release

```bash
# Add MyGet feed
dotnet nuget add source https://www.myget.org/F/umbraco-ai/api/v3/index.json -n UmbracoAi

# Install pre-release package
dotnet add package Umbraco.Ai.Core --version 17.1.0-*

# Install pre-release npm package
npm install @umbraco-ai/core@next
```

Test the packages in a real Umbraco site.

#### 7. Create Release Tag

Once testing passes, create the release tag to trigger production deployment:

```bash
git checkout release/2026.01
git pull origin release/2026.01

# Create and push release tag
git tag release-2026.01
git push origin release-2026.01
```

#### 8. Production Release Pipeline

Azure DevOps detects the `release-*` tag pattern and triggers the production release pipeline:

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
   - Examples: `Umbraco.Ai@17.1.0`, `Umbraco.Ai.OpenAi@1.2.0`
   - Tags are pushed to the repository

**NuGet URL:** `https://www.nuget.org/packages/Umbraco.Ai.Core`
**npm URL:** `https://www.npmjs.com/package/@umbraco-ai/core`

#### 9. Merge to Main

```bash
# Create PR: release/2026.01 → main
# After approval and merge, delete release branch
git checkout main
git pull origin main
git branch -d release/2026.01
git push origin --delete release/2026.01
```

**Note on Git Tags:** The release pipeline automatically creates product-specific tags (e.g., `Umbraco.Ai@17.1.0`). These tags reference the exact commit that was released and can be used to trace which code version is in production.

### Hotfix Workflow

For emergency fixes to production:

```bash
# 1. Create hotfix branch from the release tag
git checkout release-2026.01
git checkout -b hotfix/2026.01.1

# 2. Fix the issue
# Edit: Umbraco.Ai/src/...

# 3. Update version.json for affected products
# Change: "version": "17.1.1"

# 4. (Optional) Add release-manifest.json if you want an explicit pack list
# On hotfix/* branches, the manifest is optional:
#   - If present: Only listed products are packed (enforced)
#   - If absent: Change detection is used (automatic)
echo '["Umbraco.Ai"]' > release-manifest.json

# 5. Commit and push
git add .
git commit -m "fix(core): resolve critical security issue"
git push -u origin hotfix/2026.01.1

# 6. Build pipeline runs
# - Packs affected products (per manifest or change detection)
# - Publishes artifacts: all-nuget-packages, all-npm-packages, pack-manifest

# 7. Release pipeline deploys to MyGet and creates pre-release tags
# Tags example: Umbraco.Ai@17.1.1-preview

# 8. Test hotfix packages
dotnet add package Umbraco.Ai.Core --version 17.1.1-*

# 9. Create hotfix tag for production release
git tag hotfix-2026.01.1
git push origin hotfix-2026.01.1

# 10. Production release pipeline runs
# - Deploys to NuGet.org and npm registry
# - Creates production tags: Umbraco.Ai@17.1.1

# 11. Merge hotfix to main
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
   - `Umbraco.Ai@17.1.0`
   - `Umbraco.Ai.OpenAi@1.2.0`
   - `Umbraco.Ai.Anthropic@1.2.0`

**Important:** On `release/*` branches, `release-manifest.json` is **required**. CI will fail if any changed product is missing from the list. This ensures intentional releases and prevents accidental package publishing.

On `hotfix/*` branches, the manifest is **optional**. If present, it is enforced the same way; if absent, change detection is used automatically.

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
    "version": "17.1.0",
    "type": "nuget"
  },
  {
    "name": "@umbraco-ai/core",
    "version": "17.1.0",
    "type": "npm"
  }
]
```

### Git Tagging Strategy

The release pipeline automatically creates git tags for traceability:

| Tag Format | Example | Purpose | Created When |
|------------|---------|---------|--------------|
| `release-<version>` | `release-2026.01` | Triggers production deployment | Manual (by developer) |
| `hotfix-<version>` | `hotfix-2026.01.1` | Triggers production deployment | Manual (by developer) |
| `<Product>@<Version>` | `Umbraco.Ai@17.1.0` | Tracks deployed package version | Automated (by release pipeline) |

**How it works:**
1. Release pipeline reads `pack-manifest` artifact
2. For each package in the manifest, creates a git tag: `[Product_Name]@[Version]`
3. Tags are pushed to the source branch (e.g., `release/2026.01` or `main`)

**Benefits:**
- Trace which exact commit was deployed for each package
- Navigate to source code for any production version
- Compare versions across products (e.g., `git log Umbraco.Ai@17.0.0..Umbraco.Ai@17.1.0`)

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
- Pushes tags to repository (e.g., `Umbraco.Ai@17.1.0`)
- Tags point to the commit that was built

### Manual Triggers

To force a build of all products (bypass change detection):

```yaml
# Azure DevOps: Queue new build
# Set pipeline variables:
forceReleaseCore: true
forceReleaseAgent: true
forceReleasePrompt: true
forceReleaseOpenAi: true
forceReleaseAnthropic: true
```

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
- Product and version (e.g., Umbraco.Ai.Core 17.0.0)
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
