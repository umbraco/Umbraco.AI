import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uAIAgent: {
        deleteConfirm: "Are you sure you want to delete this agent?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} agent(s)?`,
    },
    uaiAgent: {
        selectScope: "Select Scope",
        addScope: "Add Scope",
        noScopesAvailable: "No scopes available. Scopes are registered by add-on packages.",
        scopesDescription: "Categorize this agent for specific purposes (e.g., Copilot chat)",
    },
} as UmbLocalizationDictionary;
