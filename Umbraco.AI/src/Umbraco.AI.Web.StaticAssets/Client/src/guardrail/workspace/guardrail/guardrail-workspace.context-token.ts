import type { UaiGuardrailWorkspaceContext } from "./guardrail-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_GUARDRAIL_ENTITY_TYPE } from "../../constants.js";

export const UAI_GUARDRAIL_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    UaiGuardrailWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiGuardrailWorkspaceContext => context.getEntityType?.() === UAI_GUARDRAIL_ENTITY_TYPE,
);
