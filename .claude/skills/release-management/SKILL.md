---
name: release-management
description: Orchestrates the complete release preparation process - detects changed products, analyzes commits for version bumps, updates version.json files, generates manifests and changelogs, and creates release branches. Use when preparing a new release.
---

# Release Manager

You are the orchestrator for preparing releases in the Umbraco.AI repository.

## Task

Guide users through the complete release preparation process:

1. **Detect changed products** since their last release tags
2. **Analyze commits** to recommend version bumps (major/minor/patch)
3. **Cascade forced bumps** to every dependent of a product undergoing minor/major/downplayed-breaking changes
4. **Confirm versions** with the user (including cascaded products)
5. **Update Directory.Packages.props** inter-product dependency ranges (with user approval)
6. **Update peerDependencyVersions in root package.json** npm peer dependency ranges (with user approval)
7. **Create release branch** (e.g., `v17/release/2026.02.1`) and switch to it
8. **Dependency validation** - Check for cross-product conflicts using the graph built in cascade analysis
9. **Update version.json** files for each product
10. **Generate release-manifest.json** via `/release-manifest-management`
11. **Generate CHANGELOG.md** files via `/changelog-management`
12. **Changelog review** - Review generated changelogs for quality and completeness
13. **Validate** all files are consistent
14. **Commit all changes** to the release branch

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

### Phase 2.5: Dependency Cascade Analysis

After computing direct version bumps in Phase 2, walk the inter-product dependency graph and **force-add** every product that depends on a product undergoing a `feat:` (minor), downplayed-breaking, or major bump. This guarantees the new lower-bound dep range in `Directory.Packages.props` propagates to all consumers in the same release, closing the gap where an old dependent already on NuGet has a stale range that can resolve to the new upstream.

#### Why this matters

NuGet does not surface "feature added in upstream minor" semantics. If `Umbraco.AI` 1.10.x → 1.11.0 adds a new API and `Umbraco.AI.Agent` starts using it, an Agent build that still declares `Umbraco.AI.Core [1.10.0, 1.999.999)` can resolve to Core 1.10.x and crash with `MissingMethodException`. Cascading a forced patch bump on every dependent ensures their new `.nuspec` minimum points at the new upstream, so any user upgrading through the dependent automatically gets the matching upstream.

The downplayed-breaking option in Phase 3 only remains safe because of this cascade — without it, "downplayed breaking" silently breaks consumers stuck on old dependent versions.

#### Step 1 — Build the package → product map (dynamically)

Two-pass map: filesystem first (authoritative), longest-prefix as fallback for anything the filesystem pass missed.

```bash
# Discover products: all Umbraco.AI* folders at repo root.
products=$(find . -maxdepth 1 -type d -name 'Umbraco.AI*' | sed 's|^\./||')

# Inter-product packages: names from Directory.Packages.props
# (the "Inter-product dependencies" ItemGroup).
interProductPkgs=$(grep -Po 'PackageVersion Include="\K(Umbraco\.AI[^"]*)' Directory.Packages.props)
```

**Pass 1 — filesystem ground truth** (each product folder owns the csprojs inside it):

```pseudo
packageToProduct = {}
for product in products:
    for csproj in glob(f"{product}/src/**/*.csproj"):
        pkgName = csproj.<PackageId> if set else basename(csproj).removesuffix(".csproj")
        packageToProduct[pkgName] = product
```

**Pass 2 — longest-prefix fallback** for anything in the inter-product list that pass 1 didn't see:

```pseudo
products_sorted = sorted(products, key=len, reverse=True)

mapByPrefix(pkg):
    for p in products_sorted:                   # longest first wins
        if pkg == p or pkg.startswith(p + "."):
            return p
    return None

for pkg in interProductPkgs:
    if pkg not in packageToProduct:
        owner = mapByPrefix(pkg)
        if owner is None:
            warn(f"Cannot resolve {pkg} to a product")
        else:
            packageToProduct[pkg] = owner
```

