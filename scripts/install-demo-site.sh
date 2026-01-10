#!/bin/bash
set -e

# Cross-platform demo site installer for Umbraco.Ai
# Works on macOS, Linux, and Windows (via Git Bash)

echo "Installing Umbraco templates..."
dotnet new install Umbraco.Templates --force

# Create demo folder if it doesn't exist
if [ ! -d "demo" ]; then
    mkdir -p demo
fi

# Create Directory.Build.props to disable package validation for demo folder
cat > demo/Directory.Build.props << 'EOF'
<Project>
  <PropertyGroup>
    <EnablePackageValidation>false</EnablePackageValidation>
  </PropertyGroup>
</Project>
EOF

# Create Directory.Packages.props to disable central package management
cat > demo/Directory.Packages.props << 'EOF'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
EOF

# Create the Umbraco demo site
echo "Creating Umbraco demo site..."
cd demo
dotnet new umbraco --force -n "Umbraco.Ai.DemoSite" \
    --friendly-name "Administrator" \
    --email "admin@example.com" \
    --password "password1234" \
    --development-database-type SQLite
cd ..

# Copy the solution file and rename it
cp Umbraco.Ai.sln Umbraco.Ai.local.sln

# Add the demo project to the local solution in a Demo solution folder
dotnet sln Umbraco.Ai.local.sln add demo/Umbraco.Ai.DemoSite/Umbraco.Ai.DemoSite.csproj --solution-folder Demo

# Add project references to the demo site
dotnet add demo/Umbraco.Ai.DemoSite/Umbraco.Ai.DemoSite.csproj reference src/Umbraco.Ai/Umbraco.Ai.csproj

echo ""
echo "Demo site setup complete!"
echo "Solution: Umbraco.Ai.local.sln"
echo "Demo site: demo/Umbraco.Ai.DemoSite"
