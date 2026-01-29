# Post-merge hook (PowerShell) - Clean up release-manifest.json on long-term branches

$ErrorActionPreference = "SilentlyContinue"

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD 2>$null

# Only run on main, dev, or support/* branches
if ($currentBranch -ne "main" -and $currentBranch -ne "dev" -and $currentBranch -notmatch "^support/") {
    exit 0
}

# Check if release-manifest.json exists
$manifestFile = Join-Path $PSScriptRoot ".." "release-manifest.json"
if (Test-Path $manifestFile) {
    Write-Host "🧹 Cleaning up release-manifest.json after merge to $currentBranch..." -ForegroundColor Cyan
    git rm release-manifest.json
    if ($LASTEXITCODE -eq 0) {
        git commit -m "chore: remove release-manifest.json after merge"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ release-manifest.json removed and committed" -ForegroundColor Green
        }
    }
}

exit 0
