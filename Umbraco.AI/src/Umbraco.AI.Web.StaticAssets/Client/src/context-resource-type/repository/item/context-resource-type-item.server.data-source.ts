import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ContextResourceTypesService } from "../../../api/sdk.gen.js";
import { UaiContextResourceTypeTypeMapper } from "../../type-mapper.js";
import type { UaiContextResourceTypeItemModel } from "../../types.js";

/**
 * Server data source for fetching contextResourceType items.
 */
export class UaiContextResourceTypeItemServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches all available contextResourceTypes.
     */
    async getItems(): Promise<{ data?: UaiContextResourceTypeItemModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ContextResourceTypesService.getAllContextResourceTypes()
        );

        if (error || !data) {
            return { error };
        }

        const items = data.map(UaiContextResourceTypeTypeMapper.toItemModel);

        return { data: items };
    }
}
