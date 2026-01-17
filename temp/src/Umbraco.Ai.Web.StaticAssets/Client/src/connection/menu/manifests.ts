import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_CONNECTION_ROOT_ENTITY_TYPE, UAI_CONNECTION_ICON } from "../constants.js";

export const connectionMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAi.MenuItem.Connections",
        name: "Connections Menu Item",
        weight: 200,
        meta: {
            label: "Connections",
            icon: UAI_CONNECTION_ICON,
            entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
