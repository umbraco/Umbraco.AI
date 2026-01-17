import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiProfileDetailModel } from "../../types.js";

export const UAI_PROFILE_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiProfileDetailStore>(
    "UaiProfileDetailStore"
);

/**
 * Store for Profile detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiProfileDetailStore extends UmbDetailStoreBase<UaiProfileDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_PROFILE_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiProfileDetailStore as api };
