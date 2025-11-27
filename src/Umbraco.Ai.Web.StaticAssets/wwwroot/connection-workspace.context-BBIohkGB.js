import { UMB_WORKSPACE_PATH_PATTERN as k, UmbSubmittableWorkspaceContextBase as N, UmbWorkspaceRouteManager as S, UmbWorkspaceIsNewRedirectController as T, UmbWorkspaceIsNewRedirectControllerAlias as A } from "@umbraco-cms/backoffice/workspace";
import { UmbBasicState as O, UmbObjectState as P } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext as V } from "@umbraco-cms/backoffice/entity";
import { UmbValidationContext as I } from "@umbraco-cms/backoffice/validation";
import { UaiConnectionDetailRepository as W } from "./connection-detail.repository-Bq4gwARE.js";
import { U as u } from "./bundle.manifests-BsBZQJMT.js";
import { html as m, when as q, css as x, state as _, customElement as $ } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as R } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as L } from "@umbraco-cms/backoffice/style";
import { U as M, a as p } from "./partial-update.command-CD7WOKi_.js";
import { UMB_SETTINGS_SECTION_PATHNAME as B } from "@umbraco-cms/backoffice/settings";
class D {
  #e = !1;
  #t = [];
  add(t) {
    this.#e || (this.#t = [
      ...this.#t.filter((e) => !t.correlationId || e.correlationId !== t.correlationId),
      t
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
const y = k.generateAbsolute({
  sectionName: B,
  entityType: u.EntityType.Root
});
var H = Object.defineProperty, z = Object.getOwnPropertyDescriptor, C = (i) => {
  throw TypeError(i);
}, d = (i, t, e, s) => {
  for (var a = s > 1 ? void 0 : s ? z(t, e) : t, n = i.length - 1, h; n >= 0; n--)
    (h = i[n]) && (a = (s ? h(t, e, a) : h(a)) || a);
  return s && a && H(t, e, a), a;
}, w = (i, t, e) => t.has(i) || C("Cannot " + e), f = (i, t, e) => (w(i, t, "read from private field"), t.get(i)), v = (i, t, e) => t.has(i) ? C("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(i) : t.set(i, e), K = (i, t, e, s) => (w(i, t, "write to private field"), t.set(i, e), e), c = (i, t, e) => (w(i, t, "access private method"), e), l, r, g, b, E, U;
let o = class extends R {
  constructor() {
    super(), v(this, r), v(this, l), this._aliasLocked = !0, this.consumeContext(M, (i) => {
      i && (K(this, l, i), this.observe(i.model, (t) => {
        this._model = t;
      }), this.observe(i.isNew, (t) => {
        this._isNew = t, t && requestAnimationFrame(() => {
          this.shadowRoot?.querySelector("#name")?.focus();
        });
      }));
    });
  }
  render() {
    return this._model ? m`
            <umb-workspace-editor alias="${u.Workspace.Entity}">
                <div id="header" slot="header">
                    <uui-button
                        href=${y}
                        label="Back to connections"
                        compact
                    >
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model.name}
                        @input="${c(this, r, g)}"
                        label="Name"
                        placeholder="Enter connection name"
                    >
                        <uui-input-lock
                            slot="append"
                            id="alias"
                            name="alias"
                            label="Alias"
                            placeholder="Enter alias"
                            .value=${this._model.alias}
                            ?auto-width=${!!this._model.name}
                            ?locked=${this._aliasLocked}
                            ?readonly=${this._aliasLocked || !this._isNew}
                            @input=${c(this, r, b)}
                            @lock-change=${c(this, r, E)}
                        ></uui-input-lock>
                    </uui-input>
                </div>

                ${q(
      !this._isNew && this._model,
      () => m`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
    )}

                <div slot="footer-info" id="footer">
                    <a href=${y}>Connections</a>
                    / ${this._model.name || "Untitled"}
                </div>
            </umb-workspace-editor>
        ` : m`<uui-loader></uui-loader>`;
  }
};
l = /* @__PURE__ */ new WeakMap();
r = /* @__PURE__ */ new WeakSet();
g = function(i) {
  i.stopPropagation();
  const e = i.composedPath()[0].value.toString();
  if (this._aliasLocked && this._isNew) {
    const s = c(this, r, U).call(this, e);
    f(this, l)?.handleCommand(
      new p({ name: e, alias: s }, "name-alias")
    );
  } else
    f(this, l)?.handleCommand(
      new p({ name: e }, "name")
    );
};
b = function(i) {
  i.stopPropagation();
  const t = i.composedPath()[0];
  f(this, l)?.handleCommand(
    new p({ alias: t.value.toString() }, "alias")
  );
};
E = function() {
  this._aliasLocked = !this._aliasLocked;
};
U = function(i) {
  return i.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
};
o.styles = [
  L,
  x`
            :host {
                display: block;
                width: 100%;
                height: 100%;
            }

            #header {
                display: flex;
                flex: 1 1 auto;
                gap: var(--uui-size-space-2);
            }

            #name {
                width: 100%;
                flex: 1 1 auto;
                align-items: center;
            }

