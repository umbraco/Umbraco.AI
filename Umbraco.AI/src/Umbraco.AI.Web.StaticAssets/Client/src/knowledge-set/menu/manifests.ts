import type { UaiEntityContainerMenuItemManifest } from "../../core/menu/types.js";
import { UAI_KNOWLEDGE_SET_ROOT_ENTITY_TYPE, UAI_KNOWLEDGE_SET_ENTITY_TYPE, UAI_KNOWLEDGE_SET_ICON } from "../constants.js";
import { UAI_CONFIGURATION_MENU_ALIAS } from "../../section/constants.ts";

export const knowledgeSetMenuManifests: Array<UaiEntityContainerMenuItemManifest> = [
    {
        type: "menuItem",
        kind: "entityContainer",
        alias: "UmbracoAI.MenuItem.KnowledgeSets",
        name: "Knowledge Sets Menu Item",
        weight: 0,
        meta: {
            label: "Knowledge Sets",
            icon: UAI_KNOWLEDGE_SET_ICON,
            entityType: UAI_KNOWLEDGE_SET_ROOT_ENTITY_TYPE,
            childEntityTypes: [UAI_KNOWLEDGE_SET_ENTITY_TYPE],
            menus: [UAI_CONFIGURATION_MENU_ALIAS],
        },
    },
];
