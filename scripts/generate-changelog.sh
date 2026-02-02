#!/bin/bash
# scripts/generate-changelog.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Parse arguments
PRODUCT=""
VERSION=""
UNRELEASED=false
LIST=false

while [[ $# -gt 0 ]]; do
  case $1 in
    --product=*)
      PRODUCT="${1#*=}"
      shift
      ;;
    --version=*)
      VERSION="${1#*=}"
      shift
      ;;
    --unreleased)
      UNRELEASED=true
      shift
      ;;
    --list)
      LIST=true
      shift
      ;;
    *)
      echo "Unknown argument: $1"
      exit 1
      ;;
  esac
done

# List products
if [[ "$LIST" == true ]]; then
  node "$SCRIPT_DIR/generate-changelog.js" --list
  exit $?
fi

# Validate product is provided
if [[ -z "$PRODUCT" ]]; then
  echo "❌ Error: --product is required"
  echo ""
  echo "Usage:"
  echo "  ./scripts/generate-changelog.sh --product=Umbraco.AI --version=17.1.0"
  echo "  ./scripts/generate-changelog.sh --product=Umbraco.AI --unreleased"
  echo "  ./scripts/generate-changelog.sh --list  # List available products"
  echo ""
  node "$SCRIPT_DIR/generate-changelog.js" --list
  exit 1
fi

# Build Node.js arguments
NODE_ARGS=("$SCRIPT_DIR/generate-changelog.js" "--product=$PRODUCT")

if [[ -n "$VERSION" ]]; then
  NODE_ARGS+=("--version=$VERSION")
fi

if [[ "$UNRELEASED" == true ]]; then
  NODE_ARGS+=("--unreleased")
fi

# Run Node.js script
echo "Generating changelog for $PRODUCT..."
node "${NODE_ARGS[@]}"

# If not in CI, show next steps
if [[ -z "$CI" ]]; then
  CHANGELOG_PATH="$ROOT_DIR/$PRODUCT/CHANGELOG.md"
  echo ""
  echo "✅ Changelog generated at: $CHANGELOG_PATH"
  echo ""
  echo "Review and commit:"
  echo "  git add $PRODUCT/CHANGELOG.md"

  # Convert product name to lowercase for commit message scope
  SCOPE=$(echo "$PRODUCT" | sed 's/Umbraco\.AI\.//' | sed 's/Umbraco\.AI/core/' | tr '[:upper:]' '[:lower:]')
  echo "  git commit -m 'docs($SCOPE): update CHANGELOG for v$VERSION'"
fi
