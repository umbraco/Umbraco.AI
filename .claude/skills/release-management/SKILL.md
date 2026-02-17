---
name: release-management
description: Orchestrates the complete release preparation process - detects changed products, analyzes commits for version bumps, updates version.json files, generates manifests and changelogs, and creates release branches. Use when preparing a new release.
allowed-tools: Bash, Read, Write, Edit, Glob, Grep, Skill, AskUserQuestion
---

# Release Manager

You are the orchestrator for preparing releases in the Umbraco.AI repository.

## Task

Guide users through the complete release preparation process:

1. **Detect changed products** since their last release tags
2. **Analyze commits** to recommend version bumps (major/minor/patch)
3. **Confirm versions** with the user
4. **Update Directory.Packages.props** inter-product dependency ranges (with user approval)
5. **Update package.peer-dependencies.json** npm peer dependency ranges (with user approval)
6. **Create release branch** (e.g., `release/2026.02.1`) and switch to it
7. **Dependency validation** - Check for cross-product conflicts
8. **Update version.json** files for each product
9. **Generate release-manifest.json** via `/release-manifest-management`
10. **Generate CHANGELOG.md** files via `/changelog-management`
11. **Validate** all files are consistent
12. **Commit all changes** to the release branch

## Workflow

### Phase 1: Change Detection

1. **Find all products** - Discover Umbraco.AI* directories at repo root

2. **For each product**, detect changes since last release:
    ```bash
    # Find the most recent release tag for this product
    git tag --list "Umbraco.AI@*" --sort=-version:refname | head -n1

    # Get commits affecting this product since that tag
    git log <tag>..HEAD --oneline -- <ProductFolder>/

    # Exclude non-substantive changes (CHANGELOG.md, version.json)
    git diff <tag>..HEAD --name-only -- <ProductFolder>/ | grep -v 'CHANGELOG.md\|version.json'
    ```

3. **Present changed products** to user:
    ```
    Detected changes since last release:

    ┌─────────────────────┬──────────┬─────────────────────────────┐
    │ Product             │ Last Tag │ Changes                     │
    ├─────────────────────┼──────────┼─────────────────────────────┤
    │ Umbraco.AI          │ 1.0.0    │ 12 commits (3 feat, 2 fix)  │
    │ Umbraco.AI.OpenAI   │ 1.0.0    │ 3 commits (1 fix)           │
    │ Umbraco.AI.Prompt   │ 1.0.0    │ 5 commits (1 BREAKING)      │
    └─────────────────────┴──────────┴─────────────────────────────┘
    ```

### Phase 2: Version Bump Analysis

For each changed product:

1. **Analyze commit types** since last tag:
    ```bash
    # Get all commits with their messages
    git log <tag>..HEAD --pretty=format:"%s" -- <ProductFolder>/
    ```

2. **Determine bump level** based on conventional commits:
    - **BREAKING CHANGE** in body or exclamation mark after scope → **Major** (1.0.0 → 2.0.0)
    - feat: or feat(scope): → **Minor** (1.0.0 → 1.1.0)
    - fix:, perf: → **Patch** (1.0.0 → 1.0.1)
    - Only docs:, chore:, refactor: → **No bump** (but user can override)

3. **Read current version** from `<Product>/version.json`

4. **Calculate new version**:
    - Major: Increment X in X.Y.Z, reset Y and Z to 0
    - Minor: Increment Y in X.Y.Z, reset Z to 0
    - Patch: Increment Z in X.Y.Z

5. **Present recommendations**:
    ```
    Version bump recommendations:

    ┌─────────────────────┬──────────┬──────────┬─────────────────────────────┐
    │ Product             │ Current  │ Proposed │ Reason                      │
    ├─────────────────────┼──────────┼──────────┼─────────────────────────────┤
    │ Umbraco.AI          │ 1.0.0    │ 1.1.0    │ 3 feat, 2 fix commits       │
    │ Umbraco.AI.OpenAI   │ 1.0.0    │ 1.0.1    │ 1 fix commit                │
    │ Umbraco.AI.Prompt   │ 1.0.0    │ 2.0.0    │ 1 BREAKING CHANGE           │
    └─────────────────────┴──────────┴──────────┴─────────────────────────────┘
    ```

### Phase 3: Version Confirmation

Use **AskUserQuestion** to confirm or adjust versions:

- **Default option**: "Use recommended versions (above)"
- **Alternative options**:
    - "Downplay breaking changes to minor" - Treat breaking changes as minor bumps (X.Y.0 → X.Y+1.0 instead of X+1.0.0)
    - "Adjust individual versions" - Manually specify version for each product
    - "Cancel release preparation"

