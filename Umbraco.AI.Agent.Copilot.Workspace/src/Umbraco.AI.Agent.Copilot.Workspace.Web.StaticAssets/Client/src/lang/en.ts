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
        sharedBadgeLabel: "Shared",
        sharedByOwner: "Shared by %0%",

        // Sidebar group headers
        groupPinned: "Pinned",
        groupSharedWithMe: "Shared with me",
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

        // Share-picker modal
        sharePickerHeadline: 'Share "%0%"',
        sharePersonLabel: "Person",
        sharePersonHelp: "Pick one colleague — not a link anyone can open.",
        sharePersonChoose: "Choose a person",
        sharePickDifferentPerson: "Pick a different person",
        shareModeLabel: "Mode",
        shareModeSnapshot: "Snapshot — frozen at today's messages",
        shareModeLive: "Live — always shows the latest messages",
        // Placeholder copy pending real product/legal review (ARCHITECTURE.md decision 2) — same status
        // as the share-notification email's copy.
        shareDisclosureBanner:
            "The person you share with will see everything referenced in this conversation, including pages or files they might not otherwise have permission to open.",
        shareSubmit: "Share",

        // Read-only (archived or shared) conversation
        readOnlyNotice: "This conversation is archived. Restore it to continue chatting.",
        sharedReadOnlyNotice: "This conversation is read-only — shared by %0%.",

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
            "Delete this project? This cannot be undone. A project that still has conversations can't be deleted — move or delete them first.",
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
        contextSharedHeading: "Shared with",
        contextNoShares: "Not shared with anyone yet.",
        contextShareChoose: "Choose",
        contextShareRemoveLabel: "Remove",
    },
} satisfies UmbLocalizationDictionary;
