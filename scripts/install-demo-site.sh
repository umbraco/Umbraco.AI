#!/bin/bash
# Unified Demo Site Setup Script
# Creates a shared demo site with all Umbraco.AI products

set -e

# Determine repository root (parent of scripts folder)
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &>/dev/null && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/.." &>/dev/null && pwd )"

# Change to repository root to ensure consistent behavior
cd "$REPO_ROOT" || exit 1

# Parse arguments
SKIP_TEMPLATE_INSTALL=false
FORCE=false

while [[ $# -gt 0 ]]; do
    case $1 in
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
            echo "  -s, --skip-template-install  Skip reinstalling Umbraco.Templates"
            echo "  -f, --force                  Recreate demo if it already exists"
            echo "  -h, --help                   Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "========================================="
echo "Umbraco.AI Unified Demo Site Setup"
echo "========================================="
echo "Working directory: $REPO_ROOT"
echo ""

# Toolchain check — required Node version comes from package.json's engines.node, so this
# stays in lockstep with the npm-side enforcement and the .nvmrc.
REQUIRED_NODE_RANGE=$(grep -oE '"node"[[:space:]]*:[[:space:]]*"[^"]+"' "$REPO_ROOT/package.json" | head -1 | grep -oE '"[^"]+"$' | tr -d '"')
REQUIRED_NODE_MAJOR=$(echo "$REQUIRED_NODE_RANGE" | grep -oE '[0-9]+' | head -1)
if [ -z "$REQUIRED_NODE_MAJOR" ]; then
    echo "ERROR: Could not parse engines.node ('$REQUIRED_NODE_RANGE') from package.json." >&2
    exit 1
fi

if ! command -v node >/dev/null 2>&1; then
    echo "ERROR: Node.js is not installed or not on PATH. package.json requires '$REQUIRED_NODE_RANGE'." >&2
    echo "Install Node $REQUIRED_NODE_MAJOR+ (e.g. 'nvm install $REQUIRED_NODE_MAJOR && nvm use $REQUIRED_NODE_MAJOR') and re-run." >&2
    exit 1
fi
NODE_VERSION_RAW=$(node --version | sed 's/^v//')
NODE_MAJOR=${NODE_VERSION_RAW%%.*}
if [ "${NODE_MAJOR:-0}" -lt "$REQUIRED_NODE_MAJOR" ]; then
    echo "ERROR: Node $NODE_VERSION_RAW detected; package.json requires '$REQUIRED_NODE_RANGE'." >&2
    echo "Run 'nvm install $REQUIRED_NODE_MAJOR && nvm use $REQUIRED_NODE_MAJOR' (or equivalent) before re-running this script." >&2
    exit 1
fi
echo "Node $NODE_VERSION_RAW detected (satisfies '$REQUIRED_NODE_RANGE')."
echo ""

# Detect template version and major from Directory.Packages.props.
# The lower bound of the Umbraco.Cms.Core range is the minimum CMS version this branch supports
# and therefore the right template version to scaffold the demo site against.
PACKAGES_PROPS_PATH="$REPO_ROOT/Directory.Packages.props"
if [ ! -f "$PACKAGES_PROPS_PATH" ]; then
    echo "ERROR: Could not find $PACKAGES_PROPS_PATH" >&2
    exit 1
fi
TEMPLATE_VERSION=$(grep -oE 'Include="Umbraco\.Cms\.Core" Version="\[[^,]+,' "$PACKAGES_PROPS_PATH" | grep -oE '\[[^,]+,' | tr -d '[,')
if [ -z "$TEMPLATE_VERSION" ]; then
    echo "ERROR: Could not find Umbraco.Cms.Core version range in $PACKAGES_PROPS_PATH" >&2
    exit 1
fi
VERSION_MAJOR=$(echo "$TEMPLATE_VERSION" | cut -d. -f1)
IS_TEMPLATE_PRERELEASE=false
if echo "$TEMPLATE_VERSION" | grep -q '-'; then
    IS_TEMPLATE_PRERELEASE=true
fi
echo "Target Umbraco.Cms template version: $TEMPLATE_VERSION (v$VERSION_MAJOR)"
echo ""

# Versioned demo directory: demos/vN/
DEMO_DIR="demos/v${VERSION_MAJOR}"
DEMO_SITE_DIR="${DEMO_DIR}/Umbraco.AI.DemoSite"

# Check if demo already exists
if [ -d "$DEMO_DIR" ] && [ "$FORCE" = false ]; then
    echo "Demo folder '$DEMO_DIR' already exists. Use --force to recreate."
    echo "Or open the existing Umbraco.AI.local.slnx"
    exit 0
fi

# Clean up existing demo if Force
if [ "$FORCE" = true ] && [ -d "$DEMO_DIR" ]; then
    echo "Removing existing demo folder '$DEMO_DIR'..."
    rm -rf "$DEMO_DIR"
fi

if [ "$FORCE" = true ] && [ -f "Umbraco.AI.local.slnx" ]; then
    rm -f "Umbraco.AI.local.slnx"
fi

# Step 1: Install Umbraco templates
if [ "$SKIP_TEMPLATE_INSTALL" = false ]; then
    echo "Installing Umbraco templates ($TEMPLATE_VERSION)..."
    # Uninstall any existing version to avoid conflicts
    echo "Removing any existing Umbraco.Templates installations..."
    if dotnet new uninstall 2>&1 | grep -q "Umbraco\.Templates"; then
        dotnet new uninstall Umbraco.Templates 2>/dev/null || true
    fi
    if [ "$IS_TEMPLATE_PRERELEASE" = true ]; then
        # Prerelease templates require the umbracoprereleases MyGet feed to be configured.
        # If not yet configured: dotnet nuget add source https://www.myget.org/F/umbracoprereleases/api/v3/index.json --name UmbracoPreReleases
        echo "NOTE: Prerelease template ($TEMPLATE_VERSION) requires the umbracoprereleases MyGet source."
    fi
    dotnet new install "Umbraco.Templates::${TEMPLATE_VERSION}" --force
fi

# Step 2: Create demo folder with build overrides
echo "Creating demo folder '$DEMO_DIR'..."
mkdir -p "$DEMO_DIR"

# Disable package validation for demo folder
cp "$SCRIPT_DIR/templates/Directory.Build.props" "$DEMO_DIR/Directory.Build.props"

# Disable central package management for demo folder
cp "$SCRIPT_DIR/templates/Directory.Packages.props" "$DEMO_DIR/Directory.Packages.props"

# Step 3: Create the Umbraco demo site
echo "Creating Umbraco demo site..."
pushd "$DEMO_DIR" > /dev/null
dotnet new umbraco --force -n "Umbraco.AI.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
popd > /dev/null

# Step 3.1: Set a fixed UserSecretsId so secrets survive demo site recreations.
# A single shared GUID is used across all versions — API keys don't change between them.
USER_SECRETS_ID="d4e8f2a1-6b3c-4e5f-9a8b-7c2d1e4f3a6b"
echo "Setting UserSecretsId ($USER_SECRETS_ID)..."
CSPROJ_PATH="$DEMO_SITE_DIR/Umbraco.AI.DemoSite.csproj"
python3 -c "
import re
path = '${CSPROJ_PATH}'
uid = '${USER_SECRETS_ID}'
content = open(path).read()
content = re.sub('</PropertyGroup>', '    <UserSecretsId>' + uid + '</UserSecretsId>\n  </PropertyGroup>', content, count=1)
open(path, 'w').write(content)
"

# Step 3.2: Install Clean starter kit and Umbraco.Automate
# Clean's major version does not match CMS major — map explicitly and add new entries as needed.
case "$VERSION_MAJOR" in
    17) CLEAN_VERSION="7.*" ;;
    18) CLEAN_VERSION="8.*" ;;
    *) CLEAN_VERSION="" ;;
