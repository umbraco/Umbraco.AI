import { UAI_CONTEXT_COLLECTION_ALIAS } from "../../constants.js";

export const contextCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAi.CollectionAction.Context.Create",
        name: "Create Context",
        element: () => import("./context-create-collection-action.element.js"),
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONTEXT_COLLECTION_ALIAS }],
    },
];
