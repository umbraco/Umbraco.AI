import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ProvidersService } from "../../../api/sdk.gen.js";
import type { UaiProviderDetailModel } from "../../types.js";

/**
 * Server data source for fetching provider details.
 */
export class UaiProviderDetailServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches a provider by ID with full details including setting definitions.
     */
    async get(id: string): Promise<{ data?: UaiProviderDetailModel; error?: unknown }> {
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ProvidersService.getProviderById({ path: { id } })
        );

        if (error || !data) {
            return { error };
        }

        // Map API response to UI model
        const detail: UaiProviderDetailModel = {
            id: data.id,
            name: data.name,
            capabilities: data.capabilities,
            settingDefinitions: data.settingDefinitions.map((setting) => ({
                key: setting.key,
                label: setting.label,
                description: setting.description ?? undefined,
                editorUiAlias: setting.editorUiAlias ?? undefined,
                editorConfig: setting.editorConfig ?? undefined,
                defaultValue: setting.defaultValue ?? undefined,
                sortOrder: setting.sortOrder,
                isRequired: setting.isRequired,
            })),
        };

        return { data: detail };
    }
}
