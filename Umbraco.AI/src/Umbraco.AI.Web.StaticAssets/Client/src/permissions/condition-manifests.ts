import { UAI_REGISTERED_GROUP_PERMISSION_CONDITION_ALIAS } from "./constants.js";

export const permissionConditionManifests: Array<UmbExtensionManifest> = [
    {
        type: "condition",
        name: "AI Registered Group Permission Condition",
        alias: UAI_REGISTERED_GROUP_PERMISSION_CONDITION_ALIAS,
        api: () => import("./registered-group-permission.condition.js"),
    },
];
