import { UAI_PROMPT_COLLECTION_ALIAS } from "../constants.js";
import { UAI_PROMPT_ROOT_ENTITY_TYPE, UAI_PROMPT_ICON } from "../../constants.js";

export const promptCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        kind: "create",
        alias: "UmbracoAIPrompt.CollectionAction.Prompt.Create",
        name: "Create Prompt Collection Action",
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_PROMPT_COLLECTION_ALIAS }],
    },
    {
        type: "entityCreateOptionAction",
        alias: "UmbracoAIPrompt.EntityCreateOptionAction.Prompt",
        name: "Create Prompt Entity Create Option Action",
        api: () => import("./prompt-create-option-action.js"),
        forEntityTypes: [UAI_PROMPT_ROOT_ENTITY_TYPE],
        meta: { icon: UAI_PROMPT_ICON, label: "Create Prompt" },
    },
];
