import { html as m, customElement as a } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement as s } from "@umbraco-cms/backoffice/collection";
var u = Object.getOwnPropertyDescriptor, b = (o, t, i, r) => {
  for (var e = r > 1 ? void 0 : r ? u(t, i) : t, l = o.length - 1, n; l >= 0; l--)
    (n = o[l]) && (e = n(e) || e);
  return e;
};
let c = class extends s {
  renderToolbar() {
    return m`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
  }
};
c = b([
  a("uai-connection-collection")
], c);
export {
  c as UaiConnectionCollectionElement,
  c as element
};
//# sourceMappingURL=connection-collection.element-BhrmeyJS.js.map
