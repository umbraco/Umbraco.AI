import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiAgentSurface: {
        copilotLabel: "Copilot",
        copilotDescription: "Enable Copilot features for this agent.",
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
        addItemLabel: "Add Item",
        addItemDescription: "Add an item to a block list, block grid or picker property on the current entity",
        removeItemLabel: "Remove Item",
        removeItemDescription: "Remove an item from a block list, block grid or picker property on the current entity",
        moveItemLabel: "Move Item",
        moveItemDescription: "Reorder an item in a block list, block grid or picker property on the current entity",
        clearValueLabel: "Clear Value",
        clearValueDescription: "Clear a property value on the current entity in the workspace",
        saveLabel: "Save",
        saveDescription: "Save the staged changes on the current entity",
        saveAndPublishLabel: "Save and Publish",
        saveAndPublishDescription: "Save and publish the staged changes on the current document",
        searchUmbracoLabel: "Search Umbraco",
        searchUmbracoDescription: "Search for content in Umbraco",
    },
} as UmbLocalizationDictionary;
