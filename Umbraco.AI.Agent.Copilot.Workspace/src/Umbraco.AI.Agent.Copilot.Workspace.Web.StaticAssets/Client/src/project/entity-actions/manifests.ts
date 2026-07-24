import { UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";

/**
 * Project entity actions (the ⋯-menu actions on a project node / workspace): New chat in this project
 * and Delete. Registered here — co-located with their action sources — rather than in the workspace
 * manifest, since they are project-entity concerns, not workspace-view concerns.
 */
export const projectEntityActionManifests: UmbExtensionManifest[] = [
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Project.NewChat",
        name: "New Chat In Project Entity Action",
        weight: 200,
        api: () => import("./project-new-chat.action.js"),
        forEntityTypes: [UAI_PROJECT_ENTITY_TYPE],
        meta: { icon: "icon-add", label: "#uaiCopilotWorkspace_projectNewChat" },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Project.Delete",
        name: "Delete Project Entity Action",
        weight: 100,
        api: () => import("./project-delete.action.js"),
        forEntityTypes: [UAI_PROJECT_ENTITY_TYPE],
        meta: { icon: "icon-trash", label: "#actions_delete" },
    },
];
