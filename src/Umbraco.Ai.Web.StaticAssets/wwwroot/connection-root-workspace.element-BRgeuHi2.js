import { html as p, css as m, customElement as d } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as u } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as h } from "@umbraco-cms/backoffice/style";
import { UaiConnectionRootWorkspaceContext as C } from "./connection-root-workspace.context-Bh-x9y1e.js";
import { U as c } from "./bundle.manifests-D_6geKLR.js";
var f = Object.getOwnPropertyDescriptor, k = (t) => {
  throw TypeError(t);
}, v = (t, e, n, s) => {
  for (var o = s > 1 ? void 0 : s ? f(e, n) : e, a = t.length - 1, i; a >= 0; a--)
    (i = t[a]) && (o = i(o) || o);
  return o;
}, w = (t, e, n) => e.has(t) ? k("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, n), l;
let r = class extends u {
  constructor() {
    super(...arguments), w(this, l, new C(this));
  }
  render() {
    return p`
            <umb-workspace-editor alias="${c.Workspace.Root}" headline="Connections">
                <umb-collection alias="${c.Collection}"></umb-collection>
            </umb-workspace-editor>
        `;
  }
};
l = /* @__PURE__ */ new WeakMap();
r.styles = [
  h,
  m`
            :host {
                display: block;
                width: 100%;
                height: 100%;
            }
        `
];
r = v([
  d("uai-connection-root-workspace")
], r);
const x = r;
export {
  r as UaiConnectionRootWorkspaceElement,
  x as default
};
//# sourceMappingURL=connection-root-workspace.element-BRgeuHi2.js.map
