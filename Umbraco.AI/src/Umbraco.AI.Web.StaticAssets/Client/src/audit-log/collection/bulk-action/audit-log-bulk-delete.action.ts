import {
    UaiBulkDeleteActionBase,
    type UaiBulkDeleteActionArgs,
} from "../../../core/entity-bulk-action/delete/bulk-delete.action.js";
import { UaiAuditLogDetailRepository } from "../../repository/detail/audit-log-detail.repository.js";

export class UaiAuditLogBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiAuditLog_bulkDeleteConfirm",
            getRepository: (host) => new UaiAuditLogDetailRepository(host),
        };
    }
}

export { UaiAuditLogBulkDeleteAction as api };
