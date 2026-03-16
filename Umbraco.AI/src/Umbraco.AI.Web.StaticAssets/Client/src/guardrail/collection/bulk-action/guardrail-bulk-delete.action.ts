import {
    UaiBulkDeleteActionBase,
    type UaiBulkDeleteActionArgs,
} from "../../../core/entity-bulk-action/delete/bulk-delete.action.js";
import { UaiGuardrailDetailRepository } from "../../repository/detail/guardrail-detail.repository.js";

export class UaiGuardrailBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiGuardrail_bulkDeleteConfirm",
            getRepository: (host) => new UaiGuardrailDetailRepository(host),
        };
    }
}

export { UaiGuardrailBulkDeleteAction as api };