**If user chooses "Downplay breaking changes to minor":**
- For all products with major bumps (X.Y.Z → X+1.0.0), change to minor bumps (X.Y.Z → X.Y+1.0)
- Keep all other bumps (minor, patch) as-is
- Show updated version table and confirm

**If user chooses "Adjust individual versions":**
- For each product, ask for custom version
- Validate version format (X.Y.Z)
- Warn if version doesn't follow semver conventions

### Phase 4: Update Inter-Product Dependency Ranges (.NET)

After confirming versions, update the `Directory.Packages.props` file to reflect new minimum version requirements **only for products with breaking changes**.

**Important:** Only update dependency ranges when there's a breaking change that requires dependent packages to update. If dependent packages can continue working with the previous version, keep the existing range.

**Workflow:**

1. **Read current Directory.Packages.props**:
   ```bash
   # Read the root Directory.Packages.props file
   cat Directory.Packages.props
   ```

2. **Identify products with breaking changes**:
   - Look for products with BREAKING CHANGE in commit bodies
   - Look for commits with `!` after scope (e.g., `feat!:`, `refactor!:`)
   - Track whether breaking changes were downplayed to minor (still breaking!)
   - **Only these products need dependency range updates**

3. **Determine which ranges need updating**:
   - Look for `<PackageVersion Include="Umbraco.AI.*" Version="[X.Y.Z, X.999.999)" />` entries
   - **Only update ranges for products with breaking changes**:
     - **Major bump** (X.Y.Z → X+1.0.0): Update both bounds: `[X+1.0.0, X+1.999.999)`
     - **Minor bump from downplayed breaking change** (X.Y.Z → X.Y+1.0): Update lower bound only: `[X.Y+1.0, X.999.999)`
   - **Do NOT update ranges for products without breaking changes** (feat/fix only bumps)

4. **Present proposed changes** to user (if any breaking changes detected):
   ```
   Directory.Packages.props updates:

   Breaking changes detected in:
   - Umbraco.AI.Core (BREAKING CHANGE: DetailLevel removal)

   The following dependency ranges will be updated:
   - Umbraco.AI.Core: [1.0.0, 1.999.999) → [1.1.0, 1.999.999)
   - Umbraco.AI.Web: [1.0.0, 1.999.999) → [1.1.0, 1.999.999)
   - Umbraco.AI.AGUI: [1.0.0, 1.999.999) → [1.1.0, 1.999.999)
   - Umbraco.AI.Startup: [1.0.0, 1.999.999) → [1.1.0, 1.999.999)

   Products without breaking changes (Agent, Prompt) will keep existing ranges.
   ```

5. **Ask for approval** using AskUserQuestion (only if breaking changes detected):
   - **Default option**: "Update dependency ranges for breaking changes (recommended)"
   - **Alternative options**:
     - "Skip dependency updates" - Continue without updating ranges
     - "Adjust manually later" - Skip now, remind user to update manually

6. **If no breaking changes detected**:
   ```
   ✓ No breaking changes detected - dependency ranges remain unchanged
   ```

7. **If approved, update the file**:
   ```bash
   # Use Edit tool to update Directory.Packages.props
   ```

8. **Confirm updates**:
   ```
   ✓ Updated 4 inter-product dependency ranges in Directory.Packages.props
   ```

**Important Notes:**
- **Conservative approach**: Only update ranges when there are breaking changes
- Products without breaking changes keep their existing ranges (allowing flexibility)
- Only the lower bound changes for minor bumps from downplayed breaking changes
- Both bounds change for true major bumps (X.0.0 → X+1.0.0)
- These changes will be staged and committed with other release files
- If unsure, err on the side of NOT updating (keeps more flexibility for consumers)

### Phase 4.5: Update Inter-Product Peer Dependencies (npm)

After updating `Directory.Packages.props`, update `package.peer-dependencies.json` to keep npm peer dependencies in sync with .NET dependency ranges.

**Important:** npm peer dependency ranges should mirror the minimum versions from `Directory.Packages.props`.

**Workflow:**

1. **Read current package.peer-dependencies.json**:
   ```bash
   cat package.peer-dependencies.json
   ```

2. **Identify products with breaking changes** (same as Phase 4):
   - Products with BREAKING CHANGE in commit bodies
   - Products with `!` after scope (e.g., `feat!:`, `refactor!:`)
   - Products where breaking changes were downplayed to minor (still breaking!)

