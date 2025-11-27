import { html as l, state as v, customElement as p } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as h } from "@umbraco-cms/backoffice/lit-element";
import { UMB_COLLECTION_CONTEXT as C } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles as d } from "@umbraco-cms/backoffice/style";
import { U as f, a as T } from "./bundle.manifests-BwiwDf1o.js";
var E = Object.defineProperty, O = Object.getOwnPropertyDescriptor, m = (t) => {
  throw TypeError(t);
}, u = (t, e, a, n) => {
  for (var s = n > 1 ? void 0 : n ? O(e, a) : e, o = t.length - 1, r; o >= 0; o--)
    (r = t[o]) && (s = (n ? r(e, a, s) : r(s)) || s);
  return n && s && E(e, a, s), s;
}, N = (t, e, a) => e.has(t) || m("Cannot " + a), b = (t, e, a) => e.has(t) ? m("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, a), I = (t, e, a) => (N(t, e, "access private method"), a), c, _;
let i = class extends h {
  constructor() {
    super(), b(this, c), this._items = [], this._columns = [
      { name: "Name", alias: "name" },
      { name: "Provider", alias: "provider" },
      { name: "Status", alias: "status" }
    ], this.consumeContext(C, (t) => {
      t && this.observe(t.items, (e) => I(this, c, _).call(this, e));
    });
  }
  render() {
    return l`<umb-table .columns=${this._columns} .items=${this._items}></umb-table>`;
  }
};
c = /* @__PURE__ */ new WeakSet();
_ = function(t) {
  this._items = t.map((e) => ({
    id: e.unique,
    icon: f,
    data: [
      {
        columnAlias: "name",
        value: l`<a
                        href="section/settings/workspace/${T}/${e.unique}"
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
i.styles = [d];
u([
  v()
], i.prototype, "_items", 2);
i = u([
  p("uai-connection-table-collection-view")
], i);
const y = i;
export {
  i as UaiConnectionTableCollectionViewElement,
  y as default
};
//# sourceMappingURL=connection-table-collection-view.element-BdLWCxEa.js.map
