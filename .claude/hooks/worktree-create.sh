#!/bin/bash
# WorktreeCreate hook for Claude Code
#
# Replaces the default git worktree creation to:
#   1. Use feature/<name> branch naming (gitflow convention)
#   2. Copy files specified in .worktreeinclude to the new worktree
#
# Input (JSON on stdin): { "name": "<slug>", "cwd": "<project-root>", ... }
# Output (stdout):       Absolute path to the created worktree directory
#
# All informational output goes to stderr to keep stdout clean for the path.
# Cross-platform: handles Windows (Git Bash) and Unix path conversions.

set -e

# --- Read input ---
INPUT=$(cat)

if ! command -v jq &>/dev/null; then
  echo "Error: jq is required for worktree hooks. Install it: https://jqlang.github.io/jq/download/" >&2
  echo "  Windows (winget): winget install jqlang.jq" >&2
  echo "  Windows (scoop):  scoop install jq" >&2
  exit 1
fi

# Claude Code on Windows may send backslash paths (D:\Work\...) which are
# invalid JSON escapes. Replace \<alphanum> with /<alphanum> to normalise
# Windows paths while preserving valid JSON escapes (\", \\, \n, etc.).
INPUT=$(printf '%s\n' "$INPUT" | sed 's/\\\([[:alnum:]]\)/\/\1/g')

NAME=$(printf '%s\n' "$INPUT" | jq -r '.name')
CWD=$(printf '%s\n' "$INPUT" | jq -r '.cwd')

# --- Cross-platform path handling ---
# Claude Code sends Windows paths (D:\Work\...) in JSON on Windows,
# but bash/find need Unix-style paths (/d/Work/...).
# cygpath is available in Git Bash on Windows.
to_unix_path() {
  if command -v cygpath &>/dev/null; then
    cygpath -u "$1"
  else
    echo "$1"
  fi
}

to_native_path() {
  if command -v cygpath &>/dev/null; then
    cygpath -w "$1"
  else
    echo "$1"
  fi
}

# --- Paths ---
# Convert CWD to Unix-style for internal use, fallback to git root
if [[ -n "$CWD" && "$CWD" != "null" ]]; then
  GIT_ROOT=$(to_unix_path "$CWD")
else
  GIT_ROOT=$(git rev-parse --show-toplevel)
fi

WORKTREE_DIR="$GIT_ROOT/.claude/worktrees"

