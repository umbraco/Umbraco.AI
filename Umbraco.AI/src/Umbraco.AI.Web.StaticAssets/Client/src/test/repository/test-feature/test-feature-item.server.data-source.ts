import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import { UaiTestTypeMapper } from "../../type-mapper.js";
import type { UaiTestFeatureItemModel } from "../../types.js";
import type { TestFeatureResponseModel } from "../../../api/types.gen.js";

/**
 * Server data source for fetching test feature items.
 */
export class UaiTestFeatureItemServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches all available test features.
     */
    async getItems(): Promise<{ data?: UaiTestFeatureItemModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, TestsService.getAllTestFeatures());

        if (error || !data) {
            return { error };
        }

        const items = data.map(UaiTestTypeMapper.toTestFeatureItemModel);

        return { data: items };
    }

    /**
     * Fetches a test feature by ID (includes schema).
     */
    async getById(id: string): Promise<{ data?: TestFeatureResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getTestFeatureById({ path: { id } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }
}
