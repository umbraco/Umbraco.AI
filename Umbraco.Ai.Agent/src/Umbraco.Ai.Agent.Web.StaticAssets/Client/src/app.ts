import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { client } from "./api/client.gen.ts";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";

// Ensure all exports from index are available from the bundle
export * from './index.js';

// Entry point initialization 
export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
    console.log("Umbraco AI Agent Entrypoint initialized");

    _host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
        if (!authContext) return;
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