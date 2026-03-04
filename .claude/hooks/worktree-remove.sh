#!/bin/bash
# WorktreeRemove hook for Claude Code
#
# Handles cleanup when a worktree session ends.
# Since WorktreeCreate replaces the default git behavior,
# we need this hook to properly run git worktree remove.
#
# Cross-platform: handles Windows (Git Bash) and Unix path conversions.
#
# Input (JSON on stdin): { "worktree_path": "<absolute-path>", ... }

set -e

INPUT=$(cat)

if ! command -v jq &>/dev/null; then
  echo "Error: jq is required for worktree hooks" >&2
  exit 1
fi

WORKTREE_PATH=$(echo "$INPUT" | jq -r '.worktree_path')

if [[ -z "$WORKTREE_PATH" || "$WORKTREE_PATH" == "null" ]]; then
  echo "No worktree_path provided" >&2
  exit 0
fi

# Convert Windows path to Unix-style for Git Bash if needed
if command -v cygpath &>/dev/null; then
  WORKTREE_PATH=$(cygpath -u "$WORKTREE_PATH")
fi

if [[ ! -d "$WORKTREE_PATH" ]]; then
  # Already removed, nothing to do
  exit 0
fi

# Try git worktree remove first (cleanest approach)
if git worktree remove "$WORKTREE_PATH" --force 2>/dev/null; then
  echo "Removed worktree: $WORKTREE_PATH" >&2
else
  # Fallback: prune and force-remove directory
  # Common on Windows when files are locked by IDE/build processes
  echo "git worktree remove failed, attempting manual cleanup..." >&2
  git worktree prune 2>/dev/null || true
  rm -rf "$WORKTREE_PATH" 2>/dev/null || true
fi

exit 0
