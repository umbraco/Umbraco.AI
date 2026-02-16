import type { UaiEntityContainerMenuItemManifest } from "../../core/menu/types.js";
import { UAI_CONTEXT_ROOT_ENTITY_TYPE, UAI_CONTEXT_ENTITY_TYPE, UAI_CONTEXT_ICON } from "../constants.js";
import { UAI_CORE_MENU_ALIAS } from "../../section/constants.ts";

export const contextMenuManifests: Array<UaiEntityContainerMenuItemManifest> = [
    {
        type: "menuItem",
        kind: "entityContainer",
        alias: "UmbracoAI.MenuItem.Contexts",
        name: "Contexts Menu Item",
        weight: 0,
        meta: {
            label: "Contexts",
            icon: UAI_CONTEXT_ICON,
            entityType: UAI_CONTEXT_ROOT_ENTITY_TYPE,
            childEntityTypes: [UAI_CONTEXT_ENTITY_TYPE],
            menus: [UAI_CORE_MENU_ALIAS],
        },
    },
];
