# scripts/generate-changelog.ps1
param(
    [Parameter(Mandatory=$false)]
    [string]$Product,

    [Parameter(Mandatory=$false)]
    [string]$Version,

    [Parameter(Mandatory=$false)]
    [switch]$Unreleased,

    [Parameter(Mandatory=$false)]
    [switch]$List
)

$scriptDir = Split-Path -Parent $PSCommandPath
$rootDir = Split-Path -Parent $scriptDir

# List products
if ($List) {
    & node "$scriptDir/generate-changelog.js" --list
    exit $LASTEXITCODE
}

# Validate product is provided
if (-not $Product) {
    Write-Error "Error: -Product is required"
    Write-Host "`nUsage:"
    Write-Host "  .\scripts\generate-changelog.ps1 -Product Umbraco.AI -Version 17.1.0"
    Write-Host "  .\scripts\generate-changelog.ps1 -Product Umbraco.AI -Unreleased"
    Write-Host "  .\scripts\generate-changelog.ps1 -List  # List available products"
    Write-Host ""
    & node "$scriptDir/generate-changelog.js" --list
    exit 1
}

# Build arguments
$nodeArgs = @("$scriptDir/generate-changelog.js", "--product=$Product")

if ($Version) {
    $nodeArgs += "--version=$Version"
}

if ($Unreleased) {
    $nodeArgs += "--unreleased"
}

# Run Node.js script
Write-Host "Generating changelog for $Product..."
& node $nodeArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to generate changelog"
    exit 1
}

# If not in CI, prompt to review
if (-not $env:CI) {
    $changelogPath = Join-Path $rootDir "$Product/CHANGELOG.md"
    Write-Host "`n✅ Changelog generated at: $changelogPath"
    Write-Host "`nReview and commit:`n"
    Write-Host "  git add $Product/CHANGELOG.md"

    # Convert product name to lowercase for commit message scope
    $scope = $Product.Replace('Umbraco.AI.', '').Replace('Umbraco.AI', 'core').ToLower()
    Write-Host "  git commit -m 'docs($scope): update CHANGELOG for v$Version'"
}
