import type { UaiEntityContainerMenuItemManifest } from "../../core/menu/types.js";
import { UAI_CONNECTION_ROOT_ENTITY_TYPE, UAI_CONNECTION_ENTITY_TYPE, UAI_CONNECTION_ICON } from "../constants.js";
import { UAI_CORE_MENU_ALIAS } from "../../section/constants.ts";

export const connectionMenuManifests: Array<UaiEntityContainerMenuItemManifest> = [
    {
        type: "menuItem",
        kind: "entityContainer",
        alias: "UmbracoAI.MenuItem.Connections",
        name: "Connections Menu Item",
        weight: 200,
        meta: {
            label: "Connections",
            icon: UAI_CONNECTION_ICON,
            entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
            childEntityTypes: [UAI_CONNECTION_ENTITY_TYPE],
            menus: [UAI_CORE_MENU_ALIAS],
        },
    },
];
