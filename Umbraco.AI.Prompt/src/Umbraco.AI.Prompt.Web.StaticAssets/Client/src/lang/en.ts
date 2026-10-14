import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiPrompt: {
        deleteConfirm: "Are you sure you want to delete this prompt?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} prompt(s)?`,
        previewTitle: "Prompt Preview",
        emptyResult: "The AI returned an empty result. Try regenerating.",
        copiedToClipboard: "Value copied to clipboard",
        copyFailed: "Failed to copy to clipboard",
    },
    tiptapGroup : {
        ai: "AI",
    }
} as UmbLocalizationDictionary;