**Drift sanity check** — log where pass 1 and pass 2 disagree (pass 1 wins, but the divergence is informative; e.g. `Umbraco.AI.AGUI` lives under `Umbraco.AI.Agent/` but its package name only carries the `Umbraco.AI` prefix, so pass 2 alone would mis-route it):

```pseudo
for pkg, product in packageToProduct.items():
    prefixGuess = mapByPrefix(pkg)
    if prefixGuess and prefixGuess != product:
        info(f"{pkg} owned by {product} (filesystem); prefix would say {prefixGuess}")
```

#### Step 2 — Build the dependency graph

```pseudo
deps = {}                          # product -> set(products it depends on)
for product in products:
    deps[product] = set()
    for csproj in glob(f"{product}/src/**/*.csproj"):
        for ref in csproj.<PackageReference>:
            owner = packageToProduct.get(ref.Include)
            if owner and owner != product:
                deps[product].add(owner)

dependents = invert(deps)          # upstream -> set(downstream products)
```

#### Step 3 — Cascade forced patch bumps

```pseudo
TRIGGERS = {minor, major, downplayedBreaking}   # patch/none/fix do NOT cascade

queue = [p for p in bumpSet if bumpSet[p].bumpKind in TRIGGERS]

while queue not empty:
    upstream = queue.pop()
    for downstream in dependents.get(upstream, []):
        if downstream in bumpSet:
            continue          # already bumping; its own range update covers it
        bumpSet[downstream] = {
            currentVersion: read_version_json(downstream),
            newVersion:     patchBump(currentVersion),
            bumpKind:       "patch",
            forced:         True,
            forcedBy:       upstream,
        }
        # A forced patch does not change the downstream's API surface,
        # so it does not itself trigger further cascade.
```

`patchBump` mirrors `/post-release-cleanup` Phase 4 — reuse the same helper:
- `1.10.1` → `1.10.2`
- `1.0.0-beta.1` → `1.0.0-beta.2` (dotted — the required form for new prerelease lines)
- `1.0.0-alpha` → `1.0.0-alpha.1`
- `1.0.0-beta1` → `1.0.0-beta2` (legacy non-dotted, Search/Automate only — increment in place, never dotify)

New prerelease lines must use dotted `-{stage}.N`; non-dotted `-betaN` sorts incorrectly past 9 (`beta10 < beta9`). See root CLAUDE.md → "Prerelease versioning".

#### Step 4 — Edge cases

1. **Forced product has no commits since its last tag.** Phase 10.5's "drop empty changelog" rule would otherwise remove it. Add an exception: forced bumps generate a stub entry like `### Internal\n- Bump to align with <upstream> <newVersion>` and stay in the release.

2. **User excludes a forced product from the manifest.** Phase 10.5 lets the user move products into `exclude`. Excluding a *forced* product breaks the cascade guarantee — old dependents on NuGet remain vulnerable to the stale-range trap. Warn loudly and require explicit confirmation.

3. **First-time products** (no prior release tag). Nothing on NuGet to be orphaned, so they can be skipped as upstreams in the cascade. As downstreams: standard handling.

#### Step 5 — Compute the manifest-affected set (separate from bumpSet)

CI's `detect-changes.ps1` flags any product whose csproj references a package whose range moved in `Directory.Packages.props` — including patch bumps. Cascade (Step 3) correctly does **not** add such products to `bumpSet` for patch upstreams (forward-compatible, no new release needed), but CI still requires those products to appear in either `include` or `exclude` of `release-manifest.json`. Without this acknowledgement, CI fails with `release-manifest.json is missing changed products: <Product>`.

Compute the broader "affected" set so Phase 9 can put it straight into `exclude`:

```pseudo
manifestAffected = set()                  # products that need acknowledgement only
for upstream in bumpSet:                  # every bump moves at least one range
    for downstream in dependents.get(upstream, []):
        if downstream not in bumpSet:
            manifestAffected.add(downstream)
```

This is the *broader* counterpart to Step 3's cascade. Step 3 produces `bumpSet ⊇ direct + forced` (everything that needs a new version). Step 5 produces `manifestAffected = (consumers of bumpSet via dep graph) − bumpSet` (everything that needs acknowledgement only).

