import { html as l, state as _, customElement as h } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as d } from "@umbraco-cms/backoffice/lit-element";
import { UMB_COLLECTION_CONTEXT as f } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles as C } from "@umbraco-cms/backoffice/style";
import { U as m } from "./bundle.manifests-D_6geKLR.js";
var E = Object.defineProperty, b = Object.getOwnPropertyDescriptor, u = (t) => {
  throw TypeError(t);
}, v = (t, e, a, s) => {
  for (var i = s > 1 ? void 0 : s ? b(e, a) : e, o = t.length - 1, r; o >= 0; o--)
    (r = t[o]) && (i = (s ? r(e, a, i) : r(i)) || i);
  return s && i && E(e, a, i), i;
}, w = (t, e, a) => e.has(t) || u("Cannot " + a), T = (t, e, a) => e.has(t) ? u("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, a), U = (t, e, a) => (w(t, e, "access private method"), a), c, p;
let n = class extends d {
  constructor() {
    super(), T(this, c), this._items = [], this._columns = [
      { name: "Name", alias: "name" },
      { name: "Provider", alias: "provider" },
      { name: "Status", alias: "status" }
    ], this.consumeContext(f, (t) => {
      t && this.observe(t.items, (e) => U(this, c, p).call(this, e));
    });
  }
  render() {
    return l`<umb-table .columns=${this._columns} .items=${this._items}></umb-table>`;
  }
};
c = /* @__PURE__ */ new WeakSet();
p = function(t) {
  this._items = t.map((e) => ({
    id: e.unique,
    icon: m.Icon.Entity,
    data: [
      {
        columnAlias: "name",
        value: l`<a
                        href="section/settings/workspace/${m.Workspace.Entity}/${e.unique}"
                        >${e.name}</a
                    >`
      },
      { columnAlias: "provider", value: e.providerId },
      {
        columnAlias: "status",
        value: l`<uui-tag color=${e.isActive ? "positive" : "danger"}>
                        ${e.isActive ? "Active" : "Inactive"}
                    </uui-tag>`
      }
    ]
  }));
};
n.styles = [C];
v([
  _()
], n.prototype, "_items", 2);
n = v([
  h("uai-connection-table-collection-view")
], n);
const P = n;
export {
  n as UaiConnectionTableCollectionViewElement,
  P as default
};
//# sourceMappingURL=connection-table-collection-view.element-C3cBIiVh.js.map
