import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_CONNECTION_ROOT_ENTITY_TYPE, UAI_CONNECTION_ICON } from "../constants.js";
import { UAI_CORE_MENU_ALIAS } from "../../section/constants.ts";

export const connectionMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAI.MenuItem.Connections",
        name: "Connections Menu Item",
        weight: 200,
        meta: {
            label: "Connections",
            icon: UAI_CONNECTION_ICON,
            entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
            menus: [UAI_CORE_MENU_ALIAS],
        },
    },
];
