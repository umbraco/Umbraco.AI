import { UAI_AUDIT_LOG_COLLECTION_ALIAS } from '../../constants.js';
import { UAI_AUDIT_LOG_ENTITY_TYPE } from '../../entity.js';
import { UMB_COLLECTION_ALIAS_CONDITION } from '@umbraco-cms/backoffice/collection';

export const auditLogBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: 'entityBulkAction',
        kind: 'default',
        alias: 'UmbracoAI.EntityBulkAction.AuditLog.Delete',
        name: 'Delete Audit Logs Bulk Action',
        weight: 100,
        api: () => import('./audit-log-bulk-delete.action.js'),
        forEntityTypes: [UAI_AUDIT_LOG_ENTITY_TYPE],
        meta: {
            icon: 'icon-trash',
            label: '#actions_delete',
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_AUDIT_LOG_COLLECTION_ALIAS,
            },
        ],
    },
];
