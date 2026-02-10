import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiContextDetailModel } from "../../types.js";

export const UAI_CONTEXT_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiContextDetailStore>("UaiContextDetailStore");

/**
 * Store for Context detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiContextDetailStore extends UmbDetailStoreBase<UaiContextDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_CONTEXT_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiContextDetailStore as api };
