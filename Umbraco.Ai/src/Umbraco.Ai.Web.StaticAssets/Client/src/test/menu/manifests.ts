import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_TEST_ROOT_ENTITY_TYPE, UAI_TEST_ICON } from "../constants.js";

export const testMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAi.MenuItem.Tests",
        name: "Tests Menu Item",
        weight: 190,
        meta: {
            label: "Tests",
            icon: UAI_TEST_ICON,
            entityType: UAI_TEST_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
