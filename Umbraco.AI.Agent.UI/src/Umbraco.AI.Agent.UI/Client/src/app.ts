// Re-export the public API
export * from "./exports.js";

/**
 * Backoffice entry point for @umbraco-ai/agent-ui.
 *
 * Registers all shared chat manifests (kinds, approval elements, localization).
 */
export const onInit = (_host: unknown) => {
    console.debug("Initializing Umbraco AI Agent UI package...");
};
