import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { CapabilitiesService } from "../../api/sdk.gen.js";

/**
 * Server data source for the enabled-capabilities endpoint.
 */
export class UaiEnabledCapabilitiesServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets the list of capabilities enabled on this installation (experimental
     * capabilities excluded when their feature flag is off).
     */
    async getEnabledCapabilities(): Promise<{ data?: string[]; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, CapabilitiesService.getEnabledCapabilities());

        if (error) {
            return { error };
        }

        return { data };
    }
}
