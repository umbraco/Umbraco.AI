const a = [
  {
    name: "Umbraco AI Entrypoint",
    alias: "UmbracoAi.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-COqOdXZx.js")
  }
], o = [
  {
    name: "Your Package Name Dashboard",
    alias: "YourPackageName.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard.element-BldXNk1X.js"),
    meta: {
      label: "Example Dashboard",
      pathname: "example-dashboard"
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Content"
      }
    ]
  }
], t = [
  ...a,
  ...o
];
export {
  t as manifests
};
//# sourceMappingURL=umbraco-ai.js.map
