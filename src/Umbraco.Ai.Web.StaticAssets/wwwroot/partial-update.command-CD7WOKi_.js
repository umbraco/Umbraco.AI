import { UmbContextToken as a } from "@umbraco-cms/backoffice/context-api";
import { U as e } from "./bundle.manifests-BsBZQJMT.js";
const c = new a(
  "UmbWorkspaceContext",
  void 0,
  (s) => s.getEntityType?.() === e.EntityType.Entity
);
class i {
  constructor(o) {
    this.correlationId = o;
  }
}
class m extends i {
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
  c as U,
  m as a
};
//# sourceMappingURL=partial-update.command-CD7WOKi_.js.map