esac
echo "Installing Clean starter kit..."
pushd "$DEMO_SITE_DIR" > /dev/null
if [ -n "$CLEAN_VERSION" ]; then
    dotnet add package Clean --version "$CLEAN_VERSION"
else
    echo "Warning: No Clean version mapping for v$VERSION_MAJOR, using latest stable."
    dotnet add package Clean
fi
dotnet add package Umbraco.Automate --version "${VERSION_MAJOR}.*-*"
popd > /dev/null

# Step 3.3: Set fixed port for consistent development
echo "Configuring fixed port (44355)..."
mkdir -p "$DEMO_SITE_DIR/Properties"
cp "$SCRIPT_DIR/templates/launchSettings.json" "$DEMO_SITE_DIR/Properties/launchSettings.json"

# Step 3.4: Add NamedPipeListenerComposer for HTTP over named pipes
echo "Adding NamedPipeListenerComposer for HTTP over named pipes..."
mkdir -p "$DEMO_SITE_DIR/Composers"
cp "$SCRIPT_DIR/templates/NamedPipeListenerComposer.cs" "$DEMO_SITE_DIR/Composers/NamedPipeListenerComposer.cs"

# Step 3.5: Add UmbracoAISeedData for demo data on first startup
echo "Adding UmbracoAISeedData..."
mkdir -p "$DEMO_SITE_DIR/SeedData"
cp "$SCRIPT_DIR/templates/UmbracoAISeedData.cs" "$DEMO_SITE_DIR/SeedData/UmbracoAISeedData.cs"

