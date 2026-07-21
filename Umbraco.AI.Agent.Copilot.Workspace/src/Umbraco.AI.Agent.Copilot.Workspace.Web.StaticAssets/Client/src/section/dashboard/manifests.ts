import { UAI_COPILOT_WORKSPACE_SECTION_ALIAS } from "../../constants.js";

const dashboard: UmbExtensionManifest = {
    type: "dashboard",
    alias: "Uai.Dashboard.CopilotWorkspace",
    name: "Copilot Workspace Dashboard",
    element: () => import("../shell/copilot-workspace-shell.element.js"),
    weight: 10,
    meta: {
        label: "#uaiCopilotWorkspace_dashboardLabel",
        pathname: "workspace",
    },
    conditions: [
        {
            alias: "Umb.Condition.SectionAlias",
            match: UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
        },
    ],
};

export const dashboardManifests: UmbExtensionManifest[] = [dashboard];
