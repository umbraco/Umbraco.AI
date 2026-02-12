import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";

/**
 * Manifest type for declaring section compatibility with copilot.
 *
 * This allows packages (including third-party) to declare that
 * their section supports the copilot feature.
 *
 * @example
 * ```ts
 * const manifest: ManifestUaiCopilotCompatibleSection = {
 *     type: "uaiCopilotCompatibleSection",
 *     alias: "MyPackage.Copilot.Section.CustomAI",
 *     name: "Custom AI Section Copilot Support",
 *     section: "MyPackage.Section.CustomAI",
 * };
 * ```
 */
export interface ManifestUaiCopilotCompatibleSection extends ManifestBase {
    /**
     * Must be "uaiCopilotCompatibleSection"
     */
    type: "uaiCopilotCompatibleSection";

    /**
     * The section alias that supports copilot.
     * Should match a registered ManifestSection.alias.
     *
     * @example "Umb.Section.Content"
     */
    section: string;
}
