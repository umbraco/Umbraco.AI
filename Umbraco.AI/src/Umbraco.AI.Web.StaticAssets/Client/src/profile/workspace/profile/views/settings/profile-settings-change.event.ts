import type { UaiProfileSettings } from "../../../../types.js";

/**
 * The event name a capability settings element dispatches when its fields change.
 *
 * @remarks
 * Deliberately not `change`: the native events from the inputs inside these elements also bubble, so a
 * parent listening for `change` would receive both and could not tell them apart.
 * @public
 */
export const UAI_PROFILE_SETTINGS_CHANGE_EVENT = "uai-profile-settings-change";

/**
 * Carries the capability's settings, complete rather than partial, so the parent stores what it is given
 * without needing to know which field moved.
 * @public
 */
export interface UaiProfileSettingsChangeEventDetail<TSettings extends UaiProfileSettings = UaiProfileSettings> {
    settings: TSettings;
}

/**
 * Builds the change event for a capability settings element.
 * @public
 */
export function uaiProfileSettingsChangeEvent<TSettings extends UaiProfileSettings>(
    settings: TSettings,
): CustomEvent<UaiProfileSettingsChangeEventDetail<TSettings>> {
    return new CustomEvent(UAI_PROFILE_SETTINGS_CHANGE_EVENT, {
        detail: { settings },
        bubbles: true,
        composed: true,
    });
}
