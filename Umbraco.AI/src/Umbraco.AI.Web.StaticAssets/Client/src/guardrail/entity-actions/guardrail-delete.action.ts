import { UaiDeleteActionBase, type UaiDeleteActionArgs } from "../../core/entity-action/delete/delete.action.js";
import { UaiGuardrailDetailRepository } from "../repository/detail/guardrail-detail.repository.js";

export class UaiGuardrailDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiGuardrail_deleteConfirm",
            getRepository: (host) => new UaiGuardrailDetailRepository(host),
        };
    }
}

export { UaiGuardrailDeleteAction as api };
