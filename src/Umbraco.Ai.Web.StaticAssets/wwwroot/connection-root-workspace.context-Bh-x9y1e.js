import { UmbContextBase as e } from "@umbraco-cms/backoffice/class-api";
import { UMB_WORKSPACE_CONTEXT as s } from "@umbraco-cms/backoffice/workspace";
import { UmbBasicState as i } from "@umbraco-cms/backoffice/observable-api";
import { U as t } from "./bundle.manifests-D_6geKLR.js";
class y extends e {
  constructor(o) {
    super(o, s.toString()), this.#t = new i(t.EntityType.Root), this.entityType = this.#t.asObservable(), this.workspaceAlias = t.Workspace.Root;
  }
  #t;
  set manifest(o) {
  }
  getEntityType() {
    return t.EntityType.Root;
  }
}
export {
  y as UaiConnectionRootWorkspaceContext,
  y as api
};
//# sourceMappingURL=connection-root-workspace.context-Bh-x9y1e.js.map
