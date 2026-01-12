import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiTraceDetailModel } from "../../types.js";

export const UAI_TRACE_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiTraceDetailStore>(
    "UaiTraceDetailStore"
);

/**
 * Store for Trace detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiTraceDetailStore extends UmbDetailStoreBase<UaiTraceDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_TRACE_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiTraceDetailStore as api };
