import { UMB_WORKSPACE_CONDITION_ALIAS as t, UmbSubmitWorkspaceAction as n } from "@umbraco-cms/backoffice/workspace";
const e = [
  {
    name: "Umbraco AI Entrypoint",
    alias: "UmbracoAi.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-BlMjOTp-.js")
  }
], i = [
  {
    type: "menu",
    alias: "UmbracoAi.Menu.Settings",
    name: "AI Settings Menu"
  }
], a = [
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
], c = [...i, ...a], o = {
  EntityType: {
    Root: "uai:connection-root",
    Entity: "uai:connection"
  },
  Icon: {
    Root: "icon-wall-plug",
    Entity: "icon-wall-plug"
  },
  Workspace: {
    Root: "UmbracoAi.Workspace.ConnectionRoot",
    Entity: "UmbracoAi.Workspace.Connection"
  },
  Store: {
    Detail: "UmbracoAi.Store.Connection.Detail"
  },
  Repository: {
    Detail: "UmbracoAi.Repository.Connection.Detail",
    Collection: "UmbracoAi.Repository.Connection.Collection"
  },
  Collection: "UmbracoAi.Collection.Connection"
}, s = [
  {
    type: "collectionAction",
    kind: "button",
    alias: "UmbracoAi.CollectionAction.Connection.Create",
    name: "Create Connection",
    meta: {
      label: "Create",
      href: `section/settings/workspace/${o.EntityType.Entity}/create`
    },
    conditions: [{ alias: "Umb.Condition.CollectionAlias", match: o.Collection }]
  }
], l = [
  {
    type: "collection",
    kind: "default",
    alias: o.Collection,
    name: "Connection Collection",
    meta: {
      repositoryAlias: o.Repository.Collection
    }
  },
  {
    type: "collectionView",
    alias: "UmbracoAi.CollectionView.Connection.Table",
    name: "Connection Table View",
    element: () => import("./connection-table-collection-view.element-CtD46GUV.js"),
    meta: {
      label: "Table",
      icon: "icon-list",
      pathName: "table"
    },
    conditions: [{ alias: "Umb.Condition.CollectionAlias", match: o.Collection }]
  },
  ...s
], r = [
  {
    type: "menuItem",
    alias: "UmbracoAi.MenuItem.Connections",
    name: "Connections Menu Item",
    weight: 100,
    meta: {
      label: "Connections",
      icon: o.Icon.Root,
      entityType: o.EntityType.Root,
      menus: ["UmbracoAi.Menu.Settings"]
    }
  }
], p = [
  {
    type: "repository",
    alias: o.Repository.Detail,
    name: "Connection Detail Repository",
    api: () => import("./connection-detail.repository-Bq4gwARE.js")
  },
  {
    type: "store",
    alias: o.Store.Detail,
    name: "Connection Detail Store",
    api: () => import("./connection-detail.store-Du8jOyAs.js")
  },
  {
    type: "repository",
    alias: o.Repository.Collection,
    name: "Connection Collection Repository",
    api: () => import("./connection-collection.repository-BIulHNG8.js")
  }
], m = [
  {
    type: "workspace",
    kind: "routable",
    alias: o.Workspace.Entity,
    name: "Connection Workspace",
    api: () => import("./connection-workspace.context-BBIohkGB.js"),
    meta: {
      entityType: o.EntityType.Entity
    }
  },
  {
    type: "workspaceView",
    alias: "UmbracoAi.Workspace.Connection.View.Details",
    name: "Connection Details Workspace View",
    js: () => import("./connection-details-workspace-view.element-cQglr8oO.js"),
    weight: 100,
    meta: {
      label: "Details",
      pathname: "details",
      icon: "icon-settings"
    },
    conditions: [
      {
        alias: t,
        match: o.Workspace.Entity
      }
    ]
  },
  {
    type: "workspaceAction",
    kind: "default",
    alias: "UmbracoAi.WorkspaceAction.Connection.Save",
    name: "Save Connection",
    api: n,
    meta: {
      label: "Save",
      look: "primary",
      color: "positive"
    },
    conditions: [
      {
        alias: t,
        match: o.Workspace.Entity
      }
    ]
  }
], C = [
  {
    type: "workspace",
    kind: "default",
    alias: o.Workspace.Root,
    name: "Connection Root Workspace",
    meta: {
      entityType: o.EntityType.Root,
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
      icon: o.Icon.Root,
      collectionAlias: o.Collection
    },
    conditions: [
      {
        alias: t,
        match: o.Workspace.Root
      }
    ]
  }
], y = [
  ...m,
  ...C
], b = [
  ...l,
  ...r,
  ...p,
  ...y
], k = [
  ...e,
  ...c,
  ...b
];
export {
  o as U,
  k as m
};
//# sourceMappingURL=bundle.manifests-BsBZQJMT.js.map
