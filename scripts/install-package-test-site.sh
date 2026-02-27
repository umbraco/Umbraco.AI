#!/bin/bash
# Package Test Site Setup Script
# Creates a fresh Umbraco site with all Umbraco.AI packages from nightly/prerelease/release feeds

set -e

# Default values
SITE_NAME="Umbraco.AI.PackageTestSite"
FEED="nightly"
SKIP_TEMPLATE_INSTALL=false
FORCE=false

# Feed URLs
declare -A FEED_URLS=(
    ["nightly"]="https://www.myget.org/F/umbraconightly/api/v3/index.json"
    ["prereleases"]="https://www.myget.org/F/umbracoprereleases/api/v3/index.json"
    ["release"]="https://api.nuget.org/v3/index.json"
)

# Determine repository root (parent of scripts folder)
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &>/dev/null && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/.." &>/dev/null && pwd )"

# Change to repository root to ensure consistent behavior
cd "$REPO_ROOT" || exit 1

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --name|-n)
            SITE_NAME="$2"
            shift 2
            ;;
        --feed)
            FEED="$2"
            if [[ "$FEED" != "nightly" && "$FEED" != "prereleases" && "$FEED" != "release" ]]; then
                echo "Error: --feed must be 'nightly', 'prereleases', or 'release'"
                exit 1
            fi
            shift 2
            ;;
        --skip-template-install|-s)
            SKIP_TEMPLATE_INSTALL=true
            shift
            ;;
        --force|-f)
            FORCE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -n, --name SITENAME              Site name (default: Umbraco.AI.PackageTestSite)"
            echo "      --feed FEED                  Feed to use: 'nightly', 'prereleases', or 'release' (default: nightly)"
            echo "  -s, --skip-template-install      Skip reinstalling Umbraco.Templates"
            echo "  -f, --force                      Recreate site if it already exists"
            echo "  -h, --help                       Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Set feed URL based on selection
FEED_URL="${FEED_URLS[$FEED]}"

echo "========================================="
echo "Umbraco.AI Feed Test Site Setup"
echo "========================================="
echo "Working directory: $REPO_ROOT"
echo "Feed: $FEED ($FEED_URL)"
echo "Site name: $SITE_NAME"
echo ""

# Check if specific site folder already exists
SITE_PATH="demo/$SITE_NAME"
if [ -d "$SITE_PATH" ] && [ "$FORCE" = false ]; then
    echo "Site folder '$SITE_PATH' already exists. Use --force to recreate."
    exit 0
fi

# Clean up existing site if Force
if [ "$FORCE" = true ] && [ -d "$SITE_PATH" ]; then
    echo "Removing existing site folder '$SITE_PATH'..."
    rm -rf "$SITE_PATH"
fi

# Clear NuGet cache for pre-release feeds (nightly builds change frequently)
# Skip cache clear for release feed (stable packages don't change)
if [ "$FEED" != "release" ]; then
    echo "Clearing NuGet cache to fetch fresh packages..."
    dotnet nuget locals all --clear
fi

# Step 1: Install Umbraco templates
if [ "$SKIP_TEMPLATE_INSTALL" = false ]; then
    echo "Installing Umbraco templates..."
    dotnet new install Umbraco.Templates --force
fi

# Step 2: Create demo folder
echo "Creating demo folder..."
mkdir -p "demo"

# Step 3: Create the Umbraco site
echo "Creating Umbraco site '$SITE_NAME'..."
pushd "demo" > /dev/null
dotnet new umbraco --force -n "$SITE_NAME" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
popd > /dev/null

# Step 4: Install Clean starter kit
echo "Installing Clean starter kit..."
pushd "demo/$SITE_NAME" > /dev/null
dotnet add package Clean
popd > /dev/null

# Step 5: Configure NuGet sources and PackageSourceMapping
echo "Configuring NuGet sources and package routing..."
pushd "demo/$SITE_NAME" > /dev/null

# Determine feed name
case "$FEED" in
    "nightly")
        FEED_NAME="UmbracoNightly"
        ;;
    "prereleases")
        FEED_NAME="UmbracoPreReleases"
        ;;
    "release")
        FEED_NAME="NuGet.org"
        ;;
esac

# Create complete nuget.config with sources and PackageSourceMapping
# For release feed, use simpler config (NuGet.org only)
if [ "$FEED" = "release" ]; then
    echo "Creating nuget.config for NuGet.org..."
    cat > nuget.config << NUGET_EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
NUGET_EOF
else
    echo "Creating nuget.config with package source mapping..."
    cat > nuget.config << NUGET_EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="$FEED_NAME" value="$FEED_URL" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="$FEED_NAME">
      <package pattern="Umbraco.AI" />
      <package pattern="Umbraco.AI.*" />
      <package pattern="Umbraco" />
      <package pattern="Umbraco.*" />
      <package pattern="Clean" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
NUGET_EOF
fi

popd > /dev/null

# Step 6: Install Umbraco.AI packages from feed
echo "Installing Umbraco.AI packages from $FEED feed..."
pushd "demo/$SITE_NAME" > /dev/null

# Determine if we need --prerelease flag (only for nightly/prereleases, not for release)
if [ "$FEED" = "release" ]; then
    PRERELEASE_FLAG=""
else
    PRERELEASE_FLAG="--prerelease"
fi

# Install Core first to establish the version baseline
echo "  Installing Umbraco.AI.Core..."
dotnet add package Umbraco.AI.Core $PRERELEASE_FLAG

# Core meta-package (includes Startup + Web.StaticAssets)
echo "  Installing Umbraco.AI..."
dotnet add package Umbraco.AI $PRERELEASE_FLAG

# Provider packages
echo "  Installing Umbraco.AI.OpenAI..."
dotnet add package Umbraco.AI.OpenAI $PRERELEASE_FLAG

echo "  Installing Umbraco.AI.Anthropic..."
dotnet add package Umbraco.AI.Anthropic $PRERELEASE_FLAG

echo "  Installing Umbraco.AI.Google..."
dotnet add package Umbraco.AI.Google $PRERELEASE_FLAG

echo "  Installing Umbraco.AI.Amazon..."
dotnet add package Umbraco.AI.Amazon $PRERELEASE_FLAG

echo "  Installing Umbraco.AI.MicrosoftFoundry..."
dotnet add package Umbraco.AI.MicrosoftFoundry $PRERELEASE_FLAG

# Add-on packages (includes Startup + Web.StaticAssets)
echo "  Installing Umbraco.AI.Prompt..."
dotnet add package Umbraco.AI.Prompt $PRERELEASE_FLAG

echo "  Installing Umbraco.AI.Agent..."
dotnet add package Umbraco.AI.Agent $PRERELEASE_FLAG

# Agent Copilot (frontend-only static assets)
echo "  Installing Umbraco.AI.Agent.Copilot..."
dotnet add package Umbraco.AI.Agent.Copilot $PRERELEASE_FLAG

popd > /dev/null

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Site location: demo/$SITE_NAME"
echo ""
echo "Credentials:"
echo "  Email: admin@example.com"
echo "  Password: password1234"
echo ""
echo "Next steps:"
echo "  1. cd demo/$SITE_NAME"
echo "  2. dotnet run"
echo "  3. Open https://localhost:44355 in your browser"
echo ""
echo "Note: All packages were installed from the $FEED feed:"
echo "  $FEED_URL"
echo ""
