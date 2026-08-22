<#
.SYNOPSIS
Rewrites intra-product NuGet dependency versions from a floor (">=") to an exact pin.

.DESCRIPTION
dotnet pack auto-derives a package's dependency on a sibling ProjectReference as a floor
version (e.g. Umbraco.AI.Startup depending on Umbraco.AI.Persistence >= 17.3.1). Siblings
within one product always ship from the same version.json in lockstep, so that floor should
always be an exact pin instead - otherwise a consumer can end up with a half-upgraded install
(see umbraco/Umbraco.AI#332). NuGet has no built-in way to make pack emit an exact version for
a ProjectReference-derived dependency, so this rewrites the already-correct version string in
the generated .nuspec (inside the .nupkg) after packing, from version="X" to version="[X]".

.PARAMETER ProductDir
Path to the product's root directory (e.g. "Umbraco.AI"), whose "src/*" subdirectories name
every package this product ships - i.e. its sibling package ids.

.PARAMETER NupkgDir
Path to the directory containing the packed .nupkg files (e.g. "./artifacts/nupkg").
#>
param(
    [Parameter(Mandatory)][string]$ProductDir,
    [Parameter(Mandatory)][string]$NupkgDir
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$siblingIds = Get-ChildItem -Path (Join-Path $ProductDir 'src') -Directory |
    Select-Object -ExpandProperty Name | Sort-Object
Write-Host "Sibling package ids for ${ProductDir}: $($siblingIds -join ', ')"

foreach ($id in $siblingIds) {
    $idPattern = [regex]::Escape($id)
    $nupkgs = Get-ChildItem -Path $NupkgDir -Filter '*.nupkg' |
        Where-Object { $_.Name -match "^$idPattern\.\d" }

    foreach ($nupkgFile in $nupkgs) {
        $archive = [System.IO.Compression.ZipFile]::Open($nupkgFile.FullName, [System.IO.Compression.ZipArchiveMode]::Update)
        try {
            $nuspecEntry = $archive.Entries |
                Where-Object { $_.FullName -notmatch '/' -and $_.FullName -like '*.nuspec' } |
                Select-Object -First 1
            if (-not $nuspecEntry) {
                continue
            }

            $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
            $content = $reader.ReadToEnd()
            $reader.Dispose()

            $changed = $false
            foreach ($sibling in $siblingIds) {
                $depPattern = '(<dependency id="' + [regex]::Escape($sibling) + '" version=")([^"\[]+)(")'
                if ($content -match $depPattern) {
                    $content = $content -replace $depPattern, '$1[$2]$3'
                    $changed = $true
                }
            }

            if ($changed) {
                Write-Host "Pinning intra-product dependency versions in $($nupkgFile.Name)"
                $entryName = $nuspecEntry.FullName
                $nuspecEntry.Delete()
                $newEntry = $archive.CreateEntry($entryName)
                $writer = New-Object System.IO.StreamWriter($newEntry.Open())
                $writer.Write($content)
                $writer.Dispose()
            }
        }
        finally {
            $archive.Dispose()
        }
    }
}
