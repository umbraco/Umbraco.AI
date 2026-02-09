import type { UmbApi } from "@umbraco-cms/backoffice/extension-api";

/**
 * Frontend tool metadata for display in tool pickers.
 * This is the minimal data needed by consumers to list available frontend tools.
 */
export interface UaiFrontendToolData {
    /** Unique tool identifier (matches AG-UI tool call name) */
    id: string;
    /** Display name for the tool */
    name: string;
    /** Description of what the tool does */
    description: string;
    /** Scope identifier for permission grouping */
    scopeId: string;
}

/**
 * Repository API for accessing frontend tool metadata.
 * Implemented by packages that provide frontend tools (e.g., Copilot).
 */
export interface UaiFrontendToolRepositoryApi extends UmbApi {
    /**
     * Get all available frontend tools.
     * @returns Array of frontend tool metadata
     */
    getTools(): Promise<UaiFrontendToolData[]>;
}
