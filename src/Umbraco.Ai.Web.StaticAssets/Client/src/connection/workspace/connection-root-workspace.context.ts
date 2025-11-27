import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { ManifestWorkspace, UmbWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import { UmbBasicState } from "@umbraco-cms/backoffice/observable-api";
import { UaiConnectionConstants } from "../constants.js";

/**
 * Workspace context for the Connection root (collection view).
 */
export class UaiConnectionRootWorkspaceContext extends UmbContextBase implements UmbWorkspaceContext {
    public workspaceAlias: string;

    #entityType = new UmbBasicState<string>(UaiConnectionConstants.EntityType.Root);
    readonly entityType = this.#entityType.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, UMB_WORKSPACE_CONTEXT.toString());
        this.workspaceAlias = UaiConnectionConstants.Workspace.Root;
    }

    set manifest(_manifest: ManifestWorkspace) {
        // Required by interface but not needed for root workspace
    }

    getEntityType(): string {
        return UaiConnectionConstants.EntityType.Root;
    }
}

export { UaiConnectionRootWorkspaceContext as api };
