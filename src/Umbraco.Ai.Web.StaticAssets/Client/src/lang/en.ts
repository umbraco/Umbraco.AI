import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiCapabilities: {
        chat: "Chat",
        embedding: "Embedding",
        media: "Media",
        moderation: "Moderation",
    },
    uaiConnection: {
        deleteConfirm: "Are you sure you want to delete this connection?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} connection(s)?`,
    },
    uaiProfile: {
        deleteConfirm: "Are you sure you want to delete this profile?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} profile(s)?`,
    },
} as UmbLocalizationDictionary;
