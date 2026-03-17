import { UAI_GUARDRAIL_COLLECTION_ALIAS } from "../constants.js";
import { UAI_GUARDRAIL_ENTITY_TYPE } from "../../constants.js";
import { UMB_COLLECTION_ALIAS_CONDITION } from "@umbraco-cms/backoffice/collection";

export const guardrailBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityBulkAction",
        kind: "default",
        alias: "UmbracoAI.EntityBulkAction.Guardrail.Delete",
        name: "Delete Guardrails Bulk Action",
        weight: 100,
        api: () => import("./guardrail-bulk-delete.action.js"),
        forEntityTypes: [UAI_GUARDRAIL_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_GUARDRAIL_COLLECTION_ALIAS,
            },
        ],
    },
];
