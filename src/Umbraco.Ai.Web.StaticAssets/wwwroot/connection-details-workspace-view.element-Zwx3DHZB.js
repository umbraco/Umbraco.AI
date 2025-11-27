import { html as u, css as C, state as m, customElement as b } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as P } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as E } from "@umbraco-cms/backoffice/style";
import { U as x, a as _ } from "./partial-update.command-BqAvPgVg.js";
var A = Object.defineProperty, U = Object.getOwnPropertyDescriptor, f = (e) => {
  throw TypeError(e);
}, p = (e, t, i, a) => {
  for (var o = a > 1 ? void 0 : a ? U(t, i) : t, l = e.length - 1, d; l >= 0; l--)
    (d = e[l]) && (o = (a ? d(t, i, o) : d(o)) || o);
  return a && o && A(t, i, o), o;
}, c = (e, t, i) => t.has(e) || f("Cannot " + i), g = (e, t, i) => (c(e, t, "read from private field"), t.get(e)), h = (e, t, i) => t.has(e) ? f("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(e) : t.set(e, i), D = (e, t, i, a) => (c(e, t, "write to private field"), t.set(e, i), i), v = (e, t, i) => (c(e, t, "access private method"), i), s, n, y, w;
let r = class extends P {
  constructor() {
    super(), h(this, n), h(this, s), this.consumeContext(x, (e) => {
      e && (D(this, s, e), this.observe(e.model, (t) => this._model = t), this.observe(e.isNew, (t) => this._isNew = t));
    });
  }
  render() {
    return this._model ? u`
            <uui-box headline="Connection Details">
                <umb-property-layout label="Provider" description="AI provider for this connection">
                    <uui-input
                        slot="editor"
                        .value=${this._model.providerId}
                        @change=${v(this, n, y)}
                        placeholder="e.g., openai"
                        ?disabled=${!this._isNew}
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Active" description="Enable or disable this connection">
                    <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${v(this, n, w)}></uui-toggle>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Provider Settings">
                <p class="placeholder-text">
                    Provider-specific settings will be displayed here once the provider is selected and saved. Future
                    enhancement: Dynamic settings form based on provider's SettingDefinitions.
                </p>
            </uui-box>
        ` : u`<uui-loader></uui-loader>`;
  }
};
s = /* @__PURE__ */ new WeakMap();
n = /* @__PURE__ */ new WeakSet();
y = function(e) {
  e.stopPropagation();
  const t = e.target;
  g(this, s)?.handleCommand(
    new _({ providerId: t.value }, "providerId")
  );
};
w = function(e) {
  e.stopPropagation();
  const t = e.target;
  g(this, s)?.handleCommand(
    new _({ isActive: t.checked }, "isActive")
  );
};
r.styles = [
  E,
  C`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }

            uui-box {
                margin-bottom: var(--uui-size-layout-1);
            }

            .placeholder-text {
                color: var(--uui-color-text-alt);
                font-style: italic;
            }
        `
];
p([
  m()
], r.prototype, "_model", 2);
p([
  m()
], r.prototype, "_isNew", 2);
r = p([
  b("uai-connection-details-workspace-view")
], r);
const W = r;
export {
  r as UaiConnectionDetailsWorkspaceViewElement,
  W as default
};
//# sourceMappingURL=connection-details-workspace-view.element-Zwx3DHZB.js.map
