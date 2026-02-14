import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestApi } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject } from "rxjs";
import type { UaiFrontendTool } from "@umbraco-ai/agent";
import type { ManifestUaiAgentFrontendTool } from "../extensions/uai-agent-frontend-tool.extension.js";
import type { UaiAgentToolApi } from "../types/tool.types.js";

/**
 * Frontend tool manager -- handles execution concerns.
 *
 * Observes `uaiAgentFrontendTool` extensions from the extension registry,
 * produces `UaiFrontendTool[]` for the AG-UI client, and provides API loading.
 *
 * Because the manager observes only *resolved* manifests from the extension registry,
 * any future conditions on the manifest are automatically respected. A tool with
 * conditions: [{ alias: "Umb.Condition.Context", context: "UAI_ENTITY_CONTEXT" }]
 * would only appear in frontendTools$ when entity context is provided by the current
 * surface. No filtering logic needed in the manager itself.
 */
export class UaiFrontendToolManager extends UmbControllerBase {
    #toolManifests = new BehaviorSubject<Map<string, ManifestUaiAgentFrontendTool>>(new Map());
    #apiCache: Map<string, UaiAgentToolApi> = new Map();
    #frontendTools = new BehaviorSubject<UaiFrontendTool[]>([]);

    /**
     * Observable stream of frontend-executable tools.
     * Emits when tools are added, removed, or updated in the registry.
     */
    readonly frontendTools$ = this.#frontendTools.asObservable();

    /**
     * Get the current snapshot of frontend-executable tools with metadata.
     * @returns Array of UaiFrontendTool definitions with scope and permission metadata
     */
    get frontendTools(): UaiFrontendTool[] {
        return [...this.#frontendTools.value];
    }

    constructor(host: UmbControllerHost) {
        super(host);

        // Observe "uaiAgentFrontendTool" extensions -- only resolved manifests appear
        // (i.e., the conditions framework has already filtered by the current surface's context)
        this.observe(umbExtensionsRegistry.byType("uaiAgentFrontendTool"), (manifests) =>
            this.#updateTools(manifests as ManifestUaiAgentFrontendTool[]),
        );
    }

    #updateTools(manifests: ManifestUaiAgentFrontendTool[]) {
        const manifestMap = new Map<string, ManifestUaiAgentFrontendTool>();
        const frontendTools: UaiFrontendTool[] = [];

        for (const manifest of manifests) {
            manifestMap.set(manifest.meta.toolName, manifest);

            frontendTools.push({
                name: manifest.meta.toolName,
                description: manifest.meta.description,
                parameters: manifest.meta.parameters,
                scope: manifest.meta.scope,
                isDestructive: manifest.meta.isDestructive ?? false,
            });
        }

        this.#toolManifests.next(manifestMap);
        this.#frontendTools.next(frontendTools);
    }

    /**
     * Check if a tool is a frontend-executable tool.
     * @param toolName The name of the tool
     */
    isFrontendTool(toolName: string): boolean {
        return this.#toolManifests.value.has(toolName);
    }

    /**
     * Get or load the API instance for a frontend tool.
     * @param toolName The name of the tool
     * @returns The tool API instance
     * @throws Error if tool not found or API fails to load
     */
    async getApi(toolName: string): Promise<UaiAgentToolApi> {
        // Return cached instance if available
        const cached = this.#apiCache.get(toolName);
        if (cached) {
            return cached;
        }

        // Get manifest
        const manifest = this.#toolManifests.value.get(toolName);
        if (!manifest?.api) {
            throw new Error(`No API found for frontend tool: ${toolName}`);
        }

        // Load and cache API
        const ApiConstructor = await loadManifestApi<UaiAgentToolApi>(manifest.api);
        if (!ApiConstructor) {
            throw new Error(`Failed to load API for frontend tool: ${toolName}`);
        }

        const api = new ApiConstructor(this.getHostElement());
        this.#apiCache.set(toolName, api);
        return api;
    }
}
