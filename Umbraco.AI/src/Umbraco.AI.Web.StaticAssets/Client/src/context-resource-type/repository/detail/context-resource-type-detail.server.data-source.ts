import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ContextResourceTypesService } from "../../../api/sdk.gen.js";
import { UaiContextResourceTypeTypeMapper } from "../../type-mapper.js";
import type { UaiContextResourceTypeDetailModel } from "../../types.js";

/**
 * Server data source for fetching contextResourceType details.
 */
export class UaiContextResourceTypeDetailServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches a contextResourceType by ID with full details including setting definitions.
     */
    async get(id: string): Promise<{ data?: UaiContextResourceTypeDetailModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ContextResourceTypesService.getContextResourceTypeById({ path: { id } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiContextResourceTypeTypeMapper.toDetailModel(data) };
    }
}
