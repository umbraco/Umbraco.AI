import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/section";
import { UAI_COPILOT_WORKSPACE_SECTION_ALIAS } from "../../constants.js";

export const sidebarManifests: ManifestSectionSidebarApp[] = [
    {
        type: "sectionSidebarApp",
        alias: "Uai.SectionSidebarApp.CopilotWorkspaceConversations",
        name: "Copilot Workspace Conversation List",
        element: () => import("./workspace-conversation-list.element.js"),
        conditions: [
            {
                alias: "Umb.Condition.SectionAlias",
                match: UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
            },
        ],
    },
];
