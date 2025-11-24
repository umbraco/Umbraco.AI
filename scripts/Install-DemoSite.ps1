# Ensure we have the latest Umbraco templates
dotnet new install Umbraco.Templates --force

# Create demo folder if it doesn't exist
if (-not (Test-Path "demo")) {
    New-Item -ItemType Directory -Path "demo"
}

# Navigate to demo folder
Push-Location "demo"

# Create Directory.Build.props to disable package validation for demo folder
$directoryBuildProps = @"
<Project>
  <PropertyGroup>
    <EnablePackageValidation>false</EnablePackageValidation>
  </PropertyGroup>
</Project>
"@
Set-Content -Path "Directory.Build.props" -Value $directoryBuildProps

# Create Directory.Packages.props to disable central package management
$directoryPackagesProps = @"
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
"@
Set-Content -Path "Directory.Packages.props" -Value $directoryPackagesProps

# Create the Umbraco demo site
dotnet new umbraco --force -n "Umbraco.Ai.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite

# Add starter kit
# dotnet add "Umbraco.Ai.DemoSite" package clean

# Navigate back to root
Pop-Location

# Copy the solution file and rename it
Copy-Item "Umbraco.Ai.sln" "Umbraco.Ai.local.sln"

# Add the demo project to the local solution in a Demo solution folder
dotnet sln "Umbraco.Ai.local.sln" add "demo\Umbraco.Ai.DemoSite\Umbraco.Ai.DemoSite.csproj" --solution-folder "Demo"

# Add project references to the demo site
dotnet add "demo\Umbraco.Ai.DemoSite\Umbraco.Ai.DemoSite.csproj" reference "src\Umbraco.Ai\Umbraco.Ai.csproj"

Write-Host "Demo site setup complete!" -ForegroundColor Green
Write-Host "Solution: Umbraco.Ai.local.sln" -ForegroundColor Cyan
Write-Host "Demo site: demo\Umbraco.Ai.DemoSite" -ForegroundColor Cyan