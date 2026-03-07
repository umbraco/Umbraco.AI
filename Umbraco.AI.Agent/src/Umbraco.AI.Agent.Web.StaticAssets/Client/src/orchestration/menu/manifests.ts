import type { UaiEntityContainerMenuItemManifest } from "@umbraco-ai/core";
import {
    UAI_ORCHESTRATION_ROOT_ENTITY_TYPE,
    UAI_ORCHESTRATION_ENTITY_TYPE,
    UAI_ORCHESTRATION_ICON,
} from "../constants.js";
import { UAI_ADDONS_MENU_ALIAS } from "@umbraco-ai/core";

export const orchestrationMenuManifests: Array<UaiEntityContainerMenuItemManifest> = [
    {
        type: "menuItem",
        kind: "entityContainer",
        alias: "UmbracoAIAgent.MenuItem.Orchestrations",
        name: "Orchestrations Menu Item",
        weight: 60,
        meta: {
            label: "Orchestrations",
            icon: UAI_ORCHESTRATION_ICON,
            entityType: UAI_ORCHESTRATION_ROOT_ENTITY_TYPE,
            childEntityTypes: [UAI_ORCHESTRATION_ENTITY_TYPE],
            menus: [UAI_ADDONS_MENU_ALIAS],
        },
    },
];
