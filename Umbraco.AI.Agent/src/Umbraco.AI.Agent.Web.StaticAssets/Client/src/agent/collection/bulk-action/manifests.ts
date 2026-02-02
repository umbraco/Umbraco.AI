import { UMB_COLLECTION_ALIAS_CONDITION } from "@umbraco-cms/backoffice/collection";
import { UAI_AGENT_ENTITY_TYPE } from "../../constants.js";
import { UAI_AGENT_COLLECTION_ALIAS } from "../constants.js";

export const agentBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityBulkAction",
        kind: "default",
        alias: "UmbracoAiAgent.EntityBulkAction.Agent.Delete",
        name: "Bulk Delete Agent Entity Action",
        weight: 100,
        api: () => import("./agent-bulk-delete.action.js"),
        forEntityTypes: [UAI_AGENT_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_AGENT_COLLECTION_ALIAS,
            },
        ],
    },
];
