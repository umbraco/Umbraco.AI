#!/bin/bash
# Pre-push hook to validate branch naming conventions for Umbraco.Ai monorepo
# Valid patterns:
#   - main
#   - develop
#   - feature/<product>-<description>
#   - release/<product>-<version>
#   - hotfix/<product>-<version>
# Where <product> is dynamically discovered from the directory structure:
#   - Umbraco.Ai/ -> core
#   - Umbraco.Ai.<name>/ -> <name> (lowercase)

# Get current branch name
current_branch=$(git symbolic-ref --short HEAD 2>/dev/null)

if [ -z "$current_branch" ]; then
    echo "Unable to determine current branch"
    exit 1
fi

# Get the repository root directory
repo_root=$(git rev-parse --show-toplevel 2>/dev/null)
if [ -z "$repo_root" ]; then
    echo "Unable to determine repository root"
    exit 1
fi

# Dynamically discover valid products from directory structure
# Umbraco.Ai/ -> "core"
# Umbraco.Ai.<name>/ -> "<name>" (lowercase)
products=()

for dir in "$repo_root"/Umbraco.Ai*/; do
    if [ -d "$dir" ]; then
        dir_name=$(basename "$dir")
        if [ "$dir_name" = "Umbraco.Ai" ]; then
            products+=("core")
        elif [[ "$dir_name" =~ ^Umbraco\.Ai\.(.+)$ ]]; then
            product_name="${BASH_REMATCH[1]}"
            # Convert to lowercase
            products+=("${product_name,,}")
        fi
    fi
done

if [ ${#products[@]} -eq 0 ]; then
    echo "No Umbraco.Ai products found in repository"
    exit 1
fi

# Allow main and develop branches
if [ "$current_branch" = "main" ] || [ "$current_branch" = "develop" ]; then
    exit 0
fi

# Check if branch matches valid patterns
valid_branch=false

# Check feature branches: feature/<product>-<description>
if [[ $current_branch =~ ^feature/([a-z]+)-.+ ]]; then
    product="${BASH_REMATCH[1]}"
    for p in "${products[@]}"; do
        if [ "$product" = "$p" ]; then
            valid_branch=true
            break
        fi
    done
fi

# Check release branches: release/<product>-<version>
if [[ $current_branch =~ ^release/([a-z]+)-[0-9]+\.[0-9]+\.[0-9]+ ]]; then
    product="${BASH_REMATCH[1]}"
    for p in "${products[@]}"; do
        if [ "$product" = "$p" ]; then
            valid_branch=true
            break
        fi
    done
fi

# Check hotfix branches: hotfix/<product>-<version>
if [[ $current_branch =~ ^hotfix/([a-z]+)-[0-9]+\.[0-9]+\.[0-9]+ ]]; then
    product="${BASH_REMATCH[1]}"
    for p in "${products[@]}"; do
        if [ "$product" = "$p" ]; then
            valid_branch=true
            break
        fi
    done
fi

if [ "$valid_branch" = false ]; then
    echo "========================================" >&2
    echo "ERROR: Invalid branch name: $current_branch" >&2
    echo "========================================" >&2
    echo "" >&2
    echo "Branch names must follow one of these patterns:" >&2
    echo "  main" >&2
    echo "  develop" >&2
    echo "  feature/<product>-<description>" >&2
    echo "  release/<product>-<version>" >&2
    echo "  hotfix/<product>-<version>" >&2
    echo "" >&2
    echo "Valid products: ${products[*]}" >&2
    echo "" >&2
    echo "Examples:" >&2
    echo "  feature/core-add-caching" >&2
    echo "  release/agent-17.1.0" >&2
    echo "  hotfix/openai-1.0.1" >&2
    echo "========================================" >&2
    exit 1
fi

exit 0
