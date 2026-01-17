# Pre-push hook to validate branch naming conventions for Umbraco.Ai monorepo
# Valid patterns:
#   - main
#   - develop
#   - feature/<product>-<description>
#   - release/<product>-<version>
#   - hotfix/<product>-<version>
# Where <product> is one of: core, agent, prompt, openai, anthropic

$ErrorActionPreference = "Stop"

# Get current branch name
$currentBranch = git symbolic-ref --short HEAD 2>$null

if ([string]::IsNullOrEmpty($currentBranch)) {
    Write-Error "Unable to determine current branch"
    exit 1
}

# List of valid products
$products = @("core", "agent", "prompt", "openai", "anthropic")

# Allow main and develop branches
if ($currentBranch -eq "main" -or $currentBranch -eq "develop") {
    exit 0
}

# Check if branch matches valid patterns
$validBranch = $false

# Check feature branches: feature/<product>-<description>
if ($currentBranch -match "^feature/([a-z]+)-.+") {
    $product = $Matches[1]
    if ($products -contains $product) {
        $validBranch = $true
    }
}

# Check release branches: release/<product>-<version>
if ($currentBranch -match "^release/([a-z]+)-\d+\.\d+\.\d+") {
    $product = $Matches[1]
    if ($products -contains $product) {
        $validBranch = $true
    }
}

# Check hotfix branches: hotfix/<product>-<version>
if ($currentBranch -match "^hotfix/([a-z]+)-\d+\.\d+\.\d+") {
    $product = $Matches[1]
    if ($products -contains $product) {
        $validBranch = $true
    }
}

if (-not $validBranch) {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "ERROR: Invalid branch name: $currentBranch" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Branch names must follow one of these patterns:"
    Write-Host "  main"
    Write-Host "  develop"
    Write-Host "  feature/<product>-<description>"
    Write-Host "  release/<product>-<version>"
    Write-Host "  hotfix/<product>-<version>"
    Write-Host ""
    Write-Host "Valid products: $($products -join ', ')"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  feature/core-add-caching"
    Write-Host "  release/agent-17.1.0"
    Write-Host "  hotfix/openai-1.0.1"
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}

exit 0
