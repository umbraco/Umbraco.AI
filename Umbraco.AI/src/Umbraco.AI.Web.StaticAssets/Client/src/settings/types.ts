import type { UaiDisclosureNoticeMode } from "../disclosure/types.js";

/**
 * Model for AI settings.
 */
export interface UaiSettingsModel {
    defaultChatProfileId: string | null;
    defaultEmbeddingProfileId: string | null;
    defaultSpeechToTextProfileId: string | null;
    defaultImageGenerationProfileId: string | null;
    defaultDecisionProfileId: string | null;
    classifierChatProfileId: string | null;
    disclosureNoticeMode: UaiDisclosureNoticeMode;
}
