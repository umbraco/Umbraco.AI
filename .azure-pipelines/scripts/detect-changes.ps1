# ============================================================================
# Umbraco.Ai Monorepo Change Detection Script
# ============================================================================
# Automatically discovers products, analyzes dependencies, and determines
# which products need to be rebuilt based on git changes.
#
# Key Features:
# - Auto-discovers products by folder naming convention (Umbraco.Ai.*)
# - Parses .csproj files to extract actual dependencies
# - Calculates build levels using topological sort
# - Propagates changes to dependent products
# ============================================================================

param(
    [string]$SourceBranch = $env:BUILD_SOURCEBRANCH,
    [string]$SourceVersion = $env:BUILD_SOURCEVERSION,
    [string]$RootPath = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

function Get-ProductKey {
    <#
    .SYNOPSIS
    Converts a product name to a key used in hashtables.
    .EXAMPLE
    "Umbraco.Ai" -> "core"
    "Umbraco.Ai.Agent" -> "agent"
    #>
    param([string]$ProductName)

    if ($ProductName -eq "Umbraco.Ai") { return "core" }
    return ($ProductName -replace "^Umbraco\.Ai\.", "").ToLower()
}

function Get-VariableName {
    <#
    .SYNOPSIS
    Converts a product key to a pipeline variable name.
    .EXAMPLE
    "core" -> "CoreChanged"
    "openai" -> "OpenaiChanged"
    #>
    param([string]$ProductKey)

    $pascalCase = ($ProductKey -split '\.' | ForEach-Object {
        $_.Substring(0,1).ToUpper() + $_.Substring(1).ToLower()
    }) -join ''

    return "${pascalCase}Changed"
}

# ============================================================================
# PRODUCT DISCOVERY
# ============================================================================

function Get-UmbracoAiProducts {
    <#
    .SYNOPSIS
    Auto-discovers all Umbraco.Ai products and their subprojects.

    .DESCRIPTION
    Scans the root directory for folders matching "Umbraco.Ai*" pattern,
    finds their main .csproj files, and maps all subprojects to enable
    dependency resolution.
    #>
    param([string]$RootPath)

    Write-Host "Auto-discovering products..." -ForegroundColor Cyan

    $products = @{}
    $productsByName = @{}

    # Find all product folders (Umbraco.Ai.*)
    $productFolders = Get-ChildItem -Path $RootPath -Directory | Where-Object {
        $_.Name -match "^Umbraco\.Ai" -and
        $_.Name -ne "Umbraco.Ai-entity-snapshot-service" -and
        -not $_.Name.StartsWith(".")
    }

    foreach ($folder in $productFolders) {
        $productName = $folder.Name
        $productKey = Get-ProductKey -ProductName $productName

        # Find main project file (meta-package or single project)
        $mainProject = Join-Path $folder.FullName "src\$productName\$productName.csproj"
        if (-not (Test-Path $mainProject)) {
            $possibleProjects = Get-ChildItem -Path (Join-Path $folder.FullName "src") -Filter "$productName.csproj" -Recurse -ErrorAction SilentlyContinue
            if ($possibleProjects) {
                $mainProject = $possibleProjects[0].FullName
            }
        }

        if (-not (Test-Path $mainProject)) {
            Write-Host "  ! Skipped: $productName (no project file found)" -ForegroundColor Yellow
            continue
        }

        Write-Host "  ✓ Found: $productName" -ForegroundColor Green

        # Register product
        $products[$productKey] = @{
            Path = "$($folder.Name)/"
            Variable = Get-VariableName -ProductKey $productKey
            DisplayName = $productName
            ProjectFile = $mainProject
            DependsOn = @()  # Populated later
        }

        # Map product name and all subprojects to this product key
        $productsByName[$productName] = $productKey

        $srcFolder = Join-Path $folder.FullName "src"
        if (Test-Path $srcFolder) {
            Get-ChildItem -Path $srcFolder -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                $subprojectName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
                if (-not $productsByName.ContainsKey($subprojectName)) {
                    $productsByName[$subprojectName] = $productKey
                }
            }
        }
    }

    return @{
        Products = $products
        ProductsByName = $productsByName
    }
}

