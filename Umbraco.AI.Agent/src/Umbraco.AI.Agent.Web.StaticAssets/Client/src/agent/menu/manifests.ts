import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_AGENT_ROOT_ENTITY_TYPE, UAI_AGENT_ICON } from "../constants.js";

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
            menus: ["UmbracoAI.Menu.Settings"],
        },
    },
];
