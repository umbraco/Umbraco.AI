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
  | "pending"    // TOOL_CALL_START received
  | "streaming"  // TOOL_CALL_ARGS being received
  | "executing"  // Frontend tool executing (after TOOL_CALL_END)
  | "complete"   // TOOL_CALL_RESULT received or frontend execution done
  | "error";     // Error occurred

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
 * - `api` - Required: The tool execution logic
 * - `element` - Optional: Custom UI element (defaults to tool-status indicator via kind)
 *
 * @example
 * ```typescript
 * // Tool with default status indicator
 * {
 *   type: 'uaiAgentTool',
 *   kind: 'default',
 *   alias: 'Uai.AgentTool.SearchDocuments',
 *   meta: { toolName: 'search_documents' },
 *   api: () => import('./search-documents.api.js')
 * }
 *
 * // Tool with custom Generative UI
 * {
 *   type: 'uaiAgentTool',
 *   kind: 'default',
 *   alias: 'Uai.AgentTool.ShowWeather',
 *   meta: { toolName: 'show_weather' },
 *   api: () => import('./weather.api.js'),
 *   element: () => import('./weather.element.js')  // Overrides default
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
  };
}

declare global {
  interface UmbExtensionManifestMap {
    uaiAgentTool: ManifestUaiAgentTool;
  }
}
