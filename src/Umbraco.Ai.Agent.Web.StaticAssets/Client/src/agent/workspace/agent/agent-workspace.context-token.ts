import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UAiAgentWorkspaceContext } from "./agent-workspace.context.js";

export const UAI_AGENT_WORKSPACE_CONTEXT = new UmbContextToken<UAiAgentWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UAiAgentWorkspaceContext => (context as UAiAgentWorkspaceContext).IS_AGENT_WORKSPACE_CONTEXT
);
