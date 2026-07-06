---
name: post-release-cleanup
description: Merges a release or hotfix branch back into vN/main and vN/dev, bumps version.json on vN/dev so nightly builds produce versions higher than the released version, and optionally deletes the release branch. If the release is a new major version, creates the new vN+1/dev and vN+1/main branches and updates the GitHub default branch. Use after a release has been deployed and tagged.
---

# Post-Release Cleanup

You are the orchestrator for post-release cleanup in the Umbraco.AI repository.

## Task

After a release has been deployed and tagged by the release pipeline, merge the release/hotfix branch back into the appropriate `vN/main` and `vN/dev`, bump `version.json` on `vN/dev` so nightly builds produce versions **higher** than the released version, and optionally clean up the branch. If this is the first release of a new major version, also create the new version's `dev`/`main` branches and update the GitHub default branch.

## Why This Matters

Without the version bump on `vN/dev`, NBGV + `Umbraco.GitVersioning.Extensions` produces packages like `1.5.0--preview.4.gabcdef0` which sorts **lower** than the stable `1.5.0` in SemVer — making nightlies useless for testing.

## Workflow

### Phase 1: Detect Release Context

1. **Check current branch** — verify it matches `vN/release/*` or `vN/hotfix/*`:
   ```bash
   git branch --show-current
   ```
   If not on a versioned release/hotfix branch, ask the user to specify which branch to process.

2. **Extract the version prefix** from the branch name. For example:
   - `v18/release/2026.06.5` → prefix = `v18`, major = `18`
   - `v17/hotfix/2026.06.1` → prefix = `v17`, major = `17`

3. **Fetch latest tags and remote state:**
   ```bash
   git fetch origin --tags
   ```

4. **Find product version tags on this branch** that are not yet on `vN/main`:
   ```bash
   merge_base=$(git merge-base origin/${prefix}/main HEAD)
   commits=$(git rev-list $merge_base..HEAD)
   for tag in $(git tag --list '*@*'); do
       tag_commit=$(git rev-parse "$tag^{commit}" 2>/dev/null)
       if echo "$commits" | grep -q "$tag_commit"; then
           echo "$tag"
       fi
   done
   ```

5. **Parse product names and versions** from tags (e.g., `Umbraco.AI@18.1.0` → product=`Umbraco.AI`, version=`18.1.0`).

6. **Detect the released major** from the tag versions (e.g. `18` from `18.1.0`).

7. **Present findings to user** for confirmation:
   ```
   Found released products on this branch (v18/release/2026.06.5):
   - Umbraco.AI @ 18.1.0
   - Umbraco.AI.OpenAI @ 18.1.0

   Proceed with merge and version bump? [Yes/Cancel]
   ```

   If NO tags are found, warn the user:
   ```
   ⚠ No product version tags found on this branch.
   This usually means the release pipeline hasn't run yet, or tags haven't been pushed.

   Options:
   - Wait for the release pipeline to complete and try again
   - Proceed anyway (merge only, skip version bump)
   - Cancel
   ```

### Phase 2: Merge to vN/main

1. **Confirm with user** before merging.

2. **Store the release branch name:**
   ```bash
   release_branch=$(git branch --show-current)   # e.g. v18/release/2026.06.5
   ```

3. **Checkout and merge:**
   ```bash
   git checkout ${prefix}/main
   git pull origin ${prefix}/main
   git merge origin/$release_branch --no-ff -m "Merge $release_branch into ${prefix}/main"
   ```

4. **Push:**
   ```bash
   git push origin ${prefix}/main
   ```

   The post-merge hook will auto-delete `release-manifest.json` if present and commit the cleanup.

### Phase 3: Merge vN/main to vN/dev

1. ```bash
   git checkout ${prefix}/dev
   git pull origin ${prefix}/dev
   git merge ${prefix}/main --no-ff -m "Merge ${prefix}/main into ${prefix}/dev"
   ```

2. **Handle merge conflicts** — if conflicts occur (likely in `version.json` or `CHANGELOG.md`):
   - For `version.json`: keep the **higher** version (overwritten in Phase 4 anyway)
   - For `CHANGELOG.md`: keep **both** sets of entries (combine)
   - For `release-manifest.json`: delete the file (must not exist on dev)
   - Ask the user for help with any other conflicts

3. **Push:**
   ```bash
   git push origin ${prefix}/dev
   ```

   The post-merge hook will auto-delete `release-manifest.json` if present and commit the cleanup.

### Phase 4: Bump Versions on vN/dev

For each released product detected in Phase 1:

1. **Read** the current `<Product>/version.json`

2. **Compute the patch bump:**
   - Stable version: `18.1.0` → `18.1.1`
   - Dotted pre-release: `18.0.0-beta.2` → `18.0.0-beta.3` (increment the numeric segment)
   - Pre-release without numeric segment: `18.0.0-alpha` → `18.0.0-alpha.1`
   - **Legacy non-dotted pre-release** (`18.0.0-beta2` → `18.0.0-beta3`): increment in place — do **not** convert to dotted. Only `Umbraco.AI.Search`/`Umbraco.AI.Automate` are on this grandfathered scheme.

3. **Update** the `"version"` field in `version.json` using the Edit tool.

4. **After all products are bumped**, commit and push:
   ```bash
   git add */version.json
   git commit -m "chore(release): Bump dev versions after release

   Products bumped:
   - Umbraco.AI: 18.1.0 → 18.1.1
   - Umbraco.AI.OpenAI: 18.1.0 → 18.1.1

   Co-Authored-By: Claude <noreply@anthropic.com>"

   git push origin ${prefix}/dev
   ```

