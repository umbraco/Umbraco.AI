import type { UaiTestWorkspaceContext } from "./test-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_TEST_ENTITY_TYPE } from "../../constants.js";

export const UAI_TEST_WORKSPACE_CONTEXT = new UmbContextToken<
	UmbSubmittableWorkspaceContext,
	UaiTestWorkspaceContext
>(
	"UmbWorkspaceContext",
	undefined,
	(context): context is UaiTestWorkspaceContext => context.getEntityType?.() === UAI_TEST_ENTITY_TYPE,
);
