import { UAI_PROMPT_ENTITY_TYPE, UAI_PROMPT_ROOT_ENTITY_TYPE } from "../constants.js";

export const promptEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAiPrompt.EntityAction.Prompt.Create",
        name: "Create Prompt Entity Action",
        weight: 1200,
        api: () => import("./prompt-create.action.js"),
        forEntityTypes: [UAI_PROMPT_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
        },
    },
    {
        type: "entityAction",
        kind: "delete",
        alias: "UmbracoAiPrompt.EntityAction.Prompt.Delete",
        name: "Delete Prompt Entity Action",
        weight: 100,
        api: () => import("./prompt-delete.action.js"),
        forEntityTypes: [UAI_PROMPT_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
