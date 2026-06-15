#!/usr/bin/env bash
#
# cleanup-orphaned-worktrees.sh
#
# Removes orphaned worktree directories under .claude/worktrees/ — directories
# that exist on disk but are NOT tracked by `git worktree list` (i.e. their git
# ref was already pruned). Tracked worktrees and the main repo are never touched.
#
# Wired as a SessionStart hook (personal, via .claude/settings.local.json).
# Directories still locked by a live session ("Device or resource busy" /
# "Permission denied") fail to delete and are skipped silently — a later run
# catches them once that session ends.
#
# Always exits 0 so it can never block session startup.

set -u

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WORKTREES_DIR="$REPO_ROOT/.claude/worktrees"

cd "$REPO_ROOT" 2>/dev/null || exit 0
[ -d "$WORKTREES_DIR" ] || exit 0

# Drop stale git refs first so the "tracked" set is accurate.
git worktree prune 2>/dev/null

# Normalized absolute paths git currently tracks as worktrees.
tracked="$(git worktree list --porcelain 2>/dev/null \
    | sed -n 's/^worktree //p' \
    | while IFS= read -r t; do (cd "$t" 2>/dev/null && pwd) || echo "$t"; done)"

removed=0
for dir in "$WORKTREES_DIR"/*/; do
    [ -d "$dir" ] || continue
    abs="$(cd "$dir" 2>/dev/null && pwd)" || continue

    # Skip anything git still tracks as a worktree.
    if printf '%s\n' "$tracked" | grep -qxF "$abs"; then
        continue
    fi

    # Orphaned — remove, but never fight a locked directory.
    if rm -rf "$abs" 2>/dev/null && [ ! -d "$abs" ]; then
        removed=$((removed + 1))
    fi
done

if [ "$removed" -gt 0 ]; then
    if [ "$removed" -eq 1 ]; then
        echo "Removed 1 orphaned worktree directory."
    else
        echo "Removed $removed orphaned worktree directories."
    fi
fi

exit 0
