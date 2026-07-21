import {
    UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
    UAI_COPILOT_WORKSPACE_SECTION_PATHNAME,
} from "../constants.js";
import { dashboardManifests } from "./dashboard/manifests.js";

const section: UmbExtensionManifest = {
    type: "section",
    alias: UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
    name: "Copilot Workspace Section",
    weight: 50,
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

export const sectionManifests: UmbExtensionManifest[] = [section, ...dashboardManifests];