# Step 3.6: Configure Umbraco.Automate to share the Umbraco database
echo "Configuring Umbraco.Automate to share Umbraco database..."
APP_SETTINGS_PATH="$DEMO_SITE_DIR/appsettings.json"
node -e "
const fs = require('fs');
const path = '${APP_SETTINGS_PATH}';
const s = JSON.parse(fs.readFileSync(path, 'utf8'));
s.Umbraco = s.Umbraco || {};
s.Umbraco.Automate = { UseNamedConnectionString: 'umbracoDbDSN' };
fs.writeFileSync(path, JSON.stringify(s, null, 2) + '\n', 'utf8');
"

# Step 4: Create unified solution
echo "Creating unified solution..."
dotnet new sln -n "Umbraco.AI.local" --force --format slnx

# Helper function to add all projects from a product's src folder
add_product_projects() {
    local product_folder="$1"
    local solution_folder="$2"
    local src_path="$product_folder/src"

    if [ -d "$src_path" ]; then
        local count=0
        while IFS= read -r -d '' proj; do
            local proj_name=$(basename "$proj")
            echo "  Adding $proj_name"
            dotnet sln "Umbraco.AI.local.slnx" add "$proj" --solution-folder "$solution_folder" 2>/dev/null || true
            ((count++))
        done < <(find "$src_path" -name "*.csproj" -print0)
        echo "  Added $count projects"
    fi
}

# Step 5: Add Core projects
echo "Adding Umbraco.AI (Core) projects..."
add_product_projects "Umbraco.AI" "Core"

# Step 6: Add provider projects
echo "Adding Umbraco.AI.OpenAI projects..."
add_product_projects "Umbraco.AI.OpenAI" "Providers/OpenAI"

echo "Adding Umbraco.AI.Anthropic projects..."
add_product_projects "Umbraco.AI.Anthropic" "Providers/Anthropic"

echo "Adding Umbraco.AI.Amazon projects..."
add_product_projects "Umbraco.AI.Amazon" "Providers/Amazon"

echo "Adding Umbraco.AI.DeepSeek projects..."
add_product_projects "Umbraco.AI.DeepSeek" "Providers/DeepSeek"

