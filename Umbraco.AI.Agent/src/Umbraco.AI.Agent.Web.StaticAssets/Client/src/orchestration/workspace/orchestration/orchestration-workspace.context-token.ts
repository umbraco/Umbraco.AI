import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiOrchestrationWorkspaceContext } from "./orchestration-workspace.context.js";

export const UAI_ORCHESTRATION_WORKSPACE_CONTEXT = new UmbContextToken<UaiOrchestrationWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiOrchestrationWorkspaceContext =>
        (context as UaiOrchestrationWorkspaceContext).IS_ORCHESTRATION_WORKSPACE_CONTEXT,
);
