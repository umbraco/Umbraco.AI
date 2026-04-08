---
name: worktree-merge
description: Merge a feature worktree branch into dev and clean up. Use for lightweight features that don't need a PR — merges locally, pushes, then removes the worktree and branch.
user-invocable: true
argument-hint: Optional worktree name or branch name. If omitted, shows a list to choose from.
---

# Worktree Merge

You are merging a feature branch from a worktree into dev and cleaning up afterwards. This is for lightweight changes that don't warrant a GitHub PR.

## Steps

1. **Determine which worktree to merge.**

   If the user provides an argument, use it as the worktree name or branch name.

   If no argument is provided, first check if the current working directory is inside a worktree:
   ```bash
   # Get the main repo root and current working directory
   MAIN_ROOT=$(git worktree list | head -1 | awk '{print $1}')
   CWD=$(pwd)
   ```
   If `CWD` is inside a `.claude/worktrees/` path (i.e., we're currently in a worktree), extract the worktree name and ask the user: "You're currently in worktree `<name>` — merge this one?" If they confirm, use it. If they decline, fall through to the list.

   If not in a worktree (or user declined), list all worktrees and ask the user which one to merge:
   ```bash
   git worktree list
   ```
   Exclude the main worktree (the first entry). If there are no worktrees, tell the user and stop.

2. **Determine the branch name** from the worktree. Check what branch the worktree is on:
   ```bash
   git -C <worktree-path> branch --show-current
   ```

3. **Check for uncommitted changes** in the worktree:
   ```bash
   git -C <worktree-path> status --short
   ```
   If there are uncommitted changes, warn the user and ask whether to proceed (changes will be lost) or stop so they can commit first.

4. **Switch to the main repo root** if currently inside the worktree:
   ```bash
   cd <main-repo-path>
   ```

5. **Ensure dev is up to date:**
   ```bash
   git checkout dev
   git pull origin dev
   ```

6. **Merge the feature branch** into dev with a merge commit:
   ```bash
   git merge --no-ff <branch-name>
   ```
   If there are merge conflicts, report them and stop — let the user resolve them.

7. **Push dev:**
   ```bash
   git push origin dev
   ```

8. **Clean up** by invoking the worktree-cleanup skill:
   ```
   /worktree-cleanup <worktree-name>
   ```

9. **Show a summary:**
   - Branch merged
   - Commit(s) merged (short log)
   - Worktree and branch cleaned up
   - Current state (on dev, pushed)

## Important

- Always use `--no-ff` to preserve the merge commit for history.
- Never force-push dev.
- If the user is currently inside the worktree, navigate out before cleanup.
- The target branch is always `dev` unless the user explicitly says otherwise.
- If merge conflicts occur, stop and let the user resolve — don't attempt auto-resolution.
