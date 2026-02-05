import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ToolsService } from "../../api/sdk.gen.js";
import { ToolScopeItemResponseModel, ToolItemResponseModel } from "../../api";

/**
 * Server data source for tool scope operations.
 */
export class UaiToolServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all registered tool scopes.
     */
    async getToolScopes(): Promise<{ data?: Array<ToolScopeItemResponseModel>; error?: unknown }> {
        return await tryExecute(
            this.#host,
          ToolsService.getAllToolScopes()
        );
    }

    /**
     * Gets all registered tools.
     */
    async getTools(): Promise<{ data?: Array<ToolItemResponseModel>; error?: unknown }> {
        return await tryExecute(
            this.#host,
            ToolsService.getAllTools()
        );
    }
}