echo "Adding Umbraco.AI.FireworksAI projects..."
add_product_projects "Umbraco.AI.FireworksAI" "Providers/FireworksAI"

echo "Adding Umbraco.AI.Google projects..."
add_product_projects "Umbraco.AI.Google" "Providers/Google"

echo "Adding Umbraco.AI.HuggingFace projects..."
add_product_projects "Umbraco.AI.HuggingFace" "Providers/HuggingFace"

echo "Adding Umbraco.AI.MicrosoftFoundry projects..."
add_product_projects "Umbraco.AI.MicrosoftFoundry" "Providers/MicrosoftFoundry"

echo "Adding Umbraco.AI.Mistral projects..."
add_product_projects "Umbraco.AI.Mistral" "Providers/Mistral"

echo "Adding Umbraco.AI.TogetherAI projects..."
add_product_projects "Umbraco.AI.TogetherAI" "Providers/TogetherAI"

# Step 7: Add add-on projects
echo "Adding Umbraco.AI.Prompt projects..."
add_product_projects "Umbraco.AI.Prompt" "Addons/Prompt"

echo "Adding Umbraco.AI.Agent projects..."
add_product_projects "Umbraco.AI.Agent" "Addons/Agent"

echo "Adding Umbraco.AI.Agent.UI projects..."
add_product_projects "Umbraco.AI.Agent.UI" "Addons/Copilot"

echo "Adding Umbraco.AI.Agent.Copilot projects..."
add_product_projects "Umbraco.AI.Agent.Copilot" "Addons/Copilot"

echo "Adding Umbraco.AI.Search projects..."
add_product_projects "Umbraco.AI.Search" "Addons/Search"

echo "Adding Umbraco.AI.Automate projects..."
add_product_projects "Umbraco.AI.Automate" "Addons/Automate"

# Step 8: Add Deploy projects
echo "Adding Umbraco.AI.Deploy projects..."
add_product_projects "Umbraco.AI.Deploy" "Deploy"

echo "Adding Umbraco.AI.Prompt.Deploy projects..."
add_product_projects "Umbraco.AI.Prompt.Deploy" "Deploy"

echo "Adding Umbraco.AI.Agent.Deploy projects..."
add_product_projects "Umbraco.AI.Agent.Deploy" "Deploy"

# Step 11: Add demo site to solution
echo "Adding demo site to solution..."
dotnet sln "Umbraco.AI.local.slnx" add "$DEMO_SITE_DIR/Umbraco.AI.DemoSite.csproj" --solution-folder "Demo"

# Step 13: Add project references to demo site
echo "Adding project references to demo site..."
DEMO_PROJECT="$DEMO_SITE_DIR/Umbraco.AI.DemoSite.csproj"

# Core references (Startup + Web.StaticAssets)
dotnet add "$DEMO_PROJECT" reference "Umbraco.AI/src/Umbraco.AI.Startup/Umbraco.AI.Startup.csproj"
dotnet add "$DEMO_PROJECT" reference "Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Umbraco.AI.Web.StaticAssets.csproj"

