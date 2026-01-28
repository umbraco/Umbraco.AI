import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uAiAgent: {
        deleteConfirm: "Are you sure you want to delete this agent?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} agent(s)?`,
    },
} as UmbLocalizationDictionary;
