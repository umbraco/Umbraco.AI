# Pre-merge-commit hook (PowerShell) - Restore release-manifest.json on release/hotfix branches

$ErrorActionPreference = "Stop"

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD 2>$null

# Only run on release or hotfix branches
if ($currentBranch -notmatch '^(release|hotfix)/') {
    exit 0
}

# Check if release-manifest.json exists in HEAD but was deleted in the merge
$fileInHead = git ls-tree HEAD release-manifest.json 2>$null
$fileInIndex = git ls-files --stage release-manifest.json 2>$null

if ($fileInHead -and -not $fileInIndex) {
    # File exists in HEAD but not in index (was deleted in merge)
    Write-Host "🔒 Restoring release-manifest.json on $currentBranch branch..." -ForegroundColor Cyan

    # Restore the file from HEAD
    git restore --staged --worktree --source=HEAD release-manifest.json

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ release-manifest.json restored successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Failed to restore release-manifest.json" -ForegroundColor Red
        exit 1
    }
}

exit 0
