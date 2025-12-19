import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uAiAgent: {
        deleteConfirm: "Are you sure you want to delete this prompt?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} prompt(s)?`,
    },
} as UmbLocalizationDictionary;
