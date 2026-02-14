import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_PROMPT_ROOT_ENTITY_TYPE, UAI_PROMPT_ICON } from "../constants.js";
import { UAI_ADDONS_MENU_ALIAS } from "@umbraco-ai/core";

export const promptMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAIPrompt.MenuItem.Prompts",
        name: "Prompts Menu Item",
        weight: 80,
        meta: {
            label: "Prompts",
            icon: UAI_PROMPT_ICON,
            entityType: UAI_PROMPT_ROOT_ENTITY_TYPE,
            menus: [UAI_ADDONS_MENU_ALIAS],
        },
    },
];
