/**
 * Model for AI settings.
 */
export interface UaiSettingsModel {
    defaultChatProfileId: string | null;
    defaultEmbeddingProfileId: string | null;
    defaultSpeechToTextProfileId: string | null;
    defaultImageGenerationProfileId: string | null;
    classifierChatProfileId: string | null;
}
