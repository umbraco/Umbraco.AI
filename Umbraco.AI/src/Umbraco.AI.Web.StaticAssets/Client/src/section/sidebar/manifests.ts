import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/section";

export const sectionSidebarManifests: ManifestSectionSidebarApp[] = [
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "UmbracoAI.SectionSidebarApp.AIMenu",
        name: "AI Section Sidebar",
        weight: 900,
        meta: {
            label: "AI",
            menu: "UmbracoAI.Menu.Settings",
        },
        conditions: [{ alias: "Umb.Condition.SectionAlias", match: "Umb.Section.Settings" }],
    },
];
