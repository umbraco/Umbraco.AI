#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate release-manifest.json for Umbraco.AI releases

.DESCRIPTION
    Scans for all Umbraco.AI* product folders and presents an interactive
    multiselect interface to choose which products to include in the release.
    Generates release-manifest.json at the repository root.

.EXAMPLE
    .\scripts\generate-release-manifest.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Get repository root (parent of scripts folder)
$repoRoot = Split-Path -Parent $PSScriptRoot

# Find all Umbraco.AI* folders at root
$products = Get-ChildItem -Path $repoRoot -Directory -Filter "Umbraco.AI*" |
    Select-Object -ExpandProperty Name |
    Sort-Object

if ($products.Count -eq 0) {
    Write-Error "No Umbraco.AI* folders found in repository root"
    exit 1
}

# Initialize selection state (all unselected by default)
$selected = @{}
$products | ForEach-Object { $selected[$_] = $false }

function Show-Menu {
    Clear-Host
    Write-Host ""
    Write-Host "=== Generate Release Manifest ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Select products to include in this release:" -ForegroundColor Yellow
    Write-Host ""

    for ($i = 0; $i -lt $products.Count; $i++) {
        $product = $products[$i]
        $checkbox = if ($selected[$product]) { "[X]" } else { "[ ]" }
        $color = if ($selected[$product]) { "Green" } else { "Gray" }
        Write-Host "  $($i + 1). $checkbox $product" -ForegroundColor $color
    }

    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Yellow
    Write-Host "  [1-$($products.Count)] - Toggle selection"
    Write-Host "  [A] - Select All"
    Write-Host "  [N] - Select None"
    Write-Host "  [G] - Generate manifest and exit"
    Write-Host "  [Q] - Quit without saving"
    Write-Host ""
}

function Get-SelectedProducts {
    return $products | Where-Object { $selected[$_] }
}

# Main loop
while ($true) {
    Show-Menu

    $selectedProducts = Get-SelectedProducts
    if ($selectedProducts) {
        Write-Host "Selected: $($selectedProducts.Count) product(s)" -ForegroundColor Green
    } else {
        Write-Host "Selected: None" -ForegroundColor Red
    }
    Write-Host ""

    $choice = Read-Host "Choose option"

    switch -Regex ($choice.Trim().ToUpper()) {
        '^[0-9]+$' {
            $num = [int]$choice
            if ($num -ge 1 -and $num -le $products.Count) {
                $product = $products[$num - 1]
                $selected[$product] = -not $selected[$product]
            } else {
                Write-Host "Invalid number. Press Enter to continue..." -ForegroundColor Red
                Read-Host
            }
        }

        'A' {
            $products | ForEach-Object { $selected[$_] = $true }
        }

        'N' {
            $products | ForEach-Object { $selected[$_] = $false }
        }

        'G' {
            $selectedProducts = Get-SelectedProducts

            if ($selectedProducts.Count -eq 0) {
                Write-Host ""
                Write-Host "Error: No products selected. Press Enter to continue..." -ForegroundColor Red
                Read-Host
                continue
            }

            # Generate manifest
            $manifestPath = Join-Path $repoRoot "release-manifest.json"
            $manifest = $selectedProducts | ConvertTo-Json

            # Pretty print with 2-space indentation
            $manifest | Set-Content -Path $manifestPath -Encoding UTF8

            Write-Host ""
            Write-Host "Generated: $manifestPath" -ForegroundColor Green
            Write-Host ""
            Write-Host "Contents:" -ForegroundColor Yellow
            Write-Host $manifest
            Write-Host ""
            Write-Host "Selected $($selectedProducts.Count) product(s)" -ForegroundColor Green

            return
        }

        'Q' {
            Write-Host ""
            Write-Host "Cancelled. No changes made." -ForegroundColor Yellow
            return
        }

        default {
            Write-Host "Invalid choice. Press Enter to continue..." -ForegroundColor Red
            Read-Host
        }
    }
}
