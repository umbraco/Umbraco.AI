import { UAI_PROMPT_COLLECTION_ALIAS } from "../constants.js";

export const promptCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAiPrompt.CollectionAction.Prompt.Create",
        name: "Create Prompt",
        element: () => import("./prompt-create-collection-action.element.js"),
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_PROMPT_COLLECTION_ALIAS }],
    },
];
