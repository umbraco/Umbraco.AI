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

# Check if demo already exists
if [ -d "demo" ] && [ "$FORCE" = false ]; then
    echo "Demo folder already exists. Use --force to recreate."
    echo "Or open the existing Umbraco.AI.local.sln"
    exit 0
fi

# Clean up existing demo if Force
if [ "$FORCE" = true ] && [ -d "demo" ]; then
    echo "Removing existing demo folder..."
    rm -rf "demo"
fi

if [ "$FORCE" = true ] && [ -f "Umbraco.AI.local.sln" ]; then
    rm -f "Umbraco.AI.local.sln"
fi

# Step 1: Install Umbraco templates
if [ "$SKIP_TEMPLATE_INSTALL" = false ]; then
    echo "Installing Umbraco templates..."
    dotnet new install Umbraco.Templates --force
fi

# Step 2: Create demo folder with build overrides
echo "Creating demo folder..."
mkdir -p "demo"

# Disable package validation for demo folder
cp "$SCRIPT_DIR/templates/Directory.Build.props" "demo/Directory.Build.props"

# Disable central package management for demo folder
cp "$SCRIPT_DIR/templates/Directory.Packages.props" "demo/Directory.Packages.props"

# Step 3: Create the Umbraco demo site
echo "Creating Umbraco demo site..."
pushd "demo" > /dev/null
dotnet new umbraco --force -n "Umbraco.AI.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
popd > /dev/null

# Step 3.1: Install Clean starter kit
echo "Installing Clean starter kit..."
pushd "demo/Umbraco.AI.DemoSite" > /dev/null
dotnet add package Clean
popd > /dev/null

# Step 3.2: Set fixed port for consistent development
echo "Configuring fixed port (44355)..."
mkdir -p "demo/Umbraco.AI.DemoSite/Properties"
cp "$SCRIPT_DIR/templates/launchSettings.json" "demo/Umbraco.AI.DemoSite/Properties/launchSettings.json"

# Step 3.3: Add NamedPipeListenerComposer for HTTP over named pipes
echo "Adding NamedPipeListenerComposer for HTTP over named pipes..."
mkdir -p "demo/Umbraco.AI.DemoSite/Composers"
cp "$SCRIPT_DIR/templates/NamedPipeListenerComposer.cs" "demo/Umbraco.AI.DemoSite/Composers/NamedPipeListenerComposer.cs"

# Step 4: Create unified solution
echo "Creating unified solution..."
dotnet new sln -n "Umbraco.AI.local" --force --format sln

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
            dotnet sln "Umbraco.AI.local.sln" add "$proj" --solution-folder "$solution_folder" 2>/dev/null || true
            ((count++))
        done < <(find "$src_path" -name "*.csproj" -print0)
        echo "  Added $count projects"
    fi
}

# Step 5: Add Core projects
echo "Adding Umbraco.AI (Core) projects..."
add_product_projects "Umbraco.AI" "Core"

# Step 6: Add OpenAI provider projects
echo "Adding Umbraco.AI.OpenAI projects..."
add_product_projects "Umbraco.AI.OpenAI" "OpenAI"

# Step 7: Add Prompt projects
echo "Adding Umbraco.AI.Prompt projects..."
add_product_projects "Umbraco.AI.Prompt" "Prompt"

# Step 8: Add Agent projects
echo "Adding Umbraco.AI.Agent projects..."
add_product_projects "Umbraco.AI.Agent" "Agent"

# Step 8.1: Add Agent UI projects
echo "Adding Umbraco.AI.Agent.UI projects..."
add_product_projects "Umbraco.AI.Agent.UI" "AgentUI"

# Step 8.2: Add Agent Copilot projects
echo "Adding Umbraco.AI.Agent.Copilot projects..."
add_product_projects "Umbraco.AI.Agent.Copilot" "AgentCopilot"

# Step 9: Add Anthropic provider projects
echo "Adding Umbraco.AI.Anthropic projects..."
add_product_projects "Umbraco.AI.Anthropic" "Anthropic"

# Step 9.1: Add Microsoft Foundry provider projects
echo "Adding Umbraco.AI.MicrosoftFoundry projects..."
add_product_projects "Umbraco.AI.MicrosoftFoundry" "MicrosoftFoundry"

# Step 10: Add Google provider projects
echo "Adding Umbraco.AI.Google projects..."
add_product_projects "Umbraco.AI.Google" "Google"

# Step 10.1: Add Amazon provider projects
echo "Adding Umbraco.AI.Amazon projects..."
add_product_projects "Umbraco.AI.Amazon" "Amazon"

# Step 11: Add demo site to solution
echo "Adding demo site to solution..."
dotnet sln "Umbraco.AI.local.sln" add "demo/Umbraco.AI.DemoSite/Umbraco.AI.DemoSite.csproj" --solution-folder "Demo"

# Step 13: Add project references to demo site
echo "Adding project references to demo site..."
DEMO_PROJECT="demo/Umbraco.AI.DemoSite/Umbraco.AI.DemoSite.csproj"

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

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Solution: Umbraco.AI.local.sln"
echo "Demo site: demo/Umbraco.AI.DemoSite"
echo ""
echo "Credentials:"
echo "  Email: admin@example.com"
echo "  Password: password1234"
echo ""
echo "Next steps:"
echo "  1. Open Umbraco.AI.local.sln in your IDE"
echo "  2. Build the solution"
echo "  3. Run the Umbraco.AI.DemoSite project"
echo ""
