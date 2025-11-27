import { html as h, when as E, css as U, state as f, customElement as L } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as W } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as b } from "@umbraco-cms/backoffice/style";
import { UaiConnectionWorkspaceContext as P } from "./connection-workspace.context-BuzkhfPG.js";
import { U as m } from "./bundle.manifests-D_6geKLR.js";
import { U as _ } from "./connection-details.element-Dvtpfcim.js";
var x = Object.defineProperty, N = Object.getOwnPropertyDescriptor, k = (e) => {
  throw TypeError(e);
}, d = (e, t, a, s) => {
  for (var o = s > 1 ? void 0 : s ? N(t, a) : t, p = e.length - 1, u; p >= 0; p--)
    (u = e[p]) && (o = (s ? u(t, a, o) : u(o)) || o);
  return s && o && x(t, a, o), o;
}, v = (e, t, a) => t.has(e) || k("Cannot " + a), l = (e, t, a) => (v(e, t, "read from private field"), a ? a.call(e) : t.get(e)), w = (e, t, a) => t.has(e) ? k("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(e) : t.set(e, a), c = (e, t, a) => (v(e, t, "access private method"), a), i, r, g, C, y, $;
let n = class extends W {
  constructor() {
    super(), w(this, r), w(this, i, new P(this)), this._aliasLocked = !0, this.observe(l(this, i).model, (e) => {
      this._model = e;
    }), this.observe(l(this, i).isNew, (e) => {
      this._isNew = e, e && requestAnimationFrame(() => {
        this.shadowRoot?.querySelector("#name")?.focus();
      });
    });
  }
  render() {
    return this._model ? h`
            <umb-workspace-editor alias="${m.Workspace.Entity}">
                <div id="header" slot="header">
                    <uui-button
                        href="section/settings/workspace/${m.Workspace.Root}"
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
                            @input=${c(this, r, C)}
                            @lock-change=${c(this, r, y)}
                        ></uui-input-lock>
                    </uui-input>
                </div>

                ${E(
      !this._isNew && this._model,
      () => h`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
    )}

                <uai-connection-details></uai-connection-details>

                <div slot="footer-info" id="footer">
                    <a href="section/settings/workspace/${m.Workspace.Root}">Connections</a>
                    / ${this._model.name || "Untitled"}
                </div>
            </umb-workspace-editor>
        ` : h`<uui-loader></uui-loader>`;
  }
};
i = /* @__PURE__ */ new WeakMap();
r = /* @__PURE__ */ new WeakSet();
g = function(e) {
  e.stopPropagation();
  const a = e.composedPath()[0].value.toString();
  if (this._aliasLocked && this._isNew) {
    const s = c(this, r, $).call(this, a);
    l(this, i).handleCommand(
      new _({ name: a, alias: s }, "name-alias")
    );
  } else
    l(this, i).handleCommand(
      new _({ name: a }, "name")
    );
};
C = function(e) {
  e.stopPropagation();
  const t = e.composedPath()[0];
  l(this, i).handleCommand(
    new _({ alias: t.value.toString() }, "alias")
  );
};
y = function() {
  this._aliasLocked = !this._aliasLocked;
};
$ = function(e) {
  return e.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
};
n.styles = [
  b,
  U`
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
  f()
], n.prototype, "_model", 2);
d([
  f()
], n.prototype, "_isNew", 2);
d([
  f()
], n.prototype, "_aliasLocked", 2);
n = d([
  L("uai-connection-workspace-editor")
], n);
const q = n;
export {
  n as UaiConnectionWorkspaceEditorElement,
  q as default
};
//# sourceMappingURL=connection-workspace-editor.element-DSEBcRGT.js.map
