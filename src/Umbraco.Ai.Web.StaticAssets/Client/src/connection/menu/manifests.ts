import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UaiConnectionConstants } from "../constants.js";

export const connectionMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAi.MenuItem.Connections",
        name: "Connections Menu Item",
        weight: 100,
        meta: {
            label: "Connections",
            icon: UaiConnectionConstants.Icon.Root,
            entityType: UaiConnectionConstants.EntityType.Root,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
