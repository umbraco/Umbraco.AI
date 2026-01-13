import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
// import { tryExecute } from "@umbraco-cms/backoffice/resources";  // TODO: Uncomment when API is available
// TODO: Replace with actual import once API is regenerated
// import { AuditService } from "../../../api/sdk.gen.js";
// import { UaiAuditTypeMapper } from "../../type-mapper.js";  // TODO: Uncomment when API is available
import type { UaiAuditDetailModel } from "../../types.js";

/**
 * Server data source for Audit detail operations.
 * Audits are read-only - they're created by the system, not by users.
 */
export class UaiAuditDetailServerDataSource implements UmbDetailDataSource<UaiAuditDetailModel> {
    // #host: UmbControllerHost;  // TODO: Uncomment when API is available

    constructor(_host: UmbControllerHost) {
        // this.#host = host;  // TODO: Uncomment when API is available
    }

    /**
     * Scaffold not needed for traces (read-only).
     */
    async createScaffold(_preset?: Partial<UaiAuditDetailModel>) {
        return { error: { message: 'Audits cannot be created manually' } as any };
    }

    /**
     * Reads a trace by its unique identifier (can be either Guid or OpenTelemetry AuditId).
     */
    async read(_unique: string) {
        // TODO: Uncomment once AuditService is available in generated API
        /*
        const { data, error } = await tryExecute(
            this.#host,
            AuditService.getAuditByIdentifier({ path: { identifier: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiAuditTypeMapper.toDetailModel(data) };
        */

        // Temporary mock until API is available
        console.warn('AuditService not yet available - read operation will fail');
        return { error: { message: 'AuditService not yet available' } as any };
    }

    /**
     * Create not supported for traces (read-only).
     */
    async create(_model: UaiAuditDetailModel, _parentUnique: string | null) {
        return { error: { message: 'Audits cannot be created manually' } as any };
    }

    /**
     * Update not supported for traces (read-only).
     */
    async update(_model: UaiAuditDetailModel) {
        return { error: { message: 'Audits cannot be updated' } as any };
    }

    /**
     * Deletes a trace by its unique identifier.
     */
    async delete(_unique: string) {
        // TODO: Uncomment once AuditService is available in generated API
        /*
        const { error } = await tryExecute(
            this.#host,
            AuditService.deleteAudit({ path: { identifier: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
        */

        // Temporary mock until API is available
        console.warn('AuditService not yet available - delete operation will fail');
        return { error: { message: 'AuditService not yet available' } as any };
    }
}
