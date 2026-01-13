import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
// import { tryExecute } from "@umbraco-cms/backoffice/resources";  // TODO: Uncomment when API is available
// TODO: Replace with actual import once API is regenerated
// import { AuditLogService } from "../../../api/sdk.gen.js";
// import { UaiAuditLogTypeMapper } from "../../type-mapper.js";  // TODO: Uncomment when API is available
import type { UaiAuditLogDetailModel } from "../../types.js";

/**
 * Server data source for AuditLog detail operations.
 * AuditLog Logs are read-only - they're created by the system, not by users.
 */
export class UaiAuditLogDetailServerDataSource implements UmbDetailDataSource<UaiAuditLogDetailModel> {
    // #host: UmbControllerHost;  // TODO: Uncomment when API is available

    constructor(_host: UmbControllerHost) {
        // this.#host = host;  // TODO: Uncomment when API is available
    }

    /**
     * Scaffold not needed for traces (read-only).
     */
    async createScaffold(_preset?: Partial<UaiAuditLogDetailModel>) {
        return { error: { message: 'AuditLogs cannot be created manually' } as any };
    }

    /**
     * Reads a trace by its unique identifier (can be either Guid or OpenTelemetry AuditLogId).
     */
    async read(_unique: string) {
        // TODO: Uncomment once AuditLogService is available in generated API
        /*
        const { data, error } = await tryExecute(
            this.#host,
            AuditLogService.getAuditLogByIdentifier({ path: { identifier: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiAuditLogTypeMapper.toDetailModel(data) };
        */

        // Temporary mock until API is available
        console.warn('AuditLogService not yet available - read operation will fail');
        return { error: { message: 'AuditLogService not yet available' } as any };
    }

    /**
     * Create not supported for traces (read-only).
     */
    async create(_model: UaiAuditLogDetailModel, _parentUnique: string | null) {
        return { error: { message: 'AuditLogs cannot be created manually' } as any };
    }

    /**
     * Update not supported for traces (read-only).
     */
    async update(_model: UaiAuditLogDetailModel) {
        return { error: { message: 'AuditLogs cannot be updated' } as any };
    }

    /**
     * Deletes a trace by its unique identifier.
     */
    async delete(_unique: string) {
        // TODO: Uncomment once AuditLogService is available in generated API
        /*
        const { error } = await tryExecute(
            this.#host,
            AuditLogService.deleteAuditLog({ path: { identifier: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
        */

        // Temporary mock until API is available
        console.warn('AuditLogService not yet available - delete operation will fail');
        return { error: { message: 'AuditLogService not yet available' } as any };
    }
}