# OpenAI provider
if [ -f "Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/Umbraco.AI.OpenAI.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/Umbraco.AI.OpenAI.csproj"
fi

# Anthropic provider
if [ -f "Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/Umbraco.AI.Anthropic.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/Umbraco.AI.Anthropic.csproj"
fi

# DeepSeek provider
if [ -f "Umbraco.AI.DeepSeek/src/Umbraco.AI.DeepSeek/Umbraco.AI.DeepSeek.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.DeepSeek/src/Umbraco.AI.DeepSeek/Umbraco.AI.DeepSeek.csproj"
fi

# Microsoft Foundry provider
if [ -f "Umbraco.AI.MicrosoftFoundry/src/Umbraco.AI.MicrosoftFoundry/Umbraco.AI.MicrosoftFoundry.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.MicrosoftFoundry/src/Umbraco.AI.MicrosoftFoundry/Umbraco.AI.MicrosoftFoundry.csproj"
fi

# Google provider
if [ -f "Umbraco.AI.Google/src/Umbraco.AI.Google/Umbraco.AI.Google.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Google/src/Umbraco.AI.Google/Umbraco.AI.Google.csproj"
fi

# Amazon provider
if [ -f "Umbraco.AI.Amazon/src/Umbraco.AI.Amazon/Umbraco.AI.Amazon.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Amazon/src/Umbraco.AI.Amazon/Umbraco.AI.Amazon.csproj"
fi

# FireworksAI provider
if [ -f "Umbraco.AI.FireworksAI/src/Umbraco.AI.FireworksAI/Umbraco.AI.FireworksAI.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.FireworksAI/src/Umbraco.AI.FireworksAI/Umbraco.AI.FireworksAI.csproj"
fi

# HuggingFace provider
if [ -f "Umbraco.AI.HuggingFace/src/Umbraco.AI.HuggingFace/Umbraco.AI.HuggingFace.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.HuggingFace/src/Umbraco.AI.HuggingFace/Umbraco.AI.HuggingFace.csproj"
fi

# Mistral provider
if [ -f "Umbraco.AI.Mistral/src/Umbraco.AI.Mistral/Umbraco.AI.Mistral.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Mistral/src/Umbraco.AI.Mistral/Umbraco.AI.Mistral.csproj"
fi

# TogetherAI provider
if [ -f "Umbraco.AI.TogetherAI/src/Umbraco.AI.TogetherAI/Umbraco.AI.TogetherAI.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.TogetherAI/src/Umbraco.AI.TogetherAI/Umbraco.AI.TogetherAI.csproj"
fi

# Prompt add-on (Startup + Web.StaticAssets)
if [ -f "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Startup/Umbraco.AI.Prompt.Startup.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Startup/Umbraco.AI.Prompt.Startup.csproj"
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Umbraco.AI.Prompt.Web.StaticAssets.csproj"
fi

# Agent add-on (Startup + Web.StaticAssets)
if [ -f "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Startup/Umbraco.AI.Agent.Startup.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Startup/Umbraco.AI.Agent.Startup.csproj"
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Umbraco.AI.Agent.Web.StaticAssets.csproj"
fi

# Agent UI library (frontend-only static assets)
if [ -f "Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Umbraco.AI.Agent.UI.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Umbraco.AI.Agent.UI.csproj"
fi

# Agent Copilot add-on (frontend-only static assets)
if [ -f "Umbraco.AI.Agent.Copilot/src/Umbraco.AI.Agent.Copilot/Umbraco.AI.Agent.Copilot.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Agent.Copilot/src/Umbraco.AI.Agent.Copilot/Umbraco.AI.Agent.Copilot.csproj"
fi

# Search add-on (Startup only — no Web.StaticAssets)
if [ -f "Umbraco.AI.Search/src/Umbraco.AI.Search.Startup/Umbraco.AI.Search.Startup.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Search/src/Umbraco.AI.Search.Startup/Umbraco.AI.Search.Startup.csproj"
fi

# Automate add-on (single project — no Startup separation)
if [ -f "Umbraco.AI.Automate/src/Umbraco.AI.Automate/Umbraco.AI.Automate.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.AI.Automate/src/Umbraco.AI.Automate/Umbraco.AI.Automate.csproj"
fi

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Solution: Umbraco.AI.local.slnx"
echo "Demo site: $DEMO_SITE_DIR"
echo ""
echo "Credentials:"
echo "  Email: admin@example.com"
echo "  Password: password1234"
echo ""
echo "Next steps:"
echo "  1. Open Umbraco.AI.local.slnx in your IDE"
echo "  2. Build the solution"
echo "  3. Run the Umbraco.AI.DemoSite project"
echo ""
