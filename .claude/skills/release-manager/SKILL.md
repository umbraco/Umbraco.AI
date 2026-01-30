---
name: release-manager
description: Generate and manage release manifests for release/hotfix branches
allowed-tools: Bash, Read, Write, Glob
---

# Release Manager

You are helping manage release manifests for the Umbraco.Ai repository.

## Task

Generate `release-manifest.json` at the repository root by discovering available products and allowing the user to select which ones to include using a numbered menu (replicating the behavior of the generate-release-manifest.sh/ps1 scripts).

## About Release Manifests

- **Required** on `release/*` branches (CI will fail without it)
- **Optional** on `hotfix/*` branches (falls back to change detection if absent)
- Lists which products to package and release
- Format: JSON array of product names (e.g., `["Umbraco.Ai", "Umbraco.Ai.OpenAi"]`)

## Workflow

1. **Discover products** - Find all `Umbraco.Ai*` directories at repository root:
   ```bash
   find . -maxdepth 1 -type d -name "Umbraco.Ai*" | sed 's|^\./||' | sort
   ```

2. **Display numbered menu** - Show all products with numbers:
   ```
   Select products to include in this release:

   1. Umbraco.Ai
   2. Umbraco.Ai.Agent
   3. Umbraco.Ai.Agent.Copilot
   4. Umbraco.Ai.Amazon
   5. Umbraco.Ai.Anthropic
   6. Umbraco.Ai.Google
   7. Umbraco.Ai.MicrosoftFoundry
   8. Umbraco.Ai.OpenAi
   9. Umbraco.Ai.Prompt
   ```

3. **Get user selection** - Display the menu and wait for user response:
   - After showing the numbered list, ask the user to provide their selection
   - Do NOT use AskUserQuestion - just wait for the user to respond naturally
   - The user will type their selection in the chat
   - Parse their input, supporting multiple formats:
     - Comma-separated: `1,3,5,8`
     - Space-separated: `1 3 5 8`
     - Range notation: `1-4,7,9`
     - Special commands: `all`, `none`, `cancel`
     - Product names: `Umbraco.Ai, Umbraco.Ai.OpenAi`

4. **Validate selection** - Check that:
   - Numbers are within valid range (1 to N)
   - Product names exist (if names provided)
   - At least one product selected (unless intentionally empty)

5. **Generate manifest** - Write selected products to `release-manifest.json`:
   ```json
   [
     "Umbraco.Ai",
     "Umbraco.Ai.OpenAi"
   ]
   ```
   - Use Write tool to create the file at repository root
   - Format as pretty-printed JSON with 2-space indentation
   - Sort products alphabetically

6. **Confirm creation** - Read and display the generated manifest

7. **Remind about next steps**:
   - Commit the manifest to version control
   - CI will validate it against changed files on push
   - Generate changelogs for included products using `/changelog-manager`

## Important Notes

- Always run from repository root
- Manifest is validated by CI on `release/*` and `hotfix/*` branches
- On release branches, CI ensures all changed products are in the manifest
- The file path must be `release-manifest.json` at the repository root
- This replicates the behavior of `scripts/generate-release-manifest.sh` and `scripts/generate-release-manifest.ps1`

## Example Flow

```
User invokes: /release-manager

You discover and display:
Select products to include in this release:

1. Umbraco.Ai
2. Umbraco.Ai.Agent
3. Umbraco.Ai.Agent.Copilot
4. Umbraco.Ai.Amazon
5. Umbraco.Ai.Anthropic
6. Umbraco.Ai.Google
7. Umbraco.Ai.MicrosoftFoundry
8. Umbraco.Ai.OpenAi
9. Umbraco.Ai.Prompt

You ask: "Enter product numbers (comma or space-separated, e.g., 1,3,5) or type 'all' for all products:"

User types: "1,2,8,9"

You parse: 1=Umbraco.Ai, 2=Umbraco.Ai.Agent, 8=Umbraco.Ai.OpenAi, 9=Umbraco.Ai.Prompt

You generate release-manifest.json:
[
  "Umbraco.Ai",
  "Umbraco.Ai.Agent",
  "Umbraco.Ai.OpenAi",
  "Umbraco.Ai.Prompt"
]

You confirm:
✓ Generated release-manifest.json with 4 products

You remind:
- Commit the manifest file
- Generate changelogs with /changelog-manager
- CI will validate on push
```
