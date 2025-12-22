import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { createExtensionApi } from "@umbraco-cms/backoffice/extension-api";
import type { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { ManifestUaiAgentTool, UaiAgentToolApi } from "../../../agent/tools/uai-agent-tool.extension.js";
import type { AgUiTool, ToolCallInfo } from "../types.js";

/**
 * Result of a tool execution.
 */
export interface ToolExecutionResult {
  /** The result content as a string */
  result: string;
  /** Whether an error occurred during execution */
  hasError: boolean;
}

/**
 * Pending tool execution info.
 */
export interface PendingToolExecution {
  id: string;
  name: string;
  args: Record<string, unknown>;
}

/**
 * Manages frontend tool loading and execution.
 *
 * Extracts tool-related logic from the consumer component:
 * - Loading tools from the extension registry
 * - Tracking which tools are frontend-executable
 * - Executing tools and returning results
 * - Managing pending tool executions
 */
export class FrontendToolManager {
  #toolManifests: Map<string, ManifestUaiAgentTool> = new Map();
  #tools: AgUiTool[] = [];
  #pendingExecutions: Map<string, PendingToolExecution> = new Map();
  #host: UmbLitElement;

  /**
   * Create a new FrontendToolManager.
   * @param host The host element for creating extension APIs
   */
  constructor(host: UmbLitElement) {
    this.#host = host;
  }

  /**
   * Get the loaded frontend tools in AG-UI format.
   */
  get tools(): AgUiTool[] {
    return [...this.#tools];
  }

  /**
   * Get the number of pending tool executions.
   */
  get pendingCount(): number {
    return this.#pendingExecutions.size;
  }

  /**
   * Check if there are pending tool executions.
   */
  get hasPending(): boolean {
    return this.#pendingExecutions.size > 0;
  }

  /**
   * Load frontend tool definitions from the extension registry.
   * Tools with an `api` property are frontend-executable tools.
   * @returns The loaded tools in AG-UI format
   */
  loadFromRegistry(): AgUiTool[] {
    // Get all uaiAgentTool manifests that have an API (frontend tools)
    const manifests = umbExtensionsRegistry.getByTypeAndFilter<
      "uaiAgentTool",
      ManifestUaiAgentTool
    >("uaiAgentTool", (manifest) => manifest.api !== undefined);

    // Clear existing and rebuild
    this.#toolManifests.clear();
    this.#tools = [];

    for (const manifest of manifests) {
      this.#toolManifests.set(manifest.meta.toolName, manifest);
      this.#tools.push({
        name: manifest.meta.toolName,
        description: manifest.meta.description ?? "",
        parameters: manifest.meta.parameters ?? { type: "object", properties: {} },
      });
    }

    return this.#tools;
  }

  /**
   * Check if a tool is a frontend-executable tool.
   * @param toolName The name of the tool
   */
  isFrontendTool(toolName: string): boolean {
    return this.#toolManifests.has(toolName)
        && this.#toolManifests.get(toolName)?.api !== undefined;
  }

  /**
   * Queue a tool call for execution.
   * Call this when TOOL_CALL_END is received.
   * @param toolCallId The ID of the tool call
   * @param toolName The name of the tool
   * @param args The parsed arguments
   */
  queueForExecution(toolCallId: string, toolName: string, args: Record<string, unknown>): void {
    if (this.isFrontendTool(toolName)) {
      this.#pendingExecutions.set(toolCallId, {
        id: toolCallId,
        name: toolName,
        args,
      });
    }
  }

  /**
   * Get and clear all pending tool executions.
   * Call this when RUN_FINISHED is received.
   * @returns Array of pending executions, cleared from the queue
   */
  consumePendingExecutions(): PendingToolExecution[] {
    const executions = Array.from(this.#pendingExecutions.values());
    this.#pendingExecutions.clear();
    return executions;
  }

  /**
   * Clear all pending executions without returning them.
   */
  clearPending(): void {
    this.#pendingExecutions.clear();
  }

  /**
   * Execute a frontend tool.
   * @param toolName The name of the tool to execute
   * @param args The arguments to pass to the tool
   * @returns The execution result
   */
  async execute(toolName: string, args: Record<string, unknown>): Promise<ToolExecutionResult> {
    const manifest = this.#toolManifests.get(toolName);
    if (!manifest) {
      return {
        result: `Error: Unknown frontend tool: ${toolName}`,
        hasError: true,
      };
    }

    try {
      // Create the tool API instance
      const api = await createExtensionApi<UaiAgentToolApi>(this.#host, manifest);
      if (!api) {
        throw new Error(`Failed to create API for tool: ${toolName}`);
      }

      // Execute the tool
      const result = await api.execute(args);
      const resultContent = typeof result === "string" ? result : JSON.stringify(result);

      return {
        result: resultContent,
        hasError: false,
      };
    } catch (error) {
      return {
        result: `Error: ${error instanceof Error ? error.message : String(error)}`,
        hasError: true,
      };
    }
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
    toolCalls: ToolCallInfo[],
    toolCallId: string,
    updates: Partial<ToolCallInfo>
  ): ToolCallInfo[] {
    return toolCalls.map((tc) =>
      tc.id === toolCallId ? { ...tc, ...updates } : tc
    );
  }
}
