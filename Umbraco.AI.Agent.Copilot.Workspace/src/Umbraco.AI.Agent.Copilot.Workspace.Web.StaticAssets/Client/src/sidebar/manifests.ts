import {
    UAI_COPILOT_WORKSPACE_SECTION_ALIAS,
    UAI_PINNED_MENU_ALIAS,
    UAI_PROJECTS_MENU_ALIAS,
    UAI_RECENT_MENU_ALIAS,
} from "../constants.js";
import {
    UaiSidebarGroupNotEmptyCondition,
    UAI_SIDEBAR_GROUP_NOT_EMPTY_CONDITION,
} from "./group-not-empty.condition.js";

const sectionCondition = { alias: "Umb.Condition.SectionAlias", match: UAI_COPILOT_WORKSPACE_SECTION_ALIAS };

/**
 * Sidebar composition, mirroring the AI section: a header app (search + create), then Pinned /
 * Projects / Recent as stacked `sectionSidebarApp`s, each backed by a `menu` whose custom element
 * renders that slice of the shared sidebar context. Projects is a `menuWithEntityActions` so its
 * header hosts the + New project action; every group hides when its slice is empty.
 */
export const sidebarManifests: UmbExtensionManifest[] = [
    {
        type: "condition",
        alias: UAI_SIDEBAR_GROUP_NOT_EMPTY_CONDITION,
        name: "Sidebar Group Not Empty Condition",
        api: UaiSidebarGroupNotEmptyCondition,
    },

    // Header (title + create + search)
    {
        type: "sectionSidebarApp",
        alias: "Uai.CopilotWorkspace.SidebarApp.Header",
        name: "Copilot Workspace Sidebar Header",
        weight: 1000,
        element: () => import("./sidebar-header.element.js"),
        conditions: [sectionCondition],
    },

    // Menus (custom elements rendering each slice)
    {
        type: "menu",
        alias: UAI_PINNED_MENU_ALIAS,
        name: "Pinned Menu",
        element: () => import("./menu/pinned-menu.element.js"),
    },
    {
        type: "menu",
        alias: UAI_PROJECTS_MENU_ALIAS,
        name: "Projects Menu",
        element: () => import("./menu/projects-menu.element.js"),
    },
    {
        type: "menu",
        alias: UAI_RECENT_MENU_ALIAS,
        name: "Recent Menu",
        element: () => import("./menu/recent-menu.element.js"),
    },

    // Group apps
    {
        type: "sectionSidebarApp",
        kind: "menu",
        alias: "Uai.CopilotWorkspace.SidebarApp.Pinned",
        name: "Pinned Sidebar Group",
        weight: 400,
        meta: { label: "#uaiCopilotWorkspace_groupPinned", menu: UAI_PINNED_MENU_ALIAS },
        conditions: [sectionCondition, { alias: UAI_SIDEBAR_GROUP_NOT_EMPTY_CONDITION, match: "pinned" }],
    },
    {
        type: "sectionSidebarApp",
        kind: "menu",
        alias: "Uai.CopilotWorkspace.SidebarApp.Projects",
        name: "Projects Sidebar Group",
        weight: 300,
        meta: { label: "#uaiCopilotWorkspace_treeProjectsHeading", menu: UAI_PROJECTS_MENU_ALIAS },
        conditions: [sectionCondition, { alias: UAI_SIDEBAR_GROUP_NOT_EMPTY_CONDITION, match: "projects" }],
    },
    {
        type: "sectionSidebarApp",
        kind: "menu",
        alias: "Uai.CopilotWorkspace.SidebarApp.Recent",
        name: "Recent Sidebar Group",
        weight: 200,
        meta: { label: "#uaiCopilotWorkspace_treeRecentHeading", menu: UAI_RECENT_MENU_ALIAS },
        conditions: [sectionCondition, { alias: UAI_SIDEBAR_GROUP_NOT_EMPTY_CONDITION, match: "recent" }],
    },
];
