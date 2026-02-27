import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { client } from "./api/client.gen.ts";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";

// Re-export everything from the main index files
export * from "./index.js";
export * from "./exports.js";

// Promise that resolves when the core client is configured with auth
let coreClientReadyResolve: (() => void) | undefined;
export const coreClientReady = new Promise<void>((resolve) => {
    coreClientReadyResolve = resolve;
});

// Entry point initialization
export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
    console.log("Umbraco AI Entrypoint initialized");

    // Workspace decorator is now initialized automatically via the
    // UaiWorkspaceRegistryContext global context

    _host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
        const config = authContext?.getOpenApiConfiguration();
        client.setConfig({
            auth: config?.token ?? undefined,
            baseUrl: config?.base ?? "",
            credentials: config?.credentials ?? "same-origin",
        });

        // Resolve the ready promise once auth is configured
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
