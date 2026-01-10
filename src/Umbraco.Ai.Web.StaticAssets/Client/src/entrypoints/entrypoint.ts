import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";
import { initWorkspaceDecorator } from "../workspace-registry/index.js";

// load up the manifests here

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
  console.log("Umbraco AI Entrypoint initialized");

  // Initialize workspace decorator for cross-DOM workspace access
  initWorkspaceDecorator(extensionRegistry);

  _host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
    const config = authContext?.getOpenApiConfiguration();
    client.setConfig({
      auth: config?.token ?? undefined,
      baseUrl: config?.base ?? "",
      credentials: config?.credentials ?? "same-origin",
    });
  });
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  // Clean up if needed
};
