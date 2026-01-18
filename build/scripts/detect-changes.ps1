# Change Detection Script for Umbraco.Ai Monorepo
# Analyzes git changes or tag names to determine which products changed
# Outputs Azure DevOps pipeline variables

param(
    [string]$SourceBranch = $env:BUILD_SOURCEBRANCH,
    [string]$SourceVersion = $env:BUILD_SOURCEVERSION
)

$ErrorActionPreference = "Stop"

Write-Host "==================================="
Write-Host "Umbraco.Ai Change Detection"
Write-Host "==================================="
Write-Host ""
Write-Host "Source Branch: $SourceBranch"
Write-Host "Source Version: $SourceVersion"
Write-Host ""

# Product configuration
$products = @{
    "core"      = @{ Path = "Umbraco.Ai/"; Variable = "CoreChanged" }
    "agent"     = @{ Path = "Umbraco.Ai.Agent/"; Variable = "AgentChanged" }
    "prompt"    = @{ Path = "Umbraco.Ai.Prompt/"; Variable = "PromptChanged" }
    "openai"    = @{ Path = "Umbraco.Ai.OpenAi/"; Variable = "OpenAiChanged" }
    "anthropic" = @{ Path = "Umbraco.Ai.Anthropic/"; Variable = "AnthropicChanged" }
}

# Product dependencies (if X changes, rebuild Y)
$dependencies = @{
    "core" = @("agent", "prompt", "openai", "anthropic")  # Core changes affect all
}

# Initialize all as unchanged
$changedProducts = @{}
foreach ($key in $products.Keys) {
    $changedProducts[$key] = $false
}

# Determine change detection strategy
if ($SourceBranch -match "^refs/tags/release-([a-z]+)-") {
    # Tag-based detection (release builds)
    $product = $Matches[1]
    Write-Host "Tag-based detection: release-$product-*" -ForegroundColor Cyan

    if ($products.ContainsKey($product)) {
        $changedProducts[$product] = $true
        Write-Host "  ✓ $product changed (release tag)" -ForegroundColor Green
    }
}
else {
    # Git diff-based detection (branch builds)
    Write-Host "Git diff-based detection" -ForegroundColor Cyan

    # Get changed files since last commit
    $changedFiles = git diff --name-only HEAD~1 HEAD

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Warning: Could not determine changed files, building all products" -ForegroundColor Yellow
        foreach ($key in $products.Keys) {
            $changedProducts[$key] = $true
        }
    }
    else {
        # Check which product folders have changes
        foreach ($file in $changedFiles) {
            foreach ($productKey in $products.Keys) {
                $productPath = $products[$productKey].Path
                if ($file.StartsWith($productPath)) {
                    $changedProducts[$productKey] = $true
                    Write-Host "  ✓ $productKey changed (file: $file)" -ForegroundColor Green
                    break
                }
            }
        }

        # Check for root-level changes that affect all products
        $rootFiles = @(
            "Directory.Packages.props",
            "Directory.Build.props",
            "global.json",
            ".gitignore"
        )

        foreach ($file in $changedFiles) {
            if ($rootFiles -contains $file) {
                Write-Host "  ✓ Root file changed: $file (affects all products)" -ForegroundColor Yellow
                foreach ($key in $products.Keys) {
                    $changedProducts[$key] = $true
                }
                break
            }
        }
    }
}

# Apply dependency propagation
Write-Host ""
Write-Host "Applying dependency propagation..." -ForegroundColor Cyan
$propagated = $false
do {
    $propagated = $false
    foreach ($productKey in @($changedProducts.Keys)) {
        if ($changedProducts[$productKey] -and $dependencies.ContainsKey($productKey)) {
            foreach ($dependent in $dependencies[$productKey]) {
                if (-not $changedProducts[$dependent]) {
                    $changedProducts[$dependent] = $true
                    $propagated = $true
                    Write-Host "  → $dependent rebuild required (depends on $productKey)" -ForegroundColor Yellow
                }
            }
        }
    }
} while ($propagated)

# Output Azure DevOps pipeline variables
Write-Host ""
Write-Host "Setting pipeline variables:" -ForegroundColor Cyan
foreach ($productKey in $products.Keys) {
    $variableName = $products[$productKey].Variable
    $value = $changedProducts[$productKey].ToString().ToLower()
    Write-Host "##vso[task.setvariable variable=$variableName;isOutput=true]$value"

    $status = if ($changedProducts[$productKey]) { "✓ BUILD" } else { "- SKIP" }
    $color = if ($changedProducts[$productKey]) { "Green" } else { "Gray" }
    Write-Host "  $status $productKey" -ForegroundColor $color
}

Write-Host ""
Write-Host "Change detection complete" -ForegroundColor Green
