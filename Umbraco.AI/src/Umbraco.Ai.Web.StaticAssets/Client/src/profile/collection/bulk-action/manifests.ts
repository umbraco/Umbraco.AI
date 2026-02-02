import { UAI_PROFILE_COLLECTION_ALIAS } from '../constants.js';
import { UAI_PROFILE_ENTITY_TYPE } from '../../constants.js';
import { UMB_COLLECTION_ALIAS_CONDITION } from '@umbraco-cms/backoffice/collection';

export const profileBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: 'entityBulkAction',
        kind: 'default',
        alias: 'UmbracoAi.EntityBulkAction.Profile.Delete',
        name: 'Delete Profiles Bulk Action',
        weight: 100,
        api: () => import('./profile-bulk-delete.action.js'),
        forEntityTypes: [UAI_PROFILE_ENTITY_TYPE],
        meta: {
            icon: 'icon-trash',
            label: '#actions_delete',
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_PROFILE_COLLECTION_ALIAS,
            },
        ],
    },
];
