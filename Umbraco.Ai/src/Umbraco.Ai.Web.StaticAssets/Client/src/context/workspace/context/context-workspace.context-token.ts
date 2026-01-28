import type { UaiContextWorkspaceContext } from "./context-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONTEXT_ENTITY_TYPE } from "../../constants.js";

export const UAI_CONTEXT_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    UaiContextWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiContextWorkspaceContext =>
        context.getEntityType?.() === UAI_CONTEXT_ENTITY_TYPE
);
