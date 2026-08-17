import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { configureAiClient } from "@umbraco-ai/core";
import { client } from "./api/client.gen.ts";

// Re-export the public API
export * from "./exports.js";

// Promise that resolves when the agent client is configured with auth
let agentClientReadyResolve: (() => void) | undefined;
export const agentClientReady = new Promise<void>((resolve) => {
    agentClientReadyResolve = resolve;
});

// Entry point initialization
export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    console.log("Umbraco AI Agent Entrypoint initialized");

    configureAiClient(host, client).then(() => {
        if (agentClientReadyResolve) {
            agentClientReadyResolve();
            agentClientReadyResolve = undefined;
        }
    });
};

// Entry point cleanup
export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed
};
