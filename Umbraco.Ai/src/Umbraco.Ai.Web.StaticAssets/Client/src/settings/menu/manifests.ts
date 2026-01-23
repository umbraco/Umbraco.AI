import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_SETTINGS_ROOT_ENTITY_TYPE } from "../entity.js";
import { UAI_SETTINGS_MENU_ITEM_ALIAS, UAI_SETTINGS_ICON } from "../constants.js";

export const settingsMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: UAI_SETTINGS_MENU_ITEM_ALIAS,
        name: "AI Settings Menu Item",
        weight: -200, // Below Logs (-100)
        meta: {
            label: "Settings",
            icon: UAI_SETTINGS_ICON,
            entityType: UAI_SETTINGS_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
