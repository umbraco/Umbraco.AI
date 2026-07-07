# Pre-push hook to validate branch naming conventions for Umbraco.AI monorepo
# Valid patterns:
#   - v<N>/dev
#   - v<N>/main
#   - v<N>/feature/<anything>
#   - v<N>/release/<anything>
#   - v<N>/hotfix/<anything>
#   - claude/<anything>

$ErrorActionPreference = "Stop"

# Get current branch name
$currentBranch = git symbolic-ref --short HEAD 2>$null

if ([string]::IsNullOrEmpty($currentBranch)) {
    Write-Error "Unable to determine current branch"
    exit 1
}

# Check if branch matches valid patterns
$validBranch = $false
if ($currentBranch -match "^v\d+/(dev|main)$") {
    $validBranch = $true
} elseif ($currentBranch -match "^v\d+/(feature|release|hotfix)/.+") {
    $validBranch = $true
} elseif ($currentBranch -match "^claude/.+") {
    $validBranch = $true
}

if (-not $validBranch) {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "ERROR: Invalid branch name: $currentBranch" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Branch names must follow one of these patterns:"
    Write-Host "  v<N>/dev"
    Write-Host "  v<N>/main"
    Write-Host "  v<N>/feature/<anything>"
    Write-Host "  v<N>/release/<anything>"
    Write-Host "  v<N>/hotfix/<anything>"
    Write-Host "  claude/<anything>"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  v18/dev"
    Write-Host "  v18/feature/add-caching"
    Write-Host "  v17/release/2026.01"
    Write-Host "  v17/hotfix/2026.01.1"
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}

exit 0
