<#
.SYNOPSIS
    Builds and packages all Umbraco.Ai NuGet packages for distribution.

.DESCRIPTION
    Builds all Umbraco.Ai products (Core, Agent, Prompt, OpenAI, Anthropic) for distribution as standalone NuGet packages.
    Uses package references instead of project references via -p:UseProjectReferences=false.

.PARAMETER Configuration
    The build configuration. Defaults to "Release".

.EXAMPLE
    .\Build-Distribution.ps1
#>

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$RootDir = $PSScriptRoot
$OutputPath = Join-Path $RootDir "dist"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Umbraco.Ai Distribution Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean and create output directory
if (Test-Path $OutputPath) {
    Write-Host "Cleaning existing dist folder..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputPath | Out-Null
$PackagesDir = Join-Path $OutputPath "nupkg"
New-Item -ItemType Directory -Path $PackagesDir | Out-Null

# Define projects to build
$Projects = @(
    @{
        Name = "Umbraco.Ai"
        Path = Join-Path $RootDir "Umbraco.Ai"
        Solution = "Umbraco.Ai.sln"
        FrontendPath = "src\Umbraco.Ai.Web.StaticAssets\Client"
    },
    @{
        Name = "Umbraco.Ai.Agent"
        Path = Join-Path $RootDir "Umbraco.Ai.Agent"
        Solution = "Umbraco.Ai.Agent.sln"
        FrontendPath = "src\Umbraco.Ai.Agent.Web.StaticAssets\Client"
    },
    @{
        Name = "Umbraco.Ai.Prompt"
        Path = Join-Path $RootDir "Umbraco.Ai.Prompt"
        Solution = "Umbraco.Ai.Prompt.sln"
        FrontendPath = "src\Umbraco.Ai.Prompt.Web.StaticAssets\Client"
    },
    @{
        Name = "Umbraco.Ai.OpenAi"
        Path = Join-Path $RootDir "Umbraco.Ai.OpenAi"
        Solution = "Umbraco.Ai.OpenAi.sln"
        FrontendPath = $null
    },
    @{
        Name = "Umbraco.Ai.Anthropic"
        Path = Join-Path $RootDir "Umbraco.Ai.Anthropic"
        Solution = "Umbraco.Ai.Anthropic.sln"
        FrontendPath = $null
    }
)

# Build each project
foreach ($Project in $Projects) {
    Write-Host ""
    Write-Host "Building $($Project.Name)..." -ForegroundColor Cyan

    $ProjectPath = $Project.Path
    $SolutionPath = Join-Path $ProjectPath $Project.Solution

    if (-not (Test-Path $SolutionPath)) {
        Write-Host "Solution not found: $SolutionPath" -ForegroundColor Red
        exit 1
    }

    # Build frontend if needed
    if ($Project.FrontendPath) {
        $FrontendFullPath = Join-Path $ProjectPath $Project.FrontendPath
        if (Test-Path $FrontendFullPath) {
            Write-Host "  Building frontend..." -ForegroundColor Gray
            Push-Location $FrontendFullPath
            try {
                npm install --silent
                if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
                npm run build
                if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
            }
            finally {
                Pop-Location
            }
        }
    }

    # Build with package references (not project references)
    Write-Host "  Building solution..." -ForegroundColor Gray
    dotnet build $SolutionPath `
        -c $Configuration `
        -p:UseProjectReferences=false `
        --verbosity quiet

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build $($Project.Name)"
        exit 1
    }

    # Pack
    Write-Host "  Creating packages..." -ForegroundColor Gray
    dotnet pack $SolutionPath `
        -c $Configuration `
        -p:UseProjectReferences=false `
        --no-build `
        -o $PackagesDir `
        --verbosity quiet

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to pack $($Project.Name)"
        exit 1
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Packages:" -ForegroundColor Cyan
Get-ChildItem $PackagesDir -Filter "*.nupkg" | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Gray }
Write-Host ""
Write-Host "Distribution folder: $OutputPath" -ForegroundColor Cyan
