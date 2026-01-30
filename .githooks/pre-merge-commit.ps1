# Pre-merge-commit hook (PowerShell) - Ensure release-manifest.json is committed on release/hotfix branches

$ErrorActionPreference = "Stop"

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD 2>$null

# Only run on release or hotfix branches
if ($currentBranch -notmatch '^(release|hotfix)/') {
    exit 0
}

# Check if release-manifest.json exists in HEAD
$fileInHead = git ls-tree HEAD release-manifest.json 2>$null
$fileInIndex = git ls-files --stage release-manifest.json 2>$null

if ($fileInHead) {
    # File exists in HEAD
    if (-not $fileInIndex) {
        # File is not in the index (was deleted in merge)
        Write-Host "🔒 Restoring release-manifest.json on $currentBranch branch..." -ForegroundColor Cyan

        # Restore the file from HEAD and add it to the merge commit
        git checkout HEAD -- release-manifest.json
        git add release-manifest.json

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ release-manifest.json restored and staged for merge commit" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Failed to restore release-manifest.json" -ForegroundColor Red
            exit 1
        }
    } else {
        # File is staged - ensure it stays staged for the merge commit
        # This handles the case where the merge driver preserved the file
        # Check if the staged version matches HEAD (no actual changes)
        $stagedHash = ($fileInIndex -split '\s+')[1]
        $headHash = ($fileInHead -split '\s+')[2]

        if ($stagedHash -eq $headHash) {
            # File is staged but unchanged from HEAD - ensure it's included in merge commit
            # by explicitly staging it again (no-op but ensures it's in the commit)
            git add release-manifest.json
        }
    }
}

exit 0