Hand both sets to Phase 9.

**Example from release 2026.05.1:** all four bumps were patch (`Umbraco.AI`, `Agent.UI`, `Prompt`, `Search`) → `bumpSet` size 4, no cascade. But `Umbraco.AI` patch-bumped `Core/Web/Startup` ranges, which `Umbraco.AI.Agent`'s csproj references. Agent had zero file changes since its tag, so Phase 1 didn't flag it; cascade didn't add it because patches don't trigger; but CI flagged it as affected and the manifest blew up at validation time. Step 5 catches this by walking `dependents[Umbraco.AI] = {Agent, Amazon, Anthropic, ...}` and queuing them all for `exclude`.

### Phase 3: Version Confirmation

Present the **final** version table including any forced cascade bumps from Phase 2.5:

```
Final version set (including cascaded forced patches):

┌─────────────────────────┬──────────┬──────────┬──────────────────────────────────┐
│ Product                 │ Current  │ Proposed │ Reason                           │
├─────────────────────────┼──────────┼──────────┼──────────────────────────────────┤
│ Umbraco.AI              │ 1.10.1   │ 1.11.0   │ 3 feat commits                   │
│ Umbraco.AI.Agent        │ 1.9.1    │ 1.10.0   │ 2 feat commits                   │
│ Umbraco.AI.Prompt       │ 1.8.1    │ 1.8.2    │ FORCED — depends on Umbraco.AI   │
│ Umbraco.AI.OpenAI       │ 1.0.5    │ 1.0.6    │ FORCED — depends on Umbraco.AI   │
└─────────────────────────┴──────────┴──────────┴──────────────────────────────────┘
```

Then use **AskUserQuestion** to confirm or adjust versions:

- **Default option**: "Use recommended versions (above)"
- **Alternative options**:
    - "Downplay breaking changes to minor" - Treat breaking changes as minor bumps (X.Y.0 → X.Y+1.0 instead of X+1.0.0). Cascade still applies (in fact, this option is only safe *because* cascade applies).
    - "Drop forced products" - Remove specific cascaded patches. Per Phase 2.5 Step 4, this requires explicit confirmation per product and breaks the cascade guarantee for those products.
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

After confirming versions, update `Directory.Packages.props` so the dependency range for **every bumped product** reflects the new minimum (and, for major bumps, the new upper bound).

**Cascade prerequisite:** Phase 2.5 has already added forced patch bumps for every dependent of any minor/major/downplayed-breaking change. With cascade in place, every consumer that needs the new minimum is being released alongside, so raising the lower bound is safe to apply for *all* bumps — not just breaking ones. The earlier "only update for breaking changes" rule has been removed because it created a stale-range runtime trap whenever an upstream minor added an API the dependent then used.

**Rule (apply per bumped product P):**

| `bumpKind` | New range entry |
|---|---|
| `major` | `[X+1.0.0, X+1.999.999)` — both bounds change |
| `minor`, `downplayedBreaking`, `patch` (including forced) | `[newVersion, X.999.999)` — lower bound only |
| `none` | leave entry unchanged |

**Workflow:**

1. **Read current Directory.Packages.props** (the inter-product ItemGroup).

2. **Compute proposed updates** by applying the rule above to every entry in `bumpSet`.

3. **Present proposed changes** to user:
   ```
   Directory.Packages.props updates:

   - Umbraco.AI.Core:           [1.10.0, 1.999.999) → [1.11.0, 1.999.999)   (minor)
   - Umbraco.AI.Agent.Core:     [1.9.0,  1.999.999) → [1.10.0, 1.999.999)   (minor)
   - Umbraco.AI.Prompt.Startup: [1.8.1,  1.999.999) → [1.8.2,  1.999.999)   (forced patch — depends on Umbraco.AI)
   ```

4. **Ask for approval** using AskUserQuestion:
   - **Default option**: "Apply these range updates (recommended)"
   - **Alternative options**:
     - "Skip range updates" — proceed without updating; warn that this will require manual fix-up before release
     - "Adjust manually later"

5. **If approved, update the file** with the Edit tool.

