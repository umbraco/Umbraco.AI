import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_CONTEXT_ROOT_ENTITY_TYPE, UAI_CONTEXT_ICON } from "../constants.js";

export const contextMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAi.MenuItem.Contexts",
        name: "Contexts Menu Item",
        weight: 80,
        meta: {
            label: "Contexts",
            icon: UAI_CONTEXT_ICON,
            entityType: UAI_CONTEXT_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
