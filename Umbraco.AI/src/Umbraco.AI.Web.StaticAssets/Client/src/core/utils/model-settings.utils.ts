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
    const declared = metadata?.[UAI_METADATA_CAPABILITY_SETTINGS_UNSUPPORTED];
    if (!declared || !fieldKey) return true;

    return !declared
        .split(",")
        .map((key) => key.trim().toLowerCase())
        .includes(fieldKey.trim().toLowerCase());
}
