import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/section";
import { UAI_ADDONS_MENU_ALIAS, UAI_SECTION_ALIAS, UAI_CONFIGURATION_MENU_ALIAS, UAI_MONITORING_MENU_ALIAS } from "../constants.js";

export const sectionSidebarManifests: ManifestSectionSidebarApp[] = [
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "UmbracoAI.SectionSidebarApp.AIConfigurationMenu",
        name: "AI Configuration Section Sidebar",
        weight: 1000,
        meta: {
            label: "Configuration",
            menu: UAI_CONFIGURATION_MENU_ALIAS,
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
        alias: "UmbracoAI.SectionSidebarApp.AIMonitoringMenu",
        name: "AI Monitoring Section Sidebar",
        weight: 500,
        meta: {
            label: "Monitoring",
            menu: UAI_MONITORING_MENU_ALIAS,
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
        alias: "UmbracoAI.SectionSidebarApp.AIAddonsMenu",
        name: "AI Add-ons Section Sidebar",
        weight: 100,
        meta: {
            label: "Add-ons",
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
