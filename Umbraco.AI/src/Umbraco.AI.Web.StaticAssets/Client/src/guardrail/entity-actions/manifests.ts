import { UAI_GUARDRAIL_ENTITY_TYPE, UAI_GUARDRAIL_ROOT_ENTITY_TYPE } from "../constants.js";

export const guardrailEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAI.EntityAction.Guardrail.Create",
        name: "Create Guardrail Entity Action",
        weight: 1200,
        api: () => import("./guardrail-create.action.js"),
        forEntityTypes: [UAI_GUARDRAIL_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
        },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAI.EntityAction.Guardrail.Delete",
        name: "Delete Guardrail Entity Action",
        weight: 100,
        api: () => import("./guardrail-delete.action.js"),
        forEntityTypes: [UAI_GUARDRAIL_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
