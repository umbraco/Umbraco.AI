import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { client } from "./api/client.gen.ts";
import { configureAiClient } from "./core/client/index.js";

// Re-export the public API
export * from "./exports.js";

// Promise that resolves when the core client is configured with auth
let coreClientReadyResolve: (() => void) | undefined;
export const coreClientReady = new Promise<void>((resolve) => {
    coreClientReadyResolve = resolve;
});

// Entry point initialization
export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    console.log("Umbraco AI Entrypoint initialized");

    // Workspace decorator is now initialized automatically via the
    // UaiWorkspaceRegistryContext global context

    configureAiClient(host, client).then(() => {
        if (coreClientReadyResolve) {
            coreClientReadyResolve();
            coreClientReadyResolve = undefined;
        }
    });
};

// Entry point cleanup
export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed
};
