#!/usr/bin/env node

/**
 * Lists valid commit types and scopes from commitlint.config.js
 * Usage: node scripts/list-commit-options.js
 */

const path = require("path");
const configPath = path.join(__dirname, "..", "commitlint.config.js");
const config = require(configPath);
const { META_SCOPES, loadProductScopes } = require("./load-commit-scopes");

const rootDir = path.join(__dirname, "..");

console.log("═══════════════════════════════════════════════════════");
console.log("Valid Commit Types and Scopes");
console.log("═══════════════════════════════════════════════════════");
console.log();

console.log("Valid Types:");
console.log("─".repeat(55));
const types = config.rules["type-enum"][2];
types.forEach((type) => console.log(`  • ${type}`));
console.log();

console.log("Valid Scopes:");
console.log("─".repeat(55));
const productScopes = loadProductScopes(rootDir);
for (const product of Object.keys(productScopes).sort()) {
    console.log(`  ${product}:`);
    console.log(`    ${productScopes[product].join(", ")}`);
}
console.log("  Meta:");
console.log(`    ${[...META_SCOPES].sort().join(", ")}`);
console.log();
console.log("  Multiple comma-separated scopes are allowed, e.g. feat(core,openai):");
console.log();

console.log("Example:");
console.log("─".repeat(55));
console.log("  feat(chat): Add streaming support");
console.log("  fix(openai): Handle rate limit errors");
console.log("  docs(core): Update API examples");
console.log();
