import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiAgentScope:{
        copilotLabel: "Copilot",
        copilotDescription: "Enable Copilot features for this agent.",  
    },
    uaiAgentCopilot: {
        // HITL Approval element defaults
        approvalDefaultTitle: "Approval Required",
        approvalDefaultMessage: "Do you want to proceed with this action?",
        approvalApprove: "Approve",
        approvalDeny: "Deny",
        approvalSubmit: "Submit",
        approvalCancel: "Cancel",
        approvalInputPlaceholder: "Enter your response...",
    },
} as UmbLocalizationDictionary;
