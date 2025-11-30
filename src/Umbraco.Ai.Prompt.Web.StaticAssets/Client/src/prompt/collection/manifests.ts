import { UAI_PROMPT_COLLECTION_ALIAS } from "./constants.js";
import { UAI_PROMPT_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { promptCollectionActionManifests } from "./action/manifests.js";

export const promptCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_PROMPT_COLLECTION_ALIAS,
        name: "Prompt Collection",
        element: () => import("./prompt-collection.element.js"),
        meta: {
            repositoryAlias: UAI_PROMPT_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAiPrompt.CollectionView.Prompt.Table",
        name: "Prompt Table View",
        element: () => import("./views/table/prompt-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_PROMPT_COLLECTION_ALIAS }],
    },
    ...promptCollectionActionManifests,
];
