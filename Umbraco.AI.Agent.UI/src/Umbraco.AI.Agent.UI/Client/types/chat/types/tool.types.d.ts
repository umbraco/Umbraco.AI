import type { UmbApi } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHostElement } from "@umbraco-cms/backoffice/controller-api";
/**
 * Tool execution API interface.
 * Implement this to create a tool that can be called by AI agents.
 *
 * Extends UmbApi so it can be used with ManifestApi and the extension registry.
 */
export interface UaiAgentToolApi extends UmbApi {
    /**
     * Execute the tool with the given arguments.
     * @param args The arguments passed by the AI agent
     * @returns The result to send back to the agent
     */
    execute(args: Record<string, unknown>): Promise<unknown>;
}
/**
 * Tool status values matching AG-UI events.
 */
export type UaiAgentToolStatus = "pending" | "streaming" | "awaiting_approval" | "executing" | "complete" | "error";
/**
 * HITL approval configuration for tools.
 *
 * When `approval` is specified, the tool will pause before execution
 * to show an approval UI and wait for user response.
 *
 * @example
 * ```typescript
 * // Simplest - use default approval with localized defaults
 * approval: true
 *
 * // With custom config
 * approval: {
 *   config: {
 *     title: "Confirm Deletion",
 *     message: "Are you sure you want to delete this content?"
 *   }
 * }
 *
 * // With custom approval element
 * approval: {
 *   elementAlias: "MyProject.AgentApprovalElement.CustomPreview",
 *   config: { showPreview: true }
 * }
 * ```
 */
export type UaiAgentToolApprovalConfig = true | {
    /** Alias of approval element (defaults to 'Uai.AgentApprovalElement.Default') */
    elementAlias?: string;
    /** Static config passed to the approval element */
    config?: Record<string, unknown>;
};
/**
 * Props interface for tool render elements.
 * All tool elements receive these standardized props.
 */
export interface UaiAgentToolElementProps {
    /** Arguments passed to the tool by the AI agent */
    args: Record<string, unknown>;
    /** Current execution status of the tool */
    status: UaiAgentToolStatus;
    /** Result from tool execution (when completed) */
    result?: unknown;
}
/**
 * Base element type for tool render elements.
 */
export type UaiAgentToolElement = UmbControllerHostElement & UaiAgentToolElementProps;
//# sourceMappingURL=tool.types.d.ts.map