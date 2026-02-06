import {
    UaiBulkDeleteActionBase,
    type UaiBulkDeleteActionArgs,
} from "../../../core/entity-bulk-action/delete/bulk-delete.action.js";
import { UaiConnectionDetailRepository } from "../../repository/detail/connection-detail.repository.js";

export class UaiConnectionBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiConnection_bulkDeleteConfirm",
            getRepository: (host) => new UaiConnectionDetailRepository(host),
        };
    }
}

export { UaiConnectionBulkDeleteAction as api };
