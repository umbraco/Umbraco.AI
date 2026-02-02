# Fix all .csproj files in Prompt and Agent

$products = @('Umbraco.AI.Prompt', 'Umbraco.AI.Agent')

foreach ($product in $products) {
    Write-Host "Processing $product..."
    
    # Get all .csproj files in this product
    $csprojFiles = Get-ChildItem -Path "$product" -Filter "*.csproj" -Recurse
    
    foreach ($file in $csprojFiles) {
        Write-Host "  Fixing $($file.FullName)"
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $original = $content
        
        # Fix Core product reference for test projects (tests/ folder → Core product)
        # Pattern: Include="..\Umbraco.AI.Core\ → Include="..\..\..\Umbraco.AI\src\Umbraco.AI.Core\
        $content = $content -replace 'Include="\.\.\Umbraco\.AI\.Core\','Include="..\..\..\Umbraco.AI\src\Umbraco.AI.Core\'
        
        # Fix old package name: Umbraco.Ai.Core → Umbraco.AI.Core
        $content = $content -replace 'Include="Umbraco\.Ai\.Core"','Include="Umbraco.AI.Core"'
        
        # Fix within-product references with missing folder names
        # These are .csproj references that are missing the folder part
        # Pattern: ..\ProjectName.csproj → ..\..\src\ProjectName\ProjectName.csproj
        
        # Extract product name (Prompt or Agent)
        $productName = if ($product -like "*Prompt*") { "Prompt" } else { "Agent" }
        
        # Fix all internal project references
        $projects = @("Core", "Persistence", "Persistence.SqlServer", "Persistence.Sqlite", "Web", "Web.StaticAssets")
        
        foreach ($proj in $projects) {
            $projectRef = "Umbraco.AI.$productName.$proj"
            # Pattern: ..\Umbraco.AI.Prompt.Core.csproj → ..\..\src\Umbraco.AI.Prompt.Core\Umbraco.AI.Prompt.Core.csproj
            $content = $content -replace "Include=`"\.\.\$projectRef\.csproj`"","Include=`"..\..\src\$projectRef\$projectRef.csproj`""
        }
        
        if ($content -ne $original) {
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
            Write-Host "    Modified"
        } else {
            Write-Host "    No changes"
        }
    }
}

Write-Host "Done!"
