/**
 * Well-known model metadata key listing the capability settings a model rejects.
 * @public
 */
export const UAI_METADATA_CAPABILITY_SETTINGS_UNSUPPORTED = "capabilitySettings.unsupported";

/**
 * Whether a provider-declared capability setting applies to a model.
 *
 * Support for these settings varies by model rather than by provider (reasoning effort is an
 * o-series/GPT-5 knob; a thinking budget is rejected by the newest Claude models), so providers declare
 * it per model and the backend folds it into the model list the editor already fetches — no extra
 * request when the model selection changes.
 *
 * Returns `false` only when the provider explicitly declared that the model rejects the setting.
 * Declarations are negative — a provider says nothing about models it has no knowledge of — so this is
 * "not known to be rejected" rather than an affirmative claim of support.
 *
 * @param metadata - The selected model descriptor's metadata.
 * @param fieldKey - The schema field key of the setting.
 * @public
 */
export function isCapabilitySettingSupported(
    metadata: Record<string, string> | undefined,
    fieldKey: string,
): boolean {
    return isSettingSupported(metadata, UAI_METADATA_CAPABILITY_SETTINGS_UNSUPPORTED, fieldKey);
}

/**
 * Well-known model metadata key listing the core profile settings a model rejects.
 * @public
 */
export const UAI_METADATA_PROFILE_SETTINGS_UNSUPPORTED = "profileSettings.unsupported";

/**
 * Whether a core profile setting (e.g. `temperature`) applies to a model.
 *
 * Same channel as {@link isCapabilitySettingSupported}, for the built-in settings every provider shares
 * rather than the ones a provider declares. Anthropic dropped `temperature` from Claude Opus 4.7 onwards
 * and OpenAI's reasoning models restrict it the same way, so the field is a permanent fixture of the
 * editor that some models simply do not accept.
 *
 * Returns `false` only when the provider explicitly declared that the model rejects the setting.
 *
 * @param metadata - The selected model descriptor's metadata.
 * @param fieldKey - The field key of the setting.
 * @public
 */
export function isProfileSettingSupported(
    metadata: Record<string, string> | undefined,
    fieldKey: string,
): boolean {
    return isSettingSupported(metadata, UAI_METADATA_PROFILE_SETTINGS_UNSUPPORTED, fieldKey);
}

/**
 * Well-known model metadata key listing the image sizes a model accepts.
 * @public
 */
export const UAI_METADATA_IMAGE_SUPPORTED_SIZES = "image.supportedSizes";

/**
 * The image sizes a model accepts, each as `"{width}x{height}"`.
 *
 * Returns an empty array when the provider declared none, which means "unknown" rather than "none
 * supported" — a caller should keep accepting a free-typed size rather than blocking every model a provider
 * happens not to describe.
 *
 * @param metadata - The selected model descriptor's metadata.
 * @public
 */
export function getSupportedImageSizes(metadata: Record<string, string> | undefined): string[] {
    const declared = metadata?.[UAI_METADATA_IMAGE_SUPPORTED_SIZES];
    if (!declared) return [];

    return declared
        .split(",")
        .map((size) => size.trim())
        .filter((size) => size.length > 0);
}

function isSettingSupported(
    metadata: Record<string, string> | undefined,
    metadataKey: string,
    fieldKey: string,
): boolean {
    const declared = metadata?.[metadataKey];
    if (!declared || !fieldKey) return true;

    return !declared
        .split(",")
        .map((key) => key.trim().toLowerCase())
        .includes(fieldKey.trim().toLowerCase());
}
