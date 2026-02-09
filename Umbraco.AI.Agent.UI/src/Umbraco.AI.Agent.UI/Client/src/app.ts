import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { manifests } from "./manifests.js";

/**
 * Backoffice entry point for @umbraco-ai/agent-ui.
 *
 * Registers all shared chat manifests (kinds, approval elements, localization).
 */
export const onInit = (_host: unknown) => {
    umbExtensionsRegistry.registerMany(manifests);
};
