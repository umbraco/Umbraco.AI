import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiAgentScope: {
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
    // Frontend tool localizations
    uaiTool: {
      setPropertyValueLabel: "Set Property Value",
      setPropertyValueDescription: "Set a property value on an entity",
      getCurrentTimeLabel: "Get Current Time",
      getCurrentTimeDescription: "Get the current date and time",
      getPageInfoLabel: "Get Page Info",
      getPageInfoDescription: "Get information about the current page",
      showWeatherLabel: "Show Weather",
      showWeatherDescription: "Display weather information",
      confirmActionLabel: "Confirm Action",
      confirmActionDescription: "Request user confirmation for an action",
      searchUmbracoLabel: "Search Umbraco",
      searchUmbracoDescription: "Search for content in Umbraco",
    }
} as UmbLocalizationDictionary;
