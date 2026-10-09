import {
    UAI_AGENT_STARTER_PROMPTS_PROPERTY_EDITOR_UI_ALIAS,
    UAI_SUGGEST_STARTER_PROMPTS_PROPERTY_ACTION_ALIAS,
} from "./constants.js";

export const agentPropertyActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "propertyAction",
        kind: "default",
        alias: UAI_SUGGEST_STARTER_PROMPTS_PROPERTY_ACTION_ALIAS,
        name: "Suggest Starter Prompts Property Action",
        forPropertyEditorUis: [UAI_AGENT_STARTER_PROMPTS_PROPERTY_EDITOR_UI_ALIAS],
        api: () => import("./suggest-starter-prompts.property-action.js"),
        element: () => import("./suggest-starter-prompts.property-action.element.js"),
        weight: 100,
        meta: {
            icon: "icon-lab",
            label: "#uaiAgent_suggestStarters",
        },
    },
];
