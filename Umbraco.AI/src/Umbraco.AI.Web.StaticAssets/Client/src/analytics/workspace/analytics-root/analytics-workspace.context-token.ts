import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiAnalyticsWorkspaceContext } from "./analytics-workspace.context.js";

export const UAI_ANALYTICS_WORKSPACE_CONTEXT = new UmbContextToken<UaiAnalyticsWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiAnalyticsWorkspaceContext =>
        (context as UaiAnalyticsWorkspaceContext).IS_ANALYTICS_WORKSPACE_CONTEXT,
);
