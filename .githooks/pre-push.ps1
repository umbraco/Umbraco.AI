# Pre-push hook to validate branch naming conventions for Umbraco.Ai monorepo
# Valid patterns:
#   - main
#   - dev
#   - feature/<anything>
#   - release/<anything>
#   - hotfix/<anything>

$ErrorActionPreference = "Stop"

# Get current branch name
$currentBranch = git symbolic-ref --short HEAD 2>$null

if ([string]::IsNullOrEmpty($currentBranch)) {
    Write-Error "Unable to determine current branch"
    exit 1
}

# Allow main and dev branches
if ($currentBranch -eq "main" -or $currentBranch -eq "dev") {
    exit 0
}

# Check if branch matches valid patterns
$validBranch = $false
if ($currentBranch -match "^(feature|claude|release|hotfix)/.+") {
    $validBranch = $true
}

if (-not $validBranch) {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "ERROR: Invalid branch name: $currentBranch" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Branch names must follow one of these patterns:"
    Write-Host "  main"
    Write-Host "  dev"
    Write-Host "  feature/<anything>"
    Write-Host "  release/<anything>"
    Write-Host "  hotfix/<anything>"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  feature/add-caching"
    Write-Host "  release/2026.01"
    Write-Host "  hotfix/2026.01.1"
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}

exit 0
