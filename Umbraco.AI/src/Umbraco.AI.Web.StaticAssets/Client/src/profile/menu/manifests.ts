import type { UaiEntityContainerMenuItemManifest } from "../../core/menu/types.js";
import { UAI_PROFILE_ROOT_ENTITY_TYPE, UAI_PROFILE_ENTITY_TYPE, UAI_PROFILE_ICON } from "../constants.js";
import { UAI_CORE_MENU_ALIAS } from "../../section/constants.ts";

export const profileMenuManifests: Array<UaiEntityContainerMenuItemManifest> = [
    {
        type: "menuItem",
        kind: "entityContainer",
        alias: "UmbracoAI.MenuItem.Profiles",
        name: "Profiles Menu Item",
        weight: 195,
        meta: {
            label: "Profiles",
            icon: UAI_PROFILE_ICON,
            entityType: UAI_PROFILE_ROOT_ENTITY_TYPE,
            childEntityTypes: [UAI_PROFILE_ENTITY_TYPE],
            menus: [UAI_CORE_MENU_ALIAS],
        },
    },
];
