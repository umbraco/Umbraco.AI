import { UAI_PROMPT_ENTITY_TYPE } from "../../constants.js";

export const promptBulkActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityBulkAction",
        kind: "default",
        alias: "UmbracoAiPrompt.EntityBulkAction.Prompt.Delete",
        name: "Bulk Delete Prompt Entity Action",
        weight: 100,
        api: () => import("./prompt-bulk-delete.action.js"),
        forEntityTypes: [UAI_PROMPT_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
