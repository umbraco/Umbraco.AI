import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ToolsService } from "../../api/sdk.gen.js";
import type { UaiToolScope, UaiToolItem } from "../types.js";

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
    async getToolScopes(): Promise<{ data?: UaiToolScope[]; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, ToolsService.getAllToolScopes());

        if (error || !data) {
            return { error };
        }

        return {
            data: data.map((s) => ({
                id: s.id,
                icon: s.icon,
                isDestructive: s.isDestructive,
                domain: s.domain,
            })),
        };
    }

    /**
     * Gets all registered tools.
     */
    async getTools(): Promise<{ data?: UaiToolItem[]; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, ToolsService.getAllTools());

        if (error || !data) {
            return { error };
        }

        return {
            data: data.map((t) => ({
                id: t.id,
                name: t.name,
                description: t.description,
                scopeId: t.scopeId,
                isDestructive: t.isDestructive,
                tags: [...t.tags],
            })),
        };
    }
}
