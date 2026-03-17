import type { UaiEntityContainerMenuItemManifest } from "../../core/menu/types.js";
import { UAI_GUARDRAIL_ROOT_ENTITY_TYPE, UAI_GUARDRAIL_ENTITY_TYPE, UAI_GUARDRAIL_ICON } from "../constants.js";
import { UAI_CONFIGURATION_MENU_ALIAS } from "../../section/constants.ts";

export const guardrailMenuManifests: Array<UaiEntityContainerMenuItemManifest> = [
    {
        type: "menuItem",
        kind: "entityContainer",
        alias: "UmbracoAI.MenuItem.Guardrails",
        name: "Guardrails Menu Item",
        weight: -10,
        meta: {
            label: "Guardrails",
            icon: UAI_GUARDRAIL_ICON,
            entityType: UAI_GUARDRAIL_ROOT_ENTITY_TYPE,
            childEntityTypes: [UAI_GUARDRAIL_ENTITY_TYPE],
            menus: [UAI_CONFIGURATION_MENU_ALIAS],
        },
    },
];
