import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import type { UaiAuditLogDetailModel } from "../../types.js";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AuditLogsService } from "../../../api";
import { UaiAuditLogTypeMapper } from "../../type-mapper.js";

/**
 * Server data source for AuditLog detail operations.
 * AuditLog Logs are read-only - they're created by the system, not by users.
 */
export class UaiAuditLogDetailServerDataSource implements UmbDetailDataSource<UaiAuditLogDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Scaffold not needed for traces (read-only).
     */
    async createScaffold(_preset?: Partial<UaiAuditLogDetailModel>) {
        return { error: { message: "AuditLogs cannot be created manually" } as any };
    }

    /**
     * Reads a trace by its unique identifier (can be either Guid or OpenTelemetry AuditLogId).
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            AuditLogsService.getAuditLogByIdentifier({ path: { auditLogId: unique } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiAuditLogTypeMapper.toDetailModel(data) };
    }

    /**
     * Create not supported for traces (read-only).
     */
    async create(_model: UaiAuditLogDetailModel, _parentUnique: string | null) {
        return { error: { message: "AuditLogs cannot be created manually" } as any };
    }

    /**
     * Update not supported for traces (read-only).
     */
    async update(_model: UaiAuditLogDetailModel) {
        return { error: { message: "AuditLogs cannot be updated" } as any };
    }

    /**
     * Deletes a trace by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            AuditLogsService.deleteAuditLog({ path: { auditLogId: unique } }),
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
