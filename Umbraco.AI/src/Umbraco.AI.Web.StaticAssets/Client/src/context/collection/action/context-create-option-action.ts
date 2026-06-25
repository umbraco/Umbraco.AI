import { UmbEntityCreateOptionActionBase } from "@umbraco-cms/backoffice/entity-create-option-action";
import type { MetaEntityCreateOptionAction } from "@umbraco-cms/backoffice/entity-create-option-action";
import { UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN } from "../../workspace/context/paths.js";

export class UaiContextCreateOptionAction extends UmbEntityCreateOptionActionBase<MetaEntityCreateOptionAction> {
    override async getHref(): Promise<string> {
        return UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN.generateAbsolute({});
    }
}

export { UaiContextCreateOptionAction as api };
