import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiAgentWorkspaceContext } from "./agent-workspace.context.js";

export const UAI_AGENT_WORKSPACE_CONTEXT = new UmbContextToken<UaiAgentWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiAgentWorkspaceContext => (context as UaiAgentWorkspaceContext).IS_AGENT_WORKSPACE_CONTEXT
);
