import { UMB_COLLECTION_ALIAS_CONDITION } from "@umbraco-cms/backoffice/collection";
import { UAI_PROMPT_ENTITY_TYPE } from "../../constants.js";
import { UAI_PROMPT_COLLECTION_ALIAS } from "../constants.js";

export const promptBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityBulkAction",
        kind: "default",
        alias: "UmbracoAIPrompt.EntityBulkAction.Prompt.Delete",
        name: "Bulk Delete Prompt Entity Action",
        weight: 100,
        api: () => import("./prompt-bulk-delete.action.js"),
        forEntityTypes: [UAI_PROMPT_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_PROMPT_COLLECTION_ALIAS,
            },
        ],
    },
];