3. **Determine which npm ranges need updating**:
   - For each product with breaking changes, check if it has an npm package (has `Client/package.json` with `types` field)
   - **Only update npm peer dependency ranges for products with breaking changes**:
     - **Major bump** (1.2.0 → 2.0.0): Update to `^2.0.0`
     - **Minor bump from downplayed breaking change** (1.2.0 → 1.3.0): Update to `^1.3.0`
   - **Do NOT update ranges for products without breaking changes**

4. **Present proposed changes** to user (if any breaking changes detected):
   ```
   package.peer-dependencies.json updates:

   Breaking changes detected in:
   - Umbraco.AI.Core (BREAKING CHANGE: DetailLevel removal)

   The following npm peer dependency ranges will be updated:
   - @umbraco-ai/core: ^1.2.0 → ^1.3.0

   Products without breaking changes (Agent, Prompt) will keep existing ranges.
   ```

5. **Validate consistency** with Directory.Packages.props:
   ```bash
   # Compare minimum versions between .NET and npm
   # Warn if mismatches detected

   Example:
   ⚠️  Version Mismatch:
   - Directory.Packages.props: Umbraco.AI.Core [1.3.0, 1.999.999)
   - package.peer-dependencies.json: @umbraco-ai/core ^1.2.0

   The npm range should match the .NET minimum version (1.3.0).
   ```

6. **Ask for approval** using AskUserQuestion (only if breaking changes detected):
   - **Default option**: "Update npm peer dependency ranges (recommended)"
   - **Alternative options**:
     - "Skip npm updates" - Continue without updating ranges
     - "Adjust manually later" - Skip now, remind user to update manually

7. **If no breaking changes detected**:
   ```
   ✓ No breaking changes detected - npm peer dependency ranges remain unchanged
   ```

8. **If approved, update the file**:
   ```bash
   # Use Edit tool to update package.peer-dependencies.json
   ```

9. **Confirm updates**:
   ```
   ✓ Updated npm peer dependency ranges in package.peer-dependencies.json
   ```

**Mapping .NET to npm ranges:**

| .NET Range                | npm Range | Notes                                      |
|---------------------------|-----------|-------------------------------------------|
| `[1.2.0, 1.999.999)`      | `^1.2.0`  | Both mean: ≥1.2.0 and <2.0.0             |
| `[2.0.0, 2.999.999)`      | `^2.0.0`  | Both mean: ≥2.0.0 and <3.0.0             |
| `[17.1.0, 17.999.999)`    | `^17.1.0` | Both mean: ≥17.1.0 and <18.0.0           |

**Important Notes:**
- npm ranges should always mirror the minimum version from Directory.Packages.props
- Only update for products with breaking changes (same logic as Phase 4)
- External dependencies like `@umbraco-cms/backoffice` are also managed here
- These changes will be staged and committed with other release files

### Phase 6: Create Release Branch

**IMPORTANT:** Create the release branch BEFORE making any file changes.

**Branch Naming Convention:**

Per CONTRIBUTING.md, the **recommended** convention is calendar-based with incrementing numbers:
- `release/YYYY.MM.N` - Year, month, and incrementing release number
- Example: `release/2026.02.1` for the first February 2026 release
- Example: `release/2026.02.2` for the second February 2026 release

This is independent from product version numbers (which follow semantic versioning). A single release branch like `release/2026.02.1` can contain multiple products at different versions (e.g., Core@1.1.0, OpenAI@2.0.0, Prompt@1.0.5).

**Workflow:**

1. **Determine current date** - Get current year and month for default branch name:
   ```bash
   # Get current year and month
   date +"%Y.%m"  # Example output: 2026.02
   ```

2. **Find next release number** - Check existing date-based release tags on GitHub:
   ```bash
   # Fetch all tags from remote to ensure we have the latest
   git fetch --tags

   # Find existing release tags for current month (e.g., 2026.02.*)
   # IMPORTANT: This pattern only matches date-based release tags (YYYY.MM.COUNT),
   # not product version tags (Product@VERSION)
   git tag --list "2026.02.*" --sort=-version:refname

   # Example output (only date-based tags):
   # 2026.02.3
   # 2026.02.2
   # 2026.02.1
   #
   # This will NOT match product tags like:
   # Umbraco.AI@1.1.0
   # Umbraco.AI.OpenAI@2.0.0

   # Parse the highest number and increment by 1
   # If no tags exist for current month, start with 1
   ```

   **Tag Parsing Logic:**

   The repository uses **two types of tags**:
   - **Date-based release tags**: `YYYY.MM.N` (e.g., `2026.02.1`) - Represent complete release events
   - **Product version tags**: `Product@Version` (e.g., `Umbraco.AI@1.1.0`) - Track individual product versions

   **For release branch naming, we only care about date-based tags:**
   - Date tags follow the format `YYYY.MM.N` where N is an incrementing counter
   - Created by the release pipeline when a release is deployed
   - Multiple products can be released together under one date tag (e.g., `2026.02.1` might include Core@1.1.0, OpenAI@2.0.0, Prompt@1.0.5)
   - The pattern `git tag --list "YYYY.MM.*"` only matches date-based tags, not product tags
   - Find the latest date tag for the current month and increment N
   - If no date tags exist for current month, N starts at 1

