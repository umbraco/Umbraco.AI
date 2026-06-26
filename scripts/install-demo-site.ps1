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

# Toolchain check — required Node version comes from package.json's engines.node, so this
# stays in lockstep with the npm-side enforcement and the .nvmrc.
$packageJson = Get-Content (Join-Path $RepoRoot "package.json") -Raw | ConvertFrom-Json
$requiredNodeRange = $packageJson.engines.node
if ($requiredNodeRange -match '(\d+)') {
    $requiredNodeMajor = [int]$matches[1]
} else {
    Write-Host "ERROR: Could not parse engines.node ('$requiredNodeRange') from package.json." -ForegroundColor Red
    exit 1
}

$nodeVersionRaw = (node --version 2>$null) -replace '^v', ''
if (-not $nodeVersionRaw) {
    Write-Host "ERROR: Node.js is not installed or not on PATH. package.json requires '$requiredNodeRange'." -ForegroundColor Red
    Write-Host "Install Node $requiredNodeMajor+ (e.g. 'nvm install $requiredNodeMajor && nvm use $requiredNodeMajor') and re-run." -ForegroundColor Yellow
    exit 1
}
$nodeMajor = [int]($nodeVersionRaw -split '\.')[0]
if ($nodeMajor -lt $requiredNodeMajor) {
    Write-Host "ERROR: Node $nodeVersionRaw detected; package.json requires '$requiredNodeRange'." -ForegroundColor Red
    Write-Host "Run 'nvm install $requiredNodeMajor && nvm use $requiredNodeMajor' (or equivalent) before re-running this script." -ForegroundColor Yellow
    exit 1
}
Write-Host "Node $nodeVersionRaw detected (satisfies '$requiredNodeRange')." -ForegroundColor Gray
Write-Host ""

# Detect template version and major from Directory.Packages.props.
# The lower bound of the Umbraco.Cms.Core range is the minimum CMS version this branch supports
# and therefore the right template version to scaffold the demo site against.
$packagesPropsPath = Join-Path $RepoRoot "Directory.Packages.props"
if (-not (Test-Path $packagesPropsPath)) {
    Write-Host "ERROR: Could not find $packagesPropsPath" -ForegroundColor Red
    exit 1
}
$packagesContent = Get-Content $packagesPropsPath -Raw
if (-not ($packagesContent -match 'Include="Umbraco\.Cms\.Core" Version="\[([^,]+),')) {
    Write-Host "ERROR: Could not find Umbraco.Cms.Core version range in $packagesPropsPath" -ForegroundColor Red
    exit 1
}
$TemplateVersion = $matches[1]
$VersionMajor = [int]($TemplateVersion -split '\.')[0]
$IsTemplatePrerelease = $TemplateVersion -match '-'
Write-Host "Target Umbraco.Cms template version: $TemplateVersion (v$VersionMajor)" -ForegroundColor Gray
Write-Host ""

# Versioned demo directory: demos/vN/
$DemoDir = "demos\v$VersionMajor"
$DemoSiteDir = "$DemoDir\Umbraco.AI.DemoSite"

# Check if demo already exists
if ((Test-Path $DemoDir) -and -not $Force) {
    Write-Host "Demo folder '$DemoDir' already exists. Use -Force to recreate." -ForegroundColor Yellow
    Write-Host "Or open the existing Umbraco.AI.local.slnx" -ForegroundColor Yellow
    exit 0
}

# Clean up existing demo if Force
if ($Force -and (Test-Path $DemoDir)) {
    Write-Host "Removing existing demo folder '$DemoDir'..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $DemoDir
}

if ($Force -and (Test-Path "Umbraco.AI.local.slnx")) {
    Remove-Item -Force "Umbraco.AI.local.slnx"
}

