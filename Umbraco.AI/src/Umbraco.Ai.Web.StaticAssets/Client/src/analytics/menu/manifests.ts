import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_ANALYTICS_ROOT_ENTITY_TYPE } from "../entity.js";
import { UAI_ANALYTICS_MENU_ITEM_ALIAS } from "../constants.js";

export const analyticsMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: UAI_ANALYTICS_MENU_ITEM_ALIAS,
        name: "AI Analytics Menu Item",
        weight: -90,
        meta: {
            label: "Analytics",
            icon: "icon-chart",
            entityType: UAI_ANALYTICS_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
