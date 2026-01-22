#!/bin/bash
# Unified Demo Site Setup Script
# Creates a shared demo site with all Umbraco.Ai products

set -e

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
echo "Umbraco.Ai Unified Demo Site Setup"
echo "========================================="
echo ""

# Check if demo already exists
if [ -d "demo" ] && [ "$FORCE" = false ]; then
    echo "Demo folder already exists. Use --force to recreate."
    echo "Or open the existing Umbraco.Ai.local.sln"
    exit 0
fi

# Clean up existing demo if Force
if [ "$FORCE" = true ] && [ -d "demo" ]; then
    echo "Removing existing demo folder..."
    rm -rf "demo"
fi

if [ "$FORCE" = true ] && [ -f "Umbraco.Ai.local.sln" ]; then
    rm -f "Umbraco.Ai.local.sln"
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
cat > "demo/Directory.Build.props" << 'EOF'
<Project>
  <PropertyGroup>
    <EnablePackageValidation>false</EnablePackageValidation>
  </PropertyGroup>
</Project>
EOF

# Disable central package management for demo folder
cat > "demo/Directory.Packages.props" << 'EOF'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
EOF

# Step 3: Create the Umbraco demo site
echo "Creating Umbraco demo site..."
pushd "demo" > /dev/null
dotnet new umbraco --force -n "Umbraco.Ai.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
popd > /dev/null

# Step 3.1: Install Clean starter kit
echo "Installing Clean starter kit..."
pushd "demo/Umbraco.Ai.DemoSite" > /dev/null
dotnet add package Clean
popd > /dev/null

# Step 3.2: Set fixed port for consistent development
echo "Configuring fixed port (44355)..."
mkdir -p "demo/Umbraco.Ai.DemoSite/Properties"
cat > "demo/Umbraco.Ai.DemoSite/Properties/launchSettings.json" << 'EOF'
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "Umbraco.Ai.DemoSite": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:44355",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
EOF

# Step 4: Create unified solution
echo "Creating unified solution..."
dotnet new sln -n "Umbraco.Ai.local" --force

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
            dotnet sln "Umbraco.Ai.local.sln" add "$proj" --solution-folder "$solution_folder" 2>/dev/null || true
            ((count++))
        done < <(find "$src_path" -name "*.csproj" -print0)
        echo "  Added $count projects"
    fi
}

# Step 5: Add Core projects
echo "Adding Umbraco.Ai (Core) projects..."
add_product_projects "Umbraco.Ai" "Core"

# Step 6: Add OpenAI provider projects
echo "Adding Umbraco.Ai.OpenAi projects..."
add_product_projects "Umbraco.Ai.OpenAi" "OpenAi"

# Step 7: Add Prompt projects
echo "Adding Umbraco.Ai.Prompt projects..."
add_product_projects "Umbraco.Ai.Prompt" "Prompt"

# Step 8: Add Agent projects
echo "Adding Umbraco.Ai.Agent projects..."
add_product_projects "Umbraco.Ai.Agent" "Agent"

# Step 9: Add Anthropic provider projects
echo "Adding Umbraco.Ai.Anthropic projects..."
add_product_projects "Umbraco.Ai.Anthropic" "Anthropic"

# Step 9.1: Add Microsoft Foundry provider projects
echo "Adding Umbraco.Ai.MicrosoftFoundry projects..."
add_product_projects "Umbraco.Ai.MicrosoftFoundry" "MicrosoftFoundry"

# Step 10: Add demo site to solution
echo "Adding demo site to solution..."
dotnet sln "Umbraco.Ai.local.sln" add "demo/Umbraco.Ai.DemoSite/Umbraco.Ai.DemoSite.csproj" --solution-folder "Demo"

# Step 11: Add project references to demo site
echo "Adding project references to demo site..."
DEMO_PROJECT="demo/Umbraco.Ai.DemoSite/Umbraco.Ai.DemoSite.csproj"

# Core references (meta-package + SQLite persistence)
dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai/src/Umbraco.Ai/Umbraco.Ai.csproj"
dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai/src/Umbraco.Ai.Persistence.Sqlite/Umbraco.Ai.Persistence.Sqlite.csproj"

# OpenAI provider
if [ -f "Umbraco.Ai.OpenAi/src/Umbraco.Ai.OpenAi/Umbraco.Ai.OpenAi.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai.OpenAi/src/Umbraco.Ai.OpenAi/Umbraco.Ai.OpenAi.csproj"
fi

# Anthropic provider
if [ -f "Umbraco.Ai.Anthropic/src/Umbraco.Ai.Anthropic/Umbraco.Ai.Anthropic.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai.Anthropic/src/Umbraco.Ai.Anthropic/Umbraco.Ai.Anthropic.csproj"
fi

# Microsoft Foundry provider
if [ -f "Umbraco.Ai.MicrosoftFoundry/src/Umbraco.Ai.MicrosoftFoundry/Umbraco.Ai.MicrosoftFoundry.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai.MicrosoftFoundry/src/Umbraco.Ai.MicrosoftFoundry/Umbraco.Ai.MicrosoftFoundry.csproj"
fi

# Prompt add-on (meta-package + SQLite persistence)
if [ -f "Umbraco.Ai.Prompt/src/Umbraco.Ai.Prompt/Umbraco.Ai.Prompt.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai.Prompt/src/Umbraco.Ai.Prompt/Umbraco.Ai.Prompt.csproj"
    dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai.Prompt/src/Umbraco.Ai.Prompt.Persistence.Sqlite/Umbraco.Ai.Prompt.Persistence.Sqlite.csproj"
fi

# Agent add-on (meta-package + SQLite persistence)
if [ -f "Umbraco.Ai.Agent/src/Umbraco.Ai.Agent/Umbraco.Ai.Agent.csproj" ]; then
    dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai.Agent/src/Umbraco.Ai.Agent/Umbraco.Ai.Agent.csproj"
    dotnet add "$DEMO_PROJECT" reference "Umbraco.Ai.Agent/src/Umbraco.Ai.Agent.Persistence.Sqlite/Umbraco.Ai.Agent.Persistence.Sqlite.csproj"
fi

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Solution: Umbraco.Ai.local.sln"
echo "Demo site: demo/Umbraco.Ai.DemoSite"
echo ""
echo "Credentials:"
echo "  Email: admin@example.com"
echo "  Password: password1234"
echo ""
echo "Next steps:"
echo "  1. Open Umbraco.Ai.local.sln in your IDE"
echo "  2. Build the solution"
echo "  3. Run the Umbraco.Ai.DemoSite project"
echo ""
