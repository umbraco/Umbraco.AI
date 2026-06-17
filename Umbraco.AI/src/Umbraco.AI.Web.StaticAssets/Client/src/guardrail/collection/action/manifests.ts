import { UaiNoOpCollectionAction } from "../../../core/collection-action/uai-no-op-collection-action.api.js";
import { UAI_GUARDRAIL_COLLECTION_ALIAS } from "../../constants.js";

export const guardrailCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAI.CollectionAction.Guardrail.Create",
        name: "Create Guardrail",
        element: () => import("./guardrail-create-collection-action.element.js"),
        api: UaiNoOpCollectionAction,
        meta: { label: "Create Guardrail" },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_GUARDRAIL_COLLECTION_ALIAS }],
    },
];
