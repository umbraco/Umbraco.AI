import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_PROMPT_ROOT_ENTITY_TYPE, UAI_PROMPT_ICON } from "../constants.js";

export const promptMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAiPrompt.MenuItem.Prompts",
        name: "Prompts Menu Item",
        weight: 80,
        meta: {
            label: "Prompts",
            icon: UAI_PROMPT_ICON,
            entityType: UAI_PROMPT_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