3. **Ask user for branch name**:
   ```
   Create release branch using recommended calendar naming?

   Latest release tag for February 2026: 2026.02.2
   Suggested branch name: release/2026.02.3 (next release in February 2026)

   Options:
   - Use suggested name (release/2026.02.3)
   - Enter custom name (e.g., release/v1.1.0 for version-based)
   - Cancel
   ```

   **If no tags exist for current month:**
   ```
   Create release branch using recommended calendar naming?

   No previous releases found for February 2026.
   Suggested branch name: release/2026.02.1 (first release in February 2026)

   Options:
   - Use suggested name (release/2026.02.1)
   - Enter custom name (e.g., release/v1.1.0 for version-based)
   - Cancel
   ```

3. **Create and checkout branch**:
    ```bash
    git checkout -b release/<name>
    ```

4. **Confirm branch creation**:
    ```
    ✓ Created and switched to branch: release/2026.02.1

    All subsequent changes will be made on this branch.

    Note: This release will contain:
    - Umbraco.AI 1.1.0
    - Umbraco.AI.OpenAI 1.0.1
    - Umbraco.AI.Prompt 2.0.0
    ```

### Phase 7: Dependency Validation

Check for cross-product dependency issues:

1. **Find dependency ranges** in `Directory.Packages.props` files:
    ```bash
    grep -r "Umbraco.AI.Core" */Directory.Packages.props
    ```

2. **Warn about breaking changes**:
    ```
    ⚠️  Warning: Version conflict detected

    Umbraco.AI → 2.0.0 (major bump)
    Umbraco.AI.Prompt requires [1.0.0, 1.999.999)

    Recommendations:
    - Include Umbraco.AI.Prompt in this release and update its dependency
    - Or: Keep Umbraco.AI at 1.x for this release
    ```

### Phase 8: Update version.json Files

For each product with confirmed version:

1. **Read current version.json**:
    ```json
    {
        "version": "1.0.0",
        "suffixes": ["-beta", ""]
    }
    ```

2. **Update version field** using Edit tool:
    ```bash
    # Edit <Product>/version.json to update version
    ```

3. **Verify update** by reading the file back

### Phase 9: Generate Release Manifest

Invoke `/release-manifest-management` skill with product list:
- Build comma-separated list of all products being released
- Pass via `--products="Product1,Product2,..."` parameter
- Example: `--products="Umbraco.AI,Umbraco.AI.Agent,Umbraco.AI.OpenAI"`
- Skill will generate manifest automatically without prompting

### Phase 10: Generate Changelogs

For each product in the manifest:

1. **Invoke `/changelog-management`** skill:
    ```bash
    /changelog-management --product=<ProductName> --version=<Version>
    ```

2. **Verify changelog** was generated correctly

### Phase 11: Validation

Verify all files are consistent:

1. **Check version.json** matches intended versions
2. **Check CHANGELOG.md** exists and has correct version header
3. **Check release-manifest.json** includes all intended products
4. **Report any issues** to user

### Phase 12: Commit Changes

**All work has been done on the release branch.** Now commit everything:

1. **Stage all changes**:
    ```bash
    git add release-manifest.json
    git add Directory.Packages.props
    git add package.peer-dependencies.json
    git add */version.json
    git add */CHANGELOG.md
    ```

2. **Create commit**:
    ```bash
    git commit -m "chore(release): Prepare release 2026.02.1

    Updated products:
    - Umbraco.AI: 1.0.0 → 1.1.0
    - Umbraco.AI.OpenAI: 1.0.0 → 1.0.1
    - Umbraco.AI.Prompt: 1.0.0 → 2.0.0

    Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
    ```

    **Note:** Use the release branch name (e.g., 2026.02.1) in the commit message, not product versions.

