#!/bin/bash
# Pre-push hook to validate branch naming conventions for Umbraco.Ai monorepo
# Valid patterns:
#   - main
#   - dev
#   - feature/<anything>
#   - release/<anything>
#   - hotfix/<anything>

# Get current branch name
current_branch=$(git symbolic-ref --short HEAD 2>/dev/null)

if [ -z "$current_branch" ]; then
    echo "Unable to determine current branch"
    exit 1
fi

# Allow main and dev branches
if [ "$current_branch" = "main" ] || [ "$current_branch" = "dev" ]; then
    exit 0
fi

# Check if branch matches valid patterns
valid_branch=false
if [[ $current_branch =~ ^(feature|claude|release|hotfix)/.+ ]]; then
    valid_branch=true
fi

if [ "$valid_branch" = false ]; then
    echo "========================================" >&2
    echo "ERROR: Invalid branch name: $current_branch" >&2
    echo "========================================" >&2
    echo "" >&2
    echo "Branch names must follow one of these patterns:" >&2
    echo "  main" >&2
    echo "  dev" >&2
    echo "  feature/<anything>" >&2
    echo "  release/<anything>" >&2
    echo "  hotfix/<anything>" >&2
    echo "" >&2
    echo "Examples:" >&2
    echo "  feature/add-caching" >&2
    echo "  release/2026.01" >&2
    echo "  hotfix/2026.01.1" >&2
    echo "========================================" >&2
    exit 1
fi

exit 0
