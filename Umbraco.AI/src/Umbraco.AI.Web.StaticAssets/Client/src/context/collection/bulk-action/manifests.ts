import { UAI_CONTEXT_COLLECTION_ALIAS } from '../constants.js';
import { UAI_CONTEXT_ENTITY_TYPE } from '../../constants.js';
import { UMB_COLLECTION_ALIAS_CONDITION } from '@umbraco-cms/backoffice/collection';

export const contextBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: 'entityBulkAction',
        kind: 'default',
        alias: 'UmbracoAI.EntityBulkAction.Context.Delete',
        name: 'Delete Contexts Bulk Action',
        weight: 100,
        api: () => import('./context-bulk-delete.action.js'),
        forEntityTypes: [UAI_CONTEXT_ENTITY_TYPE],
        meta: {
            icon: 'icon-trash',
            label: '#actions_delete',
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_CONTEXT_COLLECTION_ALIAS,
            },
        ],
    },
];
