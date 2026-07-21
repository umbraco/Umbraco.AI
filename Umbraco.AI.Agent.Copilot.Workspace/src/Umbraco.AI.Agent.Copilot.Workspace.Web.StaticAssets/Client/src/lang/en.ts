import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiCopilotWorkspace: {
        sectionLabel: "Copilot Workspace",
        dashboardLabel: "Workspace",

        // Conversation list
        newChat: "New chat",
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

        // Per-conversation actions
        actionPin: "Pin",
        actionUnpin: "Unpin",
        actionRename: "Rename",
        actionArchive: "Archive",
        actionUnarchive: "Unarchive",
        actionDelete: "Delete",
        renamePrompt: "Conversation title",
        deleteConfirmTitle: "Delete conversation",
        deleteConfirmMessage: "Are you sure you want to delete this conversation? This cannot be undone.",

        // Context panel
        contextTitle: "Context",
        contextCollapse: "Collapse context panel",
        contextNoProject: "This conversation isn't part of a project. Add it to a project to give it shared instructions and attachments.",
        contextInstructionsHeading: "Instructions",
        contextAttachmentsHeading: "Attachments",
        contextContextsHeading: "Contexts",
        contextNoAttachments: "No attachments.",
        contextContextCount: "{0} context set(s) attached",
    },
} satisfies UmbLocalizationDictionary;
