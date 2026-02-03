# Nightly Feed Test Site Setup Script
# Creates a fresh Umbraco site with all Umbraco.AI packages from the nightly feed

param(
    [string]$SiteName = "Umbraco.AI.NightlySite",
    [ValidateSet("nightly", "prereleases")]
    [string]$Feed = "nightly",
    [switch]$SkipTemplateInstall,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Feed URLs
$FeedUrls = @{
    "nightly" = "https://www.myget.org/F/umbraconightly/api/v3/index.json"
    "prereleases" = "https://www.myget.org/F/umbracoprereleases/api/v3/index.json"
}

$FeedUrl = $FeedUrls[$Feed]

# Determine repository root (parent of scripts folder)
$ScriptDir = $PSScriptRoot
$RepoRoot = (Resolve-Path (Split-Path -Parent $ScriptDir)).Path

# Change to repository root to ensure consistent behavior
Push-Location $RepoRoot

Write-Host "=== Umbraco.AI Feed Test Site Setup ===" -ForegroundColor Cyan
Write-Host "Working directory: $RepoRoot" -ForegroundColor Gray
Write-Host "Feed: $Feed ($FeedUrl)" -ForegroundColor Gray
Write-Host "Site name: $SiteName" -ForegroundColor Gray
Write-Host ""

# Check if specific site folder already exists
$sitePath = "demo\$SiteName"
if ((Test-Path $sitePath) -and -not $Force) {
    Write-Host "Site folder '$sitePath' already exists. Use -Force to recreate." -ForegroundColor Yellow
    exit 0
}

# Clean up existing site if Force
if ($Force -and (Test-Path $sitePath)) {
    Write-Host "Removing existing site folder '$sitePath'..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $sitePath
}

# Step 1: Install Umbraco templates
if (-not $SkipTemplateInstall) {
    Write-Host "Installing Umbraco templates..." -ForegroundColor Green
    dotnet new install Umbraco.Templates --force
}

# Step 2: Create demo folder
Write-Host "Creating demo folder..." -ForegroundColor Green
New-Item -ItemType Directory -Path "demo" -Force | Out-Null

# Step 3: Create the Umbraco site
Write-Host "Creating Umbraco site '$SiteName'..." -ForegroundColor Green
Push-Location "demo"
dotnet new umbraco --force -n $SiteName --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
Pop-Location

# Step 4: Install Clean starter kit
Write-Host "Installing Clean starter kit..." -ForegroundColor Green
Push-Location "demo\$SiteName"
dotnet add package Clean
Pop-Location

# Step 5: Configure NuGet sources and PackageSourceMapping
Write-Host "Configuring NuGet sources and package routing..." -ForegroundColor Green
Push-Location "demo\$SiteName"

# Determine feed name
$feedName = if ($Feed -eq "nightly") { "UmbracoNightly" } else { "UmbracoPreReleases" }

# Create complete nuget.config with sources and PackageSourceMapping
$nugetConfig = "nuget.config"
Write-Host "Creating nuget.config with package source mapping..." -ForegroundColor Gray

$configContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="$feedName" value="$FeedUrl" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="$feedName">
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
"@

$configContent | Out-File -FilePath $nugetConfig -Encoding utf8 -Force

Pop-Location

# Step 6: Install Umbraco.AI packages from feed
Write-Host "Installing Umbraco.AI packages from $Feed feed..." -ForegroundColor Green
Push-Location "demo\$SiteName"

# Install Core first to establish the version baseline
Write-Host "  Installing Umbraco.AI.Core..." -ForegroundColor Gray
dotnet add package Umbraco.AI.Core --source $FeedUrl --prerelease

# Core meta-package (includes Startup + Web.StaticAssets)
Write-Host "  Installing Umbraco.AI..." -ForegroundColor Gray
dotnet add package Umbraco.AI --source $FeedUrl --prerelease

# Provider packages
Write-Host "  Installing Umbraco.AI.OpenAI..." -ForegroundColor Gray
dotnet add package Umbraco.AI.OpenAI --source $FeedUrl --prerelease

Write-Host "  Installing Umbraco.AI.Anthropic..." -ForegroundColor Gray
dotnet add package Umbraco.AI.Anthropic --source $FeedUrl --prerelease

Write-Host "  Installing Umbraco.AI.Google..." -ForegroundColor Gray
dotnet add package Umbraco.AI.Google --source $FeedUrl --prerelease

Write-Host "  Installing Umbraco.AI.Amazon..." -ForegroundColor Gray
dotnet add package Umbraco.AI.Amazon --source $FeedUrl --prerelease

Write-Host "  Installing Umbraco.AI.MicrosoftFoundry..." -ForegroundColor Gray
dotnet add package Umbraco.AI.MicrosoftFoundry --source $FeedUrl --prerelease

# Add-on packages (includes Startup + Web.StaticAssets)
Write-Host "  Installing Umbraco.AI.Prompt..." -ForegroundColor Gray
dotnet add package Umbraco.AI.Prompt --source $FeedUrl --prerelease

Write-Host "  Installing Umbraco.AI.Agent..." -ForegroundColor Gray
dotnet add package Umbraco.AI.Agent --source $FeedUrl --prerelease

# Agent Copilot (frontend-only static assets)
Write-Host "  Installing Umbraco.AI.Agent.Copilot..." -ForegroundColor Gray
dotnet add package Umbraco.AI.Agent.Copilot --source $FeedUrl --prerelease

Pop-Location

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Site location: demo\$SiteName" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credentials:" -ForegroundColor Yellow
Write-Host "  Email: admin@example.com"
Write-Host "  Password: password1234"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. cd demo\$SiteName"
Write-Host "  2. dotnet run"
Write-Host "  3. Open https://localhost:44355 in your browser"
Write-Host ""
Write-Host "Note: All packages were installed from the $Feed feed:" -ForegroundColor Gray
Write-Host "  $FeedUrl" -ForegroundColor Gray
Write-Host ""

# Restore original directory
Pop-Location
