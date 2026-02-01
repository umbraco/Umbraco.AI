---
name: managing-repository
description: Provides a unified interface for repository management including setup, releases, changelogs, and builds. Use when unsure which management task to perform, or when needing guidance on repository operations like building, releasing, or generating changelogs.
allowed-tools: AskUserQuestion, Skill, Bash
---

# Repository Manager

You are the orchestrator for managing the Umbraco.Ai repository. You provide a unified interface to various repository management tasks.

## Task

Present an interactive menu of available repository operations and delegate to specialized skills.

## Available Operations

### 1. Setup (`/setting-up-repository`)
Initial repository setup for new developers:
- Install git hooks
- Create demo site
- Install dependencies
- Run initial build

### 2. Release Management (`/managing-releases`)
Generate release manifests for packaging:
- Interactive product selection
- Creates `release-manifest.json`
- Required for `release/*` branches

### 3. Changelog Generation (`/managing-changelogs`)
Generate changelogs from commit history:
- List available products
- Generate for specific version or unreleased
- Validates against conventional commits

### 4. Build Operations
Common build tasks:
- `dotnet build Umbraco.Ai.local.sln` - Build unified solution
- `npm run build` - Build all frontends
- `npm run watch` - Watch all frontends
- `dotnet test` - Run tests

## Workflow

1. **Present menu** - Use AskUserQuestion to show available operations:
   - "Setup repository"
   - "Generate release manifest"
   - "Generate changelog"
   - "Build solution"
   - "Watch frontends"

2. **Delegate to appropriate skill** or execute directly:
   - Setup → Invoke `/setting-up-repository` skill
   - Release → Invoke `/managing-releases` skill
   - Changelog → Invoke `/managing-changelogs` skill
   - Build → Execute command directly

3. **Report results** - Show outcome and suggest next steps

## When to Use Each Operation

| Operation | When to Use |
|-----------|-------------|
| Setup | First time cloning repo, new dev onboarding |
| Release Manager | Creating `release/*` or `hotfix/*` branch |
| Changelog | Before pushing release branch, generating release notes |
| Build | After pulling changes, switching branches |
| Watch | Active frontend development |

## Important Context

- Repository root: Contains all products as subdirectories
- Monorepo structure: Each product has own solution
- npm workspaces: Unified frontend dependency management
- Release manifest: Required on `release/*` branches
- Changelogs: Auto-generated from conventional commits

## Example Flow

```
User invokes: /managing-repository

You present menu:
"What would you like to do?"
- Setup repository (new clone/dev setup)
- Generate release manifest (for release/hotfix)
- Generate changelog (for release documentation)
- Build solution
- Watch frontends

User selects: "Generate release manifest"

You invoke: /managing-releases

After completion, you remind:
- Next: Generate changelogs with /managing-changelogs
- CI will validate manifest on push
```
