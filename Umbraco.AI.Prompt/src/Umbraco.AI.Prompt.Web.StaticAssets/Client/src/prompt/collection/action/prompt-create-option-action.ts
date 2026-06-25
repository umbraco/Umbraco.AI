import { UmbEntityCreateOptionActionBase } from "@umbraco-cms/backoffice/entity-create-option-action";
import type { MetaEntityCreateOptionAction } from "@umbraco-cms/backoffice/entity-create-option-action";
import { UAI_CREATE_PROMPT_WORKSPACE_PATH_PATTERN } from "../../workspace/prompt/paths.js";

export class UaiPromptCreateOptionAction extends UmbEntityCreateOptionActionBase<MetaEntityCreateOptionAction> {
    override async getHref(): Promise<string> {
        return UAI_CREATE_PROMPT_WORKSPACE_PATH_PATTERN.generateAbsolute({});
    }
}

export { UaiPromptCreateOptionAction as api };
