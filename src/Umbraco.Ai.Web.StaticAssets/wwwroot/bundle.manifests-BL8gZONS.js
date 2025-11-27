import { UMB_WORKSPACE_PATH_PATTERN as m, UMB_WORKSPACE_CONDITION_ALIAS as t, UmbSubmitWorkspaceAction as r } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern as C } from "@umbraco-cms/backoffice/router";
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
], N = [...I, ..._], n = "UmbracoAi.Collection.Connection", T = "UmbracoAi.Repository.Connection.Detail", O = "UmbracoAi.Store.Connection.Detail", a = "UmbracoAi.Repository.Connection.Collection", c = "uai:connection", e = "uai:connection-root", o = "UmbracoAi.Workspace.Connection", i = "UmbracoAi.Workspace.ConnectionRoot", s = "icon-wall-plug", l = m.generateAbsolute({
  sectionName: A,
  entityType: c
}), b = `${l}/create`, W = new C(
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
], y = [
  {
    type: "collection",
    kind: "default",
    alias: n,
    name: "Connection Collection",
    element: () => import("./connection-collection.element-BhrmeyJS.js"),
    meta: {
      repositoryAlias: a
    }
  },
  {
    type: "collectionView",
    alias: "UmbracoAi.CollectionView.Connection.Table",
    name: "Connection Table View",
    element: () => import("./connection-table-collection-view.element-DJEuwQrr.js"),
    meta: {
      label: "Table",
      icon: "icon-list",
      pathName: "table"
    },
    conditions: [{ alias: "Umb.Condition.CollectionAlias", match: n }]
  },
  ...E
], U = [
  {
    type: "entityAction",
    kind: "default",
    alias: "UmbracoAi.EntityAction.Connection.Create",
    name: "Create Connection Entity Action",
    weight: 1200,
    api: () => import("./connection-create.action-CqR_jWoK.js"),
    forEntityTypes: [e],
    meta: {
      icon: "icon-add",
      label: "Create",
      additionalOptions: !0
    }
  }
], S = [
  {
    type: "menuItem",
    alias: "UmbracoAi.MenuItem.Connections",
    name: "Connections Menu Item",
    weight: 100,
    meta: {
      label: "Connections",
      icon: s,
      entityType: e,
      menus: ["UmbracoAi.Menu.Settings"]
    }
  }
], d = [
  {
    type: "repository",
    alias: T,
    name: "Connection Detail Repository",
    api: () => import("./connection-detail.repository-gai7dQ-B.js")
  },
  {
    type: "store",
    alias: O,
    name: "Connection Detail Store",
    api: () => import("./connection-detail.store-Du8jOyAs.js")
  },
  {
    type: "repository",
    alias: a,
    name: "Connection Collection Repository",
    api: () => import("./connection-collection.repository-BbYjsdMG.js")
  }
], u = [
  {
    type: "workspace",
    kind: "routable",
    alias: o,
    name: "Connection Workspace",
    api: () => import("./connection-workspace.context-CTquc_4b.js"),
    meta: {
      entityType: c
    }
  },
  {
    type: "workspaceView",
    alias: "UmbracoAi.Workspace.Connection.View.Details",
    name: "Connection Details Workspace View",
    js: () => import("./connection-details-workspace-view.element-CtYVwKo2.js"),
    weight: 100,
    meta: {
      label: "Details",
      pathname: "details",
      icon: "icon-settings"
    },
    conditions: [
      {
        alias: t,
        match: o
      }
    ]
  },
  {
    type: "workspaceAction",
    kind: "default",
    alias: "UmbracoAi.WorkspaceAction.Connection.Save",
    name: "Save Connection",
    api: r,
    meta: {
      label: "Save",
      look: "primary",
      color: "positive"
    },
    conditions: [
      {
        alias: t,
        match: o
      }
    ]
  }
], k = [
  {
    type: "workspace",
    kind: "default",
    alias: i,
    name: "Connection Root Workspace",
    meta: {
      entityType: e,
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
        alias: t,
        match: i
      }
    ]
  }
], R = [
  ...u,
  ...k
], f = [
  ...y,
  ...U,
  ...S,
  ...d,
  ...R
], h = [
  ...p,
  ...N,
  ...f
];
export {
  s as U,
  W as a,
  b,
  e as c,
  o as d,
  c as e,
  h as m
};
//# sourceMappingURL=bundle.manifests-BL8gZONS.js.map