            #footer {
                padding: 0 var(--uui-size-layout-1);
            }
        `
];
d([
  _()
], o.prototype, "_model", 2);
d([
  _()
], o.prototype, "_isNew", 2);
d([
  _()
], o.prototype, "_aliasLocked", 2);
o = d([
  $("uai-connection-workspace-editor")
], o);
class st extends N {
  constructor(t) {
    super(t, u.Workspace.Entity), this.routes = new S(this), this.#e = new O(void 0), this.unique = this.#e.asObservable(), this.#t = new P(void 0), this.model = this.#t.asObservable(), this.#i = new D(), this.#a = new V(this), this.#s = new W(this), this.addValidationContext(new I(this)), this.#a.setEntityType(u.EntityType.Entity), this.observe(this.unique, (e) => this.#a.setUnique(e ?? null)), this.routes.setRoutes([
      {
        path: "create",
        component: o,
        setup: async () => {
          await this.scaffold(), new T(
            this,
            this,
            this.getHostElement().shadowRoot.querySelector("umb-router-slot")
          );
        }
      },
      {
        path: "edit/:unique",
        component: o,
        setup: (e, s) => {
          this.removeUmbControllerByAlias(A), this.load(s.match.params.unique);
        }
      }
    ]);
  }
  #e;
  #t;
  #s;
  #i;
  #a;
  resetState() {
    super.resetState(), this.#e.setValue(void 0), this.#t.setValue(void 0), this.#i.reset();
  }
  /**
   * Creates a scaffold for a new connection.
   */
  async scaffold(t) {
    this.resetState();
    const { data: e } = await this.#s.createScaffold({ providerId: t });
    e && (this.#t.setValue(e), this.setIsNew(!0));
  }
  /**
   * Loads an existing connection by ID.
   */
  async load(t) {
    this.resetState();
    const { data: e, asObservable: s } = await this.#s.requestByUnique(t);
    return s && this.observe(
      s(),
      (a) => {
        if (a) {
          this.#e.setValue(a.unique);
          const n = structuredClone(a);
          this.#i.getAll().forEach((h) => h.execute(n)), this.#t.setValue(n), this.setIsNew(!1);
        }
      },
      "_observeModel"
    ), e;
  }
  /**
   * Handles a command to update the model.
   * Commands are tracked for replay after model refresh.
   */
  handleCommand(t) {
    const e = this.#t.getValue();
    if (e) {
      const s = structuredClone(e);
      t.execute(s), this.#t.setValue(s), this.#i.add(t);
    }
  }
  getData() {
    return this.#t.getValue();
  }
  getUnique() {
    return this.#e.getValue();
  }
  getEntityType() {
    return u.EntityType.Entity;
  }
  /**
   * Saves the connection (create or update).
   */
  async submit() {
    const t = this.#t.getValue();
    if (t) {
      this.#i.mute();
      try {
        if (this.getIsNew()) {
          const { data: e, error: s } = await this.#s.create(t);
          if (s)
            throw s;
          e && (this.#e.setValue(e.unique), this.#t.setValue(e));
        } else {
          const { error: e } = await this.#s.save(t);
          if (e)
            throw e;
        }
        this.#i.reset(), this.setIsNew(!1);
      } finally {
        this.#i.unmute();
      }
    }
  }
}
export {
  st as UaiConnectionWorkspaceContext,
  st as api
};
//# sourceMappingURL=connection-workspace.context-BBIohkGB.js.map
