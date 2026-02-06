---
name: changelog-management
description: Generates changelogs for products from conventional commit history. Use when preparing a release, updating release documentation, or previewing unreleased changes before creating a release branch.
allowed-tools: Bash, Read, AskUserQuestion
---

# Changelog Manager

You are helping generate changelogs for Umbraco.AI products from conventional commit history.

## Task

Generate or update `CHANGELOG.md` files for products using the changelog generation scripts.

## How Changelogs Work

- Each product has a `CHANGELOG.md` at its root
- Auto-generated from git history using [Conventional Commits](https://www.conventionalcommits.org/)
- Each product has a `changelog.config.json` defining its commit scopes
- CI validates changelogs on `release/*` and `hotfix/*` branches

## Available Commands

### List Products

```bash
npm run changelog:list
```

### Generate Changelog

```bash
# For a specific version
npm run changelog -- --product=Umbraco.AI --version=1.1.0

# For unreleased changes
npm run changelog -- --product=Umbraco.AI --unreleased
```

### PowerShell Wrapper

```powershell
.\scripts\generate-changelog.ps1 -Product Umbraco.AI -Version 1.1.0
```

### Bash Wrapper

```bash
./scripts/generate-changelog.sh --product=Umbraco.AI --version=1.1.0
```

## Workflow

1. **Ask user for input**:
    - List available products or let user specify
    - Ask for version number (or use --unreleased flag)
    - Platform preference (npm, PowerShell, or Bash)

2. **Run the appropriate command**

3. **Verify the changelog was updated**:
    - Read the generated CHANGELOG.md
    - Show the new entry to the user

4. **Remind about next steps**:
    - Review the generated changelog
    - Edit if needed (add breaking changes details, etc.)
    - Commit the changes

## Available Products

Products are auto-discovered by scanning for `changelog.config.json` files:

- Umbraco.AI (core)
- Umbraco.AI.OpenAI
- Umbraco.AI.Anthropic
- Umbraco.AI.Amazon
- Umbraco.AI.Google
- Umbraco.AI.MicrosoftFoundry
- Umbraco.AI.Prompt
- Umbraco.AI.Agent

## Important Notes

- Always run from repository root
- Use `--unreleased` for preview before creating release branch
- Version must match the version in `version.json`
- CI will fail if changelog is missing or outdated on release branches

## Example Flow

```
User invokes: /changelog-management

You ask: "Which product?" (list available)
User selects: Umbraco.AI

You ask: "What version?" (or suggest --unreleased)
User provides: 1.1.0

You detect: Windows platform
You run: .\scripts\generate-changelog.ps1 -Product Umbraco.AI -Version 1.1.0

You verify: Read Umbraco.AI/CHANGELOG.md and show the new entry

You remind:
- Review the generated content
- Edit if needed
- Commit the updated CHANGELOG.md
```

## Troubleshooting

- If no commits found: Check commit message format (should follow conventional commits)
- If wrong scope: Update `changelog.config.json` for the product
- If multiple products changed: Run changelog generation for each product in the release manifest
