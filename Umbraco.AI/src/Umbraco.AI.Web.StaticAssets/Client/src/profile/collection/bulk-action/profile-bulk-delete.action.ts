import {
    UaiBulkDeleteActionBase,
    type UaiBulkDeleteActionArgs,
} from "../../../core/entity-bulk-action/delete/bulk-delete.action.js";
import { UaiProfileDetailRepository } from "../../repository/detail/profile-detail.repository.js";

export class UaiProfileBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiProfile_bulkDeleteConfirm",
            getRepository: (host) => new UaiProfileDetailRepository(host),
        };
    }
}

export { UaiProfileBulkDeleteAction as api };
