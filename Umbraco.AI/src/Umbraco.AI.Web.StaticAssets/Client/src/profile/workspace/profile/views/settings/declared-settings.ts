import { isProfileSettingSupported } from "../../../../../core/index.js";
import type { UaiProfileSettings } from "../../../../types.js";

/**
 * Field keys of the core settings a provider can declare a model does not accept, matching
 * `AIProfileSettingKeys` on the server.
 * @public
 */
export const UAI_PROFILE_SETTING_KEYS = {
    temperature: "temperature",
    dimensions: "dimensions",
    language: "language",
    mediaType: "mediaType",
} as const;

/**
 * How a capability's settings react to a model's declaration: which field keys it owns, and what a stored
 * value should become when the selected model rejects it.
 *
 * @remarks
 * The point of pairing these is that hiding a field and clearing its stored value are the same decision.
 * They used to live apart — a guard inside a render method, and a clause in a shared prune chain — and two
 * settings were duly given one without the other. Anything reading this contract gets both or neither.
 * @public
 */
export interface UaiDeclaredSettingRule<TSettings extends UaiProfileSettings> {
    /** The field key a provider declares, as it appears in the model metadata. */
    readonly key: string;

    /** Whether the stored settings currently carry a value for this field. */
    hasValue(settings: TSettings | null): boolean;

    /** The settings with this field cleared. */
    clear(settings: TSettings): TSettings;
}

/**
 * Whether the field a rule governs should render for the selected model.
 *
 * Declarations are negative, so an absent one means "not known to be rejected" and the field renders.
 * @public
 */
export function isRuleSupported<TSettings extends UaiProfileSettings>(
    rule: UaiDeclaredSettingRule<TSettings>,
    metadata: Record<string, string> | undefined,
): boolean {
    return isProfileSettingSupported(metadata, rule.key);
}

/**
 * The settings with every declared-unsupported field cleared, or `undefined` when nothing needs changing —
 * which the partial update command skips, leaving the stored value untouched.
 *
 * @param settings - The stored settings for the capability.
 * @param metadata - The selected model descriptor's metadata.
 * @param rules - The capability's rules.
 * @public
 */
export function pruneDeclaredSettings<TSettings extends UaiProfileSettings>(
    settings: TSettings | null,
    metadata: Record<string, string> | undefined,
    rules: readonly UaiDeclaredSettingRule<TSettings>[],
): TSettings | undefined {
    if (!settings) return undefined;

    let pruned: TSettings | undefined;

    for (const rule of rules) {
        if (!rule.hasValue(settings)) continue;
        if (isRuleSupported(rule, metadata)) continue;

        pruned = rule.clear(pruned ?? settings);
    }

    return pruned;
}