3. **Show summary**:
    ```
    ✓ Release branch created: release/2026.02.1
    ✓ Updated 3 products:
      - Umbraco.AI: 1.0.0 → 1.1.0
      - Umbraco.AI.OpenAI: 1.0.0 → 1.0.1
      - Umbraco.AI.Prompt: 1.0.0 → 2.0.0
    ✓ Generated changelogs
    ✓ All changes committed

    Next steps:
    - Review the changes: git show HEAD
    - Push to remote: git push -u origin release/2026.02.1
    - Create PR to merge into main
    - CI will validate and build packages
    ```

## Important Notes

- Always run from repository root
- **Branch naming**: Use calendar-based naming `release/YYYY.MM.N` (recommended per CONTRIBUTING.md)
  - Independent from product versions (multiple products = different versions in one release)
  - N is an incrementing number for each release in that month (1, 2, 3, etc.)
  - Example: `release/2026.02.1` can contain Core@1.1.0, OpenAI@2.0.0, Prompt@1.0.5
  - Version-based naming like `release/v1.1.0` is valid but not recommended
- Use conventional commit analysis for version recommendations
- Validate cross-product dependencies
- This skill orchestrates `/release-manifest-management` and `/changelog-management`
- Creates commits following conventional commit format
- Release branches trigger CI validation and packaging

## Version Bump Decision Logic

```
Priority (highest first):
1. BREAKING CHANGE in commit body → Major
2. ! after scope (e.g., feat!:) → Major
3. feat: or feat(<scope>): → Minor
4. fix: or perf: → Patch
5. Only docs/chore/refactor → Ask user (default: patch)
```

## Cross-Product Dependency Check

Read `Directory.Packages.props` files to detect version ranges:

```xml
<PackageVersion Include="Umbraco.AI.Core" Version="[1.0.0, 1.999.999)" />
```

If bumping Core to 2.0.0, warn about all products with `[1.x, 1.999.999)` ranges.

## Example Flow

```
User invokes: /release-management

Phase 1: Detect changes
You scan git history and show:
- Umbraco.AI: 12 commits since 1.0.0
- Umbraco.AI.OpenAI: 3 commits since 1.0.0
- Umbraco.AI.Prompt: 5 commits since 1.0.0

Phase 2: Analyze versions
You show recommendations:
- Umbraco.AI: 1.0.0 → 1.1.0 (minor - 3 feat commits)
- Umbraco.AI.OpenAI: 1.0.0 → 1.0.1 (patch - 1 fix)
- Umbraco.AI.Prompt: 1.0.0 → 2.0.0 (major - BREAKING CHANGE)

Phase 3: Confirm versions
You ask: Use these versions?
Options:
- Use recommended versions (above)
- Downplay breaking changes to minor (2.0.0 → 1.1.0)
- Adjust individual versions
- Cancel
User confirms with chosen option

Phase 4: Update Directory.Packages.props
You read Directory.Packages.props
You identify products with BREAKING CHANGES (Core only)
You present proposed dependency range updates for Core packages only
User approves updates
You update only the Core-related ranges (Agent, Prompt ranges remain unchanged)

Phase 4.5: Update package.peer-dependencies.json
You read package.peer-dependencies.json
You identify same products with BREAKING CHANGES
You map .NET minimum versions to npm peer dependency ranges
You present proposed npm range updates for Core packages only
User approves updates
You update npm peer dependency ranges to match .NET minimum versions

Phase 6: Create release branch
You fetch tags from remote
You check for existing YYYY.MM.* tags (e.g., 2026.02.*)
You find: 2026.02.1 (latest release tag for February 2026)
You suggest: release/2026.02.2 (next release in February 2026)
You create the branch and switch to it
All subsequent work happens on this branch

Phase 7: Check dependencies
You check Directory.Packages.props
You warn: Prompt requires Core 1.x, but Core is going to 1.1.0 - compatible!

Phase 8: Update version.json
You edit all three version.json files on the release branch

Phase 9: Generate manifest
You invoke /release-manifest-management --products="Umbraco.AI,Umbraco.AI.OpenAI,Umbraco.AI.Prompt"
Manifest created with 3 products (no user prompt needed)

Phase 10: Generate changelogs
You invoke /changelog-management for each product
All changelogs generated

Phase 11: Validate
You verify all files are correct

Phase 12: Commit changes
You commit all changes to the release branch
You show summary and next steps
```

## Error Handling

- **No changes detected**: Ask user if they want to proceed anyway (manual version bump)
- **Git tag not found**: Fall back to comparing with main branch
- **Invalid version.json**: Report error and ask user to fix manually
- **Changelog generation fails**: Report error but continue with other products
- **Dependency conflict**: Warn user but allow them to proceed
