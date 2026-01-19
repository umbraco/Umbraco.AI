# Unified Demo Site Setup Script
# Creates a shared demo site with all Umbraco.Ai products

param(
    [switch]$SkipTemplateInstall,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "=== Umbraco.Ai Unified Demo Site Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if demo already exists
if ((Test-Path "demo") -and -not $Force) {
    Write-Host "Demo folder already exists. Use -Force to recreate." -ForegroundColor Yellow
    Write-Host "Or open the existing Umbraco.Ai.local.sln" -ForegroundColor Yellow
    exit 0
}

# Clean up existing demo if Force
if ($Force -and (Test-Path "demo")) {
    Write-Host "Removing existing demo folder..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "demo"
}

if ($Force -and (Test-Path "Umbraco.Ai.local.sln")) {
    Remove-Item -Force "Umbraco.Ai.local.sln"
}

# Step 1: Install Umbraco templates
if (-not $SkipTemplateInstall) {
    Write-Host "Installing Umbraco templates..." -ForegroundColor Green
    dotnet new install Umbraco.Templates --force
}

# Step 2: Create demo folder with build overrides
Write-Host "Creating demo folder..." -ForegroundColor Green
New-Item -ItemType Directory -Path "demo" -Force | Out-Null

# Disable package validation for demo folder
$directoryBuildProps = @"
<Project>
  <PropertyGroup>
    <EnablePackageValidation>false</EnablePackageValidation>
  </PropertyGroup>
</Project>
"@
Set-Content -Path "demo\Directory.Build.props" -Value $directoryBuildProps

# Disable central package management for demo folder
$directoryPackagesProps = @"
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
"@
Set-Content -Path "demo\Directory.Packages.props" -Value $directoryPackagesProps

# Step 3: Create the Umbraco demo site
Write-Host "Creating Umbraco demo site..." -ForegroundColor Green
Push-Location "demo"
dotnet new umbraco --force -n "Umbraco.Ai.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
Pop-Location

# Step 3.1: Install Clean starter kit
Write-Host "Installing Clean starter kit..." -ForegroundColor Green
Push-Location "demo\Umbraco.Ai.DemoSite"
dotnet add package Clean
Pop-Location

# Step 3.2: Set fixed port for consistent development
Write-Host "Configuring fixed port (44355)..." -ForegroundColor Green
$launchSettings = @"
{
  "`$schema": "https://json.schemastore.org/launchsettings.json",
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
"@
$launchSettingsPath = "demo\Umbraco.Ai.DemoSite\Properties\launchSettings.json"
New-Item -ItemType Directory -Path (Split-Path $launchSettingsPath) -Force | Out-Null
Set-Content -Path $launchSettingsPath -Value $launchSettings

# Step 4: Create unified solution
Write-Host "Creating unified solution..." -ForegroundColor Green
dotnet new sln -n "Umbraco.Ai.local" --force

# Helper function to add all projects from a product's src folder
function Add-ProductProjects {
    param(
        [string]$ProductFolder,
        [string]$SolutionFolder
    )

    $srcPath = Join-Path $ProductFolder "src"
    if (Test-Path $srcPath) {
        $projects = Get-ChildItem -Path $srcPath -Filter "*.csproj" -Recurse
        foreach ($proj in $projects) {
            Write-Host "  Adding $($proj.Name)" -ForegroundColor Gray
            dotnet sln "Umbraco.Ai.local.sln" add $proj.FullName --solution-folder $SolutionFolder 2>$null
        }
        Write-Host "  Added $($projects.Count) projects" -ForegroundColor DarkGreen
    }
}

# Step 5: Add Core projects
Write-Host "Adding Umbraco.Ai (Core) projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.Ai" -SolutionFolder "Core"

# Step 6: Add OpenAI provider projects
Write-Host "Adding Umbraco.Ai.OpenAi projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.Ai.OpenAi" -SolutionFolder "OpenAi"

# Step 7: Add Prompt projects
Write-Host "Adding Umbraco.Ai.Prompt projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.Ai.Prompt" -SolutionFolder "Prompt"

# Step 8: Add Agent projects
Write-Host "Adding Umbraco.Ai.Agent projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.Ai.Agent" -SolutionFolder "Agent"

# Step 9: Add Anthropic provider projects
Write-Host "Adding Umbraco.Ai.Anthropic projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.Ai.Anthropic" -SolutionFolder "Anthropic"

# Step 10: Add Gemini provider projects
Write-Host "Adding Umbraco.Ai.Gemini projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.Ai.Gemini" -SolutionFolder "Gemini"

# Step 12: Add demo site to solution
Write-Host "Adding demo site to solution..." -ForegroundColor Green
dotnet sln "Umbraco.Ai.local.sln" add "demo\Umbraco.Ai.DemoSite\Umbraco.Ai.DemoSite.csproj" --solution-folder "Demo"

# Step 13: Add project references to demo site
Write-Host "Adding project references to demo site..." -ForegroundColor Green
$demoProject = "demo\Umbraco.Ai.DemoSite\Umbraco.Ai.DemoSite.csproj"

# Core references (meta-package + SQLite persistence)
dotnet add $demoProject reference "Umbraco.Ai\src\Umbraco.Ai\Umbraco.Ai.csproj"
dotnet add $demoProject reference "Umbraco.Ai\src\Umbraco.Ai.Persistence.Sqlite\Umbraco.Ai.Persistence.Sqlite.csproj"

# OpenAI provider
if (Test-Path "Umbraco.Ai.OpenAi\src\Umbraco.Ai.OpenAi\Umbraco.Ai.OpenAi.csproj") {
    dotnet add $demoProject reference "Umbraco.Ai.OpenAi\src\Umbraco.Ai.OpenAi\Umbraco.Ai.OpenAi.csproj"
}

# Anthropic provider
if (Test-Path "Umbraco.Ai.Anthropic\src\Umbraco.Ai.Anthropic\Umbraco.Ai.Anthropic.csproj") {
    dotnet add $demoProject reference "Umbraco.Ai.Anthropic\src\Umbraco.Ai.Anthropic\Umbraco.Ai.Anthropic.csproj"
}

# Gemini provider
if (Test-Path "Umbraco.Ai.Gemini\src\Umbraco.Ai.Gemini\Umbraco.Ai.Gemini.csproj") {
    dotnet add $demoProject reference "Umbraco.Ai.Gemini\src\Umbraco.Ai.Gemini\Umbraco.Ai.Gemini.csproj"
}

# Prompt add-on (meta-package + SQLite persistence)
if (Test-Path "Umbraco.Ai.Prompt\src\Umbraco.Ai.Prompt\Umbraco.Ai.Prompt.csproj") {
    dotnet add $demoProject reference "Umbraco.Ai.Prompt\src\Umbraco.Ai.Prompt\Umbraco.Ai.Prompt.csproj"
    dotnet add $demoProject reference "Umbraco.Ai.Prompt\src\Umbraco.Ai.Prompt.Persistence.Sqlite\Umbraco.Ai.Prompt.Persistence.Sqlite.csproj"
}

# Agent add-on (meta-package + SQLite persistence)
if (Test-Path "Umbraco.Ai.Agent\src\Umbraco.Ai.Agent\Umbraco.Ai.Agent.csproj") {
    dotnet add $demoProject reference "Umbraco.Ai.Agent\src\Umbraco.Ai.Agent\Umbraco.Ai.Agent.csproj"
    dotnet add $demoProject reference "Umbraco.Ai.Agent\src\Umbraco.Ai.Agent.Persistence.Sqlite\Umbraco.Ai.Agent.Persistence.Sqlite.csproj"
}

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Solution: Umbraco.Ai.local.sln" -ForegroundColor Cyan
Write-Host "Demo site: demo\Umbraco.Ai.DemoSite" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credentials:" -ForegroundColor Yellow
Write-Host "  Email: admin@example.com"
Write-Host "  Password: password1234"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open Umbraco.Ai.local.sln in your IDE"
Write-Host "  2. Build the solution"
Write-Host "  3. Run the Umbraco.Ai.DemoSite project"
Write-Host ""
