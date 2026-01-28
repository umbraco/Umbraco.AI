# Build Pipeline Documentation

This document explains how the Umbraco.Ai monorepo build pipeline works and how to add new products.

## Key Features

✅ **Zero Configuration** - Products are auto-discovered by folder naming convention
✅ **Smart Dependencies** - Dependencies extracted from actual `.csproj` files
✅ **Automatic Build Ordering** - Topological sort ensures correct build sequence
✅ **Change Propagation** - When Core changes, all dependents rebuild automatically
✅ **Parallel Builds** - Products at the same dependency level build in parallel

## Overview

The pipeline automatically detects which products have changed and builds them in the correct order based on their dependencies. Products with no dependencies build first, and dependent products build in subsequent stages.

### How It Works

The `detect-changes.ps1` script orchestrates the entire process in 6 clear steps:

```
1. Product Discovery → Scans for Umbraco.Ai.* folders
2. Dependency Analysis → Parses all .csproj files
3. Build Order Calculation → Topological sort
4. Change Detection → Git diff analysis
5. Change Propagation → Rebuild dependents
6. Output Generation → Azure DevOps variables
```

**Build Levels:**
- **Level 0**: No dependencies (e.g., Core)
- **Level 1**: Depends only on Level 0 products (e.g., Agent, Prompt, OpenAi, Anthropic)
- **Level 2+**: Depends on Level 1+ products (reserved for future use)

**Sequential Stages, Parallel Jobs**: Each level builds sequentially, but products within a level build in parallel

## Adding a New Product

**The pipeline automatically discovers products and their dependencies!** Just follow these steps:

### 1. Create Your Product Folder

Create a folder following the naming convention: `Umbraco.Ai.[ProductName]/`

**Examples:**
- `Umbraco.Ai/` (core package)
- `Umbraco.Ai.Agent/`
- `Umbraco.Ai.MyProduct/`

### 2. Add Project References

In your `.csproj` file, add references to the Umbraco.Ai products you depend on:

```xml
<!-- For local development -->
<ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
  <ProjectReference Include="..\..\..\Umbraco.Ai\src\Umbraco.Ai.Core\Umbraco.Ai.Core.csproj" />
</ItemGroup>

<!-- For CI/CD builds -->
<ItemGroup Condition="'$(UseProjectReferences)' != 'true'">
  <PackageReference Include="Umbraco.Ai.Core" />
</ItemGroup>
```

**The script will automatically:**
- Discover your product by scanning for `Umbraco.Ai.*` folders
- Parse your `.csproj` to find dependencies
- Calculate the correct build level
- Generate pipeline variables

### 3. Add the Product to the Pipeline

**Determine your build level:**
- The detection script will automatically calculate this based on your dependencies
- Run the script locally to see which level your product is assigned to
- Products depending on Core are Level 1
- Products depending on Level 1 products are Level 2, etc.

**Add to `azure-pipelines.yml` at the appropriate build level:**

**Example: Product depending on Core (Level 1)**

```yaml
- stage: BuildLevel1
  displayName: 'Build Level 1'
  jobs:
    # ... existing products ...

    - template: build/templates/build-product.yml
      parameters:
        productName: 'Umbraco.Ai.MyProduct'
        productPath: 'Umbraco.Ai.MyProduct'
        hasFrontend: true  # or false if no frontend
        configuration: $(configuration)
        condition: eq(stageDependencies.DetectChanges.DetectChanges.outputs['detectChanges.MyproductChanged'], 'true')
```

**Note:** The variable name is auto-generated:
- `Umbraco.Ai` → `CoreChanged`
- `Umbraco.Ai.Agent` → `AgentChanged`
- `Umbraco.Ai.OpenAi` → `OpenaiChanged` (note: lowercase 'ai')
- `Umbraco.Ai.MyProduct` → `MyproductChanged`

**Example: Product depending on Agent (Level 2)**

```yaml
- stage: BuildLevel2
  displayName: 'Build Level 2'
  jobs:
    # Remove the placeholder job first!

    - template: build/templates/build-product.yml
      parameters:
        productName: 'Umbraco.Ai.MyProduct'
        productPath: 'Umbraco.Ai.MyProduct'
        hasFrontend: true
        configuration: $(configuration)
        condition: eq(stageDependencies.DetectChanges.DetectChanges.outputs['detectChanges.MyproductChanged'], 'true')
```

### 3. Add Tests (Optional)

If your product has tests, add a test job to the `Test` stage in `azure-pipelines.yml`:

```yaml
- stage: Test
  jobs:
    # ... existing tests ...

    - job: TestMyProduct
      displayName: 'Test Umbraco.Ai.MyProduct'
      pool:
        vmImage: $(vmImage)
      condition: eq(stageDependencies.DetectChanges.DetectChanges.outputs['detectChanges.MyProductChanged'], 'true')
      steps:
        - task: DotNetCoreCLI@2
          displayName: 'Run MyProduct tests'
          inputs:
            command: 'test'
            projects: 'Umbraco.Ai.MyProduct/Umbraco.Ai.MyProduct.sln'
            arguments: '--configuration $(configuration)'
```

