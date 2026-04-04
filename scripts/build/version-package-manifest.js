/**
 * Adds a version query string to all JS paths in umbraco-package.json
 * to bust browser cache after package upgrades.
 *
 * The entry-point files (e.g. umbraco-ai-manifests.js) use stable names but
 * dynamically import Vite-hashed chunks that change every build. Without
 * cache-busting on the entry files themselves, browsers serve stale cached
 * entry files that reference chunks which no longer exist after an upgrade.
 *
 * Usage: node version-package-manifest.js <version> <manifest-path>
 */

import { readFileSync, writeFileSync } from 'fs';

const [,, version, manifestPath] = process.argv;

if (!version || !manifestPath) {
    console.error('Usage: node version-package-manifest.js <version> <manifest-path>');
    process.exit(1);
}

const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));

function addVersion(url) {
    if (!url || typeof url !== 'string') return url;
    // Don't double-add version
    if (url.includes('?v=')) return url;
    return `${url}?v=${version}`;
}

// Update js paths in extensions
if (Array.isArray(manifest.extensions)) {
    for (const ext of manifest.extensions) {
        if (ext.js) {
            ext.js = addVersion(ext.js);
        }
    }
}

// Update importmap imports
if (manifest.importmap?.imports) {
    for (const key of Object.keys(manifest.importmap.imports)) {
        manifest.importmap.imports[key] = addVersion(manifest.importmap.imports[key]);
    }
}

writeFileSync(manifestPath, JSON.stringify(manifest, null, 4) + '\n');
console.log(`Added version query string (v=${version}) to JS paths in ${manifestPath}`);
