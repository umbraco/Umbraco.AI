import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    // Labels for the `copilot-workspace` agent surface, shown in the agent Availability picker.
    // Convention (see IAIAgentSurface): `uaiAgentSurface_{surfaceId}{Label|Description}`.
    uaiAgentSurface: {
        "copilot-workspaceLabel": "Copilot Workspace",
        "copilot-workspaceDescription": "Make this agent available in the Copilot Workspace.",
    },
    uaiCopilotWorkspace: {
        sectionLabel: "Copilot",
        dashboardLabel: "Workspace",

        // Launcher (section landing)
        launcherHeading: "Start a conversation",
        launcherSubtitle: "Ask anything, or pick up where you left off.",
        launcherStartInProject: "Start in a project",
        launcherRecent: "Recent",

        // Conversation list / tree
        newChat: "New chat",
        newChatInProject: "New chat in a project",
        treeCreate: "Create",
        treeProjectsHeading: "Projects",
        treeRecentHeading: "Recent",
        searchPlaceholder: "Search conversations",
        listEmpty: "Your conversations will appear here.",
        listNoResults: "No conversations match your search.",
        untitledConversation: "Untitled conversation",

        // Sidebar group headers
        groupPinned: "Pinned",
        groupToday: "Today",
        groupYesterday: "Yesterday",
        groupPrevious7Days: "Previous 7 days",
        groupOlder: "Older",
        groupUnknownProject: "Project",
        groupArchived: "Archived",

        // Per-conversation actions
        actionPin: "Pin",
        actionUnpin: "Unpin",
        actionRename: "Rename",
        actionMoveToProject: "Move to project…",
        moveHeadline: "Move to project",
        moveNoProject: "No project",
        actionArchive: "Archive",
        actionRestore: "Restore",
        actionDelete: "Delete",
        renamePrompt: "Conversation title",
        deleteConfirmTitle: "Delete conversation",
        deleteConfirmMessage: "Are you sure you want to delete this conversation? This cannot be undone.",

        // Read-only (archived) conversation
        readOnlyNotice: "This conversation is archived. Restore it to continue chatting.",

        // Projects
        newProject: "New project",
        newProjectDefaultName: "Untitled project",
        projectNameLabel: "Name",
        projectNamePlaceholder: "Project name",
        projectDescriptionLabel: "Description",
        projectInstructionsLabel: "Instructions",
        projectInstructionsHelp: "Guidance applied to every conversation in this project.",
        projectContextsLabel: "Contexts",
        projectResourcesLabel: "Resources",
        projectSave: "Save",
        projectNewChat: "New chat in this project",
        projectDelete: "Delete project",
        projectDeleteConfirmTitle: "Delete project",
        projectDeleteConfirmMessage:
            "Delete this project? Its conversations are kept but detached from the project. This cannot be undone.",
        projectNotFound: "This project could not be found.",
        projectNoConversations: "No conversations yet",
        projectBack: "Back",
        projectDetailsHeadline: "Details",
        projectInfoHeadline: "Info",
        projectInfoId: "Id",
        projectInfoCreated: "Created",
        projectInfoModified: "Last modified",
        projectInfoUnsaved: "Unsaved",

        // Context panel
        contextTitle: "Context",
        contextCollapse: "Collapse context panel",
        contextExpand: "Show context panel",
        contextInstructionsHeading: "Instructions",
        contextContextsHeading: "Contexts",
        contextResourcesHeading: "Resources",
        contextNoInstructions: "No instructions.",
        contextNoContexts: "No contexts attached.",
        contextNoResources: "No resources attached.",
        contextInheritedHint: "Inherited from the project",
    },
} satisfies UmbLocalizationDictionary;
