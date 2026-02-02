import type { UaiConnectionWorkspaceContext } from "./connection-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";

export const UAI_CONNECTION_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    UaiConnectionWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiConnectionWorkspaceContext =>
        context.getEntityType?.() === UAI_CONNECTION_ENTITY_TYPE
);
