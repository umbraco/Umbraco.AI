# npm Peer Dependency Version Management

This document explains how npm peer dependency version ranges are managed in the Umbraco.AI monorepo.

## Overview

During development, packages use workspace references (`*`) for inter-product dependencies. During packaging, these are automatically replaced with proper semver ranges (e.g., `^1.2.0`) to ensure correct peer dependency resolution in published packages.

This system mirrors the .NET `Directory.Packages.props` pattern for consistency across both package ecosystems.

## File Structure

```
Umbraco.AI/                                    # Root
├── package.json                               # Root package.json with peerDependencyVersions field
├── Umbraco.AI/
│   └── src/Umbraco.AI.Web.StaticAssets/Client/
│       └── package.json                       # Can define peerDependencies for overrides
├── Umbraco.AI.Agent/
│   └── src/Umbraco.AI.Agent.Web.StaticAssets/Client/
│       └── package.json                       # Can define peerDependencies for overrides
└── scripts/build/
    └── cleanse-package-json.js               # Script that applies ranges during packaging
```

## Root Configuration

**`package.json`** (repository root) → `peerDependencyVersions` field

Defines default peer dependency version ranges for all products:

```json
{
  "name": "umbraco-ai-monorepo",
  "peerDependencyVersions": {
    "@umbraco-ai/core": "^1.2.0",
    "@umbraco-ai/agent": "^1.2.0",
    "@umbraco-ai/agent-ui": "^1.0.0",
    "@umbraco-cms/backoffice": "^17.1.0"
  },
  "_comment_peerDependencyVersions": "Default peer dependency version ranges..."
}

## Product-Level Overrides (Optional)

Products can override specific ranges by defining `peerDependencies` directly in their package.json:

**`Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/package.json`**

```json
{
  "name": "@umbraco-ai/agent",
  "peerDependencies": {
    "@umbraco-ai/core": "^1.3.0"  // Agent requires core 1.3.0+
  }
}
```

Package-level peerDependencies take precedence over root defaults.

## How It Works

### During Development

Packages reference each other using workspace protocol in `package.json`:

```json
{
  "dependencies": {
    "@umbraco-ai/core": "*",
    "@umbraco-cms/backoffice": "^17.1.0"
  },
  "peerDependencies": {
    "@umbraco-ai/core": "^1.3.0"  // Optional: Override root default for this product
  }
}
```

npm workspaces resolve `*` to the local package, enabling fast iteration.

### During CI/CD Packaging

When creating npm packages (`.azure-pipelines/templates/pack-product.yml`):

1. **Load Ranges**
   - Read root `package.json` → `peerDependencyVersions` field

2. **Cleanse package.json** (`scripts/build/cleanse-package-json.js`)
   - Update `version` to match NuGet version (from NBGV)
   - Convert `dependencies` → `peerDependencies` using resolution order:
     1. **If package already has `peerDependencies`:** Use those (highest priority)
     2. **Else if listed in root `peerDependencyVersions`:** Use that version
     3. **Else:** Keep original version from dependencies
   - Remove `devDependencies` and `scripts`

3. **Result**
   ```json
   {
     "version": "1.2.0",
     "peerDependencies": {
       "@umbraco-ai/core": "^1.3.0",       // From package's own peerDependencies
       "@umbraco-cms/backoffice": "^17.1.0", // Resolved from root config
       "chart.js": "^4.5.1"                 // Not in config, kept as-is
     }
   }
   ```

**Resolution Order (Highest Priority First):**
1. Package's own `peerDependencies` (most specific)
2. Root `peerDependencyVersions` (default for all products)
3. Original dependency version (fallback)

## Keeping in Sync with .NET

**IMPORTANT:** The `peerDependencyVersions` field should mirror the minimum versions from `Directory.Packages.props`.

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
# Edit package.json → peerDependencyVersions
{
  "peerDependencyVersions": {
    "@umbraco-ai/core": "^1.3.0"  // Updated from ^1.2.0
  }
}
```

**Option 2: Product Override (affects one product)**
```bash
# Edit Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/package.json
{
  "name": "@umbraco-ai/agent",
  "dependencies": {
    "@umbraco-ai/core": "*"
  },
  "peerDependencies": {
    "@umbraco-ai/core": "^1.3.0"  # Agent needs 1.3.0+, others stay at 1.2.0+
  }
}
```

### Validation

The `/release-management` skill should:
- Check inter-product dependencies in `Directory.Packages.props`
- Verify corresponding ranges exist in `peerDependencyVersions`
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

**Cause:** No entry in `peerDependencyVersions` and package doesn't define its own `peerDependencies`

**Fix:** Add the missing entry to root `package.json` → `peerDependencyVersions` or define it in the package's own `peerDependencies`

### Version mismatch between .NET and npm

**Symptom:** .NET requires `1.3.0+` but npm peer dep is `^1.2.0`

**Cause:** `peerDependencyVersions` not updated when `Directory.Packages.props` changed

**Fix:** Update `peerDependencyVersions` in root `package.json` to match minimum version in `Directory.Packages.props`

## See Also

- [CLAUDE.md - Cross-Product Dependency Management](../../CLAUDE.md#cross-product-dependency-management)
- [Directory.Packages.props](../../Directory.Packages.props)
- [Azure Pipeline: pack-product.yml](../../.azure-pipelines/templates/pack-product.yml)
