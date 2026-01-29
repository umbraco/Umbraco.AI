# Custom merge driver for release-manifest.json (PowerShell)
# Preserves the file on release/hotfix branches, uses default merge elsewhere
#
# Arguments: %O %A %B %L %P
#   %O = ancestor's version
#   %A = current version
#   %B = other branch's version
#   %L = conflict marker size
#   %P = file path

param(
    [string]$Ancestor,    # %O
    [string]$Current,     # %A
    [string]$Other,       # %B
    [string]$MarkerSize,  # %L
    [string]$FilePath     # %P
)

$currentBranch = git rev-parse --abbrev-ref HEAD 2>$null

# If on release or hotfix branch, keep our version
if ($currentBranch -match '^(release|hotfix)/') {
    # Keep our version by doing nothing - it's already in place
    exit 0
}

# On other branches, use default merge behavior
# If the file was deleted in their branch, delete it
if (-not (Test-Path $Other) -or (Get-Item $Other).Length -eq 0) {
    # File was deleted in the incoming branch, delete ours too
    if (Test-Path $Current) {
        Remove-Item $Current -Force
    }
    exit 0
}

# Otherwise, copy their version
Copy-Item $Other $Current -Force
exit 0
