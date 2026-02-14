---
name: repo-management
description: Provides a unified interface for repository management including setup, releases, changelogs, and builds. Use when unsure which management task to perform, or when needing guidance on repository operations like building, releasing, or generating changelogs.
allowed-tools: AskUserQuestion, Skill, Bash
---

# Repository Manager

You are the orchestrator for managing the Umbraco.AI repository. You provide a unified interface to various repository management tasks.

## Task

Present an interactive menu of available repository operations and delegate to specialized skills.

## Available Operations

### 1. Setup (`/repo-setup`)

Initial repository setup for new developers:

- Install git hooks
- Create demo site
- Install dependencies
- Run initial build

### 2. Release Management (`/release-management`)

Complete release preparation orchestration:

- Detects changed products and analyzes commits
- Recommends version bumps (major/minor/patch)
- Updates version.json files
- Generates release-manifest.json
- Generates CHANGELOG.md files
- Creates release branch with all changes committed

### 3. Changelog Generation (`/changelog-management`)

Generate changelogs from commit history:

- List available products
- Generate for specific version or unreleased
- Validates against conventional commits

### 4. Build Operations

Common build tasks:

- `dotnet build Umbraco.AI.local.sln` - Build unified solution
- `npm run build` - Build all frontends
- `npm run watch` - Watch all frontends
- `dotnet test` - Run tests

## Workflow

1. **Present menu** - Use AskUserQuestion to show available operations:
    - "Setup repository"
    - "Prepare release (full orchestration)"
    - "Generate changelog only"
    - "Build solution"
    - "Watch frontends"

2. **Delegate to appropriate skill** or execute directly:
    - Setup → Invoke `/repo-setup` skill
    - Release → Invoke `/release-management` skill
    - Changelog → Invoke `/changelog-management` skill
    - Build → Execute command directly

3. **Report results** - Show outcome and suggest next steps

## When to Use Each Operation

| Operation        | When to Use                                          |
| ---------------- | ---------------------------------------------------- |
| Setup            | First time cloning repo, new dev onboarding          |
| Release Manager  | Preparing a complete release (end-to-end automation) |
| Changelog        | Updating individual product changelog only           |
| Build            | After pulling changes, switching branches            |
| Watch            | Active frontend development                          |

## Important Context

- Repository root: Contains all products as subdirectories
- Monorepo structure: Each product has own solution
- npm workspaces: Unified frontend dependency management
- Release manifest: Required on `release/*` branches
- Changelogs: Auto-generated from conventional commits

## Example Flow

```
User invokes: /repo-management

You present menu:
"What would you like to do?"
- Setup repository (new clone/dev setup)
- Prepare release (full orchestration)
- Generate changelog only
- Build solution
- Watch frontends

User selects: "Prepare release"

You invoke: /release-management

The release-management skill will:
- Detect changed products
- Analyze commits and recommend version bumps
- Update version.json files
- Generate release-manifest.json
- Generate all changelogs
- Create release branch

After completion, you show summary and next steps.
```
