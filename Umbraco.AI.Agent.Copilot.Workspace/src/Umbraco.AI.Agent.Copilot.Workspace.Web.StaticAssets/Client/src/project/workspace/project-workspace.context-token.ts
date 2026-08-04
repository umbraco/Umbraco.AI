import type { UaiProjectWorkspaceContext } from "./project-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";

/**
 * Resolves the project workspace context. Keyed on the shared `"UmbWorkspaceContext"` string (so
 * `<umb-workspace-editor>` and workspace actions find it) and discriminated by the project entity
 * type — mirrors the core Context workspace token.
 */
export const UAI_PROJECT_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    UaiProjectWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiProjectWorkspaceContext => context.getEntityType?.() === UAI_PROJECT_ENTITY_TYPE,
);
