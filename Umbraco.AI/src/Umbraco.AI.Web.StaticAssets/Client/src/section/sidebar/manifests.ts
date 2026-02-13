import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/section";
import { UAI_ADDONS_MENU_ALIAS, UAI_SECTION_ALIAS, UAI_CORE_MENU_ALIAS } from "../constants.js";

export const sectionSidebarManifests: ManifestSectionSidebarApp[] = [
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "UmbracoAI.SectionSidebarApp.AICoreMenu",
        name: "AI Core Section Sidebar",
        weight: 1000,
        meta: {
            label: "Core",
            menu: UAI_CORE_MENU_ALIAS,
        },
        conditions: [
            {
                alias: "Umb.Condition.SectionAlias",
                match: UAI_SECTION_ALIAS
            }
        ],
    },
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "UmbracoAI.SectionSidebarApp.AICoreAddons",
        name: "AI Addons Section Sidebar",
        weight: 500,
        meta: {
            label: "Addons",
            menu: UAI_ADDONS_MENU_ALIAS,
        },
        conditions: [
            {
                alias: "Umb.Condition.SectionAlias",
                match: UAI_SECTION_ALIAS
            }
        ],
    },
];
