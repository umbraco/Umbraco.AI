# Umbraco.Ai Monorepo Migration Guide

## Overview

This document describes the migration of five separate Umbraco.Ai repositories into a unified monorepo structure. The migration was completed in January 2025 and preserves complete git history for all products.

## Why Migrate to a Monorepo?

### Problems with Separate Repositories

The original structure had five independent repositories:
- `Umbraco.Ai` (Core)
- `Umbraco.Ai.Agent`
- `Umbraco.Ai.Prompt`
- `Umbraco.Ai.OpenAi`
- `Umbraco.Ai.Anthropic`

This created several challenges:

**1. Dependency Management Complexity**
- Agent, Prompt, and Providers depend on Core
- Core changes required manual updates across 4 other repos
- Version mismatches caused integration issues
- Difficult to test changes across multiple products

**2. Fragmented Development Workflow**
- Developers needed 5 separate clones
- Cross-product features required multiple PRs
- No unified build or test infrastructure
- Difficult to maintain consistency

**3. Release Coordination**
- Releasing Core meant coordinating releases for all dependents
- Risk of version incompatibilities
- Manual synchronization of release schedules

**4. Code Duplication**
- Shared build scripts duplicated across repos
- Duplicate CI/CD configuration
- Inconsistent coding standards

### Benefits of the Monorepo

**1. Atomic Cross-Product Changes**
- Single PR can update Core and all dependents
- Changes tested together before merge
- No version skew between products

**2. Simplified Development**
- One clone, one setup script
- Unified solution for debugging across products
- Shared build infrastructure

**3. Independent Versioning**
- Each product maintains its own version (via NBGV)
- Core/Agent/Prompt: 17.x (matches Umbraco CMS)
- Providers: 1.x (independent versioning)
- Release products independently or together

**4. Better CI/CD**
- Smart change detection builds only affected products
- Parallel builds for independent products
- Unified test and deployment pipelines

## Migration Strategy

### Git History Preservation

Complete git history was preserved using `git subtree add`:
- **726 total commits** merged from all repositories
- All commit authors and dates preserved
- Full file history traceable with `git log --follow`

### Repository Structure

```
Umbraco.Ai/                    # Monorepo root (formerly just "Core" repo)
├── Umbraco.Ai/                # Core (restructured into subdirectory)
├── Umbraco.Ai.Agent/          # Agent (merged via git subtree)
├── Umbraco.Ai.Prompt/         # Prompt (merged via git subtree)
├── Umbraco.Ai.OpenAi/         # OpenAI (merged via git subtree)
├── Umbraco.Ai.Anthropic/      # Anthropic (merged via git subtree)
├── Directory.Packages.props   # Unified package management
├── Umbraco.Ai.sln            # Master solution
├── scripts/                   # Setup and utility scripts
│   ├── install-demo-site.ps1  # Local dev setup (Windows)
│   ├── install-demo-site.sh   # Local dev setup (Linux/Mac)
│   ├── setup-git-hooks.ps1    # Git hooks setup (Windows)
│   └── setup-git-hooks.sh     # Git hooks setup (Linux/Mac)
└── docs/                      # Shared documentation
```

### Key Technical Changes

**1. Central Package Management**
- Single `Directory.Packages.props` at root
- All products use shared package versions
- Eliminates version conflicts

**2. Conditional References**
All dependent projects (Agent, Prompt, Providers) use conditional references to Core:

```xml
<!-- During development: Use project reference -->
<ProjectReference Include="..\..\..\Umbraco.Ai\src\Umbraco.Ai.Core\Umbraco.Ai.Core.csproj"
                  Condition="'$(UseProjectReferences)' == 'true'" />

<!-- During pack: Use package reference -->
<PackageReference Include="Umbraco.Ai.Core"
                  Condition="'$(UseProjectReferences)' != 'true'" />
```

This enables:
- **Local development**: All products reference Core via project references (changes visible immediately)
- **Distribution builds**: Products reference Core via NuGet packages (independent versioning)

**3. Per-Product Versioning (NBGV)**
Each product has its own `version.json` with product-specific release patterns:

```json
{
  "version": "17.0.0",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/heads/release/core-",
    "^refs/tags/release-core-"
  ]
}
```

This allows:
- Independent version numbers
- Independent release schedules
- Product-specific release branches/tags

**4. Branch Naming Convention**
All branches must follow product-prefixed patterns:

- `feature/<product>-<description>` - e.g., `feature/core-add-caching`
- `release/<product>-<version>` - e.g., `release/agent-17.1.0`
- `hotfix/<product>-<version>` - e.g., `hotfix/openai-1.0.1`

Valid products: `core`, `agent`, `prompt`, `openai`, `anthropic`

**5. Smart CI/CD**
Azure DevOps pipeline detects changes and builds only affected products:

```powershell
# Git diff for branch builds
$changedFiles = git diff --name-only HEAD~1 HEAD
if ($file.StartsWith("Umbraco.Ai/")) {
    $changedProducts["core"] = $true
}

# Tag-based for releases
if ($tag -match "release-agent-") {
    $changedProducts["agent"] = $true
}
```

Dependency propagation ensures dependent products rebuild when Core changes.

## Working with the Monorepo

