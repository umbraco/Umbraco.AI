import { UAI_CONNECTION_COLLECTION_ALIAS } from "../../constants.js";

export const connectionCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAi.CollectionAction.Connection.Create",
        name: "Create Connection",
        element: () => import("./connection-create-collection-action.element.js"),
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONNECTION_COLLECTION_ALIAS }],
    },
];
