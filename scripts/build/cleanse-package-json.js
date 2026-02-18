/**
 * Cleanses package.json for npm publishing:
 * - Updates version to match NuGet version
 * - Converts dependencies to peerDependencies
 * - Resolves workspace references (*) using peerDependencyVersions from root package.json
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

// Load peer dependency version ranges from root package.json
console.log('Loading peer dependency version ranges...');

let peerDepRanges = {};

const rootPackageJsonPath = join(sourceDirectory, 'package.json');
if (existsSync(rootPackageJsonPath)) {
  const rootPackageJson = JSON.parse(readFileSync(rootPackageJsonPath, 'utf8'));
  if (rootPackageJson.peerDependencyVersions) {
    peerDepRanges = { ...rootPackageJson.peerDependencyVersions };
    console.log('  ✓ Loaded root peer dependency ranges from package.json');
  } else {
    console.warn('  ⚠ No peerDependencyVersions found in root package.json');
  }
} else {
  console.warn('  ⚠ Root package.json not found');
}

console.log('');

// Cleanse package.json for publishing
console.log('Cleansing package.json for publishing...');

// Remove devDependencies
delete pkg.devDependencies;
console.log('  ✓ Removed devDependencies');

// Convert dependencies to peerDependencies with version range resolution
// Resolution order (highest precedence first):
// 1. Existing peerDependencies in package.json
// 2. Root peerDependencyVersions
// 3. Original dependency version
if (pkg.dependencies) {
  const convertedDeps = {};
  const resolutions = [];

  Object.keys(pkg.dependencies).forEach(dep => {
    // Skip if already defined in peerDependencies (rule 1: package's own peerDeps take precedence)
    if (pkg.peerDependencies && pkg.peerDependencies[dep]) {
      return;
    }

    let version = pkg.dependencies[dep];

    // Rule 2: Check root peerDependencyVersions
    if (peerDepRanges[dep]) {
      const oldVersion = version;
      version = peerDepRanges[dep];
      if (oldVersion !== version) {
        resolutions.push(`    ${dep}: ${oldVersion} → ${version}`);
      }
    } else if (version === '*' || version.startsWith('workspace:')) {
      // Rule 3: No configured range, warn but keep as-is
      console.warn(`  ⚠ Warning: No peer dependency range configured for ${dep}, keeping as-is`);
    }

    convertedDeps[dep] = version;
  });

  // Merge converted dependencies with existing peerDependencies (existing take precedence)
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
