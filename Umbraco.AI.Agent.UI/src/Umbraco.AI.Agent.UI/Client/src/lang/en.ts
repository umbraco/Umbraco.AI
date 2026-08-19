import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiChat: {
        // HITL Approval element defaults
        approvalDefaultTitle: "Approval Required",
        approvalDefaultMessage: "Do you want to proceed with this action?",
        approvalApprove: "Approve",
        approvalDeny: "Deny",
        approvalSubmit: "Submit",
        approvalCancel: "Cancel",
        approvalInputPlaceholder: "Enter your response...",
        approvalConfirmPhraseLabel: "Type <strong>'%0%'</strong> to confirm",
        // Plain-text twin of approvalConfirmPhraseLabel for the input's accessible name -- a screen
        // reader shouldn't hear literal "<strong>" tag text.
        approvalConfirmPhraseLabelPlain: "Type '%0%' to confirm",
    },
} as UmbLocalizationDictionary;