# ============================================================================
# DEPENDENCY ANALYSIS
# ============================================================================

function Get-ProjectDependencies {
    <#
    .SYNOPSIS
    Extracts Umbraco.Ai dependencies from a .csproj file.

    .DESCRIPTION
    Parses ProjectReference and PackageReference elements to find
    dependencies on other Umbraco.Ai products.
    #>
    param(
        [string]$ProjectPath,
        [hashtable]$ProductsByName
    )

    if (-not (Test-Path $ProjectPath)) { return @() }

    try {
        [xml]$projectXml = Get-Content $ProjectPath -ErrorAction Stop
        $dependencies = @()

        # Find all references (ProjectReference + PackageReference)
        $references = $projectXml.Project.ItemGroup.ProjectReference + $projectXml.Project.ItemGroup.PackageReference

        foreach ($reference in $references) {
            if ($null -eq $reference -or -not $reference.Include) { continue }

            $refName = $null

            # Extract project name from ProjectReference path
            if ($reference.Include -match '([^\\\/]+)\.csproj$') {
                $refName = $Matches[1]
            }
            # Use PackageReference Include directly if it's Umbraco.Ai.*
            elseif ($reference.Include -match '^Umbraco\.Ai') {
                $refName = $reference.Include
            }

            # Map to product key if it's an Umbraco.Ai product
            if ($refName -and $ProductsByName.ContainsKey($refName)) {
                $depKey = $ProductsByName[$refName]
                if ($depKey -and $dependencies -notcontains $depKey) {
                    $dependencies += $depKey
                }
            }
        }

        return $dependencies
    }
    catch {
        Write-Host "  Warning: Failed to parse $ProjectPath - $_" -ForegroundColor Yellow
        return @()
    }
}

function Get-AllProductDependencies {
    <#
    .SYNOPSIS
    Analyzes all subprojects to build the complete dependency graph.
    #>
    param(
        [hashtable]$Products,
        [hashtable]$ProductsByName,
        [string]$RootPath
    )

    Write-Host ""
    Write-Host "Analyzing project dependencies..." -ForegroundColor Cyan

    foreach ($productKey in $Products.Keys) {
        $product = $Products[$productKey]
        $allDeps = @()

        $srcFolder = Join-Path $RootPath ($product.Path.TrimEnd('/')) | Join-Path -ChildPath "src"

        if (Test-Path $srcFolder) {
            # Scan all .csproj files in this product
            Get-ChildItem -Path $srcFolder -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                $deps = Get-ProjectDependencies -ProjectPath $_.FullName -ProductsByName $ProductsByName

                # Merge dependencies (exclude self-references and duplicates)
                foreach ($dep in $deps) {
                    if ($dep -and $dep -ne $productKey -and $allDeps -notcontains $dep) {
                        $allDeps += $dep
                    }
                }
            }
        }

        $product.DependsOn = $allDeps | Sort-Object

        # Log dependencies
        if ($allDeps.Count -gt 0) {
            $depList = ($allDeps | ForEach-Object { $Products[$_].DisplayName }) -join ", "
            Write-Host "  $($product.DisplayName) → $depList" -ForegroundColor Cyan
        }
        else {
            Write-Host "  $($product.DisplayName) → (no dependencies)" -ForegroundColor Gray
        }
    }
}

# ============================================================================
# BUILD ORDER CALCULATION
# ============================================================================

