import { UMB_WORKSPACE_PATH_PATTERN as m, UMB_WORKSPACE_CONDITION_ALIAS as e, UmbSubmitWorkspaceAction as C } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern as r } from "@umbraco-cms/backoffice/router";
import { UMB_SETTINGS_SECTION_PATHNAME as A } from "@umbraco-cms/backoffice/settings";
const p = [
  {
    name: "Umbraco AI Entrypoint",
    alias: "UmbracoAi.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-BlMjOTp-.js")
  }
], I = [
  {
    type: "menu",
    alias: "UmbracoAi.Menu.Settings",
    name: "AI Settings Menu"
  }
], _ = [
  {
    type: "sectionSidebarApp",
    kind: "menuWithEntityActions",
    alias: "UmbracoAi.SectionSidebarApp.AiMenu",
    name: "AI Section Sidebar",
    weight: 100,
    meta: {
      label: "AI",
      menu: "UmbracoAi.Menu.Settings"
    },
    conditions: [{ alias: "Umb.Condition.SectionAlias", match: "Umb.Section.Settings" }]
  }
], N = [...I, ..._], n = "UmbracoAi.Collection.Connection", T = "UmbracoAi.Repository.Connection.Detail", O = "UmbracoAi.Store.Connection.Detail", i = "UmbracoAi.Repository.Connection.Collection", a = "uai:connection", c = "uai:connection-root", o = "UmbracoAi.Workspace.Connection", t = "UmbracoAi.Workspace.ConnectionRoot", s = "icon-wall-plug", l = m.generateAbsolute({
  sectionName: A,
  entityType: a
}), b = `${l}/create`, w = new r(
  "edit/:unique",
  l
), E = [
  {
    type: "collectionAction",
    kind: "button",
    alias: "UmbracoAi.CollectionAction.Connection.Create",
    name: "Create Connection",
    meta: {
      label: "Create",
      href: b
    },
    conditions: [{ alias: "Umb.Condition.CollectionAlias", match: n }]
  }
], U = [
  {
    type: "collection",
    kind: "default",
    alias: n,
    name: "Connection Collection",
    meta: {
      repositoryAlias: i
    }
  },
  {
    type: "collectionView",
    alias: "UmbracoAi.CollectionView.Connection.Table",
    name: "Connection Table View",
    element: () => import("./connection-table-collection-view.element-Bcsg5fit.js"),
    meta: {
      label: "Table",
      icon: "icon-list",
      pathName: "table"
    },
    conditions: [{ alias: "Umb.Condition.CollectionAlias", match: n }]
  },
  ...E
], S = [
  {
    type: "menuItem",
    alias: "UmbracoAi.MenuItem.Connections",
    name: "Connections Menu Item",
    weight: 100,
    meta: {
      label: "Connections",
      icon: s,
      entityType: c,
      menus: ["UmbracoAi.Menu.Settings"]
    }
  }
], y = [
  {
    type: "repository",
    alias: T,
    name: "Connection Detail Repository",
    api: () => import("./connection-detail.repository-BAfg33E6.js")
  },
  {
    type: "store",
    alias: O,
    name: "Connection Detail Store",
    api: () => import("./connection-detail.store-Du8jOyAs.js")
  },
  {
    type: "repository",
    alias: i,
    name: "Connection Collection Repository",
    api: () => import("./connection-collection.repository-kxpVrCYp.js")
  }
], d = [
  {
    type: "workspace",
    kind: "routable",
    alias: o,
    name: "Connection Workspace",
    api: () => import("./connection-workspace.context-BSF8OLSu.js"),
    meta: {
      entityType: a
    }
  },
  {
    type: "workspaceView",
    alias: "UmbracoAi.Workspace.Connection.View.Details",
    name: "Connection Details Workspace View",
    js: () => import("./connection-details-workspace-view.element-CfcNJMkC.js"),
    weight: 100,
    meta: {
      label: "Details",
      pathname: "details",
      icon: "icon-settings"
    },
    conditions: [
      {
        alias: e,
        match: o
      }
    ]
  },
  {
    type: "workspaceAction",
    kind: "default",
    alias: "UmbracoAi.WorkspaceAction.Connection.Save",
    name: "Save Connection",
    api: C,
    meta: {
      label: "Save",
      look: "primary",
      color: "positive"
    },
    conditions: [
      {
        alias: e,
        match: o
      }
    ]
  }
], u = [
  {
    type: "workspace",
    kind: "default",
    alias: t,
    name: "Connection Root Workspace",
    meta: {
      entityType: c,
      headline: "Connections"
    }
  },
  {
    type: "workspaceView",
    kind: "collection",
    alias: "UmbracoAi.WorkspaceView.ConnectionRoot.Collection",
    name: "Connection Root Collection Workspace View",
    meta: {
      label: "Collection",
      pathname: "collection",
      icon: s,
      collectionAlias: n
    },
    conditions: [
      {
        alias: e,
        match: t
      }
    ]
  }
], R = [
  ...d,
  ...u
], k = [
  ...U,
  ...S,
  ...y,
  ...R
], W = [
  ...p,
  ...N,
  ...k
];
export {
  s as U,
  w as a,
  c as b,
  o as c,
  a as d,
  W as m
};
//# sourceMappingURL=bundle.manifests-CiGQzgXI.js.map
