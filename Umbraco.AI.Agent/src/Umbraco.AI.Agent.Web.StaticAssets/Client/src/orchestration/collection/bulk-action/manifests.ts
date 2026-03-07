import { UMB_COLLECTION_ALIAS_CONDITION } from "@umbraco-cms/backoffice/collection";
import { UAI_ORCHESTRATION_ENTITY_TYPE } from "../../constants.js";
import { UAI_ORCHESTRATION_COLLECTION_ALIAS } from "../constants.js";

export const orchestrationBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityBulkAction",
        kind: "default",
        alias: "UmbracoAIAgent.EntityBulkAction.Orchestration.Delete",
        name: "Bulk Delete Orchestration Entity Action",
        weight: 100,
        api: () => import("./orchestration-bulk-delete.action.js"),
        forEntityTypes: [UAI_ORCHESTRATION_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_ORCHESTRATION_COLLECTION_ALIAS,
            },
        ],
    },
];
