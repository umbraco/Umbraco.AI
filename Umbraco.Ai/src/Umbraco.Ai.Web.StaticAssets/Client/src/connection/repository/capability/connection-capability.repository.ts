import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiConnectionCapabilityServerDataSource } from "./connection-capability.server.data-source.js";

/**
 * Repository for Connection capability operations.
 * Used to query available capabilities and connections by capability.
 */
export class UaiConnectionCapabilityRepository extends UmbRepositoryBase {
    #dataSource: UaiConnectionCapabilityServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiConnectionCapabilityServerDataSource(host);
    }

    /**
     * Requests the list of available capabilities from configured connections.
     */
    async requestAvailableCapabilities() {
        return this.#dataSource.getAvailableCapabilities();
    }

    /**
     * Requests connections that support a specific capability.
     */
    async requestConnectionsByCapability(capability: string) {
        return this.#dataSource.getConnectionsByCapability(capability);
    }
}

export { UaiConnectionCapabilityRepository as api };
