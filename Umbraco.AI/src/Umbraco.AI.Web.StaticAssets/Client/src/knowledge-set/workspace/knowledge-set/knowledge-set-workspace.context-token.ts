import type { UaiKnowledgeSetWorkspaceContext } from "./knowledge-set-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_KNOWLEDGE_SET_ENTITY_TYPE } from "../../entity.js";

export const UAI_KNOWLEDGE_SET_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbWorkspaceContext,
    UaiKnowledgeSetWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiKnowledgeSetWorkspaceContext =>
        context.getEntityType?.() === UAI_KNOWLEDGE_SET_ENTITY_TYPE,
);
