import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_AUDIT_LOG_ROOT_ENTITY_TYPE } from "../entity.js";
import { UAI_AUDIT_LOG_ICON } from "../collection/constants.js";
import { UAI_AUDIT_LOG_MENU_ITEM_ALIAS } from "../constants.js";

export const auditLogMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: UAI_AUDIT_LOG_MENU_ITEM_ALIAS,
        name: "AI AuditLog Logs Menu Item",
        weight: -100, 
        meta: {
            label: "Logs",
            icon: UAI_AUDIT_LOG_ICON,
            entityType: UAI_AUDIT_LOG_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
