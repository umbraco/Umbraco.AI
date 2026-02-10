import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { ManifestUaiAgentToolRenderer } from "../extensions/uai-agent-tool-renderer.extension.js";
import type { UaiAgentToolElement } from "../types/tool.types.js";
/** Element constructor type for tool UI components */
type UaiToolElementConstructor = new () => UaiAgentToolElement;
/**
 * Tool renderer manager -- handles rendering concerns.
 *
 * Responsibilities:
 * - Observing `uaiAgentToolRenderer` manifests from the extension registry
 * - Providing manifest lookup by tool name (for approval config, icon, label)
 * - Loading and caching tool UI elements (Generative UI)
 *
 * This manager handles ONLY rendering. For execution, see UaiFrontendToolManager.
 */
export declare class UaiToolRendererManager extends UmbControllerBase {
    #private;
    constructor(host: UmbControllerHost);
    /**
     * Get the renderer manifest for a tool by name.
     * @param toolName The name of the tool
     * @returns The tool renderer manifest, or undefined if not found
     */
    getManifest(toolName: string): ManifestUaiAgentToolRenderer | undefined;
    /**
     * Get or load the element constructor for a tool's UI.
     * @param toolName The name of the tool
     * @returns The element constructor, or undefined if tool has no custom UI
     */
    getElement(toolName: string): Promise<UaiToolElementConstructor | undefined>;
}
export {};
//# sourceMappingURL=tool-renderer.manager.d.ts.map