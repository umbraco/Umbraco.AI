# Unified Demo Site Setup Script
# Creates a shared demo site with all Umbraco.AI products

param(
    [switch]$SkipTemplateInstall,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Determine repository root (parent of scripts folder)
$ScriptDir = $PSScriptRoot
$RepoRoot = (Resolve-Path (Split-Path -Parent $ScriptDir)).Path

# Change to repository root to ensure consistent behavior
Push-Location $RepoRoot

Write-Host "=== Umbraco.AI Unified Demo Site Setup ===" -ForegroundColor Cyan
Write-Host "Working directory: $RepoRoot" -ForegroundColor Gray
Write-Host ""

# Check if demo already exists
if ((Test-Path "demo") -and -not $Force) {
    Write-Host "Demo folder already exists. Use -Force to recreate." -ForegroundColor Yellow
    Write-Host "Or open the existing Umbraco.AI.local.sln" -ForegroundColor Yellow
    exit 0
}

# Clean up existing demo if Force
if ($Force -and (Test-Path "demo")) {
    Write-Host "Removing existing demo folder..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "demo"
}

if ($Force -and (Test-Path "Umbraco.AI.local.sln")) {
    Remove-Item -Force "Umbraco.AI.local.sln"
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
$directoryBuildPropsSource = Join-Path $ScriptDir "templates\Directory.Build.props"
Copy-Item -Path $directoryBuildPropsSource -Destination "demo\Directory.Build.props" -Force

# Disable central package management for demo folder
$directoryPackagesPropsSource = Join-Path $ScriptDir "templates\Directory.Packages.props"
Copy-Item -Path $directoryPackagesPropsSource -Destination "demo\Directory.Packages.props" -Force

# Step 3: Create the Umbraco demo site
Write-Host "Creating Umbraco demo site..." -ForegroundColor Green
Push-Location "demo"
dotnet new umbraco --force -n "Umbraco.AI.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
Pop-Location

# Step 3.1: Install Clean starter kit
Write-Host "Installing Clean starter kit..." -ForegroundColor Green
Push-Location "demo\Umbraco.AI.DemoSite"
dotnet add package Clean
Pop-Location

# Step 3.2: Set fixed port for consistent development
Write-Host "Configuring fixed port (44355)..." -ForegroundColor Green
$launchSettingsSource = Join-Path $ScriptDir "templates\launchSettings.json"
$launchSettingsPath = "demo\Umbraco.AI.DemoSite\Properties\launchSettings.json"
New-Item -ItemType Directory -Path (Split-Path $launchSettingsPath) -Force | Out-Null
Copy-Item -Path $launchSettingsSource -Destination $launchSettingsPath -Force

# Step 3.3: Add NamedPipeListenerComposer for HTTP over named pipes
Write-Host "Adding NamedPipeListenerComposer for HTTP over named pipes..." -ForegroundColor Green
$composerSourcePath = Join-Path $ScriptDir "templates\NamedPipeListenerComposer.cs"
$composerDestPath = "demo\Umbraco.AI.DemoSite\Composers\NamedPipeListenerComposer.cs"
New-Item -ItemType Directory -Path (Split-Path $composerDestPath) -Force | Out-Null
Copy-Item -Path $composerSourcePath -Destination $composerDestPath -Force

# Step 4: Create unified solution
Write-Host "Creating unified solution..." -ForegroundColor Green
dotnet new sln -n "Umbraco.AI.local" --force --format sln

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
            dotnet sln "Umbraco.AI.local.sln" add $proj.FullName --solution-folder $SolutionFolder 2>$null
        }
        Write-Host "  Added $($projects.Count) projects" -ForegroundColor DarkGreen
    }
}

# Step 5: Add Core projects
Write-Host "Adding Umbraco.AI (Core) projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI" -SolutionFolder "Core"

# Step 6: Add OpenAI provider projects
Write-Host "Adding Umbraco.AI.OpenAI projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.OpenAI" -SolutionFolder "OpenAI"

# Step 7: Add Prompt projects
Write-Host "Adding Umbraco.AI.Prompt projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Prompt" -SolutionFolder "Prompt"

# Step 8: Add Agent projects
Write-Host "Adding Umbraco.AI.Agent projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Agent" -SolutionFolder "Agent"

# Step 8.1: Add Agent UI projects
Write-Host "Adding Umbraco.AI.Agent.UI projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Agent.UI" -SolutionFolder "AgentUI"

