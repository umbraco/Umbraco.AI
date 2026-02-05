import type {
  ManifestElementAndApi,
  UmbApi,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHostElement } from "@umbraco-cms/backoffice/controller-api";

/**
 * Tool execution API interface.
 * Implement this to create a tool that can be called by AI agents.
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
export type UaiAgentToolStatus =
  | "pending"           // TOOL_CALL_START received
  | "streaming"         // TOOL_CALL_ARGS being received
  | "awaiting_approval" // HITL: Waiting for user approval
  | "executing"         // Frontend tool executing (after TOOL_CALL_END)
  | "complete"          // TOOL_CALL_RESULT received or frontend execution done
  | "error";            // Error occurred

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
export type UaiAgentToolApprovalConfig =
  | true
  | {
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
export type UaiAgentToolElement = UmbControllerHostElement &
  UaiAgentToolElementProps;

/**
 * Manifest for AI Agent Tools.
 *
 * Tools can be called by AI agents and optionally render custom UI (Generative UI).
 * - `api` - Optional: The tool execution logic (required for frontend tools)
 * - `element` - Optional: Custom UI element (defaults to tool-status indicator via kind)
 * - `approval` - Optional: HITL approval configuration
 *
 * Tool types:
 * - Frontend tool (has `api`): Executes locally in the browser
 * - Backend tool (no `api`): Render-only, execution happens on server
 * - HITL tool (has `approval`): Pauses for user approval before execution
 *
 * @example
 * ```typescript
 * // Auto-execute frontend tool
 * {
 *   type: 'uaiAgentTool',
 *   kind: 'default',
 *   alias: 'Uai.AgentTool.SearchDocuments',
 *   meta: { toolName: 'search_documents' },
 *   api: () => import('./search-documents.api.js')
 * }
 *
 * // HITL frontend tool with approval
 * {
 *   type: 'uaiAgentTool',
 *   kind: 'default',
 *   alias: 'Uai.AgentTool.DeleteContent',
 *   meta: {
 *     toolName: 'delete_content',
 *     approval: true  // Uses default approval element
 *   },
 *   api: () => import('./delete-content.api.js')
 * }
 *
 * // Backend tool with custom UI (no api)
 * {
 *   type: 'uaiAgentTool',
 *   kind: 'default',
 *   alias: 'Uai.AgentTool.PublishContent',
 *   meta: {
 *     toolName: 'publish_content',
 *     approval: {
 *       config: { title: 'Confirm Publication' }
 *     }
 *   }
 *   // No api = backend tool, frontend just provides UI
 * }
 * ```
 */
export interface ManifestUaiAgentTool
  extends ManifestElementAndApi<UaiAgentToolElement, UaiAgentToolApi> {
  type: "uaiAgentTool";
  kind?: "default";
  meta: {
    /** Tool name that matches the AG-UI tool call name */
    toolName: string;
    /** Display label for the tool */
    label?: string;
    /** Description for LLM (required for frontend tools) */
    description?: string;
    /** JSON Schema for tool parameters (required for frontend tools) */
    parameters?: Record<string, unknown>;
    /** Icon to display with the tool */
    icon?: string;
    /**
     * Tool scope for permission grouping (e.g., 'entity.write', 'navigation').
     * Used to control which agents can access this tool.
     */
    scope?: string;
    /**
     * HITL approval configuration.
     * When specified, tool pauses for user approval before execution.
     * - `true` - Use default approval element with localized defaults
     * - `{ elementAlias?, config? }` - Custom approval element and/or config
     */
    approval?: UaiAgentToolApprovalConfig;
  };
}

declare global {
  interface UmbExtensionManifestMap {
    uaiAgentTool: ManifestUaiAgentTool;
  }
}
