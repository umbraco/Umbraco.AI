import {
    UAI_PERMISSION_ENTITY_TYPE,
    UAI_PERMISSION_GROUP_ADD_ONS,
    UAI_PERMISSION_WEIGHT_ADD_ON_START,
} from "@umbraco-ai/core";
import { UAI_PERMISSION_VERB_MANAGE_AGENTS } from "./constants.js";

export const agentPermissionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityUserPermission",
        alias: "Uai.EntityUserPermission.Agent.Manage",
        name: "AI Permission Agent Manage",
        weight: UAI_PERMISSION_WEIGHT_ADD_ON_START,
        forEntityTypes: [UAI_PERMISSION_ENTITY_TYPE],
        meta: {
            verbs: [UAI_PERMISSION_VERB_MANAGE_AGENTS],
            label: "#uaiAgentPermissions_manageLabel",
            description: "#uaiAgentPermissions_manageDescription",
            group: UAI_PERMISSION_GROUP_ADD_ONS,
        },
    },
];
