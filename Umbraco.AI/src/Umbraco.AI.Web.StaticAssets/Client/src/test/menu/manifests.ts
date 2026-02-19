import { UAI_TEST_ENTITY_TYPE, UAI_TEST_ROOT_ENTITY_TYPE } from "../entity.js";
import { UAI_TEST_MENU_ITEM_ALIAS, UAI_TEST_ICON } from "../constants.js";
import { UAI_CORE_MENU_ALIAS } from "../../section/constants.ts";
import type { UaiEntityContainerMenuItemManifest } from "../../core/menu";

export const testMenuManifests: UaiEntityContainerMenuItemManifest[] = [
    {
        type: "menuItem",
        kind: "entityContainer",
        alias: UAI_TEST_MENU_ITEM_ALIAS,
        name: "AI Tests Menu Item",
        weight: -80,
        meta: {
            label: "Tests",
            icon: UAI_TEST_ICON,
            entityType: UAI_TEST_ROOT_ENTITY_TYPE,
            childEntityTypes: [UAI_TEST_ENTITY_TYPE],
            menus: [UAI_CORE_MENU_ALIAS],
        },
    },
];
