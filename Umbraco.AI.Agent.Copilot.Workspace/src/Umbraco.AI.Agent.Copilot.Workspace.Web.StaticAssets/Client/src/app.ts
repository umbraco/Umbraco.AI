import type { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";

// Ensure all exports are available from the bundle.
export * from "./index.js";
export * from "./exports.js";

export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
    // Reserved for Phase 5 runtime wiring (context providers, etc.).
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed.
};
