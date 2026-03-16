import { UAI_GUARDRAIL_COLLECTION_ALIAS } from "../../constants.js";

export const guardrailCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAI.CollectionAction.Guardrail.Create",
        name: "Create Guardrail",
        element: () => import("./guardrail-create-collection-action.element.js"),
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_GUARDRAIL_COLLECTION_ALIAS }],
    },
];
