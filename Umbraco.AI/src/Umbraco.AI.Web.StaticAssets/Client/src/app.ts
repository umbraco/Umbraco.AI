import { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { client } from "./api/client.gen.ts";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { UmbApiInterceptorController } from "@umbraco-cms/backoffice/resources";

// Re-export everything from the main index files
export * from "./index.js";
export * from "./exports.js";

// Promise that resolves when the core client is configured with auth
let coreClientReadyResolve: (() => void) | undefined;
export const coreClientReady = new Promise<void>((resolve) => {
    coreClientReadyResolve = resolve;
});

// Entry point initialization
export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    console.log("Umbraco AI Entrypoint initialized");

    // Bind the default response interceptors (401 recovery, error handling, notifications)
    // to our generated client so it self-heals like umbHttpClient. See CMS issue #22647.
    // Cast: per-project hey-api codegen produces nominally distinct Client types, but
    // bindDefaultInterceptors only uses the shared `interceptors.response.use(...)` surface.
    new UmbApiInterceptorController(host).bindDefaultInterceptors(client as never);

    // Workspace decorator is now initialized automatically via the
    // UaiWorkspaceRegistryContext global context

    host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
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
