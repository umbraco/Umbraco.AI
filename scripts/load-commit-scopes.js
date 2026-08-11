/**
 * Discovers the commit scopes allowed by commitlint.
 *
 * Shared by commitlint.config.js (validation) and scripts/list-commit-options.js
 * (discovery), so both always report the same list.
 */

const fs = require("fs");
const path = require("path");

// Common meta scopes, valid regardless of product
const META_SCOPES = [
    "deps", // Dependency updates
    "ci", // CI/CD changes
    "docs", // Documentation
    "release", // Release-related changes
    "hooks", // Git hooks
    "build", // Build system
    "config", // Configuration files
];

/**
 * Loads product scopes from every Umbraco.AI*\/changelog.config.json, keyed by product.
 * @param {string} rootDir Repository root
 * @returns {Record<string, string[]>}
 */
function loadProductScopes(rootDir = path.join(__dirname, "..")) {
    const productScopes = {};

    // Find all Umbraco.AI* directories
    const entries = fs.readdirSync(rootDir, { withFileTypes: true });

    for (const entry of entries) {
        if (!entry.isDirectory()) continue;
        if (!entry.name.startsWith("Umbraco.AI")) continue;

        const configPath = path.join(rootDir, entry.name, "changelog.config.json");

        // Load product scopes from changelog.config.json
        if (fs.existsSync(configPath)) {
            try {
                const config = JSON.parse(fs.readFileSync(configPath, "utf-8"));
                if (config.scopes && Array.isArray(config.scopes)) {
                    productScopes[entry.name] = [...config.scopes].sort();
                }
            } catch (err) {
                console.warn(`Warning: Could not load scopes from ${configPath}:`, err.message);
            }
        }
    }

    return productScopes;
}

/**
 * Loads the full sorted list of allowed scopes (product scopes + meta scopes).
 * @param {string} rootDir Repository root
 * @returns {string[]}
 */
function loadScopes(rootDir = path.join(__dirname, "..")) {
    const scopes = new Set();

    for (const productScopeList of Object.values(loadProductScopes(rootDir))) {
        productScopeList.forEach((scope) => scopes.add(scope));
    }

    META_SCOPES.forEach((scope) => scopes.add(scope));

    return Array.from(scopes).sort();
}

module.exports = { META_SCOPES, loadProductScopes, loadScopes };
