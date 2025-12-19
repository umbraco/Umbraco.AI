import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UAiAgentWorkspaceContext } from "./prompt-workspace.context.js";

export const UAI_PROMPT_WORKSPACE_CONTEXT = new UmbContextToken<UAiAgentWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UAiAgentWorkspaceContext => (context as UAiAgentWorkspaceContext).IS_PROMPT_WORKSPACE_CONTEXT
);
