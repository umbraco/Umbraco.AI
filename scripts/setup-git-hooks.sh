#!/bin/bash
# Setup script to configure git hooks for Umbraco.AI monorepo

set -e

echo "========================================="
echo "Umbraco.AI Git Hooks Setup"
echo "========================================="
echo ""

# Get the repository root
REPO_ROOT="$(git rev-parse --show-toplevel)"

if [ -z "$REPO_ROOT" ]; then
    echo "Error: Not in a git repository" >&2
    exit 1
fi

HOOKS_DIR="$REPO_ROOT/.githooks"

# Check if hooks directory exists
if [ ! -d "$HOOKS_DIR" ]; then
    echo "Error: .githooks directory not found at $HOOKS_DIR" >&2
    exit 1
fi

# Make hook scripts executable
echo "Making hook scripts executable..."
chmod +x "$HOOKS_DIR/pre-push"
chmod +x "$HOOKS_DIR/pre-push.sh"
chmod +x "$HOOKS_DIR/commit-msg"
chmod +x "$HOOKS_DIR/post-merge"
chmod +x "$HOOKS_DIR/pre-merge-commit"
chmod +x "$HOOKS_DIR/merge-preserve-on-release.sh"

# Configure git to use the custom hooks directory
echo "Configuring git to use custom hooks directory..."
git config core.hooksPath .githooks

# Configure custom merge driver for release-manifest.json
echo "Configuring custom merge driver for release-manifest.json..."
git config merge.preserve-on-release.name "Preserve release-manifest.json on release/hotfix branches"
git config merge.preserve-on-release.driver ".githooks/merge-preserve-on-release.sh %O %A %B %L %P"

echo ""
echo "✓ Git hooks configured successfully!"
echo ""
echo "The following hooks are now active:"
echo "  - pre-push: Validates branch naming conventions"
echo "  - commit-msg: Validates commit messages (conventional commits)"
echo "  - post-merge: Commits staged release-manifest.json on release/hotfix; removes it on main/dev/support/*"
echo "  - pre-merge-commit: Ensures release-manifest.json is staged on release/hotfix branches"
echo "  - merge driver: Preserves release-manifest.json on release/hotfix branches during merges"
echo ""
echo "To disable hooks, run:"
echo "  git config --unset core.hooksPath"
echo ""
