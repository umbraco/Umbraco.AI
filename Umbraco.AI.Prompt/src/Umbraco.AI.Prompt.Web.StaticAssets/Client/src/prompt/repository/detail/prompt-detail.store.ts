import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiPromptDetailModel } from "../../types.js";

export const UAI_PROMPT_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiPromptDetailStore>(
    "UaiPromptDetailStore"
);

/**
 * Store for Prompt detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiPromptDetailStore extends UmbDetailStoreBase<UaiPromptDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_PROMPT_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiPromptDetailStore as api };
