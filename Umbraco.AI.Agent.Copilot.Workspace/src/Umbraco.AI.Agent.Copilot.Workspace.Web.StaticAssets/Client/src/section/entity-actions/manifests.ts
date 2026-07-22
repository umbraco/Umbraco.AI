import { UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE } from "../../constants.js";
import {
    UaiCopilotWorkspaceNewChatAction,
    UaiCopilotWorkspaceNewProjectAction,
} from "./root-create.actions.js";

const forRoot = { forEntityTypes: [UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE] };

/** Root/collection entity actions surfaced by the sidebar header's create (+) menu. */
export const rootEntityActionManifests: UmbExtensionManifest[] = [
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Root.NewChat",
        name: "New Chat Entity Action",
        weight: 200,
        api: UaiCopilotWorkspaceNewChatAction,
        ...forRoot,
        meta: { icon: "icon-add", label: "#uaiCopilotWorkspace_newChat" },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Root.NewProject",
        name: "New Project Entity Action",
        weight: 100,
        api: UaiCopilotWorkspaceNewProjectAction,
        ...forRoot,
        meta: { icon: "icon-folder", label: "#uaiCopilotWorkspace_newProject" },
    },
];
