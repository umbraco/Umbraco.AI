import { UAI_ORCHESTRATION_ENTITY_TYPE, UAI_ORCHESTRATION_ROOT_ENTITY_TYPE } from "../constants.js";

export const orchestrationEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAIAgent.EntityAction.Orchestration.Create",
        name: "Create Orchestration Entity Action",
        weight: 1200,
        api: () => import("./orchestration-create.action.js"),
        forEntityTypes: [UAI_ORCHESTRATION_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
        },
    },
    {
        type: "entityAction",
        kind: "delete",
        alias: "UmbracoAIAgent.EntityAction.Orchestration.Delete",
        name: "Delete Orchestration Entity Action",
        weight: 100,
        api: () => import("./orchestration-delete.action.js"),
        forEntityTypes: [UAI_ORCHESTRATION_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
