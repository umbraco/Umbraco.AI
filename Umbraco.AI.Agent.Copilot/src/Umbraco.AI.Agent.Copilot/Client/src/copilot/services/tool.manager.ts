import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestApi, loadManifestElement } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject } from "@umbraco-cms/backoffice/external/rxjs";
import type { UaiToolCallInfo } from "../types.js";
import { ManifestUaiAgentTool, UaiAgentToolApi, UaiAgentToolElement } from "../tools";
import { type UaiFrontendTool } from "@umbraco-ai/agent";

/** Element constructor type for tool UI components */
type UaiToolElementConstructor = new () => UaiAgentToolElement;

/**
 * Tool registry manager with reactive updates.
 *
 * Responsibilities:
 * - Observing ALL tool manifests from the extension registry (frontend and backend)
 * - Automatically updating when tools are added/removed dynamically
 * - Converting frontend-executable tools to AG-UI format for the LLM
 * - Resolving and caching tool API instances (frontend tools only)
 * - Providing manifest lookup for tool-renderer (all tools)
 * - Providing element constructors for custom UI (all tools)
 */
export class UaiToolManager extends UmbControllerBase {
    #toolManifests = new BehaviorSubject<Map<string, ManifestUaiAgentTool>>(new Map());
    #apiCache: Map<string, UaiAgentToolApi> = new Map();
    #elementCache: Map<string, UaiToolElementConstructor> = new Map();
    #frontendTools = new BehaviorSubject<UaiFrontendTool[]>([]);

    /**
     * Observable stream of frontend-executable tools.
     * Emits when tools are added, removed, or updated in the registry.
     */
    readonly frontendTools$ = this.#frontendTools.asObservable();

    /**
     * Get the current snapshot of frontend-executable tools with metadata.
     * These are tools with an `api` property that can be executed in the browser.
     * @returns Array of UaiFrontendTool definitions with scope and permission metadata
     */
    get frontendTools(): UaiFrontendTool[] {
        return [...this.#frontendTools.value];
    }

    constructor(host: UmbControllerHost) {
        super(host);

        // Observe extension registry for tool changes
        this.observe(umbExtensionsRegistry.byType("uaiAgentTool"), (manifests) =>
            this.#updateTools(manifests as ManifestUaiAgentTool[]),
        );
    }

    /**
     * Update internal state when registry changes.
     * @private
     */
    #updateTools(manifests: ManifestUaiAgentTool[]) {
        const manifestMap = new Map<string, ManifestUaiAgentTool>();
        const frontendTools: UaiFrontendTool[] = [];

        for (const manifest of manifests) {
            // Store ALL manifests (for rendering and element lookup)
            manifestMap.set(manifest.meta.toolName, manifest);

            // Only add frontend-executable tools (with api) to frontend tools list
            if (manifest.api !== undefined) {
                frontendTools.push({
                    name: manifest.meta.toolName,
                    description: manifest.meta.description ?? "",
                    parameters: manifest.meta.parameters ?? { type: "object", properties: {} },
                    // Add metadata fields for permission filtering
                    scope: manifest.meta.scope,
                    isDestructive: manifest.meta.isDestructive ?? false,
                });
            }
        }

        this.#toolManifests.next(manifestMap);
        this.#frontendTools.next(frontendTools);
    }

    /**
     * Check if a tool is a frontend-executable tool.
     * @param toolName The name of the tool
     */
    isFrontendTool(toolName: string): boolean {
        const manifests = this.#toolManifests.value;
        return manifests.has(toolName) && manifests.get(toolName)?.api !== undefined;
    }

    /**
     * Get the manifest for a tool by name.
     * @param toolName The name of the tool
     * @returns The tool manifest, or undefined if not found
     */
    getManifest(toolName: string): ManifestUaiAgentTool | undefined {
        return this.#toolManifests.value.get(toolName);
    }

    /**
     * Get or load the API instance for a tool.
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
            throw new Error(`No API found for tool: ${toolName}`);
        }

        // Load and cache API
        const ApiConstructor = await loadManifestApi<UaiAgentToolApi>(manifest.api);
        if (!ApiConstructor) {
            throw new Error(`Failed to load API for tool: ${toolName}`);
        }

        const api = new ApiConstructor(this.getHostElement());
        this.#apiCache.set(toolName, api);
        return api;
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

    /**
     * Parse tool call arguments from JSON string.
     * @param argsJson The JSON string of arguments
     * @returns Parsed arguments object, or empty object if parsing fails
     */
    static parseArgs(argsJson: string): Record<string, unknown> {
        try {
            return JSON.parse(argsJson) as Record<string, unknown>;
        } catch {
            return {};
        }
    }

    /**
     * Update a tool call in an array with new values.
     * Utility method for immutable updates.
     * @param toolCalls The array of tool calls
     * @param toolCallId The ID of the tool call to update
     * @param updates The values to update
     * @returns New array with the updated tool call
     */
    static updateToolCall(
        toolCalls: UaiToolCallInfo[],
        toolCallId: string,
        updates: Partial<UaiToolCallInfo>,
    ): UaiToolCallInfo[] {
        return toolCalls.map((tc) => (tc.id === toolCallId ? { ...tc, ...updates } : tc));
    }
}
