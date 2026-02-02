import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from '@umbraco-cms/backoffice/extension-api';

// Ensure all exports from index are available from the bundle
export * from './index.js';
export * from './exports.js';


// Entry point initialization
export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
    console.log("Umbraco AI Copilot Entrypoint initialized");
};

// Entry point cleanup
export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed
};