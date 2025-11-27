import { UMB_WORKSPACE_PATH_PATTERN as T, UmbSubmittableWorkspaceContextBase as O, UmbWorkspaceRouteManager as A, UmbWorkspaceIsNewRedirectController as k, UmbWorkspaceIsNewRedirectControllerAlias as S } from "@umbraco-cms/backoffice/workspace";
import { UmbBasicState as I, UmbObjectState as P } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext as V } from "@umbraco-cms/backoffice/entity";
import { UmbValidationContext as q } from "@umbraco-cms/backoffice/validation";
import { UaiConnectionDetailRepository as x } from "./connection-detail.repository-gai7dQ-B.js";
import { c as R, d as N, e as w } from "./bundle.manifests-BL8gZONS.js";
import { html as d, when as W, css as $, state as _, customElement as L } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as M } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as B } from "@umbraco-cms/backoffice/style";
import { U as D, a as m } from "./partial-update.command-CVQwdipO.js";
import { UMB_SETTINGS_SECTION_PATHNAME as H } from "@umbraco-cms/backoffice/settings";
class K {
  #t = !1;
  #e = [];
  add(e) {
    this.#t || (this.#e = [
      ...this.#e.filter((t) => !e.correlationId || t.correlationId !== e.correlationId),
      e
    ]);
  }
  getAll() {
    return this.#t ? [] : [...this.#e];
  }
  mute() {
    this.#t = !0;
  }
  unmute() {
    this.#t = !1;
  }
  clear() {
    this.#e = [];
  }
  reset() {
    this.clear(), this.unmute();
  }
}
const C = T.generateAbsolute({
  sectionName: H,
  entityType: R
});
var Y = Object.defineProperty, z = Object.getOwnPropertyDescriptor, g = (s) => {
  throw TypeError(s);
}, c = (s, e, t, i) => {
  for (var a = i > 1 ? void 0 : i ? z(e, t) : e, r = s.length - 1, h; r >= 0; r--)
    (h = s[r]) && (a = (i ? h(e, t, a) : h(a)) || a);
  return i && a && Y(e, t, a), a;
}, f = (s, e, t) => e.has(s) || g("Cannot " + t), p = (s, e, t) => (f(s, e, "read from private field"), e.get(s)), v = (s, e, t) => e.has(s) ? g("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(s) : e.set(s, t), G = (s, e, t, i) => (f(s, e, "write to private field"), e.set(s, t), t), u = (s, e, t) => (f(s, e, "access private method"), t), l, n, y, E, b, U;
let o = class extends M {
  constructor() {
    super(), v(this, n), v(this, l), this._aliasLocked = !0, this.consumeContext(D, (s) => {
      s && (G(this, l, s), this.observe(s.model, (e) => {
        this._model = e;
      }), this.observe(s.isNew, (e) => {
        this._isNew = e, e && requestAnimationFrame(() => {
          this.shadowRoot?.querySelector("#name")?.focus();
        });
      }));
    });
  }
  render() {
    return this._model ? d`
            <umb-workspace-editor alias="${N}">
                <div id="header" slot="header">
                    <uui-button
                        href=${C}
                        label="Back to connections"
                        compact
                    >
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model.name}
                        @input="${u(this, n, y)}"
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
                            @input=${u(this, n, E)}
                            @lock-change=${u(this, n, b)}
                        ></uui-input-lock>
                    </uui-input>
                </div>

                ${W(
      !this._isNew && this._model,
      () => d`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
    )}

                <div slot="footer-info" id="footer">
                    <a href=${C}>Connections</a>
                    / ${this._model.name || "Untitled"}
                </div>
            </umb-workspace-editor>
        ` : d`<uui-loader></uui-loader>`;
  }
};
l = /* @__PURE__ */ new WeakMap();
n = /* @__PURE__ */ new WeakSet();
y = function(s) {
  s.stopPropagation();
  const t = s.composedPath()[0].value.toString();
  if (this._aliasLocked && this._isNew) {
    const i = u(this, n, U).call(this, t);
    p(this, l)?.handleCommand(
      new m({ name: t, alias: i }, "name-alias")
    );
  } else
    p(this, l)?.handleCommand(
      new m({ name: t }, "name")
    );
};
E = function(s) {
  s.stopPropagation();
  const e = s.composedPath()[0];
  p(this, l)?.handleCommand(
    new m({ alias: e.value.toString() }, "alias")
  );
};
b = function() {
  this._aliasLocked = !this._aliasLocked;
};
U = function(s) {
  return s.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
};
o.styles = [
  B,
  $`
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
c([
  _()
], o.prototype, "_model", 2);
c([
  _()
], o.prototype, "_isNew", 2);
c([
  _()
], o.prototype, "_aliasLocked", 2);
o = c([
  L("uai-connection-workspace-editor")
], o);
class oe extends O {
  constructor(e) {
    super(e, N), this.routes = new A(this), this.#t = new I(void 0), this.unique = this.#t.asObservable(), this.#e = new P(void 0), this.model = this.#e.asObservable(), this.#s = new K(), this.#a = new V(this), this.#i = new x(this), this.addValidationContext(new q(this)), this.#a.setEntityType(w), this.observe(this.unique, (t) => this.#a.setUnique(t ?? null)), this.routes.setRoutes([
      {
        path: "create",
        component: o,
        setup: async () => {
          await this.scaffold(), new k(
            this,
            this,
            this.getHostElement().shadowRoot.querySelector("umb-router-slot")
          );
        }
      },
      {
        path: "edit/:unique",
        component: o,
        setup: (t, i) => {
          this.removeUmbControllerByAlias(S), this.load(i.match.params.unique);
        }
      }
    ]);
  }
  #t;
  #e;
  #i;
  #s;
  #a;
  resetState() {
    super.resetState(), this.#t.setValue(void 0), this.#e.setValue(void 0), this.#s.reset();
  }
  /**
   * Creates a scaffold for a new connection.
   */
  async scaffold(e) {
    this.resetState();
    const { data: t } = await this.#i.createScaffold({ providerId: e });
    t && (this.#e.setValue(t), this.setIsNew(!0));
  }
  /**
   * Loads an existing connection by ID.
   */
  async load(e) {
    this.resetState();
    const { data: t, asObservable: i } = await this.#i.requestByUnique(e);
    return i && this.observe(
      i(),
      (a) => {
        if (a) {
          this.#t.setValue(a.unique);
          const r = structuredClone(a);
          this.#s.getAll().forEach((h) => h.execute(r)), this.#e.setValue(r), this.setIsNew(!1);
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
    const t = this.#e.getValue();
    if (t) {
      const i = structuredClone(t);
      e.execute(i), this.#e.setValue(i), this.#s.add(e);
    }
  }
  getData() {
    return this.#e.getValue();
  }
  getUnique() {
    return this.#t.getValue();
  }
  getEntityType() {
    return w;
  }
  /**
   * Saves the connection (create or update).
   */
  async submit() {
    const e = this.#e.getValue();
    if (e) {
      this.#s.mute();
      try {
        if (this.getIsNew()) {
          const { data: t, error: i } = await this.#i.create(e);
          if (i)
            throw i;
          t && (this.#t.setValue(t.unique), this.#e.setValue(t));
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
  oe as UaiConnectionWorkspaceContext,
  oe as api
};
//# sourceMappingURL=connection-workspace.context-CTquc_4b.js.map
