#!/bin/bash
# Pre-push hook to validate branch naming conventions for Umbraco.AI monorepo
# Valid patterns:
#   - v<N>/dev
#   - v<N>/main
#   - v<N>/feature/<anything>
#   - v<N>/release/<anything>
#   - v<N>/hotfix/<anything>
#   - claude/<anything>

# Get current branch name
current_branch=$(git symbolic-ref --short HEAD 2>/dev/null)

if [ -z "$current_branch" ]; then
    echo "Unable to determine current branch"
    exit 1
fi

# Check if branch matches valid patterns
valid_branch=false
if [[ $current_branch =~ ^v[0-9]+/(dev|main)$ ]]; then
    valid_branch=true
elif [[ $current_branch =~ ^v[0-9]+/(feature|release|hotfix)/.+ ]]; then
    valid_branch=true
elif [[ $current_branch =~ ^claude/.+ ]]; then
    valid_branch=true
fi

if [ "$valid_branch" = false ]; then
    echo "========================================" >&2
    echo "ERROR: Invalid branch name: $current_branch" >&2
    echo "========================================" >&2
    echo "" >&2
    echo "Branch names must follow one of these patterns:" >&2
    echo "  v<N>/dev" >&2
    echo "  v<N>/main" >&2
    echo "  v<N>/feature/<anything>" >&2
    echo "  v<N>/release/<anything>" >&2
    echo "  v<N>/hotfix/<anything>" >&2
    echo "  claude/<anything>" >&2
    echo "" >&2
    echo "Examples:" >&2
    echo "  v18/dev" >&2
    echo "  v18/feature/add-caching" >&2
    echo "  v17/release/2026.01" >&2
    echo "  v17/hotfix/2026.01.1" >&2
    echo "========================================" >&2
    exit 1
fi

exit 0
