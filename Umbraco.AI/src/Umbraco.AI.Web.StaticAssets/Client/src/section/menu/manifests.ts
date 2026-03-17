import type { ManifestMenu } from "@umbraco-cms/backoffice/menu";
import { UAI_ADDONS_MENU_ALIAS, UAI_CONFIGURATION_MENU_ALIAS, UAI_MONITORING_MENU_ALIAS } from "../constants.ts";

export const sectionMenuManifests: ManifestMenu[] = [
    {
        type: "menu",
        alias: UAI_CONFIGURATION_MENU_ALIAS,
        name: "AI Configuration Menu",
    },
    {
        type: "menu",
        alias: UAI_MONITORING_MENU_ALIAS,
        name: "AI Monitoring Menu",
    },
    {
        type: "menu",
        alias: UAI_ADDONS_MENU_ALIAS,
        name: "AI Add-ons Menu",
    },
];