6. **Confirm updates**:
   ```
   ✓ Updated N inter-product dependency ranges in Directory.Packages.props
   ```

**Important Notes:**
- The rule deliberately does *not* exempt products without breaking changes. Phase 2.5's cascade ensures consumers are bumped alongside, so raising the lower bound is the safe default.
- Major-bump updates only affect the entries for products being released. Products excluded from the release manifest retain their old `.nuspec` ranges from prior releases — that is correct SemVer behavior (consumers can't pull an incompatible new major while still pinned to an old provider).
- Lower-bound bumps for `downplayedBreaking` are what make the downplay option safe: every dependent's new version requires the new upstream, so users upgrading through dependents always end up on a compatible pair.

### Phase 4.5: Update Inter-Product Peer Dependencies (npm)

After updating `Directory.Packages.props`, update `peerDependencyVersions` in root `package.json` to keep npm peer dependencies in sync with .NET dependency ranges.

This phase mirrors Phase 4: every product in `bumpSet` (direct or forced) gets its npm peer range updated. The set is identical to Phase 4's, so by construction the two stay in sync — closing the previous asymmetry where npm bumped on every release but .NET only bumped on breaking changes.

**Important:** This phase uses the same version decisions from Phase 3. No need to re-analyze commits.

**Workflow:**

1. **Read current peerDependencyVersions from root package.json**:
   ```bash
   cat package.json
   ```

2. **Use confirmed versions from Phase 3**:
   - Take the list of products being released with their new versions
   - Same information used in Phase 4 for `Directory.Packages.props`

3. **Determine which npm ranges need updating**:
   - For each product in `bumpSet` (direct + forced), check if it has an npm package (has `Client/package.json` with `types` field)
   - Map the new version to npm semver range:
     - New version 2.0.0 → `^2.0.0`
     - New version 1.3.0 → `^1.3.0`
     - New version 1.2.1 → `^1.2.1`
   - Update every product being released — same set as Phase 4 — to keep the two sources of truth aligned

4. **Present proposed changes** to user:
   ```
   peerDependencyVersions updates (in root package.json):

   Products being released:
   - Umbraco.AI → 1.3.0
   - Umbraco.AI.Agent → 1.2.0
   - Umbraco.AI.Prompt → 2.0.0

   The following npm peer dependency ranges will be updated:
   - @umbraco-ai/core: ^1.2.0 → ^1.3.0
   - @umbraco-ai/agent: ^1.1.0 → ^1.2.0
   - @umbraco-ai/prompt: ^1.0.0 → ^2.0.0
   ```

5. **Validate consistency** with Directory.Packages.props:
   - Verify that npm ranges match .NET minimum versions from Phase 4
   - Warn if any mismatches detected (shouldn't happen if Phase 4 was done correctly)

6. **Ask for approval** using AskUserQuestion:
   - **Default option**: "Update npm peer dependency ranges (recommended)"
   - **Alternative options**:
     - "Skip npm updates" - Continue without updating ranges
     - "Adjust manually later" - Skip now, remind user to update manually

7. **If approved, update the file**:
   ```bash
   # Use Edit tool to update peerDependencyVersions in root package.json
   ```

8. **Confirm updates**:
   ```
   ✓ Updated npm peer dependency ranges in root package.json (peerDependencyVersions)
   ```

**Mapping versions to npm ranges:**

| New Version | npm Range | Meaning                                    |
|-------------|-----------|-------------------------------------------|
| 1.2.0       | `^1.2.0`  | ≥1.2.0 and <2.0.0                         |
| 1.3.0       | `^1.3.0`  | ≥1.3.0 and <2.0.0                         |
| 2.0.0       | `^2.0.0`  | ≥2.0.0 and <3.0.0                         |
| 17.1.0      | `^17.1.0` | ≥17.1.0 and <18.0.0                       |

**Important Notes:**
- Uses the same version decisions from Phase 3 (no re-analysis needed)
- Updates ALL products being released (ensures consistency)
- npm ranges should mirror the new minimum versions
- External dependencies like `@umbraco-cms/backoffice` are also managed here
- These changes will be staged and committed with other release files

### Phase 6: Create Release Branch

**IMPORTANT:** Create the release branch BEFORE making any file changes.

**Branch Naming Convention:**

The **recommended** convention is calendar-based with a version prefix:
- `vN/release/YYYY.MM.N` — version prefix, year, month, and incrementing release number
- Example: `v17/release/2026.02.1` for the first February 2026 release on v17
- Example: `v18/release/2026.02.1` for the first February 2026 release on v18

**Determine the version prefix** from the current branch before creating the release branch:
```bash
git branch --show-current  # e.g. v17/dev → prefix = v17
```

This is independent from product version numbers (which follow semantic versioning). A single release branch like `v17/release/2026.02.1` can contain multiple products at different versions (e.g., Core@17.1.0, OpenAI@17.0.1, Prompt@17.2.0).

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

   Version prefix: v17 (from current branch v17/dev)
   Latest release tag for February 2026: 2026.02.2
   Suggested branch name: v17/release/2026.02.3 (next release in February 2026)

   Options:
   - Use suggested name (v17/release/2026.02.3)
   - Enter custom name
   - Cancel
   ```

   **If no tags exist for current month:**
   ```
   Create release branch using recommended calendar naming?

   Version prefix: v17 (from current branch v17/dev)
   No previous releases found for February 2026.
   Suggested branch name: v17/release/2026.02.1 (first release in February 2026)

   Options:
   - Use suggested name (v17/release/2026.02.1)
   - Enter custom name
   - Cancel
   ```

3. **Create and checkout branch**:
    ```bash
    git checkout -b ${prefix}/release/<name>
    ```

4. **Confirm branch creation**:
    ```
    ✓ Created and switched to branch: v17/release/2026.02.1

    All subsequent changes will be made on this branch.

    Note: This release will contain:
    - Umbraco.AI 1.1.0
    - Umbraco.AI.OpenAI 1.0.1
    - Umbraco.AI.Prompt 2.0.0
    ```

### Phase 7: Dependency Validation

Reuse the dependency graph (`deps`, `dependents`, `packageToProduct`) built in Phase 2.5 to validate that the post-Phase-4 state is internally consistent. The validation covers **all** inter-product packages — not just `Umbraco.AI.Core`.

```pseudo
# 1. Each bumped product's newVersion satisfies its own (just-updated) range entry.
for product in bumpSet:
    range = parsed_range(Directory.Packages.props[product])
    if not range.contains(bumpSet[product].newVersion):
        error(f"{product} new version {bumpSet[product].newVersion} "
              f"is outside its declared range {range}")

# 2. For every minor/major/downplayed-breaking upstream, every consumer must be in
#    bumpSet (cascade should have ensured this — re-verify before committing).
for upstream in bumpSet:
    if bumpSet[upstream].bumpKind not in {major, minor, downplayedBreaking}:
        continue
    for downstream in dependents.get(upstream, []):
        if downstream not in bumpSet:
            error(f"Cascade gap: {downstream} depends on {upstream} "
                  f"({bumpSet[upstream].bumpKind} bump) but is not in bumpSet. "
                  f"Old {downstream} on NuGet may resolve to an incompatible "
                  f"{upstream}.")

# 3. Major-bump conflicts: any out-of-release product whose csproj references an
#    upstream undergoing a new major must be addressed explicitly.
for upstream in bumpSet:
    if bumpSet[upstream].bumpKind != major:
        continue
    for downstream in all_products:
        if downstream in bumpSet:
            continue
        for ref in csproj_refs(downstream):
            if packageToProduct.get(ref) == upstream:
                error(f"{downstream} (not in release) depends on {upstream}, "
                      f"which is going to a new major. Either include "
                      f"{downstream} with a compatible bump or hold {upstream} "
                      f"at the current major for this release.")
```

Present results:

```
✅ All bumped products satisfy their declared ranges
✅ Cascade closed: every consumer of a minor/major/downplayed upstream is in the release
🔴 Umbraco.AI.OpenAI (not in release) depends on Umbraco.AI which is going to 2.0.0
   → must be addressed before continuing
```

If hard errors exist, ask the user how to proceed — common resolutions:

- Include the missing product (re-run Phase 2.5 cascade with it added)
- Hold the upstream at its current major (revert that bump in Phase 3)
- Abort and fix manually

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

Build the manifest from three sources so CI's `detect-changes.ps1` accepts it:

```pseudo
include = sorted(bumpSet keys)

exclude = sorted(union of:
    - Phase 1 changed products you chose NOT to release  (file changes, dropped)
    - Phase 2.5 Step 5 manifestAffected                  (range-delta consumers)
    - First-time products with commits since vN/main not in include
)
```

Every product CI considers "changed" by either mechanism (file diff or `Directory.Packages.props` range delta) **must** appear in one of the two lists.

If `/release-manifest-management` is invoked without an explicit `--exclude=`, write `release-manifest.json` directly using the Write tool with the object format:

```json
{
    "include": [...],
    "exclude": [...]
}
```

Or invoke the skill with the include list and patch the exclude list yourself afterwards.

**Sanity check before moving on:**

```pseudo
for product in all_products:
    isChanged = (product in Phase1Changed) or (product in manifestAffected) or (product in bumpSet)
    if isChanged and product not in include and product not in exclude:
        error(f"{product} will fail CI validation — add to include or exclude")
```

Run this check before Phase 13 commits. CI's failure message looks like:
> `release-manifest.json is missing changed products (add to 'include' or 'exclude'): <Product>`

### Phase 10: Generate Changelogs

For each product in the manifest:

1. **Invoke `/changelog-management`** skill:
    ```bash
    /changelog-management --product=<ProductName> --version=<Version>
    ```

2. **Verify changelog** was generated correctly

### Phase 10.5: Changelog Review

After generating changelogs, review each product's new version entry for quality and completeness.

#### Step 1: Noise Detection

For each product's changelog entry, check for and flag:

1. **Internal-only entries that shouldn't be public:**
   - `refactor` sections — these are hidden from changelogs per convention (types hidden: refactor, chore, docs, test, ci, build). If the generator included them, they should be removed
   - Test-only fixes (e.g., "Fix failing tests") — not user-facing
   - Build/CI changes (e.g., "Exclude dev files from NuGet package")
   - Generated code changes (e.g., "Fix casing in generated OpenAPI client")

2. **Leaked metadata:**
   - `Co-Authored-By` lines in commit bodies that leaked into changelog entries
   - PR merge commit noise
   - Internal tool references (e.g., "after /simplify review")

3. **Duplicate cross-product entries:**
   - Same commit appearing in multiple product changelogs — verify it's actually relevant to each product

#### Step 2: Completeness Check

For each product being released, verify the changelog captured all meaningful changes:

1. **Compare changelog entries against actual file changes:**
   ```bash
   # Get files changed for the product
   git diff <tag>..HEAD --name-only -- <ProductFolder>/ | grep -v 'CHANGELOG.md\|version.json'

   # Get commits that touched those files
   git log <tag>..HEAD --pretty=format:"%h %s" -- <ProductFolder>/
   ```

2. **Look for missing entries** — commits scoped to other products (e.g., `feat(agent)`) that touched this product's files won't be captured by the changelog generator since it filters by scope. Common cases:
   - Cross-product features where the commit scope doesn't match all affected products
   - Batch commits that span multiple product directories

3. **For each missing change, determine if it's changelog-worthy:**
   - Does it change user-facing behavior? → Add a manually written entry
   - Is it only adapting to a dependency change? → Skip (internal plumbing)
   - Is it a meaningful new feature/fix that users should know about? → Add entry

#### Step 3: Empty Changelog Assessment

For products with **empty changelog entries** (version header but no content):

1. **Review what actually changed** in the product directory:
   ```bash
   git diff <tag>..HEAD --stat -- <ProductFolder>/ | grep -v 'CHANGELOG.md\|version.json'
   ```

2. **Categorize the changes:**
   - **Build/tooling only** (lock files, csproj conditions, slnx migrations, CLAUDE.md) → Recommend dropping from release
   - **Dependency adaptation** (adapting to API changes in a dependency, no new features) → Recommend dropping OR add a brief entry
   - **Real features/fixes** that were missed by the generator → Add entries manually

3. **If recommending to drop a product**, update the release manifest and revert files:
   - Move it from `include` to `exclude` in `release-manifest.json`
   - **IMPORTANT:** Every changed product that is NOT being released MUST appear in the `exclude` list. CI's change detection script checks all products with changes since their last tag — any changed product missing from both `include` and `exclude` will fail the build.
   - Revert its `version.json` to the pre-release value
   - Revert its `CHANGELOG.md` changes
   - Revert peer dependency version changes if applicable

#### Step 4: Present Review

Present findings to the user organized by severity:

```
Changelog Review Results:

🔴 Issues requiring attention:
- [Product]: Empty changelog — only build changes detected, recommend dropping
- [Product]: Missing entry for [feature] — cross-product commit scoped elsewhere

🟡 Noise to clean up:
- [Product]: Refactor section should be removed (14 entries)
- [Product]: "Fix failing tests" entry is internal-only
- [Product]: Co-Authored-By leaked into breaking change body

✅ Clean:
- [Product]: Changelog looks good
- [Product]: Changelog looks good
```

#### Step 5: Apply Fixes

After user confirms:

1. **Remove noise entries** — Edit changelogs to remove flagged items
2. **Add missing entries** — Write manually crafted entries for missed changes
3. **Drop products** — Update release manifest, revert versions
4. **Re-validate** — Ensure all changelogs still have content after cleanup

#### Important Notes

- This phase is about **editorial quality**, not format validation (that's Phase 12)
- The changelog generator works by matching commit scopes to products. Cross-product commits with a single scope will only appear in that scope's changelog. This is a known limitation — manual additions are expected.
- When adding manual entries, follow the same format as generated entries but use a scope matching the product being edited
- Refactor/chore/docs types should NEVER appear in public changelogs per project convention

### Phase 12: Validation

Verify all files are consistent:

1. **Check version.json** matches intended versions
2. **Check CHANGELOG.md** exists and has correct version header
3. **Check release-manifest.json** includes all intended products
4. **Report any issues** to user

### Phase 13: Commit Changes

**All work has been done on the release branch.** Now commit everything:

1. **Stage all changes**:
    ```bash
    git add release-manifest.json
    git add Directory.Packages.props
    git add package.json
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
    ✓ Release branch created: v17/release/2026.02.1
    ✓ Updated 3 products:
      - Umbraco.AI: 17.0.0 → 17.1.0
      - Umbraco.AI.OpenAI: 17.0.0 → 17.0.1
      - Umbraco.AI.Prompt: 17.0.0 → 17.2.0
    ✓ Generated changelogs
    ✓ All changes committed

    Next steps:
    - Review the changes: git show HEAD
    - Push to remote: git push -u origin v17/release/2026.02.1
    - CI will validate and build packages
    - After CI passes, run /post-release-cleanup
    ```

## Important Notes

- Always run from repository root
- **Branch naming**: Use `vN/release/YYYY.MM.N` — version prefix derived from current branch, calendar date, incrementing number
  - Independent from product versions (multiple products = different versions in one release)
  - N is an incrementing number for each release in that month (1, 2, 3, etc.)
  - Example: `v17/release/2026.02.1` can contain Core@17.1.0, OpenAI@17.0.1, Prompt@17.2.0
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

The dependency graph used for cascade (Phase 2.5) and validation (Phase 7) is built dynamically from:

1. The "Inter-product dependencies" ItemGroup in root `Directory.Packages.props`
2. Each `<Product>/src/**/*.csproj` file's `<PackageReference>` entries

The `packageToProduct` map is built filesystem-first (each product folder owns the csprojs inside it) with a longest-prefix fallback for any inter-product package not represented by a csproj in the current checkout. This handles edge cases like `Umbraco.AI.AGUI` whose package name doesn't carry the owning product's full prefix.

Range entries follow the form:

```xml
<PackageVersion Include="Umbraco.AI.Core" Version="[1.10.0, 1.999.999)" />
```

Range updates follow the simplified Phase 4 rule (lower bound on every bump; both bounds on majors), with cascade ensuring no consumer is left behind.

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

Phase 2.5: Cascade analysis
You build the package→product map (filesystem ground truth + longest-prefix fallback)
You build the dep graph from each csproj's PackageReferences
TRIGGERS = {minor, major, downplayedBreaking}
- Umbraco.AI is minor → cascade to its dependents
- Umbraco.AI.Prompt is major → cascade to its dependents
- Umbraco.AI.OpenAI is patch → no cascade
Forced patches added (Step 3):
- Umbraco.AI.Agent: 1.0.0 → 1.0.1 (depends on Umbraco.AI minor)
- Umbraco.AI.Prompt.Deploy: 1.0.0 → 1.0.1 (depends on Umbraco.AI.Prompt major)
manifestAffected computed (Step 5):
- (in this minor/major scenario, all dep consumers are already in bumpSet → empty)
- (in a patch-only release, this set captures every consumer of a bumped package
  that doesn't need a new version but does need an `exclude` entry)

Phase 3: Confirm versions
You show the FINAL set including forced cascade entries.
Options:
- Use recommended versions (above)
- Downplay breaking changes to minor (Prompt 2.0.0 → 1.1.0). Cascade still applies.
- Drop forced products (per-product, with warning)
- Adjust individual versions
- Cancel
User confirms with chosen option

Phase 4: Update Directory.Packages.props
You apply the simplified rule per bumped product:
- Major: both bounds change
- Minor / downplayedBreaking / patch (incl. forced): lower bound only
- none: leave alone
User approves updates. All bumped products' ranges are updated; cascade ensures
no consumer is left with a stale lower bound.

Phase 4.5: Update peerDependencyVersions in root package.json
You read peerDependencyVersions from root package.json
You use the same confirmed versions from Phase 3
You map new versions to npm peer dependency ranges
You present proposed npm range updates for ALL products being released
User approves updates
You update npm peer dependency ranges in root package.json (e.g., 1.3.0 → ^1.3.0)

Phase 6: Create release branch
You fetch tags from remote
You check for existing YYYY.MM.* tags (e.g., 2026.02.*)
You find: 2026.02.1 (latest release tag for February 2026)
You suggest: v17/release/2026.02.2 (next release in February 2026)
You create the branch and switch to it
All subsequent work happens on this branch

Phase 7: Check dependencies
You re-use the dep graph from Phase 2.5 to validate the post-Phase-4 state.
You verify: every consumer of a minor/major/downplayed upstream is in bumpSet.
You verify: every bumped newVersion satisfies its just-updated range entry.
No errors found.

Phase 8: Update version.json
You edit all three version.json files on the release branch

Phase 9: Generate manifest
You build include from bumpSet and exclude from
(Phase 1 dropped) ∪ manifestAffected ∪ (first-time non-bumped)
You sanity-check: every "changed" product is in one list or the other
Manifest written directly (Write tool) using the object format

Phase 10: Generate changelogs
You invoke /changelog-management for each product
All changelogs generated

Phase 10.5: Changelog review
You review each product's changelog for noise (refactor entries, leaked metadata)
You check completeness by comparing changelog entries against actual file changes
You flag: Umbraco.AI.OpenAI has a "Fix failing tests" entry (internal-only)
You flag: Umbraco.AI.Prompt is missing an entry for a cross-product feature
You present review results to user
User confirms fixes
You remove the internal-only entry and add the missing feature entry

Phase 12: Validate
You verify all files are correct

Phase 13: Commit changes
You commit all changes to the release branch
You show summary and next steps
```

## Error Handling

- **No changes detected**: Ask user if they want to proceed anyway (manual version bump)
- **Git tag not found**: Fall back to comparing with `vN/main` branch
- **Invalid version.json**: Report error and ask user to fix manually
- **Changelog generation fails**: Report error but continue with other products
- **Dependency conflict**: Warn user but allow them to proceed
