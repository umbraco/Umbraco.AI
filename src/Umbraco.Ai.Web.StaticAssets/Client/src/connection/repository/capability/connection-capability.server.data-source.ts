import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionItemModel } from "../../types.js";

/**
 * Server data source for Connection capability operations.
 */
export class UaiConnectionCapabilityServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all available capabilities from configured connections.
     */
    async getAvailableCapabilities() {
        const { data, error } = await tryExecute(
            this.#host,
            ConnectionsService.getAvailableCapabilities()
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }

    /**
     * Gets connections that support a specific capability.
     */
    async getConnectionsByCapability(capability: string): Promise<{ data?: UaiConnectionItemModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ConnectionsService.getConnections({
                query: {
                    capability,
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiConnectionTypeMapper.toItemModel);
        return { data: items };
    }
}
