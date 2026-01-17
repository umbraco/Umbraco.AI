import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestApi, loadManifestElement } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiToolCallInfo } from "../types.js";
import { ManifestUaiAgentTool, UaiAgentToolApi, UaiAgentToolElement } from "../../agent/tools";
import { AguiTool } from "../transport";

/** Element constructor type for tool UI components */
type UaiToolElementConstructor = new () => UaiAgentToolElement;

/**
 * Frontend tool registry manager.
 *
 * Responsibilities:
 * - Loading tool manifests from the extension registry
 * - Converting tools to AG-UI format for the LLM
 * - Resolving and caching tool API instances
 * - Providing manifest lookup for tool-renderer
 */
export class UaiFrontendToolManager {
  #host: UmbControllerHost;
  #toolManifests: Map<string, ManifestUaiAgentTool> = new Map();
  #apiCache: Map<string, UaiAgentToolApi> = new Map();
  #elementCache: Map<string, UaiToolElementConstructor> = new Map();
  #tools: AguiTool[] = [];

  constructor(host: UmbControllerHost) {
    this.#host = host;
  }

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
    const manifest = this.#toolManifests.get(toolName);
    if (!manifest?.api) {
      throw new Error(`No API found for tool: ${toolName}`);
    }

    // Load and cache API
    const ApiConstructor = await loadManifestApi<UaiAgentToolApi>(manifest.api);
    if (!ApiConstructor) {
      throw new Error(`Failed to load API for tool: ${toolName}`);
    }

    const api = new ApiConstructor(this.#host);
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
    const manifest = this.#toolManifests.get(toolName);
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
    updates: Partial<UaiToolCallInfo>
  ): UaiToolCallInfo[] {
    return toolCalls.map((tc) =>
      tc.id === toolCallId ? { ...tc, ...updates } : tc
    );
  }
}
