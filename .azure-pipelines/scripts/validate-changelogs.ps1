#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that CHANGELOGs are present and up-to-date for release branches.

.DESCRIPTION
    This script runs on release/* branches to ensure:
    1. Each product in release-manifest.json has a CHANGELOG.md
    2. The CHANGELOG.md was updated in recent commits
    3. The version in CHANGELOG.md matches version.json

.PARAMETER ManifestPath
    Path to release-manifest.json (default: ./release-manifest.json)

.EXAMPLE
    ./validate-changelogs.ps1
    ./validate-changelogs.ps1 -ManifestPath ./release-manifest.json
#>

param(
    [string]$ManifestPath = "./release-manifest.json"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ANSI color codes
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = $Reset
    )
    Write-Host "${Color}${Message}${Reset}"
}

function Get-VersionFromJson {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    $json = Get-Content $Path -Raw | ConvertFrom-Json
    return $json.version
}

function Get-VersionFromChangelog {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    # Look for the first version header: ## [X.Y.Z] - YYYY-MM-DD or ## [X.Y.Z] - TBC
    $content = Get-Content $Path -Raw
    if ($content -match '## \[(\d+\.\d+\.\d+)\]') {
        return $Matches[1]
    }

    return $null
}

function Test-ChangelogUpdated {
    param(
        [string]$ChangelogPath,
        [int]$CommitDepth = 10
    )

    # Check if changelog was modified in recent commits
    $changedFiles = git diff --name-only HEAD~$CommitDepth HEAD 2>$null
    if ($LASTEXITCODE -ne 0) {
        # If we can't go back that far, check what we can
        $changedFiles = git diff --name-only --cached 2>$null
        if ($LASTEXITCODE -ne 0) {
            # Fallback: check if file exists and has content
            if (Test-Path $ChangelogPath) {
                $content = Get-Content $ChangelogPath -Raw
                return $content.Length -gt 100  # Arbitrary minimum size
            }
            return $false
        }
    }

    $normalizedPath = $ChangelogPath -replace '\\', '/'
    return $changedFiles -split "`n" | Where-Object { $_ -eq $normalizedPath }
}

# Main execution
Write-ColorOutput "`n=== Changelog Validation ===" $Blue
Write-ColorOutput "Validating CHANGELOGs for release branch...`n" $Blue

# Check if manifest exists
if (-not (Test-Path $ManifestPath)) {
    Write-ColorOutput "❌ Release manifest not found: $ManifestPath" $Red
    Write-ColorOutput "Release branches require a release-manifest.json file." $Yellow
    exit 1
}

# Load manifest
$manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
Write-ColorOutput "Found release manifest with $($manifest.Count) product(s):" $Green
$manifest | ForEach-Object { Write-ColorOutput "  - $_" $Green }
Write-Host ""

$hasErrors = $false
$warnings = @()

foreach ($product in $manifest) {
    Write-ColorOutput "Validating $product..." $Blue

    $productDir = $product
    $changelogPath = Join-Path $productDir "CHANGELOG.md"
    $versionJsonPath = Join-Path $productDir "version.json"

    # Check 1: CHANGELOG.md exists
    if (-not (Test-Path $changelogPath)) {
        Write-ColorOutput "  ❌ CHANGELOG.md not found at: $changelogPath" $Red
        $hasErrors = $true
        continue
    }
    Write-ColorOutput "  ✓ CHANGELOG.md exists" $Green

    # Check 2: CHANGELOG.md was updated
    $wasUpdated = Test-ChangelogUpdated -ChangelogPath $changelogPath
    if (-not $wasUpdated) {
        Write-ColorOutput "  ⚠️  CHANGELOG.md does not appear to have been updated in recent commits" $Yellow
        $warnings += "CHANGELOG.md for $product may not have been updated"
    } else {
        Write-ColorOutput "  ✓ CHANGELOG.md was updated" $Green
    }

    # Check 3: Version in CHANGELOG matches version.json
    $versionJson = Get-VersionFromJson -Path $versionJsonPath
    $versionChangelog = Get-VersionFromChangelog -Path $changelogPath

    if (-not $versionJson) {
        Write-ColorOutput "  ⚠️  Could not read version from version.json" $Yellow
        $warnings += "Could not validate version for $product"
    } elseif (-not $versionChangelog) {
        Write-ColorOutput "  ❌ Could not find version in CHANGELOG.md (expected format: ## [X.Y.Z] - YYYY-MM-DD)" $Red
        $hasErrors = $true
    } elseif ($versionJson -ne $versionChangelog) {
        Write-ColorOutput "  ❌ Version mismatch!" $Red
        Write-ColorOutput "     version.json: $versionJson" $Red
        Write-ColorOutput "     CHANGELOG.md: $versionChangelog" $Red
        $hasErrors = $true
    } else {
        Write-ColorOutput "  ✓ Version matches: $versionJson" $Green
    }

    Write-Host ""
}

# Summary
Write-ColorOutput "=== Validation Summary ===" $Blue

if ($warnings.Count -gt 0) {
    Write-ColorOutput "`nWarnings:" $Yellow
    $warnings | ForEach-Object { Write-ColorOutput "  ⚠️  $_" $Yellow }
}

if ($hasErrors) {
    Write-ColorOutput "`n❌ Changelog validation FAILED" $Red
    Write-ColorOutput "`nTo fix these issues:" $Yellow
    Write-ColorOutput "  1. Generate changelogs: npm run changelog -- --product=<ProductName> --version=<Version>" $Yellow
    Write-ColorOutput "  2. Ensure the version in CHANGELOG.md matches version.json" $Yellow
    Write-ColorOutput "  3. Commit the updated CHANGELOGs to your release branch" $Yellow
    Write-ColorOutput "`nSee CONTRIBUTING.md for detailed instructions." $Yellow
    exit 1
}

Write-ColorOutput "`n✓ All changelog validations passed!" $Green
exit 0