### Phase 5: Major Version Cutover (conditional)

**Only run this phase if the released major is a NEW major** — i.e., the GitHub default branch points to a lower major version than what was just released.

#### Detect whether a cutover is needed

```bash
# Get the current GitHub default branch
gh api repos/umbraco/Umbraco.AI --jq '.default_branch'
# e.g. → "v18/dev"
```

Parse the major from the default branch (e.g. `v18/dev` → `18`). Compare with the released major:
- Released major **equal to** default branch major → no cutover needed, skip this phase.
- Released major **greater than** default branch major → cutover needed (e.g. default is `v18/dev`, just released `v19.0.0`).

#### Cutover steps

Let `new_prefix` = `v{released_major}` (e.g. `v19`).

1. **Check whether `vN+1/dev` and `vN+1/main` already exist:**
   ```bash
   git ls-remote origin refs/heads/${new_prefix}/dev refs/heads/${new_prefix}/main
   ```

2. **Create missing branches** if they do not exist yet:
   - `${new_prefix}/main` — create from the tip of `${prefix}/main` (the freshly merged stable state):
     ```bash
     git push origin ${prefix}/main:refs/heads/${new_prefix}/main
     ```
   - `${new_prefix}/dev` — create from `${prefix}/dev` (after version bumps):
     ```bash
     git push origin ${prefix}/dev:refs/heads/${new_prefix}/dev
     ```
   If both already exist (dev team created them ahead of the release), skip creation.

3. **Update the GitHub default branch:**
   ```bash
   gh api repos/umbraco/Umbraco.AI -X PATCH -f default_branch=${new_prefix}/dev \
     --jq '.default_branch'
   ```
   Confirm the returned value matches `${new_prefix}/dev`.

4. **Inform the user** — they will need to update their local checkout:
   ```
   ✅ Major version cutover complete!

   New branches created:
   - ${new_prefix}/main (from ${prefix}/main)
   - ${new_prefix}/dev  (from ${prefix}/dev)

   GitHub default branch updated: ${prefix}/dev → ${new_prefix}/dev

   Developers should run:
     git fetch origin
     git checkout ${new_prefix}/dev
   ```

### Phase 6: Cleanup (Optional)

1. **Ask the user** if they want to delete the release/hotfix branch (local + remote):
   ```
   Delete the release branch '$release_branch'?
   - Local and remote
   - Local only
   - Skip (keep branch)
   ```

2. If deleting:
   ```bash
   git branch -d $release_branch
   git push origin --delete $release_branch
   ```

3. **Return to the appropriate dev branch** (the new one if a cutover occurred, otherwise `${prefix}/dev`):
   ```bash
   git checkout ${active_dev}
   ```

### Phase 7: Summary

Present a summary of everything that was done:

```
✅ Post-release cleanup complete!

Merged:
- $release_branch → ${prefix}/main
- ${prefix}/main → ${prefix}/dev

Version bumps on ${prefix}/dev:
- Umbraco.AI: 18.1.0 → 18.1.1
- Umbraco.AI.OpenAI: 18.1.0 → 18.1.1

[If major cutover:]
Major version cutover:
- Created v19/main from v18/main
- Created v19/dev  from v18/dev
- GitHub default branch: v18/dev → v19/dev

Branch cleanup: [deleted/kept]

Nightly builds on ${prefix}/dev will now produce versions higher than the released versions.
```

## Version Bump Logic

### Stable Versions

Simply increment the patch version:
- `18.1.0` → `18.1.1`
- `18.0.0` → `18.0.1`

### Pre-release Versions

New prerelease lines use the **dotted** form `-{stage}.N` (`-alpha.1`, `-beta.1`, `-rc.1`) — never non-dotted `-beta1`, which sorts incorrectly past 9 (`beta10 < beta9`). See root CLAUDE.md → "Prerelease versioning".

Increment the numeric portion of the pre-release identifier:
- `18.0.0-beta.2` → `18.0.0-beta.3`
- `18.0.0-rc.1` → `18.0.0-rc.2`
- `18.0.0-alpha` → `18.0.0-alpha.1` (append `.1` if no numeric segment)
- `18.0.0-beta2` → `18.0.0-beta3` (legacy non-dotted — increment in place, never dotify)

## Important Notes

- **Always fetch tags first** — the release pipeline creates tags asynchronously after deploy
- **Use `--no-ff` merges** — preserves the merge commit for clear history
- **Post-merge hooks handle `release-manifest.json` cleanup** — don't manually delete it
- **version.json only has a `"version"` field** — update only that field, preserve all other properties
- **Both `vN/release/*` and `vN/hotfix/*` branches are supported** — the workflow is identical
- **If no tags are found**, the user can still proceed with merge-only (skip Phase 4)
- **Major cutover only triggers when the released major > the default branch major** — a patch or minor release on the current latest version never triggers it

## Error Recovery

- If the merge to `vN/main` fails (conflicts), help the user resolve conflicts before continuing
- If the push fails, check if the branch is protected and advise accordingly
- If version.json has unexpected format, show the user and ask how to proceed
- Never force-push — if push is rejected, pull and retry the merge
- If the GitHub default branch update fails, confirm admin permissions on the repo (`gh api repos/umbraco/Umbraco.AI --jq '.permissions'`)
