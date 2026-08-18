import type { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { configureAiClient } from "@umbraco-ai/core";
import { client } from "./api/client.gen.js";

// Re-export the public API
export * from "./exports.js";

/**
 * Resolves once the generated hey-api client is configured with backoffice auth.
 * Repositories/data-sources await this before their first request so the very
 * first call already carries a valid token (mirrors `agentClientReady`).
 */
let copilotWorkspaceClientReadyResolve: (() => void) | undefined;
export const copilotWorkspaceClientReady = new Promise<void>((resolve) => {
    copilotWorkspaceClientReadyResolve = resolve;
});

export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    configureAiClient(host, client).then(() => {
        copilotWorkspaceClientReadyResolve?.();
        copilotWorkspaceClientReadyResolve = undefined;
    });
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed.
};
