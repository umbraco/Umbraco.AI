import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestUaiAgentTool } from "../../../agent/tools/uai-agent-tool.extension.js";
import type { AguiTool, ToolCallInfo } from "../models/chat.types.js";

/**
 * Frontend tool registry manager.
 *
 * Responsibilities:
 * - Loading tool manifests from the extension registry
 * - Converting tools to AG-UI format for the LLM
 * - Providing manifest lookup for tool-renderer
 *
 * Note: Execution is handled by tool-renderer (single owner of tool lifecycle).
 */
export class FrontendToolManager {
  #toolManifests: Map<string, ManifestUaiAgentTool> = new Map();
  #tools: AguiTool[] = [];

  /**
   * Get the loaded frontend tools in AG-UI format.
   */
  get tools(): AguiTool[] {
    return [...this.#tools];
  }

  /**
   * Load frontend tool definitions from the extension registry.
   * Tools with an `api` property are frontend-executable tools.
   * @returns The loaded tools in AG-UI format
   */
  loadFromRegistry(): AguiTool[] {
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
   * Get the manifest for a tool by name.
   * @param toolName The name of the tool
   * @returns The tool manifest, or undefined if not found
   */
  getManifest(toolName: string): ManifestUaiAgentTool | undefined {
    return this.#toolManifests.get(toolName);
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
