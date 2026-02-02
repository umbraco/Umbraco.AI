# Post-merge hook (PowerShell) - Clean up or commit release-manifest.json based on branch type

$ErrorActionPreference = "SilentlyContinue"

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD 2>$null

# Handle release/hotfix branches - commit staged manifest if present
if ($currentBranch -match '^(release|hotfix)/') {
    # Check if release-manifest.json is staged but not committed
    $stagedFiles = git diff --cached --name-only 2>$null
    if ($stagedFiles -and ($stagedFiles -split "`n" | Where-Object { $_ -eq "release-manifest.json" })) {
        Write-Host "📝 Committing staged release-manifest.json after merge to $currentBranch..." -ForegroundColor Cyan
        git commit -m "chore(ci): Preserve release-manifest.json after merge"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ release-manifest.json committed" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Failed to commit release-manifest.json" -ForegroundColor Yellow
        }
    }
    exit 0
}

# Handle long-term branches (main/dev/support) - remove manifest
if ($currentBranch -eq "main" -or $currentBranch -eq "dev" -or $currentBranch -match "^support/") {
    # Check if release-manifest.json exists
    $manifestFile = Join-Path $PSScriptRoot ".." "release-manifest.json"
    if (Test-Path $manifestFile) {
        Write-Host "🧹 Cleaning up release-manifest.json after merge to $currentBranch..." -ForegroundColor Cyan
        git rm release-manifest.json
        if ($LASTEXITCODE -eq 0) {
            git commit -m "chore(ci): Remove release-manifest.json after merge"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ release-manifest.json removed and committed" -ForegroundColor Green
            }
        }
    }
}

exit 0
