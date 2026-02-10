---
name: release-manifest-management
description: Generates and manages the release-manifest.json file. Use when you need to create or update the manifest file that specifies which products to include in a release/hotfix. Called by the release-management orchestration skill.
allowed-tools: Bash, Read, Write, Glob
---

# Release Manifest Manager

You are helping manage the `release-manifest.json` file for the Umbraco.AI repository.

## Task

Generate `release-manifest.json` at the repository root. Products can be provided as a parameter or selected interactively via a numbered menu.

**Usage:**
- With products: `/release-manifest-management --products="Umbraco.AI,Umbraco.AI.OpenAI,Umbraco.AI.Agent"`
- Interactive: `/release-manifest-management` (shows menu)

## About Release Manifests

- **Required** on `release/*` branches (CI will fail without it)
- **Optional** on `hotfix/*` branches (falls back to change detection if absent)
- Lists which products to package and release
- Format: JSON array of product names (e.g., `["Umbraco.AI", "Umbraco.AI.OpenAI"]`)

## Workflow

### Mode Detection

Check the initial prompt/task for a `--products` parameter:
- If `--products="Product1,Product2,..."` is present → Use **Automated Mode**
- If no products specified → Use **Interactive Mode**

### Automated Mode (products provided)

1. **Parse product list** from parameter (comma-separated)
2. **Discover available products** to validate:
    ```bash
    find . -maxdepth 1 -type d -name "Umbraco.AI*" | sed 's|^\./||' | sort
    ```
3. **Validate** each product exists in the repository
4. **Skip to Generate Manifest** (step 5 below)

### Interactive Mode (no products provided)

1. **Discover products** - Find all `Umbraco.AI*` directories at repository root:

    ```bash
    find . -maxdepth 1 -type d -name "Umbraco.AI*" | sed 's|^\./||' | sort
    ```

2. **Display numbered menu** - Show all products with numbers:

    ```
    Select products to include in this release:

    1. Umbraco.AI
    2. Umbraco.AI.Agent
    3. Umbraco.AI.Agent.Copilot
    4. Umbraco.AI.Amazon
    5. Umbraco.AI.Anthropic
    6. Umbraco.AI.Google
    7. Umbraco.AI.MicrosoftFoundry
    8. Umbraco.AI.OpenAI
    9. Umbraco.AI.Prompt
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
        - Product names: `Umbraco.AI, Umbraco.AI.OpenAI`

4. **Validate selection** - Check that:
    - Numbers are within valid range (1 to N)
    - Product names exist (if names provided)
    - At least one product selected (unless intentionally empty)

### Common Steps (both modes)

5. **Generate manifest** - Write selected products to `release-manifest.json`:

    ```json
    ["Umbraco.AI", "Umbraco.AI.OpenAI"]
    ```

    - Use Write tool to create the file at repository root
    - Format as pretty-printed JSON with 2-space indentation
    - Sort products alphabetically

6. **Confirm creation** - Read and display the generated manifest

## Important Notes

- Always run from repository root
- Manifest is validated by CI on `release/*` and `hotfix/*` branches
- On release branches, CI ensures all changed products are in the manifest
- The file path must be `release-manifest.json` at the repository root
- This skill is typically invoked by the `/release-management` orchestration skill

## Example Flows

### Automated Mode (from release-management skill)

```
Skill invoked with: --products="Umbraco.AI,Umbraco.AI.Agent,Umbraco.AI.OpenAI,Umbraco.AI.Prompt"

You discover products to validate against
You validate all provided products exist
You generate release-manifest.json:
[
  "Umbraco.AI",
  "Umbraco.AI.Agent",
  "Umbraco.AI.OpenAI",
  "Umbraco.AI.Prompt"
]

You confirm:
✓ Generated release-manifest.json with 4 products
```

### Interactive Mode (user selection)

```
User invokes: /release-manifest-management

You discover and display:
Select products to include in this release:

1. Umbraco.AI
2. Umbraco.AI.Agent
3. Umbraco.AI.Agent.Copilot
4. Umbraco.AI.Amazon
5. Umbraco.AI.Anthropic
6. Umbraco.AI.Google
7. Umbraco.AI.MicrosoftFoundry
8. Umbraco.AI.OpenAI
9. Umbraco.AI.Prompt

You ask: "Enter product numbers (comma or space-separated, e.g., 1,3,5) or type 'all' for all products:"

User types: "1,2,8,9"

You parse: 1=Umbraco.AI, 2=Umbraco.AI.Agent, 8=Umbraco.AI.OpenAI, 9=Umbraco.AI.Prompt

You generate release-manifest.json:
[
  "Umbraco.AI",
  "Umbraco.AI.Agent",
  "Umbraco.AI.OpenAI",
  "Umbraco.AI.Prompt"
]

You confirm:
✓ Generated release-manifest.json with 4 products
```
