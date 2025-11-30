import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";

export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
  console.log("Umbraco AI Prompt Entrypoint initialized");
  _host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
    if (!authContext) return;
    const config = authContext?.getOpenApiConfiguration();
    client.setConfig({
      baseUrl: config.base,
      auth: async () => await authContext.getLatestToken(),
      credentials: config.credentials ?? "same-origin",
    });
  });
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  // Clean up if needed
};