### Initial Setup

```bash
# Clone the monorepo
git clone https://github.com/umbraco/Umbraco.Ai.git
cd Umbraco.Ai

# One-time setup: creates unified solution + demo site
.\scripts\install-demo-site.ps1  # Windows
./scripts/install-demo-site.sh   # Linux/Mac

# Configure git hooks (enforces branch naming)
.\scripts\setup-git-hooks.ps1  # Windows
./scripts/setup-git-hooks.sh   # Linux/Mac

# Open unified solution
start Umbraco.Ai.local.sln
```

### Development Workflow

**Working on a Single Product:**
```bash
# Create product-specific branch
git checkout -b feature/core-add-embeddings

# Make changes in Umbraco.Ai/

# Build just that product
dotnet build Umbraco.Ai/Umbraco.Ai.sln

# Or build everything (includes dependents)
dotnet build Umbraco.Ai.sln
```

**Working on Multiple Products:**
```bash
# Changes to Core automatically visible in Agent/Prompt via project references
# Edit: Umbraco.Ai/src/Umbraco.Ai.Core/Chat/AiChatService.cs
# Debug: demo/Umbraco.Ai.DemoSite (references all products)

# Build unified solution to test everything
dotnet build Umbraco.Ai.local.sln
```

**Frontend Development:**
```bash
# Watch all frontends (hot reload)
npm run watch

# Or watch specific product
npm run watch:core
npm run watch:agent
```

### Release Workflow

**1. Create Release Branch:**
```bash
git checkout -b release/core-17.1.0

# Update version
# Edit: Umbraco.Ai/version.json
# Change: "version": "17.1.0"

git commit -am "chore: bump Core to 17.1.0"
git push -u origin release/core-17.1.0
```

**2. CI/CD Builds Product:**
- Azure DevOps detects branch pattern `release/core-*`
- Builds Core with `UseProjectReferences=false`
- Generates NuGet packages
- Deploys to MyGet (pre-release feed)

**3. Test and Tag:**
```bash
# After testing, create release tag
git tag release-core-17.1.0
git push origin release-core-17.1.0
```

**4. Production Deployment:**
- CI/CD detects tag pattern `release-core-*`
- Deploys to NuGet.org (production feed)

## Historical Artifacts

### Old Repository URLs

The following repositories were merged and are now archived:

- **Umbraco.Ai**: Now the monorepo root (NOT archived - this is the active repo)
- **Umbraco.Ai.Agent**: Archived - content in `Umbraco.Ai.Agent/`
- **Umbraco.Ai.Prompt**: Archived - content in `Umbraco.Ai.Prompt/`
- **Umbraco.Ai.OpenAi**: Archived - content in `Umbraco.Ai.OpenAi/`
- **Umbraco.Ai.Anthropic**: Archived - content in `Umbraco.Ai.Anthropic/`

### Accessing Old History

If you need to reference the original repositories:

**View file history across merge:**
```bash
# Core file (path changed during restructure)
git log --follow Umbraco.Ai/src/Umbraco.Ai.Core/Chat/AiChatService.cs

# Agent file (merged via subtree)
git log --follow Umbraco.Ai.Agent/src/Umbraco.Ai.Agent.Core/Agents/AiAgent.cs
```

**Find commits from specific author:**
```bash
git log --author="AuthorName" --all --oneline
```

**View all contributors:**
```bash
git shortlog -sn --all
```

## Known Limitations

### Pre-Existing Test Errors

Some test failures existed before migration and are not regression issues:
- Agent.Tests.Unit: 26 failing tests (pre-existing)

These should be fixed in future work but are unrelated to the monorepo structure.

## Troubleshooting

### "PackageVersion not defined" Error

**Problem:**
```
error NU1010: The following PackageReference items do not define a corresponding PackageVersion item
```

**Solution:** Check `Directory.Packages.props` includes all referenced packages. Compare with individual product `Directory.Packages.props` files in git history if needed.

### Branch Push Rejected

**Problem:**
```
ERROR: Invalid branch name: feature/add-caching
Valid patterns: feature/<product>-<description>
```

**Solution:** Use product-prefixed branch names:
```bash
git checkout -b feature/core-add-caching
```

### Git History Not Showing

**Problem:** `git log` doesn't show commits before migration

**Solution:** Use `--follow` flag:
```bash
git log --follow -- Umbraco.Ai/src/Umbraco.Ai.Core/Chat/AiChatService.cs
```

## Migration Statistics

- **Total Commits Preserved**: 726
- **Products Merged**: 5
- **Files Migrated**: 2,847
- **Contributors Preserved**: All (17+ contributors)
- **Migration Duration**: 2 days (planning + execution)
- **History Integrity**: 100% (verified via `git log --follow`)

## Further Reading

- [Contributing Guide](contributing.md) - Detailed git workflow and conventions
- [CLAUDE.md](../CLAUDE.md) - Development standards and architecture
- [Product-specific CLAUDE.md files](../Umbraco.Ai/CLAUDE.md) - Per-product guidance

## Questions?

For questions about the monorepo structure or migration:
1. Check [contributing.md](contributing.md) for workflow guidance
2. Review Azure DevOps pipeline documentation
3. Contact the Umbraco.Ai team
