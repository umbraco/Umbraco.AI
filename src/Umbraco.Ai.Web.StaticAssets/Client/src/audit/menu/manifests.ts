import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_AUDIT_ROOT_ENTITY_TYPE } from "../entity.js";
import { UAI_AUDIT_ICON } from "../collection/constants.js";
import { UAI_AUDIT_MENU_ITEM_ALIAS } from "../constants.js";

export const auditMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: UAI_AUDIT_MENU_ITEM_ALIAS,
        name: "AI Audits Menu Item",
        weight: 100,
        meta: {
            label: "Audit",
            icon: UAI_AUDIT_ICON,
            entityType: UAI_AUDIT_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
