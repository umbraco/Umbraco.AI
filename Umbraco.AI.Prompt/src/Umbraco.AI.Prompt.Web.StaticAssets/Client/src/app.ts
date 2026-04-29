import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { configureAiClient } from "@umbraco-ai/core";
import { client } from "./api/client.gen.ts";
import { UmbPromptRegistrarController } from "./prompt/controllers";

// Re-export everything from the main index
export * from "./index.js";

// Promise that resolves when the prompt client is configured with auth
let promptClientReadyResolve: (() => void) | undefined;
export const promptClientReady = new Promise<void>((resolve) => {
    promptClientReadyResolve = resolve;
});

// Keep registrar alive for the lifetime of the app (prevents garbage collection)
let promptRegistrar: UmbPromptRegistrarController | null = null;

// Initialize the entry point
export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    console.log("Umbraco AI Prompt Entrypoint initialized");

    configureAiClient(host, client).then(async () => {
        if (promptClientReadyResolve) {
            promptClientReadyResolve();
            promptClientReadyResolve = undefined;
        }

        // Register prompt property actions after authentication is established
        // Store in module variable to prevent garbage collection
        promptRegistrar = new UmbPromptRegistrarController(host);
        await promptRegistrar.registerPrompts();
    });
};

// Cleanup if needed
export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed
};
