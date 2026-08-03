import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiAgentSurface: {
        copilotLabel: "Copilot",
        copilotDescription: "Enable Copilot features for this agent.",
    },
    // Copilot chrome (floating button + sidebar)
    uaiCopilot: {
        name: "AI Assistant",
        openLabel: "Open AI Assistant",
        closeLabel: "Close AI Assistant",
        clearLabel: "Clear chat",
        lengthMeterTitle: "This chat is getting long. Consider clearing it to start fresh.",
        sidebarTitle: "Umbraco Copilot",
        // Context framing (which item the copilot is acting on)
        inputPlaceholder: "Ask about %0%…",
        introHeading: "How can I help you with %0%?",
        introMessage: "I can search your whole site for reference, but I only make changes to this item.",
    },
    // Copilot-specific tool localizations
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
        setValueLabel: "Set Value",
        setValueDescription: "Update a property value on the current entity in the workspace",
        searchUmbracoLabel: "Search Umbraco",
        searchUmbracoDescription: "Search for content in Umbraco",
    },
} as UmbLocalizationDictionary;
