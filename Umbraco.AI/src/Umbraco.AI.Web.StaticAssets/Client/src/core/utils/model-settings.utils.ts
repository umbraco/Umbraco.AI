/**
 * Well-known model metadata key listing the capability settings a model accepts.
 * @public
 */
export const UAI_METADATA_CAPABILITY_SETTINGS_SUPPORTED = "capabilitySettings.supported";

/**
 * Well-known model metadata key listing the capability settings a model rejects.
 * @public
 */
export const UAI_METADATA_CAPABILITY_SETTINGS_UNSUPPORTED = "capabilitySettings.unsupported";

/**
 * Whether a setting applies to a given model.
 * @public
 */
export type UaiSettingSupport = "supported" | "unsupported" | "unknown";

/**
 * Reads whether a provider-declared capability setting applies to a model, from the metadata carried
 * by the model descriptor.
 *
 * Providers declare this per model (support for reasoning effort, thinking budgets and the like varies
 * by model, not by provider) and the backend folds it into the model list the editor already fetches,
 * so no extra request is needed when the model selection changes.
 *
 * `unknown` means the provider said nothing about this setting for this model — render it normally, as
 * a provider only ever declares what it positively knows.
 *
 * @param metadata - The selected model descriptor's metadata.
 * @param fieldKey - The schema field key of the setting.
 * @public
 */
export function getCapabilitySettingSupport(
    metadata: Record<string, string> | undefined,
    fieldKey: string,
): UaiSettingSupport {
    if (!metadata || !fieldKey) return "unknown";

    // Unsupported wins, so a provider that lists a key in both gets the safer answer.
    if (listContains(metadata[UAI_METADATA_CAPABILITY_SETTINGS_UNSUPPORTED], fieldKey)) return "unsupported";
    if (listContains(metadata[UAI_METADATA_CAPABILITY_SETTINGS_SUPPORTED], fieldKey)) return "supported";

    return "unknown";
}

/**
 * Whether a capability setting was explicitly declared unsupported for a model.
 * @public
 */
export function isCapabilitySettingUnsupported(
    metadata: Record<string, string> | undefined,
    fieldKey: string,
): boolean {
    return getCapabilitySettingSupport(metadata, fieldKey) === "unsupported";
}

function listContains(list: string | undefined, fieldKey: string): boolean {
    if (!list) return false;

    return list
        .split(",")
        .map((key) => key.trim().toLowerCase())
        .includes(fieldKey.trim().toLowerCase());
}
