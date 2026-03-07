import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiOrchestrationDetailModel } from "../../types.js";

export const UAI_ORCHESTRATION_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiOrchestrationDetailStore>(
    "UaiOrchestrationDetailStore",
);

/**
 * Store for Orchestration detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiOrchestrationDetailStore extends UmbDetailStoreBase<UaiOrchestrationDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_ORCHESTRATION_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiOrchestrationDetailStore as api };
