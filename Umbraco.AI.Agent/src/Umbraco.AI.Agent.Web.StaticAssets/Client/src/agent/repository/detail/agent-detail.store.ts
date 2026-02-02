import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiAgentDetailModel } from "../../types.js";

export const UAI_AGENT_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiAgentDetailStore>(
    "UaiAgentDetailStore"
);

/**
 * Store for Agent detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiAgentDetailStore extends UmbDetailStoreBase<UaiAgentDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_AGENT_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiAgentDetailStore as api };
