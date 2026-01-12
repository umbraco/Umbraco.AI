import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
// import { tryExecute } from "@umbraco-cms/backoffice/resources";  // TODO: Uncomment when API is available
// TODO: Replace with actual import once API is regenerated
// import { TraceService } from "../../../api/sdk.gen.js";
// import { UaiTraceTypeMapper } from "../../type-mapper.js";  // TODO: Uncomment when API is available
import type { UaiTraceDetailModel } from "../../types.js";

/**
 * Server data source for Trace detail operations.
 * Traces are read-only - they're created by the system, not by users.
 */
export class UaiTraceDetailServerDataSource implements UmbDetailDataSource<UaiTraceDetailModel> {
    // #host: UmbControllerHost;  // TODO: Uncomment when API is available

    constructor(_host: UmbControllerHost) {
        // this.#host = host;  // TODO: Uncomment when API is available
    }

    /**
     * Scaffold not needed for traces (read-only).
     */
    async createScaffold(_preset?: Partial<UaiTraceDetailModel>) {
        return { error: { message: 'Traces cannot be created manually' } as any };
    }

    /**
     * Reads a trace by its unique identifier (can be either Guid or OpenTelemetry TraceId).
     */
    async read(_unique: string) {
        // TODO: Uncomment once TraceService is available in generated API
        /*
        const { data, error } = await tryExecute(
            this.#host,
            TraceService.getTraceByIdentifier({ path: { identifier: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiTraceTypeMapper.toDetailModel(data) };
        */

        // Temporary mock until API is available
        console.warn('TraceService not yet available - read operation will fail');
        return { error: { message: 'TraceService not yet available' } as any };
    }

    /**
     * Create not supported for traces (read-only).
     */
    async create(_model: UaiTraceDetailModel, _parentUnique: string | null) {
        return { error: { message: 'Traces cannot be created manually' } as any };
    }

    /**
     * Update not supported for traces (read-only).
     */
    async update(_model: UaiTraceDetailModel) {
        return { error: { message: 'Traces cannot be updated' } as any };
    }

    /**
     * Deletes a trace by its unique identifier.
     */
    async delete(_unique: string) {
        // TODO: Uncomment once TraceService is available in generated API
        /*
        const { error } = await tryExecute(
            this.#host,
            TraceService.deleteTrace({ path: { identifier: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
        */

        // Temporary mock until API is available
        console.warn('TraceService not yet available - delete operation will fail');
        return { error: { message: 'TraceService not yet available' } as any };
    }
}
