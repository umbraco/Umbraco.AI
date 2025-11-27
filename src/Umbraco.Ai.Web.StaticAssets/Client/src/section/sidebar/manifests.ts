import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/section";

export const sectionSidebarManifests: ManifestSectionSidebarApp[] = [
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "UmbracoAi.SectionSidebarApp.AiMenu",
        name: "AI Section Sidebar",
        weight: 100,
        meta: {
            label: "AI",
            menu: "UmbracoAi.Menu.Settings",
        },
        conditions: [{ alias: "Umb.Condition.SectionAlias", match: "Umb.Section.Settings" }],
    },
];
