import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { client } from "./api/client.gen.ts";
import { configureAiClient } from "./core/client/index.js";
import { resolveCoreClientReady } from "./client-ready.js";

// Re-export the public API
export * from "./exports.js";

// Entry point initialization
export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    console.log("Umbraco AI Entrypoint initialized");

    // Workspace decorator is now initialized automatically via the
    // UaiWorkspaceRegistryContext global context

    configureAiClient(host, client).then(() => {
        resolveCoreClientReady();
    });
};

// Entry point cleanup
export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed
};
