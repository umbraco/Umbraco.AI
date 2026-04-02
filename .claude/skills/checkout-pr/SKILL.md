---
name: checkout-pr
description: Check out a pull request into an isolated git worktree. Use when you want to review, test, or inspect a PR without affecting your current working directory.
user-invocable: true
argument-hint: PR number or URL (e.g., "112", "#112", "https://github.com/owner/repo/pull/112")
---

# Checkout PR to Worktree

You are checking out a pull request into an isolated git worktree so the user can review or test it.

## Input

The user provides a PR number, `#number`, or a full GitHub PR URL as the argument.

## Steps

1. **Parse the PR identifier** from the argument. Extract just the number.

2. **Fetch PR metadata** using `mcp__github__pull_request_read` with the repo `umbraco/Umbraco.AI` and the PR number. Note the:
   - Branch name (`headRefName`)
   - Title
   - Author
   - Base branch

3. **Fetch the latest from origin** so the branch is available locally:
   ```bash
   git fetch origin <branch-name>
   ```

4. **Enter the worktree** using `EnterWorktree` with `name` set to the PR's branch name (e.g., `feature/add-streaming`). The branch name contains a `/`, so the worktree hook will treat it as an explicit branch and track the remote.

5. **Show a summary** to the user:
   - PR title and number
   - Author
   - Branch name
   - Base branch
   - A note that they're now in an isolated worktree

## Important

- The worktree hook handles the `/` convention: branch names with `/` are used as-is (no `feature/` prefix added).
- If the PR branch doesn't exist on origin, report the error clearly.
- Do NOT modify any files or make commits. This is a read-only checkout for review.
