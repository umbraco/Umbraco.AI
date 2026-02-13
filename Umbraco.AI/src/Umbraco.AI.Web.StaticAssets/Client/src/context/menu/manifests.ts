import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_CONTEXT_ROOT_ENTITY_TYPE, UAI_CONTEXT_ICON } from "../constants.js";
import { UAI_CORE_MENU_ALIAS } from "../../section/constants.ts";

export const contextMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAI.MenuItem.Contexts",
        name: "Contexts Menu Item",
        weight: 0,
        meta: {
            label: "Contexts",
            icon: UAI_CONTEXT_ICON,
            entityType: UAI_CONTEXT_ROOT_ENTITY_TYPE,
            menus: [UAI_CORE_MENU_ALIAS],
        },
    },
];
