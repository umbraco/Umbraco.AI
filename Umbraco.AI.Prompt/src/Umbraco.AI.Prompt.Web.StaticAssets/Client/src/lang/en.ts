import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiPromptPermissions: {
        manageLabel: "Manage prompts",
        manageDescription: "Create, edit and delete prompts, and choose which user groups can use them",
    },
    uaiPrompt: {
        deleteConfirm: "Are you sure you want to delete this prompt?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} prompt(s)?`,
        previewTitle: "Prompt Preview",
        emptyResult: "The AI returned an empty result. Try regenerating.",
        copiedToClipboard: "Value copied to clipboard",
        copyFailed: "Failed to copy to clipboard",
        selectionRequired: "Select some text first",
    },
    tiptapGroup: {
        ai: "AI",
    },
} as UmbLocalizationDictionary;
