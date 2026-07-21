import {
    UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
    UAI_COPILOT_WORKSPACE_SECTION_PATHNAME,
} from "../constants.js";

/**
 * The Copilot Workspace is a **standalone custom section**: the section supplies its own element
 * (the three-region shell) rather than hosting dashboards. This keeps the section closed — other
 * packages cannot register dashboards/section-views into it — and gives us full control of the
 * layout and routing.
 */
const section: UmbExtensionManifest = {
    type: "section",
    alias: UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
    name: "Copilot Workspace Section",
    weight: 50,
    element: () => import("./shell/copilot-workspace-shell.element.js"),
    meta: {
        label: "#uaiCopilotWorkspace_sectionLabel",
        pathname: UAI_COPILOT_WORKSPACE_SECTION_PATHNAME,
    },
    conditions: [
        {
            alias: "Umb.Condition.SectionUserPermission",
            match: UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
        },
    ],
};

export const sectionManifests: UmbExtensionManifest[] = [section];
