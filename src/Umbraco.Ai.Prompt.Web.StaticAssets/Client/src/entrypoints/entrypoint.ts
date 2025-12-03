import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";
import { UmbPromptRegistrarController } from "../prompt/controllers/prompt-registrar.controller.js";

export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
  console.log("Umbraco AI Prompt Entrypoint initialized");
  _host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
    if (!authContext) return;
    const config = authContext?.getOpenApiConfiguration();
    client.setConfig({
      auth: config?.token ?? undefined,
      baseUrl: config?.base ?? "",
      credentials: config?.credentials ?? "same-origin",
    });

    // Register prompt property actions after authentication is established
    const registrar = new UmbPromptRegistrarController(_host);
    await registrar.registerPrompts();
  });
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  // Clean up if needed
};