# Step 1: Install Umbraco templates
if (-not $SkipTemplateInstall) {
    Write-Host "Installing Umbraco templates ($TemplateVersion)..." -ForegroundColor Green

    # Uninstall all existing versions to avoid conflicts
    Write-Host "Removing any existing Umbraco.Templates installations..." -ForegroundColor Gray
    $installedTemplates = dotnet new uninstall 2>&1 | Out-String
    if ($installedTemplates -match "Umbraco\.Templates") {
        # Extract all unique Umbraco.Templates entries
        $templateLines = $installedTemplates -split "`n" | Where-Object { $_ -match "Umbraco\.Templates" }
        foreach ($line in $templateLines) {
            if ($line -match "Umbraco\.Templates") {
                try {
                    dotnet new uninstall Umbraco.Templates 2>&1 | Out-Null
                } catch {
                    # Ignore errors during uninstall
                }
            }
        }
    }

    if ($IsTemplatePrerelease) {
        # Prerelease templates require the umbracoprereleases MyGet feed to be configured.
        # If not yet configured: dotnet nuget add source https://www.myget.org/F/umbracoprereleases/api/v3/index.json --name UmbracoPreReleases
        Write-Host "NOTE: Prerelease template ($TemplateVersion) requires the umbracoprereleases MyGet source." -ForegroundColor Yellow
    }
    dotnet new install "Umbraco.Templates::$TemplateVersion" --force
}

# Step 2: Create demo folder with build overrides
Write-Host "Creating demo folder '$DemoDir'..." -ForegroundColor Green
New-Item -ItemType Directory -Path $DemoDir -Force | Out-Null

# Disable package validation for demo folder
$directoryBuildPropsSource = Join-Path $ScriptDir "templates\Directory.Build.props"
Copy-Item -Path $directoryBuildPropsSource -Destination "$DemoDir\Directory.Build.props" -Force

# Disable central package management for demo folder
$directoryPackagesPropsSource = Join-Path $ScriptDir "templates\Directory.Packages.props"
Copy-Item -Path $directoryPackagesPropsSource -Destination "$DemoDir\Directory.Packages.props" -Force

# Step 3: Create the Umbraco demo site
Write-Host "Creating Umbraco demo site..." -ForegroundColor Green
Push-Location $DemoDir
dotnet new umbraco --force -n "Umbraco.AI.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
Pop-Location

# Step 3.1: Install Clean starter kit
Write-Host "Installing Clean starter kit..." -ForegroundColor Green
Push-Location $DemoSiteDir
dotnet add package Clean
Pop-Location

# Step 3.2: Set fixed port for consistent development
Write-Host "Configuring fixed port (44355)..." -ForegroundColor Green
$launchSettingsSource = Join-Path $ScriptDir "templates\launchSettings.json"
$launchSettingsPath = "$DemoSiteDir\Properties\launchSettings.json"
New-Item -ItemType Directory -Path (Split-Path $launchSettingsPath) -Force | Out-Null
Copy-Item -Path $launchSettingsSource -Destination $launchSettingsPath -Force

# Step 3.3: Add NamedPipeListenerComposer for HTTP over named pipes
Write-Host "Adding NamedPipeListenerComposer for HTTP over named pipes..." -ForegroundColor Green
$composerSourcePath = Join-Path $ScriptDir "templates\NamedPipeListenerComposer.cs"
$composerDestPath = "$DemoSiteDir\Composers\NamedPipeListenerComposer.cs"
New-Item -ItemType Directory -Path (Split-Path $composerDestPath) -Force | Out-Null
Copy-Item -Path $composerSourcePath -Destination $composerDestPath -Force

# Step 4: Create unified solution
Write-Host "Creating unified solution..." -ForegroundColor Green
dotnet new sln -n "Umbraco.AI.local" --force --format slnx

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
            dotnet sln "Umbraco.AI.local.slnx" add $proj.FullName --solution-folder $SolutionFolder 2>$null
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

# Step 9.05: Add DeepSeek provider projects
Write-Host "Adding Umbraco.AI.DeepSeek projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.DeepSeek" -SolutionFolder "DeepSeek"

# Step 9.1: Add Microsoft Foundry provider projects
Write-Host "Adding Umbraco.AI.MicrosoftFoundry projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.MicrosoftFoundry" -SolutionFolder "MicrosoftFoundry"

