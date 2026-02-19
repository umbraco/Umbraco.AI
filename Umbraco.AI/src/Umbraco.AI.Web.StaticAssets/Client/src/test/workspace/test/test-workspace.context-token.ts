import type { UaiTestWorkspaceContext } from "./test-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

export const UAI_TEST_WORKSPACE_CONTEXT = new UmbContextToken<UaiTestWorkspaceContext>(
	"UaiTestWorkspaceContext",
);
