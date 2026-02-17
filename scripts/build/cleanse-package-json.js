/**
 * Cleanses package.json for npm publishing:
 * - Updates version to match NuGet version
 * - Converts dependencies to peerDependencies
 * - Resolves workspace references (*) using package.peer-dependencies.json
 * - Removes devDependencies and scripts
 *
 * Usage: node cleanse-package-json.js <version> <product> <sourceDirectory>
 */

import { readFileSync, writeFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

// Get arguments
const [,, version, product, sourceDirectory] = process.argv;

if (!version || !product || !sourceDirectory) {
  console.error('Usage: node cleanse-package-json.js <version> <product> <sourceDirectory>');
  process.exit(1);
}

// Read package.json
const pkg = JSON.parse(readFileSync('package.json', 'utf8'));

// Update version to match NuGet version
pkg.version = version;
console.log(`Updated package.json version to: ${pkg.version}`);

// Load peer dependency version ranges
console.log('Loading peer dependency version ranges...');

let peerDepRanges = {};

// Load root package.peer-dependencies.json
const rootPeerDepsPath = join(sourceDirectory, 'package.peer-dependencies.json');
if (existsSync(rootPeerDepsPath)) {
  const rootConfig = JSON.parse(readFileSync(rootPeerDepsPath, 'utf8'));
  // Remove special keys like $schema and _comment
  Object.keys(rootConfig).forEach(key => {
    if (!key.startsWith('$') && !key.startsWith('_')) {
      peerDepRanges[key] = rootConfig[key];
    }
  });
  console.log('  ✓ Loaded root peer dependency ranges');
} else {
  console.warn('  ⚠ No root package.peer-dependencies.json found');
}

// Load product-level package.peer-dependencies.json (if exists)
const productPeerDepsPath = join(sourceDirectory, product, 'package.peer-dependencies.json');
if (existsSync(productPeerDepsPath)) {
  const productConfig = JSON.parse(readFileSync(productPeerDepsPath, 'utf8'));
  // Remove special keys and merge (product overrides root)
  Object.keys(productConfig).forEach(key => {
    if (!key.startsWith('$') && !key.startsWith('_')) {
      peerDepRanges[key] = productConfig[key];
    }
  });
  console.log('  ✓ Loaded product-level peer dependency ranges (overrides applied)');
}

console.log('');

// Cleanse package.json for publishing
console.log('Cleansing package.json for publishing...');

// Remove devDependencies
delete pkg.devDependencies;
console.log('  ✓ Removed devDependencies');

// Convert dependencies to peerDependencies with version range resolution
if (pkg.dependencies) {
  const convertedDeps = {};
  const resolutions = [];

  Object.keys(pkg.dependencies).forEach(dep => {
    let version = pkg.dependencies[dep];

    // If package is defined in peer-dependencies.json, ALWAYS use that version
    // This ensures package.peer-dependencies.json is the single source of truth
    if (peerDepRanges[dep]) {
      const oldVersion = version;
      version = peerDepRanges[dep];
      if (oldVersion !== version) {
        resolutions.push(`    ${dep}: ${oldVersion} → ${version}`);
      }
    } else if (version === '*' || version.startsWith('workspace:')) {
      // Warn if workspace reference has no configured range
      console.warn(`  ⚠ Warning: No peer dependency range configured for ${dep}, keeping as-is`);
    }

    convertedDeps[dep] = version;
  });

  // Merge with existing peerDependencies (existing take precedence)
  pkg.peerDependencies = {
    ...convertedDeps,
    ...(pkg.peerDependencies || {})
  };

  delete pkg.dependencies;

  console.log('  ✓ Converted dependencies to peerDependencies');
  if (resolutions.length > 0) {
    console.log('  ✓ Resolved workspace references:');
    resolutions.forEach(r => console.log(r));
  }
}

// Remove scripts (not needed in published package)
delete pkg.scripts;
console.log('  ✓ Removed scripts');

// Write back to disk
writeFileSync('package.json', JSON.stringify(pkg, null, 2) + '\n');
console.log('');
console.log('✓ Package cleansing complete');
