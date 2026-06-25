import { UmbEntityCreateOptionActionBase } from "@umbraco-cms/backoffice/entity-create-option-action";
import type { MetaEntityCreateOptionAction } from "@umbraco-cms/backoffice/entity-create-option-action";
import { UAI_CREATE_GUARDRAIL_WORKSPACE_PATH_PATTERN } from "../../workspace/guardrail/paths.js";

export class UaiGuardrailCreateOptionAction extends UmbEntityCreateOptionActionBase<MetaEntityCreateOptionAction> {
    override async getHref(): Promise<string> {
        return UAI_CREATE_GUARDRAIL_WORKSPACE_PATH_PATTERN.generateAbsolute({});
    }
}

export { UaiGuardrailCreateOptionAction as api };
