# npm Peer Dependency Version Management

This document explains how npm peer dependency version ranges are managed in the Umbraco.AI monorepo.

## Overview

During development, packages use workspace references (`*`) for inter-product dependencies. During packaging, these are automatically replaced with proper semver ranges (e.g., `^1.2.0`) to ensure correct peer dependency resolution in published packages.

This system mirrors the .NET `Directory.Packages.props` pattern for consistency across both package ecosystems.

## File Structure

```
Umbraco.AI/                                    # Root
├── package.peer-dependencies.json             # Default ranges for all products
├── Umbraco.AI/
│   └── package.peer-dependencies.json        # (optional) Product-specific overrides
├── Umbraco.AI.Agent/
│   └── package.peer-dependencies.json        # (optional) Product-specific overrides
└── scripts/build/
    └── cleanse-package-json.js               # Script that applies ranges during packaging
```

## Root Configuration

**`package.peer-dependencies.json`** (repository root)

Defines default peer dependency version ranges for all products:

```json
{
  "$schema": "https://json.schemastore.org/package.json",
  "_comment": "Default peer dependency version ranges...",
  "@umbraco-ai/core": "^1.2.0",
  "@umbraco-ai/agent": "^1.2.0",
  "@umbraco-ai/agent-ui": "^1.0.0",
  "@umbraco-cms/backoffice": "^17.1.0"
}
```

**Note:** Keys starting with `$` or `_` are ignored (metadata only).

## Product-Level Overrides (Optional)

Products can override specific ranges by creating their own `package.peer-dependencies.json`:

**`Umbraco.AI.Agent/package.peer-dependencies.json`**

```json
{
  "@umbraco-ai/core": "^1.3.0"  // Agent requires core 1.3.0+
}
```

Product overrides take precedence over root defaults.

## How It Works

### During Development

Packages reference each other using workspace protocol in `package.json`:

```json
{
  "dependencies": {
    "@umbraco-ai/core": "*",
    "@umbraco-cms/backoffice": "^17.1.0"
  }
}
```

npm workspaces resolve `*` to the local package, enabling fast iteration.

### During CI/CD Packaging

When creating npm packages (`.azure-pipelines/templates/pack-product.yml`):

1. **Load Ranges**
   - Read root `package.peer-dependencies.json`
   - If exists, read product `<Product>/package.peer-dependencies.json`
   - Merge (product overrides root)

2. **Cleanse package.json** (`scripts/build/cleanse-package-json.js`)
   - Update `version` to match NuGet version (from NBGV)
   - Convert `dependencies` → `peerDependencies`
   - For each dependency:
     - **If listed in `package.peer-dependencies.json`:** Use that version (source of truth)
     - **If NOT listed:** Keep original version from package.json
   - Remove `devDependencies` and `scripts`

3. **Result**
   ```json
   {
     "version": "1.2.0",
     "peerDependencies": {
       "@umbraco-ai/core": "^1.2.0",       // Resolved from config
       "@umbraco-cms/backoffice": "^17.1.0", // Resolved from config
       "chart.js": "^4.5.1"                 // Not in config, kept as-is
     }
   }
   ```

**Important:** `package.peer-dependencies.json` is the single source of truth. If a package is listed there, that version range will ALWAYS be used, overriding whatever is in the individual package.json. This prevents version drift and ensures consistency.

## Keeping in Sync with .NET

**IMPORTANT:** `package.peer-dependencies.json` should mirror the minimum versions from `Directory.Packages.props`.

### .NET Version Range
```xml
<PackageVersion Include="Umbraco.AI.Core" Version="[1.2.0, 1.999.999)" />
```
- `[1.2.0, 1.999.999)` = >=1.2.0 AND <2.0.0

### npm Equivalent
```json
{
  "@umbraco-ai/core": "^1.2.0"
}
```
- `^1.2.0` = >=1.2.0 AND <2.0.0

Both express "minimum 1.2.0, accept all 1.x".

## Version Range Guidelines

| .NET Range                    | npm Range | Meaning                            |
|-------------------------------|-----------|------------------------------------|
| `[1.0.0, 1.999.999)`         | `^1.0.0`  | 1.0.0 ≤ version < 2.0.0            |
| `[1.2.0, 1.999.999)`         | `^1.2.0`  | 1.2.0 ≤ version < 2.0.0            |
| `[17.1.0, 17.999.999)`       | `^17.1.0` | 17.1.0 ≤ version < 18.0.0          |

## Maintenance

### When Bumping Minimum Versions

If a product now requires a newer minimum version:

**Option 1: Root Change (affects all products)**
```bash
# Edit package.peer-dependencies.json
{
  "@umbraco-ai/core": "^1.3.0"  # Updated from ^1.2.0
}
```

**Option 2: Product Override (affects one product)**
```bash
# Create Umbraco.AI.Agent/package.peer-dependencies.json
{
  "@umbraco-ai/core": "^1.3.0"  # Agent needs 1.3.0+, others stay at 1.2.0+
}
```

### Validation

The `/release-management` skill should:
- Check inter-product dependencies in `Directory.Packages.props`
- Verify corresponding ranges exist in `package.peer-dependencies.json`
- Warn about mismatches

## Testing Locally

You can test the cleanse script locally:

```bash
cd Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client

# Run cleanse (simulates CI/CD behavior)
node ../../../../scripts/build/cleanse-package-json.js "1.2.0" "Umbraco.AI" "../../../../"

# Check output
cat package.json
```

**Note:** This modifies `package.json` in-place. Run `git restore package.json` to undo.

## Troubleshooting

### Workspace reference not replaced

**Symptom:** Published package has `"@umbraco-ai/core": "*"`

**Cause:** No entry in `package.peer-dependencies.json` for that package

**Fix:** Add the missing entry to root or product-level config

### Version mismatch between .NET and npm

**Symptom:** .NET requires `1.3.0+` but npm peer dep is `^1.2.0`

**Cause:** `package.peer-dependencies.json` not updated when `Directory.Packages.props` changed

**Fix:** Update `package.peer-dependencies.json` to match minimum version in `Directory.Packages.props`

## See Also

- [CLAUDE.md - Cross-Product Dependency Management](../../CLAUDE.md#cross-product-dependency-management)
- [Directory.Packages.props](../../Directory.Packages.props)
- [Azure Pipeline: pack-product.yml](../../.azure-pipelines/templates/pack-product.yml)
