import { UAI_TEST_COLLECTION_ALIAS, UAI_TEST_ENTITY_TYPE } from "../../constants.js";
import { UMB_COLLECTION_ALIAS_CONDITION } from "@umbraco-cms/backoffice/collection";

export const testBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityBulkAction",
        kind: "default",
        alias: "UmbracoAI.EntityBulkAction.Test.Delete",
        name: "Delete Tests Bulk Action",
        weight: 100,
        api: () => import("./test-bulk-delete.action.js"),
        forEntityTypes: [UAI_TEST_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_TEST_COLLECTION_ALIAS,
            },
        ],
    },
];
