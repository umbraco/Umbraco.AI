import { UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE, UAI_PROJECT_ROOT_ENTITY_TYPE } from "../../constants.js";
import {
    UaiCopilotWorkspaceNewChatAction,
    UaiCopilotWorkspaceNewProjectAction,
} from "./root-create.actions.js";

/**
 * Create entity actions: New chat on the section root (sidebar header + menu), New project on the
 * project root (the Projects sidebar group's entity-action header).
 */
export const rootEntityActionManifests: UmbExtensionManifest[] = [
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Root.NewChat",
        name: "New Chat Entity Action",
        weight: 200,
        api: UaiCopilotWorkspaceNewChatAction,
        forEntityTypes: [UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE],
        meta: { icon: "icon-add", label: "#uaiCopilotWorkspace_newChat" },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Root.NewProject",
        name: "New Project Entity Action",
        weight: 100,
        api: UaiCopilotWorkspaceNewProjectAction,
        forEntityTypes: [UAI_PROJECT_ROOT_ENTITY_TYPE],
        meta: { icon: "icon-add", label: "#uaiCopilotWorkspace_newProject" },
    },
];
