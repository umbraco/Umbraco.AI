---
name: worktree-cleanup
description: Clean up a git worktree and its local branch after a feature has been merged. Use after a PR is merged or a feature branch is no longer needed.
allowed-tools: Bash, Read
user-invocable: true
argument: Optional worktree name or branch name. If omitted, shows a list to choose from.
---

# Worktree Cleanup

You are cleaning up a git worktree and its associated local branch after the feature has been merged or is no longer needed.

## Steps

1. **Determine which worktree to clean up.**

   If the user provides an argument, use it as the worktree name or branch name.

   If no argument is provided, list all worktrees and ask the user which one to remove:
   ```bash
   git worktree list
   ```
   Exclude the main worktree (the first entry, which is the primary repo checkout).

   If there are no worktrees to clean up, tell the user and stop.

2. **Check if we're currently inside the worktree** being removed. If `pwd` is inside the worktree path, switch to the main repo root first using the primary worktree path from `git worktree list`.

3. **Remove the worktree** using git's worktree command:
   ```bash
   git worktree remove <path> --force
   ```
   If that fails due to permissions (common on Windows), fall back to manual removal:
   ```bash
   rm -rf <path>
   git worktree prune
   ```

4. **Delete the local branch** if it exists. Determine the branch name from the worktree or the user's input:
   ```bash
   git branch -d <branch-name>
   ```
   If `-d` fails because the branch isn't merged to HEAD (but is merged to its remote tracking branch), use `-D` after confirming with the user.

5. **Prune stale worktree references:**
   ```bash
   git worktree prune
   ```

6. **Show a summary** of what was cleaned up:
   - Worktree path removed
   - Branch deleted (if applicable)
   - Confirmation that everything is clean

## Important

- Always check we're not inside the worktree before removing it.
- The worktree directory lives under `.claude/worktrees/<name>` by convention.
- The branch name is typically `feature/<name>` where `<name>` matches the worktree directory name.
- If the worktree directory doesn't exist but the git worktree ref is stale, `git worktree prune` handles it.
- Never delete the `main`, `dev`, or `master` branches.
