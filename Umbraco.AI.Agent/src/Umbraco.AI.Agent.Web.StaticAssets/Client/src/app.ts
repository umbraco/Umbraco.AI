import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { configureAiClient } from "@umbraco-ai/core";
import { client } from "./api/client.gen.ts";
import { resolveAgentClientReady } from "./client-ready.js";

// Re-export the public API
export * from "./exports.js";

// Entry point initialization
export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    console.log("Umbraco AI Agent Entrypoint initialized");

    configureAiClient(host, client).then(() => {
        resolveAgentClientReady();
    });
};

// Entry point cleanup
export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed
};