# Step 8.2: Add Agent Copilot projects
Write-Host "Adding Umbraco.AI.Agent.Copilot projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Agent.Copilot" -SolutionFolder "AgentCopilot"

# Step 9: Add Anthropic provider projects
Write-Host "Adding Umbraco.AI.Anthropic projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Anthropic" -SolutionFolder "Anthropic"

# Step 9.1: Add Microsoft Foundry provider projects
Write-Host "Adding Umbraco.AI.MicrosoftFoundry projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.MicrosoftFoundry" -SolutionFolder "MicrosoftFoundry"

# Step 10: Add Google provider projects
Write-Host "Adding Umbraco.AI.Google projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Google" -SolutionFolder "Google"

# Step 10.1: Add Amazon provider projects
Write-Host "Adding Umbraco.AI.Amazon projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Amazon" -SolutionFolder "Amazon"

# Step 11: Add demo site to solution
Write-Host "Adding demo site to solution..." -ForegroundColor Green
dotnet sln "Umbraco.AI.local.sln" add "demo/Umbraco.AI.DemoSite/Umbraco.AI.DemoSite.csproj" --solution-folder "Demo"

# Step 13: Add project references to demo site
Write-Host "Adding project references to demo site..." -ForegroundColor Green
$demoProject = "demo/Umbraco.AI.DemoSite/Umbraco.AI.DemoSite.csproj"

# Core references (Startup + Web.StaticAssets)
dotnet add $demoProject reference "Umbraco.AI/src/Umbraco.AI.Startup/Umbraco.AI.Startup.csproj"
dotnet add $demoProject reference "Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Umbraco.AI.Web.StaticAssets.csproj"

# OpenAI provider
if (Test-Path "Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/Umbraco.AI.OpenAI.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/Umbraco.AI.OpenAI.csproj"
}

# Anthropic provider
if (Test-Path "Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/Umbraco.AI.Anthropic.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/Umbraco.AI.Anthropic.csproj"
}

# Microsoft Foundry provider
if (Test-Path "Umbraco.AI.MicrosoftFoundry/src/Umbraco.AI.MicrosoftFoundry/Umbraco.AI.MicrosoftFoundry.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.MicrosoftFoundry/src/Umbraco.AI.MicrosoftFoundry/Umbraco.AI.MicrosoftFoundry.csproj"
}

# Google provider
if (Test-Path "Umbraco.AI.Google/src/Umbraco.AI.Google/Umbraco.AI.Google.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Google/src/Umbraco.AI.Google/Umbraco.AI.Google.csproj"
}

# Amazon provider
if (Test-Path "Umbraco.AI.Amazon/src/Umbraco.AI.Amazon/Umbraco.AI.Amazon.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Amazon/src/Umbraco.AI.Amazon/Umbraco.AI.Amazon.csproj"
}

# Prompt add-on (Startup + Web.StaticAssets)
if (Test-Path "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Startup/Umbraco.AI.Prompt.Startup.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Startup/Umbraco.AI.Prompt.Startup.csproj"
    dotnet add $demoProject reference "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Umbraco.AI.Prompt.Web.StaticAssets.csproj"
}

# Agent add-on (Startup + Web.StaticAssets)
if (Test-Path "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Startup/Umbraco.AI.Agent.Startup.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Startup/Umbraco.AI.Agent.Startup.csproj"
    dotnet add $demoProject reference "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Umbraco.AI.Agent.Web.StaticAssets.csproj"
}

# Agent UI library (frontend-only static assets)
if (Test-Path "Umbraco.AI.Agent.UI\src\Umbraco.AI.Agent.UI\Umbraco.AI.Agent.UI.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Agent.UI\src\Umbraco.AI.Agent.UI\Umbraco.AI.Agent.UI.csproj"
}

# Agent Copilot add-on (frontend-only static assets)
if (Test-Path "Umbraco.AI.Agent.Copilot\src\Umbraco.AI.Agent.Copilot\Umbraco.AI.Agent.Copilot.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Agent.Copilot\src\Umbraco.AI.Agent.Copilot\Umbraco.AI.Agent.Copilot.csproj"
}

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Solution: Umbraco.AI.local.sln" -ForegroundColor Cyan
Write-Host "Demo site: demo/Umbraco.AI.DemoSite" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credentials:" -ForegroundColor Yellow
Write-Host "  Email: admin@example.com"
Write-Host "  Password: password1234"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open Umbraco.AI.local.sln in your IDE"
Write-Host "  2. Build the solution"
Write-Host "  3. Run the Umbraco.AI.DemoSite project"
Write-Host ""

# Restore original directory
Pop-Location
