import { html as u, css as b, state as m, customElement as w } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as E } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as P } from "@umbraco-cms/backoffice/style";
import { UAI_CONNECTION_WORKSPACE_CONTEXT as x } from "./connection-workspace.context-BuzkhfPG.js";
class O {
  constructor(e) {
    this.correlationId = e;
  }
}
class _ extends O {
  #e;
  constructor(e, i) {
    super(i), this.#e = e;
  }
  execute(e) {
    Object.keys(this.#e).filter((i) => this.#e[i] !== void 0).forEach((i) => {
      e[i] = this.#e[i];
    });
  }
}
var U = Object.defineProperty, D = Object.getOwnPropertyDescriptor, f = (t) => {
  throw TypeError(t);
}, d = (t, e, i, a) => {
  for (var o = a > 1 ? void 0 : a ? D(e, i) : e, l = t.length - 1, c; l >= 0; l--)
    (c = t[l]) && (o = (a ? c(e, i, o) : c(o)) || o);
  return a && o && U(e, i, o), o;
}, p = (t, e, i) => e.has(t) || f("Cannot " + i), g = (t, e, i) => (p(t, e, "read from private field"), e.get(t)), h = (t, e, i) => e.has(t) ? f("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, i), A = (t, e, i, a) => (p(t, e, "write to private field"), e.set(t, i), i), v = (t, e, i) => (p(t, e, "access private method"), i), s, n, y, C;
let r = class extends E {
  constructor() {
    super(), h(this, n), h(this, s), this.consumeContext(x, (t) => {
      t && (A(this, s, t), this.observe(t.model, (e) => this._model = e), this.observe(t.isNew, (e) => this._isNew = e));
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
                    <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${v(this, n, C)}></uui-toggle>
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
y = function(t) {
  t.stopPropagation();
  const e = t.target;
  g(this, s)?.handleCommand(
    new _({ providerId: e.value }, "providerId")
  );
};
C = function(t) {
  t.stopPropagation();
  const e = t.target;
  g(this, s)?.handleCommand(
    new _({ isActive: e.checked }, "isActive")
  );
};
r.styles = [
  P,
  b`
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
d([
  m()
], r.prototype, "_model", 2);
d([
  m()
], r.prototype, "_isNew", 2);
r = d([
  w("uai-connection-details")
], r);
const S = r, W = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  get UaiConnectionDetailsElement() {
    return r;
  },
  default: S
}, Symbol.toStringTag, { value: "Module" }));
export {
  _ as U,
  W as c
};
//# sourceMappingURL=connection-details.element-Dvtpfcim.js.map
