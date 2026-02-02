import type { UaiProfileWorkspaceContext } from "./profile-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROFILE_ENTITY_TYPE } from "../../constants.js";

export const UAI_PROFILE_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    UaiProfileWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiProfileWorkspaceContext =>
        context.getEntityType?.() === UAI_PROFILE_ENTITY_TYPE
);
