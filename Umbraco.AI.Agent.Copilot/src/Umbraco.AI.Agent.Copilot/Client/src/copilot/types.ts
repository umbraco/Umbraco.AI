import { ManifestBase } from "@umbraco-cms/backoffice/extension-api";

/**
 * Copilot-specific agent item extending the shared agent item.
 * Currently identical to UaiAgentItem but allows copilot-specific extensions in the future.
 */
export type UaiCopilotAgentItem = import("@umbraco-ai/agent-ui").UaiAgentItem;

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
