import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_PROFILE_ROOT_ENTITY_TYPE, UAI_PROFILE_ICON } from "../constants.js";

export const profileMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAi.MenuItem.Profiles",
        name: "Profiles Menu Item",
        weight: 90,
        meta: {
            label: "Profiles",
            icon: UAI_PROFILE_ICON,
            entityType: UAI_PROFILE_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
