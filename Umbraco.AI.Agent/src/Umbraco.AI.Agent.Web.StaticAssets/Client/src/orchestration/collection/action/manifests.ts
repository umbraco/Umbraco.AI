import { UAI_ORCHESTRATION_COLLECTION_ALIAS } from "../constants.js";

export const orchestrationCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAIAgent.CollectionAction.Orchestration.Create",
        name: "Create Orchestration",
        element: () => import("./orchestration-create-collection-action.element.js"),
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_ORCHESTRATION_COLLECTION_ALIAS }],
    },
];
