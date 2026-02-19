import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiTestDetailModel } from "../../types.js";

export const UAI_TEST_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiTestDetailStore>("UaiTestDetailStore");

/**
 * Store for Test detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiTestDetailStore extends UmbDetailStoreBase<UaiTestDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_TEST_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiTestDetailStore as api };
