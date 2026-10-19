import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiEnabledCapabilitiesServerDataSource } from "./enabled-capabilities.server.data-source.js";

/**
 * The enabled-capability list is fixed for the lifetime of a backoffice session (it only
 * changes on a server restart), so a single in-flight/resolved request is shared across
 * every repository instance instead of one call per consumer.
 */
let sharedRequest: Promise<{ data?: string[]; error?: unknown }> | undefined;

/**
 * Repository for the enabled-capabilities list. Internal only -- not part of this
 * package's public API.
 */
export class UaiEnabledCapabilitiesRepository extends UmbRepositoryBase {
    #dataSource: UaiEnabledCapabilitiesServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiEnabledCapabilitiesServerDataSource(host);
    }

    /**
     * Requests the list of enabled capabilities. Fetched once per backoffice session and
     * shared with every other caller.
     */
    async getEnabledCapabilities(): Promise<{ data?: string[]; error?: unknown }> {
        sharedRequest ??= this.#dataSource.getEnabledCapabilities();

        const result = await sharedRequest;
        if (result.error) {
            // Don't cache a failure forever -- let the next caller retry.
            sharedRequest = undefined;
        }

        return result;
    }
}
