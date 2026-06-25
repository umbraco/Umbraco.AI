import { UaiNoOpCollectionAction } from "../../../core/collection-action/uai-no-op-collection-action.api.js";
import { UAI_CONNECTION_COLLECTION_ALIAS } from "../../constants.js";

export const connectionCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAI.CollectionAction.Connection.Create",
        name: "Create Connection",
        element: () => import("./connection-create-collection-action.element.js"),
        api: UaiNoOpCollectionAction,
        meta: { label: "Create Connection" },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONNECTION_COLLECTION_ALIAS }],
    },
];