## Testing Locally

You can test the auto-discovery and dependency resolution locally:

```powershell
# Run the detection script
.\build\scripts\detect-changes.ps1

# Or specify a custom root path
.\build\scripts\detect-changes.ps1 -RootPath "C:\path\to\repo"
```

**Output will show:**
- All discovered products
- Dependencies for each product
- Build levels calculated from dependencies
- Which products would be built (based on git changes)

**Example output:**
```
Auto-discovering products...
  ✓ Found: Umbraco.Ai
  ✓ Found: Umbraco.Ai.Agent
  ✓ Found: Umbraco.Ai.OpenAi

Analyzing project dependencies...
  Umbraco.Ai → (no dependencies)
  Umbraco.Ai.Agent → Umbraco.Ai
  Umbraco.Ai.OpenAi → Umbraco.Ai

Build levels:
  Level 0: core
    Changed: core
  Level 1: agent, openai
    Changed: agent, openai
```

## How Dependencies Work

### Automatic Dependency Resolution

**The pipeline discovers dependencies by parsing your `.csproj` files!**

No manual configuration needed - the script:
1. Scans for all `Umbraco.Ai.*` folders
2. Finds the main `.csproj` file for each product
3. Parses `<ProjectReference>` and `<PackageReference>` elements
4. Extracts references to other Umbraco.Ai products
5. Builds a dependency graph
6. Calculates build levels using topological sorting

**Current build hierarchy (auto-detected):**

```
Level 0: Core (no dependencies)
         ↓
Level 1: Agent, Prompt, OpenAi, Anthropic (all depend on Core)
         ↓
Level 2: [Any future products that depend on Level 1 products]
```

### Change Propagation

When a product changes, all dependent products are automatically rebuilt:

```
Core changes → Agent, Prompt, OpenAi, Anthropic rebuild
Agent changes → Any future products depending on Agent rebuild
```

This ensures that dependent products always build against the latest version of their dependencies.

## Project References vs Package References

### During Development (Local)

Projects use `<ProjectReference>` when `UseProjectReferences=true` (the default):

```xml
<ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
  <ProjectReference Include="..\..\..\Umbraco.Ai\src\Umbraco.Ai.Core\Umbraco.Ai.Core.csproj" />
</ItemGroup>
```

### During CI/CD Build

The pipeline builds with `UseProjectReferences=false`, which switches to `<PackageReference>`:

```xml
<ItemGroup Condition="'$(UseProjectReferences)' != 'true'">
  <PackageReference Include="Umbraco.Ai.Core" />
</ItemGroup>
```

This is why build levels are important - dependent products need their dependency packages to be built and available first.

## Build Levels

The pipeline supports up to 5 build levels (0-4). Each level:
- Builds only after the previous level completes successfully
- Builds all products in that level in parallel
- Can be skipped if no products at that level need building

### Current Build Levels

| Level | Products | Dependencies |
|-------|----------|--------------|
| 0 | Core | None |
| 1 | Agent, Prompt, OpenAi, Anthropic | Core |
| 2-4 | (Reserved) | (Future use) |

## Troubleshooting

### "The type or namespace name 'X' does not exist"

This usually means a product is building before its dependencies. Check:

1. The `DependsOn` array in `detect-changes.ps1` is correct
2. The product is in the correct build level stage in `azure-pipelines.yml`
3. The build level condition allows the stage to run

### "No Level X products yet"

This is normal - placeholder jobs exist for unused build levels. They will automatically be replaced when you add products at those levels.

### Products building unnecessarily

Check the dependency configuration in `detect-changes.ps1`. Overly broad dependencies will cause unnecessary rebuilds.

## Best Practices

1. **Minimize dependencies**: Only depend on what you actually need
2. **Keep levels shallow**: Try to avoid deep dependency chains (Level 3+)
3. **Document dependencies**: Update this README when adding new products
4. **Test locally first**: Use `UseProjectReferences=false` locally to test the package reference mode

## Script Architecture

The `detect-changes.ps1` script is organized into focused, well-documented sections:

**📋 Sections:**
- **Utility Functions** - Product key/variable name conversions
- **Product Discovery** - Auto-discovery of products and subprojects
- **Dependency Analysis** - `.csproj` parsing and dependency extraction
- **Build Order Calculation** - Topological sorting algorithms
- **Change Detection** - Git diff and tag-based detection
- **Output Generation** - Azure DevOps pipeline variable output

**✨ Key Improvements:**
- **Comment-based help** - Every function has `.SYNOPSIS` and `.DESCRIPTION`
- **Error handling** - Try-catch blocks for XML parsing failures
- **Clear flow** - Main execution explicitly shows 6-step process
- **Focused functions** - Each function has a single, clear responsibility
- **Better organization** - Logical grouping with section headers

**📊 Complexity:**
- ~420 lines (including documentation)
- 11 focused functions
- O(n) complexity for most operations (n = number of products)
- O(n²) for dependency graph construction (acceptable for small n)
