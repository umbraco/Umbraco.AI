import { UAI_GUARDRAIL_COLLECTION_ALIAS } from "../constants.js";
import { UAI_GUARDRAIL_ROOT_ENTITY_TYPE, UAI_GUARDRAIL_ICON } from "../../constants.js";

export const guardrailCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        kind: "create",
        alias: "UmbracoAI.CollectionAction.Guardrail.Create",
        name: "Create Guardrail Collection Action",
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_GUARDRAIL_COLLECTION_ALIAS }],
    },
    {
        type: "entityCreateOptionAction",
        alias: "UmbracoAI.EntityCreateOptionAction.Guardrail",
        name: "Create Guardrail Entity Create Option Action",
        api: () => import("./guardrail-create-option-action.js"),
        forEntityTypes: [UAI_GUARDRAIL_ROOT_ENTITY_TYPE],
        meta: { icon: UAI_GUARDRAIL_ICON, label: "Create Guardrail" },
    },
];
