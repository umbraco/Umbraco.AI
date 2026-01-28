# Level 2 Product Support Implementation

This document describes the changes made to support Level 2 products (products that depend on Level 1 packages) in the Azure DevOps CI pipeline.

## Overview

The pipeline now supports three dependency levels:
- **Level 0**: Core package (Umbraco.Ai) - no internal dependencies
- **Level 1**: Packages that depend only on Core (providers, Agent, Prompt)
- **Level 2**: Packages that depend on Level 1 packages (e.g., Umbraco.Ai.Agent.Copilot depends on Umbraco.Ai.Agent)

## Changes Made

### 1. New Pipeline Parameter: `level2Products`

Added a new parameter to `azure-pipelines.yml` for Level 2 products:

```yaml
- name: level2Products
  type: object
  default:
    # Copilot (depends on Agent - exports npm types)
    - name: Umbraco.Ai.Agent.Copilot
      changeVar: CopilotChanged
      hasNpm: true
```

### 2. New Reusable Templates

Created three new template files in `.azure-pipelines/templates/` to reduce code duplication:

#### `check-should-pack.yml`
- Determines if a product in a matrix job should be packed based on change detection
- Sets `check.shouldPack` output variable
- Used by both PackLevel1 and PackLevel2 jobs

#### `setup-pack-job.yml`
- Common setup steps for pack jobs (setup .NET, download build outputs)
- Conditionally downloads dependency packages (Core, Level 1) as local NuGet feed
- Parameterized to support different dependency levels

#### `upload-packages.yml`
- Uploads both NuGet packages and npm packages (if present)
- Reduces duplication across PackCore, PackLevel1, and PackLevel2 jobs

### 3. New Pack Job: `PackLevel2`

Added a new job to pack Level 2 products:
- Depends on: `DetectChanges`, `PackCore`, `PackLevel1`
- Runs only if `detect.Level2Changed` is true
- Uses matrix strategy to pack products in parallel
- Downloads Core and Level 1 packages as local NuGet feed for dependency resolution
- Follows same pattern as PackLevel1 job

### 4. Updated `CollectPackages` Job

Extended the package collection job to include Level 2:
- Added `PackLevel2` to job dependencies
- Added Level 2 change variables to job variables
- Added download steps for Level 2 NuGet packages
- Added download steps for Level 2 npm packages (for packages with `hasNpm: true`)
- Updated pack manifest generation script to include Level 2 products

### 5. Simplified Existing Jobs

Refactored PackCore and PackLevel1 jobs to use the new reusable templates:
- **PackCore**: Uses `upload-packages.yml` template
- **PackLevel1**: Uses all three new templates (`check-should-pack.yml`, `setup-pack-job.yml`, `upload-packages.yml`)

## How It Works

### Change Detection

The existing `detect-changes.ps1` script already supports arbitrary dependency levels through dynamic discovery:
1. Auto-discovers all products by scanning for `Umbraco.Ai*` folders
2. Parses `.csproj` files to extract dependencies
3. Calculates build levels using topological sort
4. Outputs level-specific variables (`Level0Changed`, `Level1Changed`, `Level2Changed`, etc.)

**No changes to `detect-changes.ps1` were needed** - it automatically detects Copilot as Level 2 based on its dependency on Agent.

### Pack Process

For a Level 2 product (e.g., Umbraco.Ai.Agent.Copilot):

1. **DetectChanges** runs and determines Copilot is Level 2
2. **PackCore** runs if Core changed (provides Core packages)
3. **PackLevel1** runs if any Level 1 changed (provides Agent packages)
4. **PackLevel2** runs if any Level 2 changed:
   - Downloads build outputs
   - Downloads Core packages (if Core changed)
   - Downloads Level 1 packages (if any Level 1 changed)
   - Packs Copilot with `UseProjectReferences=false` (uses downloaded NuGet packages as dependencies)
   - Packs npm package (since Copilot has `hasNpm: true`)
   - Uploads packages as artifacts
5. **CollectPackages** combines all packages into unified artifacts

### Local NuGet Feed

When packing Level 2 products, the pipeline creates a local NuGet feed in `./artifacts/nupkg` containing:
- Core packages (if Core changed in this build)
- Level 1 packages (if any Level 1 changed in this build)

This allows Level 2 products to resolve dependencies from packages built in the same pipeline run, enabling coordinated releases.

## Adding More Level 2 Products

To add additional Level 2 products:

1. **Add to `level2Products` parameter** in `azure-pipelines.yml`:
   ```yaml
   - name: Umbraco.Ai.NewProduct
     changeVar: NewproductChanged
     hasNpm: false  # or true if it exports npm types
   ```

2. **No other pipeline changes needed** - the matrix strategy handles any number of Level 2 products

3. The `detect-changes.ps1` script will automatically:
   - Discover the new product
   - Calculate its dependency level
   - Output appropriate change variables

## Benefits

1. **Reduced Duplication**: Reusable templates eliminate ~100 lines of repeated code
2. **Scalable**: Easy to add more Level 2 products without pipeline changes
3. **Coordinated Releases**: Level 2 products can depend on Level 1 packages built in same run
4. **Consistent Pattern**: All pack jobs follow the same structure
5. **Maintainable**: Changes to pack logic can be made in one place (templates)

## Testing

To test the Level 2 implementation:

1. Make a change to `Umbraco.Ai.Agent.Copilot/` folder
2. Commit and push to `dev` branch
3. Verify in Azure DevOps:
   - DetectChanges reports `Level2Changed=true`, `CopilotChanged=true`
   - PackLevel2 job runs for Copilot
   - CollectPackages includes Copilot packages in final artifacts

## Notes

- The pipeline supports arbitrary dependency levels (Level 3, 4, etc.) through `detect-changes.ps1`
- Level 2+ products must specify `hasNpm: true` if they export TypeScript types for npm
- The `changeVar` naming convention should match the product key (e.g., "copilot" → "CopilotChanged")
