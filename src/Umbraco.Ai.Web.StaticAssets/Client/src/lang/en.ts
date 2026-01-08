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
    uaiFields: {
        // Text resource fields
        textContentLabel: "Content",
        textContentDescription: "The text content (plain text or markdown)",

        // Brand Voice resource fields
        brandVoiceToneDescriptionLabel: "Tone",
        brandVoiceToneDescriptionDescription: "Description of the tone to use (e.g., \"Professional but approachable\")",
        brandVoiceTargetAudienceLabel: "Target Audience",
        brandVoiceTargetAudienceDescription: "Description of the target audience (e.g., \"B2B tech decision makers\")",
        brandVoiceStyleGuidelinesLabel: "Style Guidelines",
        brandVoiceStyleGuidelinesDescription: "Style guidelines to follow (e.g., \"Use active voice, be concise\")",
        brandVoiceAvoidPatternsLabel: "Patterns to Avoid",
        brandVoiceAvoidPatternsDescription: "Patterns and phrases to avoid (e.g., \"Jargon, exclamation marks\")",
    },
} as UmbLocalizationDictionary;
