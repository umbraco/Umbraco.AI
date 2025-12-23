import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uAiAgent: {
        deleteConfirm: "Are you sure you want to delete this agent?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} agent(s)?`,

        // HITL Approval element defaults
        approval_defaultTitle: "Approval Required",
        approval_defaultMessage: "Do you want to proceed with this action?",
        approval_approve: "Approve",
        approval_deny: "Deny",
        approval_submit: "Submit",
        approval_cancel: "Cancel",
        approval_inputPlaceholder: "Enter your response...",
    },
} as UmbLocalizationDictionary;
