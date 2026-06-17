import { UaiNoOpCollectionAction } from "@umbraco-ai/core";
import { UAI_PROMPT_COLLECTION_ALIAS } from "../constants.js";

export const promptCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAIPrompt.CollectionAction.Prompt.Create",
        name: "Create Prompt",
        element: () => import("./prompt-create-collection-action.element.js"),
        api: UaiNoOpCollectionAction,
        meta: { label: "Create Prompt" },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_PROMPT_COLLECTION_ALIAS }],
    },
];
