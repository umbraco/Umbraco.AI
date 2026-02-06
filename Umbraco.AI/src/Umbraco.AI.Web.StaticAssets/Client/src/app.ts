import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { client } from "./api/client.gen.ts";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";

// Re-export everything from the main index files
export * from "./index.js";
export * from "./exports.js";

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
    });
};

// Entry point cleanup
export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Clean up if needed
};
