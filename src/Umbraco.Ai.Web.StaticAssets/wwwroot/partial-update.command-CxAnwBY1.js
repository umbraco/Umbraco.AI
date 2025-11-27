import { UmbContextToken as a } from "@umbraco-cms/backoffice/context-api";
import { d as e } from "./bundle.manifests-CiGQzgXI.js";
const n = new a(
  "UmbWorkspaceContext",
  void 0,
  (s) => s.getEntityType?.() === e
);
class r {
  constructor(o) {
    this.correlationId = o;
  }
}
class m extends r {
  #t;
  constructor(o, t) {
    super(t), this.#t = o;
  }
  execute(o) {
    Object.keys(this.#t).filter((t) => this.#t[t] !== void 0).forEach((t) => {
      o[t] = this.#t[t];
    });
  }
}
export {
  n as U,
  m as a
};
//# sourceMappingURL=partial-update.command-CxAnwBY1.js.map
