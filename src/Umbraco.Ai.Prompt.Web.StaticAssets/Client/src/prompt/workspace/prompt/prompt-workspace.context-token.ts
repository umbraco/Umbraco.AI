import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiPromptWorkspaceContext } from "./prompt-workspace.context.js";

export const UAI_PROMPT_WORKSPACE_CONTEXT = new UmbContextToken<UaiPromptWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiPromptWorkspaceContext => (context as UaiPromptWorkspaceContext).IS_PROMPT_WORKSPACE_CONTEXT
);
