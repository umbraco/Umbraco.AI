import { UaiNoOpCollectionAction } from "../../../core/collection-action/uai-no-op-collection-action.api.js";
import { UAI_CONTEXT_COLLECTION_ALIAS } from "../../constants.js";

export const contextCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAI.CollectionAction.Context.Create",
        name: "Create Context",
        element: () => import("./context-create-collection-action.element.js"),
        api: UaiNoOpCollectionAction,
        meta: { label: "Create Context" },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONTEXT_COLLECTION_ALIAS }],
    },
];
