import { UMB_WORKSPACE_CONDITION_ALIAS as e, UmbSubmitWorkspaceAction as l } from "@umbraco-cms/backoffice/workspace";
const m = [
  {
    name: "Umbraco AI Entrypoint",
    alias: "UmbracoAi.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-BlMjOTp-.js")
  }
], r = [
  {
    type: "menu",
    alias: "UmbracoAi.Menu.Settings",
    name: "AI Settings Menu"
  }
], C = [
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
], p = [...r, ...C], n = "UmbracoAi.Collection.Connection", A = "UmbracoAi.Repository.Connection.Detail", b = "UmbracoAi.Store.Connection.Detail", i = "UmbracoAi.Repository.Connection.Collection", a = "uai:connection", c = "uai:connection-root", o = "UmbracoAi.Workspace.Connection", t = "UmbracoAi.Workspace.ConnectionRoot", s = "icon-wall-plug", I = [
  {
    type: "collectionAction",
    kind: "button",
    alias: "UmbracoAi.CollectionAction.Connection.Create",
    name: "Create Connection",
    meta: {
      label: "Create",
      href: `section/settings/workspace/${a}/create`
    },
    conditions: [{ alias: "Umb.Condition.CollectionAlias", match: n }]
  }
], O = [
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
    element: () => import("./connection-table-collection-view.element-BdLWCxEa.js"),
    meta: {
      label: "Table",
      icon: "icon-list",
      pathName: "table"
    },
    conditions: [{ alias: "Umb.Condition.CollectionAlias", match: n }]
  },
  ...I
], N = [
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
    alias: A,
    name: "Connection Detail Repository",
    api: () => import("./connection-detail.repository-DU_QWilS.js")
  },
  {
    type: "store",
    alias: b,
    name: "Connection Detail Store",
    api: () => import("./connection-detail.store-Du8jOyAs.js")
  },
  {
    type: "repository",
    alias: i,
    name: "Connection Collection Repository",
    api: () => import("./connection-collection.repository-Ccr9MQtn.js")
  }
], U = [
  {
    type: "workspace",
    kind: "routable",
    alias: o,
    name: "Connection Workspace",
    api: () => import("./connection-workspace.context-DXdyedOk.js"),
    meta: {
      entityType: a
    }
  },
  {
    type: "workspaceView",
    alias: "UmbracoAi.Workspace.Connection.View.Details",
    name: "Connection Details Workspace View",
    js: () => import("./connection-details-workspace-view.element-Zwx3DHZB.js"),
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
    api: l,
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
], _ = [
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
], S = [
  ...U,
  ..._
], T = [
  ...O,
  ...N,
  ...y,
  ...S
], k = [
  ...m,
  ...p,
  ...T
];
export {
  s as U,
  a,
  c as b,
  o as c,
  k as m
};
//# sourceMappingURL=bundle.manifests-BwiwDf1o.js.map