# --- Determine branch name and worktree directory name ---
# Version-prefixed branch models (vN/feature/<name>) are supported but OPTIONAL:
# projects that don't use them get no version and fall through to plain
# feature/<name>. VERSION (e.g. "v18") is discovered from two signals, in order:
#   1. An explicit leading token in the name: "v17/add-streaming"
#   2. The current HEAD branch, if it is version-prefixed: "v18/dev" -> "v18"
# The directory slug flattens slashes to dashes and drops the feature/ segment.
VERSION=""
if [[ "$NAME" == */* ]]; then
  # Name already contains a slash.
  if [[ "$NAME" =~ ^v[0-9]+/(feature|hotfix|release)/.+ ]] || [[ "$NAME" =~ ^v[0-9]+/(dev|main)$ ]]; then
    # Already a fully-qualified version branch (e.g. PR checkout) -> literal.
    BRANCH_NAME="$NAME"
  elif [[ "$NAME" =~ ^(v[0-9]+)/(.+)$ ]]; then
    # Version-prefixed short name: v17/add-streaming -> v17/feature/add-streaming
    VERSION="${BASH_REMATCH[1]}"
    BRANCH_NAME="$VERSION/feature/${BASH_REMATCH[2]}"
  else
    # Any other explicit branch (e.g. user/patch) -> literal.
    BRANCH_NAME="$NAME"
  fi
else
  # No slash: short name. Infer the version from the current branch if it is
  # version-prefixed; otherwise keep the plain feature/ convention.
  CURRENT_BRANCH=$(git -C "$GIT_ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null) || true
  if [[ "$CURRENT_BRANCH" =~ ^(v[0-9]+)/ ]]; then
    VERSION="${BASH_REMATCH[1]}"
    BRANCH_NAME="$VERSION/feature/$NAME"
  else
    BRANCH_NAME="feature/$NAME"
  fi
fi

# Slug: drop the feature/ segment for short dir names, flatten remaining slashes.
#   feature/add-streaming     -> add-streaming
#   v18/feature/add-streaming -> v18-add-streaming
WORKTREE_SLUG=$(printf '%s' "$BRANCH_NAME" | sed -E 's#(^|/)feature/#\1#')
WORKTREE_SLUG="${WORKTREE_SLUG//\//-}"
WORKTREE_PATH="$WORKTREE_DIR/$WORKTREE_SLUG"

# --- Ensure .claude/worktrees is in .gitignore ---
if ! grep -qF '.claude/worktrees' "$GIT_ROOT/.gitignore" 2>/dev/null; then
  # Add a newline if file doesn't end with one
  if [[ -f "$GIT_ROOT/.gitignore" ]] && [[ -n "$(tail -c 1 "$GIT_ROOT/.gitignore")" ]]; then
    echo "" >> "$GIT_ROOT/.gitignore"
  fi
  echo ".claude/worktrees" >> "$GIT_ROOT/.gitignore"
  echo "Added .claude/worktrees to .gitignore" >&2
fi

# --- Determine base branch ---
# Use the default remote branch (usually dev or main)
DEFAULT_BRANCH=$(git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@^refs/remotes/origin/@@') || true
if [[ -z "$DEFAULT_BRANCH" ]]; then
  # Fallback: check common branch names
  for candidate in dev main master; do
    if git show-ref --verify --quiet "refs/remotes/origin/$candidate" 2>/dev/null; then
      DEFAULT_BRANCH="$candidate"
      break
    fi
  done
fi
DEFAULT_BRANCH="${DEFAULT_BRANCH:-dev}"

# If this is version-scoped work, prefer that version's dev line as the base,
# then its main line (e.g. VERSION=v17 -> origin/v17/dev, else origin/v17/main).
# Falls back to the default above when the version line has neither on the remote.
if [[ -n "$VERSION" ]]; then
  if git show-ref --verify --quiet "refs/remotes/origin/$VERSION/dev" 2>/dev/null; then
    DEFAULT_BRANCH="$VERSION/dev"
  elif git show-ref --verify --quiet "refs/remotes/origin/$VERSION/main" 2>/dev/null; then
    DEFAULT_BRANCH="$VERSION/main"
  fi
fi

# --- Create worktree ---
mkdir -p "$WORKTREE_DIR"

if [[ -d "$WORKTREE_PATH" ]]; then
  echo "Worktree already exists: $WORKTREE_PATH" >&2
elif git show-ref --verify --quiet "refs/heads/$BRANCH_NAME" 2>/dev/null; then
  echo "Using existing local branch: $BRANCH_NAME" >&2
  git worktree add "$WORKTREE_PATH" "$BRANCH_NAME" >&2
elif git show-ref --verify --quiet "refs/remotes/origin/$BRANCH_NAME" 2>/dev/null; then
  echo "Tracking remote branch: origin/$BRANCH_NAME" >&2
  git worktree add --track -b "$BRANCH_NAME" "$WORKTREE_PATH" "origin/$BRANCH_NAME" >&2
else
  echo "Creating branch: $BRANCH_NAME (from origin/$DEFAULT_BRANCH)" >&2
  git worktree add -b "$BRANCH_NAME" "$WORKTREE_PATH" "origin/$DEFAULT_BRANCH" >&2
fi

# --- Output the worktree path FIRST (this is what Claude Code reads) ---
# Emit the path before file-copy so Claude Code gets it immediately.
# Convert to native path format so Claude Code (Node.js) can use it.
# On Windows: /d/Work/... -> D:\Work\...
# On Unix: passes through unchanged.
ABSOLUTE_PATH=$(cd "$WORKTREE_PATH" && pwd)
echo "$(to_native_path "$ABSOLUTE_PATH")"

# --- Copy .worktreeinclude files ---
# .worktreeinclude uses gitignore syntax (globs, negation, directory patterns).
# We pass it directly to git's pattern matching engine via --exclude-from,
# so all gitignore rules work natively: *, **, !, trailing /, etc.
INCLUDE_FILE="$GIT_ROOT/.worktreeinclude"

if [[ -f "$INCLUDE_FILE" ]]; then
  file_list=$(git -C "$GIT_ROOT" ls-files --others --ignored --exclude-from="$INCLUDE_FILE" 2>/dev/null) || true

  if [[ -z "$file_list" ]]; then
    echo "No files matched .worktreeinclude patterns" >&2
  else
    count=$(echo "$file_list" | wc -l | tr -d ' ')
    echo "Copying $count file(s) from .worktreeinclude..." >&2

    # Bulk copy via tar (fast even for thousands of files, handles paths with spaces)
    # Non-fatal: file copy is nice-to-have, must not prevent path output to stdout
    git -C "$GIT_ROOT" ls-files -z --others --ignored --exclude-from="$INCLUDE_FILE" 2>/dev/null | \
      tar -C "$GIT_ROOT" --null -T - -cf - 2>/dev/null | \
      tar -C "$WORKTREE_PATH" -xf - 2>/dev/null || \
      echo "Warning: some files could not be copied" >&2

    # Summary: group by top-level directory, show root files individually
    echo "$file_list" | awk -F/ '{print ($2 ? $1"/" : $0)}' | sort | uniq -c | \
      while read -r cnt path; do
        if [[ "$cnt" -eq 1 && "$path" != */ ]]; then
          echo "  + $path" >&2
        else
          echo "  + $path ($cnt files)" >&2
        fi
      done || true
  fi
else
  echo "No .worktreeinclude file found - skipping file copy" >&2
fi

echo "Worktree ready: $BRANCH_NAME -> $WORKTREE_SLUG" >&2
