import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiFrontendTool } from "@umbraco-ai/agent";
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
export declare class UaiFrontendToolManager extends UmbControllerBase {
    #private;
    /**
     * Observable stream of frontend-executable tools.
     * Emits when tools are added, removed, or updated in the registry.
     */
    readonly frontendTools$: import("rxjs").Observable<UaiFrontendTool[]>;
    /**
     * Get the current snapshot of frontend-executable tools with metadata.
     * @returns Array of UaiFrontendTool definitions with scope and permission metadata
     */
    get frontendTools(): UaiFrontendTool[];
    constructor(host: UmbControllerHost);
    /**
     * Check if a tool is a frontend-executable tool.
     * @param toolName The name of the tool
     */
    isFrontendTool(toolName: string): boolean;
    /**
     * Get or load the API instance for a frontend tool.
     * @param toolName The name of the tool
     * @returns The tool API instance
     * @throws Error if tool not found or API fails to load
     */
    getApi(toolName: string): Promise<UaiAgentToolApi>;
}
//# sourceMappingURL=frontend-tool.manager.d.ts.map