import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiSettingsWorkspaceContext } from "./settings-workspace.context.js";

export const UAI_SETTINGS_WORKSPACE_CONTEXT = new UmbContextToken<UaiSettingsWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiSettingsWorkspaceContext => (context as UaiSettingsWorkspaceContext).IS_SETTINGS_WORKSPACE_CONTEXT
);
