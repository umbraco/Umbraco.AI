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

NAME=$(echo "$INPUT" | jq -r '.name')
CWD=$(echo "$INPUT" | jq -r '.cwd')

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
WORKTREE_PATH="$WORKTREE_DIR/$NAME"
BRANCH_NAME="feature/$NAME"

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

# --- Create worktree ---
mkdir -p "$WORKTREE_DIR"

if git show-ref --verify --quiet "refs/heads/$BRANCH_NAME" 2>/dev/null; then
  echo "Using existing branch: $BRANCH_NAME" >&2
  git worktree add "$WORKTREE_PATH" "$BRANCH_NAME" >&2
else
  echo "Creating branch: $BRANCH_NAME (from origin/$DEFAULT_BRANCH)" >&2
  git worktree add -b "$BRANCH_NAME" "$WORKTREE_PATH" "origin/$DEFAULT_BRANCH" >&2
fi

# --- Copy .worktreeinclude files ---
INCLUDE_FILE="$GIT_ROOT/.worktreeinclude"

if [[ -f "$INCLUDE_FILE" ]]; then
  echo "Copying files from .worktreeinclude..." >&2

  include_patterns=()
  exclude_patterns=()

  # Parse patterns (gitignore-style: # comments, ! exclusions)
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
    line=$(echo "$line" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
    [[ -z "$line" ]] && continue

    if [[ "$line" == !* ]]; then
      exclude_patterns+=("${line:1}")
    else
      include_patterns+=("$line")
    fi
  done < "$INCLUDE_FILE"

  # Build prune list dynamically from .gitignore (respects all gitignore rules)
  # This avoids hardcoding directories like bin/, obj/, refs/, node_modules/ etc.
  # Only prune DIRECTORIES (trailing /), not individual gitignored files -
  # those are the files we actually want to find and copy.
  prune_args=(-path "$GIT_ROOT/.git")
  while IFS= read -r ignored_dir; do
    ignored_dir="${ignored_dir%/}"  # Remove trailing slash
    [[ -n "$ignored_dir" ]] && prune_args+=(-o -path "$GIT_ROOT/$ignored_dir")
  done < <(git -C "$GIT_ROOT" ls-files --others --ignored --exclude-standard --directory 2>/dev/null | grep '/$')

  copied=0

  for pattern in "${include_patterns[@]}"; do
    # --- Directory patterns (demo/*, config/**) ---
    if [[ "$pattern" == *"/*" || "$pattern" == *"/**" ]]; then
      dir_path="${pattern%/\*\*}"
      dir_path="${dir_path%/\*}"
      source="$GIT_ROOT/$dir_path"

      if [[ -d "$source" ]]; then
        mkdir -p "$(dirname "$WORKTREE_PATH/$dir_path")"
        cp -r "$source" "$WORKTREE_PATH/$dir_path"
        echo "  + $dir_path/ (directory)" >&2
        copied=$((copied + 1))
      fi
      continue
    fi

    # --- File patterns (*.user, appsettings.Development.json, .env*) ---
    # Search recursively, pruning all gitignored directories so we only
    # find files in tracked source trees (not build artifacts or references)
    while IFS= read -r -d '' file; do
      rel_path="${file#$GIT_ROOT/}"

      # Apply exclude patterns
      excluded=false
      filename=$(basename "$rel_path")
      for excl in "${exclude_patterns[@]}"; do
        if [[ "$filename" == $excl || "$rel_path" == $excl ]]; then
          excluded=true
          break
        fi
      done
      [[ "$excluded" == true ]] && continue

      # Copy to worktree
      dest="$WORKTREE_PATH/$rel_path"
      mkdir -p "$(dirname "$dest")"
      cp "$file" "$dest"
      echo "  + $rel_path" >&2
      copied=$((copied + 1))
    done < <(find "$GIT_ROOT" \
      \( "${prune_args[@]}" \) -prune -o \
      -type f -name "$pattern" -print0 2>/dev/null)
  done

  echo "Copied $copied item(s)" >&2
else
  echo "No .worktreeinclude file found - skipping file copy" >&2
fi

# --- Output the worktree path (this is what Claude Code reads) ---
# Convert to native path format so Claude Code (Node.js) can use it.
# On Windows: /d/Work/... -> D:\Work\...
# On Unix: passes through unchanged.
ABSOLUTE_PATH=$(cd "$WORKTREE_PATH" && pwd)
echo "$(to_native_path "$ABSOLUTE_PATH")"