function Get-BuildLevels {
    <#
    .SYNOPSIS
    Calculates build levels using topological sort.

    .DESCRIPTION
    Level 0 = no dependencies
    Level 1 = depends only on Level 0
    Level 2 = depends on Level 1, etc.
    #>
    param([hashtable]$Products)

    $levels = @{}
    $visited = @{}

    function Get-Level {
        param([string]$ProductKey, [hashtable]$Levels, [hashtable]$Visited)

        if ($Visited.ContainsKey($ProductKey)) {
            return $Levels[$ProductKey]
        }

        $Visited[$ProductKey] = $true
        $deps = $Products[$ProductKey].DependsOn

        # No dependencies = Level 0
        if ($null -eq $deps -or @($deps).Count -eq 0) {
            $Levels[$ProductKey] = 0
            return 0
        }

        # Level = max(dependency levels) + 1
        $maxDepLevel = -1
        foreach ($dep in $deps) {
            $depLevel = Get-Level -ProductKey $dep -Levels $Levels -Visited $Visited
            if ($depLevel -gt $maxDepLevel) {
                $maxDepLevel = $depLevel
            }
        }

        $Levels[$ProductKey] = $maxDepLevel + 1
        return $maxDepLevel + 1
    }

    # Calculate level for each product
    foreach ($productKey in $Products.Keys) {
        $null = Get-Level -ProductKey $productKey -Levels $levels -Visited $visited
    }

    return $levels
}

function Get-TransitiveDependents {
    <#
    .SYNOPSIS
    Builds reverse dependency map (product -> products that depend on it).
    #>
    param([hashtable]$Products)

    $dependents = @{}
    foreach ($productKey in $Products.Keys) {
        $dependents[$productKey] = @()

        foreach ($otherKey in $Products.Keys) {
            if ($Products[$otherKey].DependsOn -contains $productKey) {
                $dependents[$productKey] += $otherKey
            }
        }
    }

    return $dependents
}

# ============================================================================
# CHANGE DETECTION
# ============================================================================

function Get-ChangedProducts {
    <#
    .SYNOPSIS
    Determines which products have changed based on git diff or tag name.
    #>
    param(
        [hashtable]$Products,
        [string]$SourceBranch
    )

    $changed = @{}
    $Products.Keys | ForEach-Object { $changed[$_] = $false }

    # Tag-based detection (release builds)
    if ($SourceBranch -match "^refs/tags/release-([a-z]+)-") {
        $productKey = $Matches[1]
        Write-Host "Tag-based detection: release-$productKey-*" -ForegroundColor Cyan

        if ($Products.ContainsKey($productKey)) {
            $changed[$productKey] = $true
            Write-Host "  ✓ $productKey changed (release tag)" -ForegroundColor Green
        }
        return $changed
    }

    # Git diff-based detection
    Write-Host "Git diff-based detection" -ForegroundColor Cyan

    $changedFiles = git diff --name-only HEAD~1 HEAD 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Warning: Could not determine changed files, building all products" -ForegroundColor Yellow
        $Products.Keys | ForEach-Object { $changed[$_] = $true }
        return $changed
    }

    # Check product folders
    foreach ($file in $changedFiles) {
        foreach ($productKey in $Products.Keys) {
            if ($file.StartsWith($Products[$productKey].Path)) {
                $changed[$productKey] = $true
                Write-Host "  ✓ $productKey changed (file: $file)" -ForegroundColor Green
                break
            }
        }
    }

    # Check root-level files that affect all products
    $globalFiles = @("Directory.Packages.props", "Directory.Build.props", "global.json", ".gitignore")
    foreach ($file in $changedFiles) {
        if ($globalFiles -contains $file) {
            Write-Host "  ✓ Root file changed: $file (affects all products)" -ForegroundColor Yellow
            $Products.Keys | ForEach-Object { $changed[$_] = $true }
            break
        }
    }

    return $changed
}

function Add-DependentChanges {
    <#
    .SYNOPSIS
    Propagates changes to dependent products (if X changes, rebuild Y).
    #>
    param(
        [hashtable]$ChangedProducts,
        [hashtable]$Dependents,
        [hashtable]$Products
    )

    Write-Host ""
    Write-Host "Applying dependency propagation..." -ForegroundColor Cyan

    do {
        $propagated = $false

        foreach ($productKey in @($ChangedProducts.Keys)) {
            if ($ChangedProducts[$productKey] -and $Dependents.ContainsKey($productKey)) {
                foreach ($dependent in $Dependents[$productKey]) {
                    if (-not $ChangedProducts[$dependent]) {
                        $ChangedProducts[$dependent] = $true
                        $propagated = $true
                        Write-Host "  → $dependent rebuild required (depends on $productKey)" -ForegroundColor Yellow
                    }
                }
            }
        }
    } while ($propagated)
}

