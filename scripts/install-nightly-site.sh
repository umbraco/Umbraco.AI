#!/bin/bash
# Nightly Feed Test Site Setup Script
# Creates a fresh Umbraco site with all Umbraco.AI packages from the nightly feed

set -e

# Default values
SITE_NAME="Umbraco.AI.NightlySite"
FEED="nightly"
SKIP_TEMPLATE_INSTALL=false
FORCE=false

# Feed URLs
declare -A FEED_URLS=(
    ["nightly"]="https://www.myget.org/F/umbraconightly/api/v3/index.json"
    ["prereleases"]="https://www.myget.org/F/umbracoprereleases/api/v3/index.json"
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
            if [[ "$FEED" != "nightly" && "$FEED" != "prereleases" ]]; then
                echo "Error: --feed must be 'nightly' or 'prereleases'"
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
            echo "  -n, --name SITENAME              Site name (default: Umbraco.AI.NightlySite)"
            echo "      --feed FEED                  Feed to use: 'nightly' or 'prereleases' (default: nightly)"
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

# Step 5: Add feed sources and configure PackageSourceMapping
echo "Configuring NuGet sources..."
pushd "demo/$SITE_NAME" > /dev/null

# Determine feed name
if [ "$FEED" = "nightly" ]; then
    FEED_NAME="UmbracoNightly"
else
    FEED_NAME="UmbracoPreReleases"
fi

# Create or ensure nuget.config exists
if [ ! -f "nuget.config" ]; then
    echo "Creating nuget.config..."
    cat > nuget.config << 'NUGET_EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
NUGET_EOF
fi

# Add test feed
echo "Adding $FEED_NAME feed..."
dotnet nuget add source "$FEED_URL" --name "$FEED_NAME" 2>&1 > /dev/null || true

# Add nuget.org for external dependencies (Microsoft.Extensions.AI, provider SDKs, etc.)
echo "Adding nuget.org feed..."
dotnet nuget add source "https://api.nuget.org/v3/index.json" --name "nuget.org" 2>&1 > /dev/null || true

# Configure PackageSourceMapping to route packages to correct sources
echo "Configuring PackageSourceMapping..."

# Read existing nuget.config, remove packageSourceMapping if present, and add new configuration
# Using python for XML manipulation as it's more portable than xmlstarlet
python3 << EOF
import xml.etree.ElementTree as ET
import sys

try:
    tree = ET.parse('nuget.config')
    root = tree.getroot()

    # Remove existing packageSourceMapping if present
    for psm in root.findall('packageSourceMapping'):
        root.remove(psm)

    # Create new packageSourceMapping element
    psm = ET.Element('packageSourceMapping')

    # Umbraco.AI packages from test feed
    ps1 = ET.SubElement(psm, 'packageSource', key='$FEED_NAME')
    ET.SubElement(ps1, 'package', pattern='Umbraco.AI*')

    # All other Umbraco packages from test feed (Umbraco.*, Clean, etc.)
    ps2 = ET.SubElement(psm, 'packageSource', key='$FEED_NAME')
    ET.SubElement(ps2, 'package', pattern='Umbraco.*')
    ET.SubElement(ps2, 'package', pattern='Clean')

    # Everything else from nuget.org (Microsoft.*, Anthropic, Google.*, AWSSDK.*, Azure.*, etc.)
    ps3 = ET.SubElement(psm, 'packageSource', key='nuget.org')
    ET.SubElement(ps3, 'package', pattern='*')

    root.append(psm)

    # Write back with proper formatting
    ET.indent(tree, space='  ')
    tree.write('nuget.config', encoding='utf-8', xml_declaration=True)
    sys.exit(0)
except Exception as e:
    print(f"Error: {e}", file=sys.stderr)
    sys.exit(1)
EOF

if [ $? -ne 0 ]; then
    echo "Warning: Failed to configure PackageSourceMapping. Python3 may not be available."
    echo "You may need to manually configure nuget.config for external dependencies."
fi

popd > /dev/null

# Step 6: Install Umbraco.AI packages from feed
echo "Installing Umbraco.AI packages from $FEED feed..."
pushd "demo/$SITE_NAME" > /dev/null

# Install Core first to establish the version baseline
echo "  Installing Umbraco.AI.Core..."
dotnet add package Umbraco.AI.Core --source "$FEED_URL" --prerelease

# Core meta-package (includes Startup + Web.StaticAssets)
echo "  Installing Umbraco.AI..."
dotnet add package Umbraco.AI --source "$FEED_URL" --prerelease

# Provider packages
echo "  Installing Umbraco.AI.OpenAI..."
dotnet add package Umbraco.AI.OpenAI --source "$FEED_URL" --prerelease

echo "  Installing Umbraco.AI.Anthropic..."
dotnet add package Umbraco.AI.Anthropic --source "$FEED_URL" --prerelease

echo "  Installing Umbraco.AI.Google..."
dotnet add package Umbraco.AI.Google --source "$FEED_URL" --prerelease

echo "  Installing Umbraco.AI.Amazon..."
dotnet add package Umbraco.AI.Amazon --source "$FEED_URL" --prerelease

echo "  Installing Umbraco.AI.MicrosoftFoundry..."
dotnet add package Umbraco.AI.MicrosoftFoundry --source "$FEED_URL" --prerelease

# Add-on packages (includes Startup + Web.StaticAssets)
echo "  Installing Umbraco.AI.Prompt..."
dotnet add package Umbraco.AI.Prompt --source "$FEED_URL" --prerelease

echo "  Installing Umbraco.AI.Agent..."
dotnet add package Umbraco.AI.Agent --source "$FEED_URL" --prerelease

# Agent Copilot (frontend-only static assets)
echo "  Installing Umbraco.AI.Agent.Copilot..."
dotnet add package Umbraco.AI.Agent.Copilot --source "$FEED_URL" --prerelease

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
