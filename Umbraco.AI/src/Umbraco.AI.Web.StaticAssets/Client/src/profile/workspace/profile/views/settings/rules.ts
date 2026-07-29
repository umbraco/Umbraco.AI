import type { UaiDeclaredSettingRule } from "./declared-settings.js";
import { UAI_PROFILE_SETTING_KEYS } from "./declared-settings.js";
import type {
    UaiChatProfileSettings,
    UaiEmbeddingProfileSettings,
    UaiImageGenerationProfileSettings,
    UaiSpeechToTextProfileSettings,
} from "../../../../types.js";

/**
 * The declaration rules for each capability's core settings.
 *
 * @remarks
 * One list per capability, read by both the settings element (to decide what renders) and the details view
 * (to decide what to clear on a model change). Adding a declarable setting means adding one entry here, and
 * both behaviours follow.
 *
 * Settings with no entry are ones no provider can declare: chat max tokens (removing a token limit would
 * fail a request rather than degrade it), and the Umbraco-level concepts — system prompt, contexts and
 * guardrails — which are not provider options at all. Image size is absent for a different reason: its
 * support is described by enumerating what a model accepts, not by declaring it unsupported.
 * @public
 */
export const UAI_CHAT_SETTING_RULES: readonly UaiDeclaredSettingRule<UaiChatProfileSettings>[] = [
    {
        key: UAI_PROFILE_SETTING_KEYS.temperature,
        hasValue: (settings) => settings?.temperature !== null && settings?.temperature !== undefined,
        clear: (settings) => ({ ...settings, temperature: null }),
    },
];

/** @public */
export const UAI_EMBEDDING_SETTING_RULES: readonly UaiDeclaredSettingRule<UaiEmbeddingProfileSettings>[] = [
    {
        key: UAI_PROFILE_SETTING_KEYS.dimensions,
        hasValue: (settings) => settings?.dimensions !== null && settings?.dimensions !== undefined,
        clear: (settings) => ({ ...settings, dimensions: null }),
    },
];

/** @public */
export const UAI_SPEECH_TO_TEXT_SETTING_RULES: readonly UaiDeclaredSettingRule<UaiSpeechToTextProfileSettings>[] = [
    {
        key: UAI_PROFILE_SETTING_KEYS.language,
        hasValue: (settings) => !!settings?.language,
        clear: (settings) => ({ ...settings, language: null }),
    },
];

/** @public */
export const UAI_IMAGE_GENERATION_SETTING_RULES: readonly UaiDeclaredSettingRule<UaiImageGenerationProfileSettings>[] = [
    {
        key: UAI_PROFILE_SETTING_KEYS.mediaType,
        hasValue: (settings) => !!settings?.mediaType,
        clear: (settings) => ({ ...settings, mediaType: null }),
    },
];