# ============================================================================
# OUTPUT GENERATION
# ============================================================================

function Write-PipelineVariables {
    <#
    .SYNOPSIS
    Outputs Azure DevOps pipeline variables for changed products and build levels.

    .DESCRIPTION
    Outputs the following variables:
    - Per-product: CoreChanged, OpenaiChanged, etc.
    - Per-level: Level0Changed, Level0Products, Level0Matrix, etc.
    - Global: AnyChanged, AllChangedProducts, MaxLevel
    #>
    param(
        [hashtable]$Products,
        [hashtable]$ChangedProducts,
        [hashtable]$BuildLevels,
        [string]$RootPath
    )

    Write-Host ""
    Write-Host "Setting pipeline variables:" -ForegroundColor Cyan

    # Output per-product variables
    foreach ($productKey in $Products.Keys) {
        $variable = $Products[$productKey].Variable
        $value = $ChangedProducts[$productKey].ToString().ToLower()
        $level = $BuildLevels[$productKey]

        Write-Host "##vso[task.setvariable variable=$variable;isOutput=true]$value"

        $status = if ($ChangedProducts[$productKey]) { "✓ BUILD" } else { "- SKIP" }
        $color = if ($ChangedProducts[$productKey]) { "Green" } else { "Gray" }
        Write-Host "  $status $productKey (level $level)" -ForegroundColor $color
    }

    # Build level-based product lists
    $maxLevel = if ($BuildLevels.Count -gt 0) { ($BuildLevels.Values | Measure-Object -Maximum).Maximum } else { 0 }

    Write-Host ""
    Write-Host "Build levels:" -ForegroundColor Cyan

    # Track all changed products for summary variables
    $allChangedDisplayNames = @()
    $maxChangedLevel = -1

    # Create JSON structure for dynamic pipeline generation (matrix format)
    for ($level = 0; $level -le $maxLevel; $level++) {
        $productsAtLevel = @()
        $changedAtLevel = @()
        $changedDisplayNamesAtLevel = @()
        $matrixJson = @{}

        foreach ($productKey in $Products.Keys) {
            if ($BuildLevels[$productKey] -eq $level) {
                $product = $Products[$productKey]

                # Add to all products at this level
                $productsAtLevel += $productKey

                # If changed, add to matrix JSON with Azure Pipelines matrix format
                if ($ChangedProducts[$productKey]) {
                    $changedAtLevel += $productKey
                    $changedDisplayNamesAtLevel += $product.DisplayName
                    $allChangedDisplayNames += $product.DisplayName
                    $maxChangedLevel = $level

                    # Create matrix key (replace dots and hyphens with underscores)
                    $matrixKey = $product.DisplayName -replace '[.-]', '_'

                    # Check if product has frontend
                    $frontendPath = Join-Path $RootPath $product.Path "src" "$($product.DisplayName).Web.StaticAssets" "Client"
                    $hasFrontend = Test-Path $frontendPath

                    # Check if frontend package exports types (has "types" field in package.json)
                    $hasNpmTypes = $false
                    if ($hasFrontend) {
                        $packageJsonPath = Join-Path $frontendPath "package.json"
                        if (Test-Path $packageJsonPath) {
                            try {
                                $packageJson = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
                                $hasNpmTypes = $null -ne $packageJson.types
                            }
                            catch {
                                Write-Host "    Warning: Failed to parse $packageJsonPath" -ForegroundColor Yellow
                            }
                        }
                    }

                    $matrixJson[$matrixKey] = @{
                        name = $product.DisplayName
                        path = $product.Path.TrimEnd('/')
                        hasFrontend = $hasFrontend.ToString().ToLower()
                        hasNpmTypes = $hasNpmTypes.ToString().ToLower()
                    }
                }
            }
        }

        if ($productsAtLevel.Count -gt 0) {
            $productList = $productsAtLevel -join ", "
            $changedList = if ($changedAtLevel.Count -gt 0) { $changedAtLevel -join ", " } else { "none" }

            Write-Host "  Level $level`: $productList" -ForegroundColor Cyan
            Write-Host "    Changed: $changedList" -ForegroundColor $(if ($changedAtLevel.Count -gt 0) { "Yellow" } else { "Gray" })

            # Set level variables
            $anyChangedAtLevel = ($changedAtLevel.Count -gt 0).ToString().ToLower()
            # Use display names for products list (needed for artifact download)
            $changedDisplayNamesStr = $changedDisplayNamesAtLevel -join ","

            Write-Host "##vso[task.setvariable variable=Level${level}Changed;isOutput=true]$anyChangedAtLevel"
            Write-Host "##vso[task.setvariable variable=Level${level}Products;isOutput=true]$changedDisplayNamesStr"

            # Output matrix JSON for this level
            if ($matrixJson.Count -gt 0) {
                $matrixJsonStr = $matrixJson | ConvertTo-Json -Depth 3 -Compress
                Write-Host "##vso[task.setvariable variable=Level${level}Matrix;isOutput=true]$matrixJsonStr"
                Write-Host "    Matrix JSON: $matrixJsonStr" -ForegroundColor Magenta
            }
            else {
                # Output empty matrix to avoid errors when no products changed at this level
                Write-Host "##vso[task.setvariable variable=Level${level}Matrix;isOutput=true]{}"
            }
        }
    }

    # Output global summary variables
    Write-Host ""
    Write-Host "Summary variables:" -ForegroundColor Cyan

    # AnyChanged - whether any product changed
    $anyChanged = ($allChangedDisplayNames.Count -gt 0).ToString().ToLower()
    Write-Host "##vso[task.setvariable variable=AnyChanged;isOutput=true]$anyChanged"
    Write-Host "  AnyChanged: $anyChanged" -ForegroundColor $(if ($anyChanged -eq "true") { "Green" } else { "Gray" })

    # AllChangedProducts - comma-separated list of all changed product display names
    $allChangedProductsStr = $allChangedDisplayNames -join ","
    Write-Host "##vso[task.setvariable variable=AllChangedProducts;isOutput=true]$allChangedProductsStr"
    Write-Host "  AllChangedProducts: $(if ($allChangedProductsStr) { $allChangedProductsStr } else { '(none)' })" -ForegroundColor Cyan

    # MaxLevel - the highest build level with changed products
    Write-Host "##vso[task.setvariable variable=MaxLevel;isOutput=true]$maxChangedLevel"
    Write-Host "  MaxLevel: $maxChangedLevel" -ForegroundColor Cyan
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host "==================================="
Write-Host "Umbraco.Ai Change Detection"
Write-Host "==================================="
Write-Host ""
Write-Host "Source Branch: $SourceBranch"
Write-Host "Source Version: $SourceVersion"
Write-Host "Root Path: $RootPath"
Write-Host ""

# 1. Discover products
$discovery = Get-UmbracoAiProducts -RootPath $RootPath
$products = $discovery.Products
$productsByName = $discovery.ProductsByName

# 2. Analyze dependencies
Get-AllProductDependencies -Products $products -ProductsByName $productsByName -RootPath $RootPath

# 3. Calculate build order
$buildLevels = Get-BuildLevels -Products $products
$dependents = Get-TransitiveDependents -Products $products

# 4. Detect changes
$changedProducts = Get-ChangedProducts -Products $products -SourceBranch $SourceBranch

# 5. Propagate to dependents
Add-DependentChanges -ChangedProducts $changedProducts -Dependents $dependents -Products $products

# 6. Output pipeline variables
Write-PipelineVariables -Products $products -ChangedProducts $changedProducts -BuildLevels $buildLevels -RootPath $RootPath

Write-Host ""
Write-Host "Change detection complete" -ForegroundColor Green
