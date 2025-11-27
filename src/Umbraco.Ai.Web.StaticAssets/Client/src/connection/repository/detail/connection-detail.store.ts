import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiConnectionDetailModel } from "../../types.js";

export const UAI_CONNECTION_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiConnectionDetailStore>(
    "UaiConnectionDetailStore"
);

/**
 * Store for Connection detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiConnectionDetailStore extends UmbDetailStoreBase<UaiConnectionDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_CONNECTION_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiConnectionDetailStore as api };
