import {
    UaiBulkDeleteActionBase,
    type UaiBulkDeleteActionArgs,
} from "../../../core/entity-bulk-action/delete/bulk-delete.action.js";
import { UaiTestDetailRepository } from "../../repository/detail/test-detail.repository.js";

export class UaiTestBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiTest_bulkDeleteConfirm",
            getRepository: (host) => new UaiTestDetailRepository(host),
        };
    }
}

export { UaiTestBulkDeleteAction as api };
