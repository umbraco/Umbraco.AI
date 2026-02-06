import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiToolServerDataSource } from "./tool.server.data-source.js";
import type { ToolScopeItemResponseModel, ToolItemResponseModel } from "../../api/types.gen.js";

// Re-export types for use by other components
export type { ToolScopeItemResponseModel, ToolItemResponseModel };

/**
 * Repository for tool operations.
 */
export class UaiToolRepository extends UmbControllerBase {
    #dataSource: UaiToolServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiToolServerDataSource(host);
    }

    /**
     * Gets all registered tool scopes.
     */
    async getToolScopes(): Promise<{ data?: Array<ToolScopeItemResponseModel>; error?: unknown }> {
        return this.#dataSource.getToolScopes();
    }

    /**
     * Gets all registered tools.
     */
    async getTools(): Promise<{ data?: Array<ToolItemResponseModel>; error?: unknown }> {
        return this.#dataSource.getTools();
    }
}
