import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_TRACE_ROOT_ENTITY_TYPE } from "../entity.js";
import { UAI_TRACE_ICON } from "../collection/constants.js";
import { UAI_TRACE_MENU_ITEM_ALIAS } from "../constants.js";

export const traceMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: UAI_TRACE_MENU_ITEM_ALIAS,
        name: "AI Traces Menu Item",
        weight: 100,
        meta: {
            label: "AI Traces",
            icon: UAI_TRACE_ICON,
            entityType: UAI_TRACE_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
