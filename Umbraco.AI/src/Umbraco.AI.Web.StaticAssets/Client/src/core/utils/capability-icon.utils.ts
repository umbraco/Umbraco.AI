/**
 * Maps an AI capability to the Umbraco icon that best represents it, so every place a capability
 * is shown (profile picker, profile create options, collection create action, etc.) stays visually
 * consistent instead of each carrying its own copy of the switch.
 *
 * Capability names are matched case-insensitively to tolerate both the raw API casing
 * (e.g. `speechToText`) and the lower-cased form used for localization keys (`speechtotext`).
 */
export function getCapabilityIcon(capability: string): string {
    switch (capability.toLowerCase()) {
        case "chat":
            return "icon-chat";
        case "embedding":
            return "icon-layers";
        case "media":
            return "icon-video";
        case "moderation":
            return "icon-shield";
        case "speechtotext":
            return "icon-voice";
        case "imagegeneration":
            return "icon-picture";
        case "decision":
            return "icon-directions";
        case "extraction":
            return "icon-filter";
        default:
            return "icon-wand";
    }
}
