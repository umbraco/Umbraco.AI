import { UmbContextToken as s } from "@umbraco-cms/backoffice/context-api";
import { e as a } from "./bundle.manifests-BL8gZONS.js";
const n = new s(
  "UmbWorkspaceContext",
  void 0,
  (e) => e.getEntityType?.() === a
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
//# sourceMappingURL=partial-update.command-CVQwdipO.js.map
