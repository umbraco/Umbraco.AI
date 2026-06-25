import { UAI_CONTEXT_COLLECTION_ALIAS } from "../constants.js";
import { UAI_CONTEXT_ROOT_ENTITY_TYPE, UAI_CONTEXT_ICON } from "../../constants.js";

export const contextCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        kind: "create",
        alias: "UmbracoAI.CollectionAction.Context.Create",
        name: "Create Context Collection Action",
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONTEXT_COLLECTION_ALIAS }],
    },
    {
        type: "entityCreateOptionAction",
        alias: "UmbracoAI.EntityCreateOptionAction.Context",
        name: "Create Context Entity Create Option Action",
        api: () => import("./context-create-option-action.js"),
        forEntityTypes: [UAI_CONTEXT_ROOT_ENTITY_TYPE],
        meta: { icon: UAI_CONTEXT_ICON, label: "Create Context" },
    },
];
