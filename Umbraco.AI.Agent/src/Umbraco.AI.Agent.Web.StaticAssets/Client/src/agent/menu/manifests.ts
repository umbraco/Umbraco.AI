import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_AGENT_ROOT_ENTITY_TYPE, UAI_AGENT_ICON } from "../constants.js";
import { UAI_ADDONS_MENU_ALIAS } from "@umbraco-ai/core";

export const agentMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAIAgent.MenuItem.Agents",
        name: "Agents Menu Item",
        weight: 70,
        meta: {
            label: "Agents",
            icon: UAI_AGENT_ICON,
            entityType: UAI_AGENT_ROOT_ENTITY_TYPE,
            menus: [UAI_ADDONS_MENU_ALIAS],
        },
    },
];