# Step 10: Add Google provider projects
Write-Host "Adding Umbraco.AI.Google projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Google" -SolutionFolder "Google"

# Step 10.1: Add Amazon provider projects
Write-Host "Adding Umbraco.AI.Amazon projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Amazon" -SolutionFolder "Amazon"

# Step 10.1.1: Add FireworksAI provider projects
Write-Host "Adding Umbraco.AI.FireworksAI projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.FireworksAI" -SolutionFolder "FireworksAI"

# Step 10.1.2: Add HuggingFace provider projects
Write-Host "Adding Umbraco.AI.HuggingFace projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.HuggingFace" -SolutionFolder "HuggingFace"

# Step 10.1.3: Add Mistral provider projects
Write-Host "Adding Umbraco.AI.Mistral projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Mistral" -SolutionFolder "Mistral"

# Step 10.1.4: Add TogetherAI provider projects
Write-Host "Adding Umbraco.AI.TogetherAI projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.TogetherAI" -SolutionFolder "TogetherAI"

# Step 10.2: Add Search projects
Write-Host "Adding Umbraco.AI.Search projects..." -ForegroundColor Green
Add-ProductProjects -ProductFolder "Umbraco.AI.Search" -SolutionFolder "Search"

# Step 11: Add demo site to solution
Write-Host "Adding demo site to solution..." -ForegroundColor Green
dotnet sln "Umbraco.AI.local.slnx" add "$DemoSiteDir/Umbraco.AI.DemoSite.csproj" --solution-folder "Demo"

# Step 13: Add project references to demo site
Write-Host "Adding project references to demo site..." -ForegroundColor Green
$demoProject = "$DemoSiteDir/Umbraco.AI.DemoSite.csproj"

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

# DeepSeek provider
if (Test-Path "Umbraco.AI.DeepSeek/src/Umbraco.AI.DeepSeek/Umbraco.AI.DeepSeek.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.DeepSeek/src/Umbraco.AI.DeepSeek/Umbraco.AI.DeepSeek.csproj"
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

# FireworksAI provider
if (Test-Path "Umbraco.AI.FireworksAI/src/Umbraco.AI.FireworksAI/Umbraco.AI.FireworksAI.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.FireworksAI/src/Umbraco.AI.FireworksAI/Umbraco.AI.FireworksAI.csproj"
}

# HuggingFace provider
if (Test-Path "Umbraco.AI.HuggingFace/src/Umbraco.AI.HuggingFace/Umbraco.AI.HuggingFace.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.HuggingFace/src/Umbraco.AI.HuggingFace/Umbraco.AI.HuggingFace.csproj"
}

# Mistral provider
if (Test-Path "Umbraco.AI.Mistral/src/Umbraco.AI.Mistral/Umbraco.AI.Mistral.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Mistral/src/Umbraco.AI.Mistral/Umbraco.AI.Mistral.csproj"
}

# TogetherAI provider
if (Test-Path "Umbraco.AI.TogetherAI/src/Umbraco.AI.TogetherAI/Umbraco.AI.TogetherAI.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.TogetherAI/src/Umbraco.AI.TogetherAI/Umbraco.AI.TogetherAI.csproj"
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

# Search add-on (Startup only — no Web.StaticAssets)
if (Test-Path "Umbraco.AI.Search\src\Umbraco.AI.Search.Startup\Umbraco.AI.Search.Startup.csproj") {
    dotnet add $demoProject reference "Umbraco.AI.Search\src\Umbraco.AI.Search.Startup\Umbraco.AI.Search.Startup.csproj"
}

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Solution: Umbraco.AI.local.slnx" -ForegroundColor Cyan
Write-Host "Demo site: $DemoSiteDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credentials:" -ForegroundColor Yellow
Write-Host "  Email: admin@example.com"
Write-Host "  Password: password1234"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open Umbraco.AI.local.slnx in your IDE"
Write-Host "  2. Build the solution"
Write-Host "  3. Run the Umbraco.AI.DemoSite project"
Write-Host ""

# Restore original directory
Pop-Location
