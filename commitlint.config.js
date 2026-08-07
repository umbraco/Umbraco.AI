// Scope discovery lives in scripts/load-commit-scopes.js so `npm run commit-options`
// reports exactly what this config validates against.
const { loadScopes: loadAllScopes } = require("./scripts/load-commit-scopes");

// Dynamically discover and load all scopes from product config files
function loadScopes() {
    return loadAllScopes(__dirname);
}

module.exports = {
    extends: ["@commitlint/config-conventional"],
    plugins: [
        {
            rules: {
                "scope-not-type": (parsed) => {
                    const { type, scope } = parsed;
                    if (scope && type && scope === type) {
                        return [
                            false,
                            `Scope "${scope}" should not match type "${type}". Use just "${type}:" without a scope, or use a more specific scope like "${type}(hooks):", "${type}(build):", etc.`,
                        ];
                    }
                    return [true];
                },
                // Custom rule to validate multiple scopes (comma-separated)
                "scope-enum-multiple": (parsed) => {
                    const { scope } = parsed;
                    if (!scope) {
                        return [true]; // No scope is handled by scope-empty rule
                    }

                    const allowedScopes = loadScopes();
                    const scopes = scope.split(",").map((s) => s.trim());

                    // Validate each scope against the allowed list
                    const invalidScopes = scopes.filter((s) => !allowedScopes.includes(s));

                    if (invalidScopes.length > 0) {
                        return [
                            false,
                            `Invalid scope(s): ${invalidScopes.join(", ")}. Allowed scopes: ${allowedScopes.join(", ")}`,
                        ];
                    }

                    return [true];
                },
            },
        },
    ],
    rules: {
        // Disable the default scope-enum rule (we use scope-enum-multiple instead)
        "scope-enum": [0],
        "scope-enum-multiple": [2, "always"], // Enable our custom multi-scope validation
        "scope-empty": [1, "never"], // Warn if scope is missing (don't fail)
        "scope-case": [2, "always", ["lower-case", "kebab-case"]],
        "scope-not-type": [2, "always"], // Enable the custom rule
        "subject-case": [2, "always", "sentence-case"],
        "type-enum": [
            2,
            "always",
            ["feat", "fix", "docs", "chore", "refactor", "test", "perf", "ci", "revert", "build"],
        ],
    },
};
