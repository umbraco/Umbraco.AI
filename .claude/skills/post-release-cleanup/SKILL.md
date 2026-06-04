---
name: post-release-cleanup
description: Merges a release or hotfix branch back into main and dev, bumps version.json on dev so nightly builds produce versions higher than the released version, and optionally deletes the release branch. Use after a release has been deployed and tagged.
---

# Post-Release Cleanup

You are the orchestrator for post-release cleanup in the Umbraco.AI repository.

## Task

After a release has been deployed and tagged by the release pipeline, merge the release/hotfix branch back into `main` and `dev`, bump `version.json` on `dev` so nightly builds produce versions **higher** than the released version, and optionally clean up the branch.

## Why This Matters

Without the version bump on `dev`, NBGV + `Umbraco.GitVersioning.Extensions` produces packages like `1.5.0--preview.4.gabcdef0` which sorts **lower** than the stable `1.5.0` in SemVer — making nightlies useless for testing.

## Workflow

### Phase 1: Detect Release Context

1. **Check current branch** — verify it is `release/*` or `hotfix/*`:
   ```bash
   git branch --show-current
   ```
   If not on a release/hotfix branch, ask the user to specify which branch to process.

2. **Fetch latest tags and remote state:**
   ```bash
   git fetch origin --tags
   ```

3. **Find product version tags on this branch** that are not yet on `main`:
   ```bash
   # Get the merge-base between the release branch and main
   merge_base=$(git merge-base origin/main HEAD)

   # Get all commits on the release branch since the merge-base
   commits=$(git rev-list $merge_base..HEAD)

   # For each product tag matching *@*, check if it points at one of these commits
   for tag in $(git tag --list '*@*'); do
       tag_commit=$(git rev-parse "$tag^{commit}" 2>/dev/null)
       if echo "$commits" | grep -q "$tag_commit"; then
           echo "$tag"
       fi
   done
   ```

4. **Parse product names and versions** from tags (e.g., `Umbraco.AI@1.5.0` → product=`Umbraco.AI`, version=`1.5.0`).

5. **Present findings to user** for confirmation:
   ```
   Found released products on this branch:
   - Umbraco.AI @ 1.5.0
   - Umbraco.AI.OpenAI @ 1.2.0

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

### Phase 2: Merge to Main

1. **Confirm with user** before merging.

2. **Store the release branch name** for later:
   ```bash
   release_branch=$(git branch --show-current)
   ```

3. **Checkout and merge:**
   ```bash
   git checkout main
   git pull origin main
   git merge origin/$release_branch --no-ff -m "Merge $release_branch into main"
   ```

4. **Push main:**
   ```bash
   git push origin main
   ```

   The post-merge hook will auto-delete `release-manifest.json` if present and commit the cleanup.

### Phase 3: Merge Main to Dev

1. ```bash
   git checkout dev
   git pull origin dev
   git merge main --no-ff -m "Merge main into dev"
   ```

2. **Handle merge conflicts** — if conflicts occur (likely in `version.json` or `CHANGELOG.md`):
   - For `version.json`: keep the **higher** version (this will be overwritten in Phase 4 anyway)
   - For `CHANGELOG.md`: keep **both** sets of entries (combine)
   - For `release-manifest.json`: delete the file (it should not exist on dev)
   - Ask the user for help with any other conflicts

3. **Push dev:**
   ```bash
   git push origin dev
   ```

   The post-merge hook will auto-delete `release-manifest.json` if present and commit the cleanup.

### Phase 4: Bump Versions on Dev

For each released product detected in Phase 1:

1. **Read** the current `<Product>/version.json`

2. **Compute the patch bump:**
   - Stable version: `1.5.0` → `1.5.1`
   - Dotted pre-release: `1.0.0-beta.2` → `1.0.0-beta.3` (increment the numeric segment)
   - Pre-release without numeric segment: `1.0.0-alpha` → `1.0.0-alpha.1`
   - **Legacy non-dotted pre-release** (`1.0.0-beta2` → `1.0.0-beta3`): increment in place to preserve ordering within that already-published line — do **not** convert it to dotted (`-beta.3` sorts *below* `-beta2`). Only `Umbraco.AI.Search`/`Umbraco.AI.Automate` are on this grandfathered scheme; all new lines are dotted (see root CLAUDE.md → Prerelease versioning).

3. **Update** the `"version"` field in `version.json` using the Edit tool

4. **After all products are bumped**, commit and push:
   ```bash
   git add */version.json
   git commit -m "chore(release): Bump dev versions after release

   Products bumped:
   - Umbraco.AI: 1.5.0 → 1.5.1
   - Umbraco.AI.OpenAI: 1.2.0 → 1.2.1

   Co-Authored-By: Claude <noreply@anthropic.com>"

   git push origin dev
   ```

### Phase 5: Cleanup (Optional)

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

3. **Return to dev branch:**
   ```bash
   git checkout dev
   ```

### Phase 6: Summary

Present a summary of everything that was done:

```
✅ Post-release cleanup complete!

Merged:
- $release_branch → main
- main → dev

Version bumps on dev:
- Umbraco.AI: 1.5.0 → 1.5.1
- Umbraco.AI.OpenAI: 1.2.0 → 1.2.1

Branch cleanup: [deleted/kept]

Nightly builds on dev will now produce versions higher than the released versions.
```

## Version Bump Logic

### Stable Versions

Simply increment the patch version:
- `1.5.0` → `1.5.1`
- `2.0.0` → `2.0.1`
- `1.0.3` → `1.0.4`

### Pre-release Versions

New prerelease lines use the **dotted** form `-{stage}.N` (`-alpha.1`, `-beta.1`, `-rc.1`) — never non-dotted `-beta1`, which sorts incorrectly past 9 (`beta10 < beta9`). See root CLAUDE.md → "Prerelease versioning".

Increment the numeric portion of the pre-release identifier:
- `1.0.0-beta.2` → `1.0.0-beta.3`
- `1.0.0-rc.1` → `1.0.0-rc.2`
- `1.0.0-alpha` → `1.0.0-alpha.1` (append `.1` if no numeric segment)
- `1.0.0-beta2` → `1.0.0-beta3` (legacy non-dotted, e.g. Search/Automate — increment in place, never dotify; dotifying would sort the result *below* the existing version)

## Important Notes

- **Always fetch tags first** — the release pipeline creates tags asynchronously after deploy
- **Use `--no-ff` merges** — preserves the merge commit for clear history
- **Post-merge hooks handle `release-manifest.json` cleanup** — don't manually delete it
- **version.json only has a `"version"` field** — update only that field, preserve all other properties
- **Both `release/*` and `hotfix/*` branches are supported** — the workflow is identical
- **If no tags are found**, the user can still proceed with merge-only (skip Phase 4)

## Error Recovery

- If the merge to main fails (conflicts), help the user resolve conflicts before continuing
- If the push fails, check if the branch is protected and advise accordingly
- If version.json has unexpected format, show the user and ask how to proceed
- Never force-push — if push is rejected, pull and retry the merge
