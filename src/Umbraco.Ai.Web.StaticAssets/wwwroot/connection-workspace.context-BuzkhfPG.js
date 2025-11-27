import { UmbSubmittableWorkspaceContextBase as h, UmbWorkspaceRouteManager as u } from "@umbraco-cms/backoffice/workspace";
import { UmbContextToken as l } from "@umbraco-cms/backoffice/context-api";
import { UmbBasicState as c, UmbObjectState as m } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext as d } from "@umbraco-cms/backoffice/entity";
import { UmbValidationContext as f } from "@umbraco-cms/backoffice/validation";
import { UaiConnectionDetailRepository as y } from "./connection-detail.repository-C1LvVqz5.js";
import { U as i } from "./bundle.manifests-D_6geKLR.js";
class p {
  #e = !1;
  #t = [];
  add(e) {
    this.#e || (this.#t = [
      ...this.#t.filter((t) => !e.correlationId || t.correlationId !== e.correlationId),
      e
    ]);
  }
  getAll() {
    return this.#e ? [] : [...this.#t];
  }
  mute() {
    this.#e = !0;
  }
  unmute() {
    this.#e = !1;
  }
  clear() {
    this.#t = [];
  }
  reset() {
    this.clear(), this.unmute();
  }
}
const v = new l(
  "UmbWorkspaceContext",
  void 0,
  (a) => a.getEntityType() === i.EntityType.Entity
);
class x extends h {
  constructor(e) {
    super(e, i.Workspace.Entity), this.routes = new u(this), this.#e = new c(void 0), this.unique = this.#e.asObservable(), this.#t = new m(void 0), this.model = this.#t.asObservable(), this.#s = new p(), this.#a = new d(this), this.#i = new y(this), this.addValidationContext(new f(this)), this.#a.setEntityType(i.EntityType.Entity), this.observe(this.unique, (t) => this.#a.setUnique(t ?? null));
  }
  #e;
  #t;
  #i;
  #s;
  #a;
  resetState() {
    super.resetState(), this.#e.setValue(void 0), this.#t.setValue(void 0), this.#s.reset();
  }
  /**
   * Creates a scaffold for a new connection.
   */
  async scaffold(e) {
    this.resetState();
    const { data: t } = await this.#i.createScaffold({ providerId: e });
    t && (this.#t.setValue(t), this.setIsNew(!0));
  }
  /**
   * Loads an existing connection by ID.
   */
  async load(e) {
    this.resetState();
    const { data: t, asObservable: s } = await this.#i.requestByUnique(e);
    return s && this.observe(
      s(),
      (r) => {
        if (r) {
          this.#e.setValue(r.unique);
          const n = structuredClone(r);
          this.#s.getAll().forEach((o) => o.execute(n)), this.#t.setValue(n), this.setIsNew(!1);
        }
      },
      "_observeModel"
    ), t;
  }
  /**
   * Handles a command to update the model.
   * Commands are tracked for replay after model refresh.
   */
  handleCommand(e) {
    const t = this.#t.getValue();
    if (t) {
      const s = structuredClone(t);
      e.execute(s), this.#t.setValue(s), this.#s.add(e);
    }
  }
  getData() {
    return this.#t.getValue();
  }
  getUnique() {
    return this.#e.getValue();
  }
  getEntityType() {
    return i.EntityType.Entity;
  }
  /**
   * Saves the connection (create or update).
   */
  async submit() {
    const e = this.#t.getValue();
    if (e) {
      this.#s.mute();
      try {
        if (this.getIsNew()) {
          const { data: t, error: s } = await this.#i.create(e);
          if (s)
            throw s;
          t && (this.#e.setValue(t.unique), this.#t.setValue(t));
        } else {
          const { error: t } = await this.#i.save(e);
          if (t)
            throw t;
        }
        this.#s.reset(), this.setIsNew(!1);
      } finally {
        this.#s.unmute();
      }
    }
  }
}
export {
  v as UAI_CONNECTION_WORKSPACE_CONTEXT,
  x as UaiConnectionWorkspaceContext,
  x as api
};
//# sourceMappingURL=connection-workspace.context-BuzkhfPG.js.map
