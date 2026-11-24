import {
    UAI_PERMISSION_ENTITY_TYPE,
    UAI_PERMISSION_GROUP_ADD_ONS,
    UAI_PERMISSION_WEIGHT_ADD_ON_START,
} from "@umbraco-ai/core";
import { UAI_PERMISSION_VERB_MANAGE_PROMPTS } from "./constants.js";

export const promptPermissionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityUserPermission",
        alias: "Uai.EntityUserPermission.Prompt.Manage",
        name: "AI Permission Prompt Manage",
        weight: UAI_PERMISSION_WEIGHT_ADD_ON_START - 1, // one below Agent, so Prompt lists after it
        forEntityTypes: [UAI_PERMISSION_ENTITY_TYPE],
        meta: {
            verbs: [UAI_PERMISSION_VERB_MANAGE_PROMPTS],
            label: "#uaiPromptPermissions_manageLabel",
            description: "#uaiPromptPermissions_manageDescription",
            group: UAI_PERMISSION_GROUP_ADD_ONS,
        },
    },
];
