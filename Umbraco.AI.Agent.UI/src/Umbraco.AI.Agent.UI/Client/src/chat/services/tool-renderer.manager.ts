import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestElement } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject } from "rxjs";
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
export class UaiToolRendererManager extends UmbControllerBase {
    #toolManifests = new BehaviorSubject<Map<string, ManifestUaiAgentToolRenderer>>(new Map());
    #elementCache: Map<string, UaiToolElementConstructor> = new Map();

    constructor(host: UmbControllerHost) {
        super(host);

        // Observe extension registry for tool renderer changes
        this.observe(umbExtensionsRegistry.byType("uaiAgentToolRenderer"), (manifests) =>
            this.#updateManifests(manifests as ManifestUaiAgentToolRenderer[]),
        );
    }

    #updateManifests(manifests: ManifestUaiAgentToolRenderer[]) {
        const manifestMap = new Map<string, ManifestUaiAgentToolRenderer>();
        for (const manifest of manifests) {
            manifestMap.set(manifest.meta.toolName, manifest);
        }
        this.#toolManifests.next(manifestMap);
    }

    /**
     * Get the renderer manifest for a tool by name.
     * @param toolName The name of the tool
     * @returns The tool renderer manifest, or undefined if not found
     */
    getManifest(toolName: string): ManifestUaiAgentToolRenderer | undefined {
        return this.#toolManifests.value.get(toolName);
    }

    /**
     * Get or load the element constructor for a tool's UI.
     * @param toolName The name of the tool
     * @returns The element constructor, or undefined if tool has no custom UI
     */
    async getElement(toolName: string): Promise<UaiToolElementConstructor | undefined> {
        // Return cached constructor if available
        const cached = this.#elementCache.get(toolName);
        if (cached) {
            return cached;
        }

        // Get manifest
        const manifest = this.#toolManifests.value.get(toolName);
        if (!manifest?.element) {
            return undefined;
        }

        // Load and cache element constructor
        const ElementConstructor = await loadManifestElement<UaiAgentToolElement>(manifest.element);
        if (!ElementConstructor) {
            return undefined;
        }

        this.#elementCache.set(toolName, ElementConstructor as UaiToolElementConstructor);
        return ElementConstructor as UaiToolElementConstructor;
    }
}
