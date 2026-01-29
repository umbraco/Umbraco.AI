# Setup script to configure git hooks for Umbraco.Ai monorepo

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Umbraco.Ai Git Hooks Setup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Get the repository root
$repoRoot = git rev-parse --show-toplevel 2>$null

if ([string]::IsNullOrEmpty($repoRoot)) {
    Write-Error "Error: Not in a git repository"
    exit 1
}

# Convert Unix path to Windows path if needed
$repoRoot = $repoRoot -replace '/', '\'
$hooksDir = Join-Path $repoRoot ".githooks"

# Check if hooks directory exists
if (-not (Test-Path $hooksDir)) {
    Write-Error "Error: .githooks directory not found at $hooksDir"
    exit 1
}

# Make hook scripts executable (not needed on Windows, but good for cross-platform compatibility)
Write-Host "Configuring git hooks..."

# Configure git to use the custom hooks directory
git config core.hooksPath .githooks

Write-Host ""
Write-Host "✓ Git hooks configured successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "The following hooks are now active:"
Write-Host "  - pre-push: Validates branch naming conventions"
Write-Host "  - commit-msg: Validates commit messages (conventional commits)"
Write-Host "  - post-merge: Cleans up release-manifest.json after merge to main/dev/support/*"
Write-Host ""
Write-Host "To disable hooks, run:"
Write-Host "  git config --unset core.hooksPath"
Write-Host ""
