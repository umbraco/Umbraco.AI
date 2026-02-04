import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiGeneral: {
        select: "Select",
        close: "Close"
    },
    uaiComponents: {
        pollingButtonTogglePolling: "Toggle Polling",
        pollingButtonPolling: "Polling",
        pollingButtonChoosePollingInterval: "Choose Polling Interval",
        pollingButtonPollingActive: "Polling {0} seconds",
        pollingButtonPollingInterval: "Every {0} seconds",
    },
    uaiCapabilities: {
        chat: "Chat",
        embedding: "Embedding",
        media: "Media",
        moderation: "Moderation",
    },
    uaiConnection: {
        deleteConfirm: "Are you sure you want to delete this connection?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} connection(s)?`,
        actions: "Actions",
        testConnection: "Test Connection",
        testConnectionSuccess: "Connection test successful",
        testConnectionFailed: "Connection test failed",
    },
    uaiProfile: {
        selectProfile: "Select AI profile",
        addProfile: "Add profile",
        noProfilesAvailable: "No AI profiles available. Create one in the AI section.",
        deleteConfirm: "Are you sure you want to delete this profile?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} profile(s)?`,
    },
    uaiContext: {
        selectContext: "Select AI Context",
        addContext: "Add context",
        noContextsAvailable: "No AI contexts available. Create one in the AI section.",
        deleteConfirm: "Are you sure you want to delete this context?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} context(s)?`,
    },
    uaiAuditLog: {
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} log entry(ies)?`,
    },
    uaiVersionHistory: {
        history: "History",
        version: "Version",
        date: "Date",
        user: "User",
        compare: "Compare",
        current: "current",
        noVersionsYet: "No versions yet",
        pageInfo: (current: number, total: number) => `Page ${current} of ${total}`,
        compareVersions: (from: number, to: number) => `Compare v${from} to Current (v${to})`,
        rollbackDescription: (version: number) => `Rolling back will create a new version with the content from v${version}. This action cannot be undone.`,
        rollbackTo: (version: number) => `Rollback to v${version}`,
        rollback: "Rollback",
        changes: "Changes",
        noChanges: "No changes detected",
        oldValue: "Old",
        newValue: "New",
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

        // Amazon Bedrock fields
        amazonRegionLabel: "AWS Region",
        amazonRegionDescription: "The AWS region for Bedrock services (e.g., \"us-east-1\")",
        amazonAccessKeyIdLabel: "Access Key ID",
        amazonAccessKeyIdDescription: "The AWS Access Key ID for authenticating with Bedrock services",
        amazonSecretAccessKeyLabel: "Secret Access Key",
        amazonSecretAccessKeyDescription: "The AWS Secret Access Key for authenticating with Bedrock services",
        amazonEndpointLabel: "Custom Endpoint",
        amazonEndpointDescription: "Custom endpoint URL for Bedrock services (optional)",
    },
} as UmbLocalizationDictionary;
