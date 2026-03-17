import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiGuardrailDetailModel } from "../../types.js";

export const UAI_GUARDRAIL_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiGuardrailDetailStore>(
    "UaiGuardrailDetailStore",
);

/**
 * Store for Guardrail detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiGuardrailDetailStore extends UmbDetailStoreBase<UaiGuardrailDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_GUARDRAIL_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiGuardrailDetailStore as api };
