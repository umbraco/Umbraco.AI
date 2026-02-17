import type { ManifestMenu } from "@umbraco-cms/backoffice/menu";
import { UAI_ADDONS_MENU_ALIAS, UAI_CORE_MENU_ALIAS } from "../constants.ts";

export const sectionMenuManifests: ManifestMenu[] = [
    {
        type: "menu",
        alias: UAI_CORE_MENU_ALIAS,
        name: "AI Core Settings Menu",
    },
    {
        type: "menu",
        alias: UAI_ADDONS_MENU_ALIAS,
        name: "AI Addons Settings Menu",
    },
];
